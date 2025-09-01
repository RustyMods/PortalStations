using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using HarmonyLib;
using JetBrains.Annotations;
using PortalStations.UI;
using UnityEngine;
using static PortalStations.PortalStationsPlugin;

namespace PortalStations.Stations;

[HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
public static class Piece_SetCreator_Patch
{
    [UsedImplicitly]
    private static void Postfix(Piece __instance, long uid)
    {
        if (!__instance.TryGetComponent(out PortalStation station)) return;
        if (Player.m_localPlayer.GetPlayerID() == uid)
        {
            station.m_nview.GetZDO().Set(StationVars.CreatorName, Player.m_localPlayer.GetPlayerName());
        } 
        else if (ZNet.instance.GetPeer(uid) is { } peer)
        {
            station.m_nview.GetZDO().Set(StationVars.CreatorName, peer.m_playerName);
        }
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.CheckCanRemovePiece))]
public static class Player_CheckCanRemovePiece_Patch
{
    [UsedImplicitly]
    private static void Postfix(Player __instance, Piece piece, ref bool __result)
    {
        if (!piece.TryGetComponent(out PortalStation component)) return;
        if (component.GetCreator() == __instance.GetPlayerID()) return;
        __instance.Message(MessageHud.MessageType.Center, "$msg_not_creator");
        __result = false;
    }
}
public static class StationVars
{
    public static readonly int Name = "stationName".GetStableHashCode();
    public static readonly int Guild = "GuildNetwork".GetStableHashCode();
    public static readonly int Filter = "StationFilter".GetStableHashCode();
    public static readonly int Free = "StationFree".GetStableHashCode();
    public static readonly int CreatorName = "StationCreator".GetStableHashCode();
    public static readonly int GUID = "StationGUID".GetStableHashCode();
    public const string FavoriteKey = "PortalStationFavorites";
}
public class PortalStation : MonoBehaviour, Interactable, Hoverable, TextReceiver
{
    private const float use_distance = 5.0f;
    public ZNetView m_nview = null!;

    public float m_fadeDuration = 1f;
    public ParticleSystem[] m_particles = null!;
    public Light m_light = null!;
    public AudioSource m_audioSource = null!;
    private readonly List<Emission> m_emissions = new();
    public Color m_emissionColor = Color.black;
    public float m_lightBaseIntensity;
    public bool m_active = true;
    public float m_intensity;
    private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionTex = Shader.PropertyToID("_EmissiveTex");
    public string GUID = string.Empty;
    public bool m_assetsLoaded;

    private void Awake()
    {
        LoadAssets();
        m_nview = GetComponent<ZNetView>();
        if (!m_nview.IsValid()) return;
        GUID = m_nview.GetZDO().GetString(StationVars.GUID, Guid.NewGuid().ToString());
        m_nview.GetZDO().Set(StationVars.GUID, GUID);
        m_nview.Register<string>(nameof(RPC_SetStationName), RPC_SetStationName);
        m_nview.Register<int>(nameof(RPC_SetFilter), RPC_SetFilter);
        m_nview.Register<string>(nameof(RPC_SetGuild), RPC_SetGuild);
        m_nview.Register<bool>(nameof(RPC_SetFree),RPC_SetFree);
        
        if (!m_nview.IsOwner() || !Player.m_localPlayer) return;
        if (m_nview.GetZDO().GetString(StationVars.Name).IsNullOrWhiteSpace())
        {
            m_nview.GetZDO().Set(StationVars.Name, Player.m_localPlayer.GetPlayerName() + " Portal");
        }
    }

    public void LoadAssets()
    {
        Transform parent = Utils.FindChild(transform, "Portal Effects");
        if (!parent)
        {
            parent = Utils.FindChild(transform, "_target_found_red");
            if (!parent)
            {
                parent = Utils.FindChild(transform, "_target_found");
                if (!parent) return;
            }
        }
        GameObject portalEffects = parent.gameObject;

        m_particles = portalEffects.GetComponentsInChildren<ParticleSystem>(true);
        m_light = portalEffects.GetComponentInChildren<Light>(true);
        m_audioSource = portalEffects.GetComponentInChildren<AudioSource>(true);

        foreach (MeshRenderer? renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            foreach (Material? material in renderer.sharedMaterials)
            {
                if (material.HasProperty(EmissionMap) && material.GetTexture(EmissionMap) != null || material.HasProperty(EmissionTex) && material.GetTexture(EmissionTex) != null)
                {
                    var emission = new Emission(material, m_emissionColor == Color.black ? material.GetColor(EmissionColor) : m_emissionColor);
                    m_emissions.Add(emission);
                }
            }
        }

        if (m_light)
        {
            m_lightBaseIntensity = m_light.intensity;
            m_light.intensity = 0.0f;
        }

        if (m_audioSource)
        {
            m_audioSource.volume = 0.0f;
        }
        
        SetParticles(false);
        m_assetsLoaded = true;
    }

