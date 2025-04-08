using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using PortalStations.UI;
using UnityEngine;
using static PortalStations.PortalStationsPlugin;

namespace PortalStations.Stations;
public class PortalStation : MonoBehaviour, Interactable, Hoverable, TextReceiver
{
    public static readonly int m_stationName = "stationName".GetStableHashCode();
    public static readonly int m_stationGuild = "GuildNetwork".GetStableHashCode();
    public static readonly int m_stationFilter = "StationFilter".GetStableHashCode();
    public static readonly int m_free = "StationFree".GetStableHashCode();
    public const string _FavoriteKey = "PortalStationFavorites";

    private const float use_distance = 5.0f;
    public ZNetView m_nview = null!;

    public float m_fadeDuration = 1f;
    public ParticleSystem[] m_particles = null!;
    public Light m_light = null!;
    public AudioSource m_audioSource = null!;
    private readonly Dictionary<Material, Color> m_materials = new();
    public Color m_emissionColor = Color.black;
    public float m_lightBaseIntensity;
    public bool m_active = true;
    public float m_intensity;
    private static readonly int EmissionMap = Shader.PropertyToID("_EmissionMap");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private static readonly int EmissionTex = Shader.PropertyToID("_EmissionTex");
    public bool m_assetsLoaded;

    private void Awake()
    {
        LoadAssets();
        m_nview = GetComponent<ZNetView>();
        if (!m_nview.IsValid()) return;

        m_nview.Register<string>(nameof(RPC_SetStationName), RPC_SetStationName);
        m_nview.Register<int>(nameof(RPC_SetFilter), RPC_SetFilter);
        m_nview.Register<string>(nameof(RPC_SetGuild), RPC_SetGuild);
        m_nview.Register<bool>(nameof(RPC_SetFree),RPC_SetFree);
        
        if (!m_nview.IsOwner() || !Player.m_localPlayer) return;
        if (m_nview.GetZDO().GetString(m_stationName).IsNullOrWhiteSpace())
        {
            m_nview.GetZDO().Set(m_stationName, Player.m_localPlayer.GetPlayerName() + " Portal");
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

        foreach (var renderer in GetComponentsInChildren<MeshRenderer>(true))
        {
            foreach (var material in renderer.materials)
            {
                if (material.HasProperty("_EmissionMap") && material.GetTexture(EmissionMap) != null)
                {
                    m_materials[material] = m_emissionColor == Color.black ? material.GetColor(EmissionColor) : m_emissionColor;
                }
                else if (material.HasProperty("_EmissionTex") && material.GetTexture(EmissionTex) != null)
                {
                    m_materials[material] = m_emissionColor == Color.black ? material.GetColor(EmissionColor) : m_emissionColor;
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

    public void Update()
    {
        if (!m_assetsLoaded) return;
        Player closestPlayer = Player.GetClosestPlayer(transform.position, use_distance);
        bool flag = closestPlayer && Teleportation.IsTeleportable(closestPlayer);
        SetParticles(flag);
        
        m_intensity = Mathf.MoveTowards(m_intensity, m_active ? 1f : 0.0f, Time.deltaTime / m_fadeDuration);
        if (m_light)
        {
            m_light.intensity = m_intensity * m_lightBaseIntensity;
            m_light.enabled = m_light.intensity > 0.0;
        }
        if (m_audioSource) m_audioSource.volume = m_intensity * _PortalVolume.Value;
        
        foreach (var emission in m_materials)
        {
            emission.Key.SetColor(EmissionColor, Color.Lerp(Color.black, emission.Value, m_intensity));
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
    private void OnDestroy() => StationUI.m_instance.OnClose();
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold)
        {
            return false;
        }
        if (alt)
        {
            if (_OnlyAdminRename.Value is Toggle.On && !ZNet.instance.LocalPlayerIsAdminOrHost()) return false;
            TextInput.instance.RequestText(this, "$text_rename", 40);
            return true;
        }

        if (!InUseDistance(user))
        {
            return false;
        }
        StationUI.m_instance.Show(this);
        return true;
    }

    public long GetCreator() => m_nview.GetZDO().GetLong(ZDOVars.s_creator);
    public void SetName(string value) => m_nview.InvokeRPC(nameof(RPC_SetStationName), value);
    public void RPC_SetStationName(long sender, string value)
    {
        if (!m_nview.IsValid() || !m_nview.IsOwner() || GetStationName() == value) return;
        m_nview.GetZDO().Set(m_stationName, value);
    }
    public int GetFilter() => m_nview.GetZDO().GetInt(m_stationFilter);
    public void SetFilter(int index) => m_nview.InvokeRPC(nameof(RPC_SetFilter), index);
    public void RPC_SetFilter(long sender, int value)
    {
        if (!m_nview.IsValid() || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(m_stationFilter, value);
    }

    public bool IsFree() => m_nview.GetZDO().GetBool(m_free);
    public void SetFree(bool enable) => m_nview.InvokeRPC(nameof(RPC_SetFree), enable);
    public void RPC_SetFree(long sender, bool value)
    {
        if (!m_nview.IsValid() || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(m_free, value);
    }
    public string GetGuild() => m_nview.GetZDO().GetString(m_stationGuild);
    public void SetGuild(string guild) => m_nview.InvokeRPC(nameof(RPC_SetGuild), guild);
    public void RPC_SetGuild(long sender, string value)
    {
        if (!m_nview.IsValid() || !m_nview.IsOwner()) return;
        m_nview.GetZDO().Set(m_stationGuild, value);
    }
    
    private string GetStationName() => m_nview.GetZDO().GetString(m_stationName);
    private bool InUseDistance(Humanoid human) => Vector3.Distance(human.transform.position, transform.position) <= use_distance;
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;

    public string GetPrivacy()
    {
        if (StationUI.m_instance.m_dropdown.options.Count <= 0) return "";
        var index = GetFilter();
        if (index < 0 || index >= StationUI.m_instance.m_dropdown.options.Count)
        {
            SetFilter(0);
            return StationUI.m_instance.m_dropdown.options[0].text;
        }
        return StationUI.m_instance.m_dropdown.options[index].text;
    }
    public string GetHoverText()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(GetStationName());
        stringBuilder.Append("\n[<color=yellow><b>$KEY_Use</b></color>] $text_use");
        if (!GetGuild().IsNullOrWhiteSpace()) stringBuilder.Append($"\n$text_guild: {GetGuild()}");
        stringBuilder.Append($"\n$text_privacy: {GetPrivacy()}");
        if (_OnlyAdminRename.Value is Toggle.On && !ZNet.instance.LocalPlayerIsAdminOrHost())
            return Localization.instance.Localize(stringBuilder.ToString());
        stringBuilder.Append("\n[<color=yellow><b>L.Shift + $KEY_Use</b></color>] $text_rename");
        return Localization.instance.Localize(stringBuilder.ToString());
    } 
    public string GetHoverName() => "";
    public string GetText() => !m_nview.IsValid() ? "" : m_nview.GetZDO().GetString(m_stationName);
    public void SetText(string text)
    {
        if (!m_nview.IsValid()) return;
        if (String.IsNullOrWhiteSpace(text)) return;
        m_nview.InvokeRPC(nameof(RPC_SetStationName), text);
    }
}