    private class Emission
    {
        private readonly Material material;
        private readonly Color color;

        public Emission(Material material, Color color)
        {
            this.material = material;
            this.color = color;
        }

        public void Update(float intensity)
        {
            material.SetColor(EmissionColor, Color.Lerp(Color.black, color, intensity));
        }
    }

    public void Update()
    {
        if (!m_assetsLoaded) return;
        Player closestPlayer = Player.GetClosestPlayer(transform.position, use_distance);
        bool flag = closestPlayer && closestPlayer.IsTeleportable();
        SetParticles(flag);
        
        m_intensity = Mathf.MoveTowards(m_intensity, m_active ? 1f : 0.0f, Time.deltaTime / m_fadeDuration);
        if (m_light)
        {
            m_light.intensity = m_intensity * m_lightBaseIntensity;
            m_light.enabled = m_light.intensity > 0.0;
        }
        if (m_audioSource) m_audioSource.volume = m_intensity * _PortalVolume.Value;
        
        foreach (Emission emission in m_emissions)
        {
            emission.Update(m_intensity);
        }
    }

    public void SetParticles(bool active)
    {
        if (m_active == active) return;
        m_active = active;
        foreach (ParticleSystem particle in m_particles)
        {
            ParticleSystem.EmissionModule particleEmission = particle.emission;
            particleEmission.enabled = active;
        }
    }
    private void OnDestroy() => PortalStationUI.instance?.Hide();
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (user is not Player player || PortalStationUI.instance == null) return false;
        if (hold)
        {
            return false;
        }

        if (!InUseDistance(user))
        {
            return false;
        }

        PortalStationUI.instance.Show(this, player);
        return true;
    }

    public long GetCreator() => m_nview.GetZDO().GetLong(ZDOVars.s_creator);
    public void SetName(string value) => m_nview.InvokeRPC(nameof(RPC_SetStationName), value);
    public void RPC_SetStationName(long sender, string value)
    {
        if (!m_nview.IsValid() || !m_nview.IsOwner() || GetStationName() == value) return;
        m_nview.GetZDO().Set(StationVars.Name, value);
    }
    public FilterOptions GetFilter() => GetFilterOption(m_nview.GetZDO().GetInt(StationVars.Filter));

    public enum FilterOptions
    {
        Public,
        Private,
        GuildOnly,
        GroupOnly,
        GuildGroupOnly
    }

    private static FilterOptions GetFilterOption(int index)
    {
        return index switch
        {
            0 => FilterOptions.Public,
            1 => FilterOptions.Private,
            2 => FilterOptions.GuildOnly,
            3 => FilterOptions.GroupOnly,
            4 => FilterOptions.GuildGroupOnly,
            _ => FilterOptions.Public
        };
    }

    public static int GetFilterIndex(FilterOptions filter)
    {
        return filter switch
        {
            FilterOptions.Public => 0,
            FilterOptions.Private => 1,
            FilterOptions.GuildOnly => 2,
            FilterOptions.GroupOnly => 3,
            FilterOptions.GuildGroupOnly => 4,
            _ => 0
        };
    }
    public void SetFilter(FilterOptions option) => m_nview.InvokeRPC(nameof(RPC_SetFilter), GetFilterIndex(option));
    public void RPC_SetFilter(long sender, int value)
    {
        if (!m_nview.IsValid() || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(StationVars.Filter, value);
    }

    public bool IsFree() => m_nview.GetZDO().GetBool(StationVars.Free);
    public void SetFree(bool enable) => m_nview.InvokeRPC(nameof(RPC_SetFree), enable);
    public void RPC_SetFree(long sender, bool value)
    {
        if (!m_nview.IsValid() || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(StationVars.Free, value);
    }
    public string GetGuild() => m_nview.GetZDO().GetString(StationVars.Guild);
    public void SetGuild(string guild) => m_nview.InvokeRPC(nameof(RPC_SetGuild), guild);
    public void RPC_SetGuild(long sender, string value)
    {
        if (!m_nview.IsValid() || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(StationVars.Guild, value);
    }
    
    private string GetStationName() => m_nview.GetZDO().GetString(StationVars.Name);
    private bool InUseDistance(Humanoid human) => Vector3.Distance(human.transform.position, transform.position) <= use_distance;
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    public string GetPrivacy()
    {
        var option = GetFilter();
        return option.ToString();
    }
    public string GetHoverText()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(GetStationName());
        stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $text_use");
        return Localization.instance.Localize(stringBuilder.ToString());
    } 
    public string GetHoverName() => "";
    public string GetText() => !m_nview.IsValid() ? "" : m_nview.GetZDO().GetString(StationVars.Name);
    public void SetText(string text)
    {
        if (!m_nview.IsValid()) return;
        if (String.IsNullOrWhiteSpace(text)) return;
        m_nview.InvokeRPC(nameof(RPC_SetStationName), text);
    }
}