using System;
using BepInEx;
using UnityEngine;
using static PortalStations.PortalStationsPlugin;

namespace PortalStations.Stations;
public class PortalStation : MonoBehaviour, Interactable, Hoverable, TextReceiver
{
    public static readonly int _prop_station_name = "stationName".GetStableHashCode();
    public static readonly int _prop_station_code = "stationNetwork".GetStableHashCode();
    public static readonly string _FavoriteKey = "PortalStationFavorites";
    
    private const float use_distance = 5.0f;
    private ZNetView _znv = null!;

    public float m_fadeDuration = 1f;
    public ParticleSystem[] m_particles = null!;
    public Light m_light = null!;
    public AudioSource m_audioSource = null!;
    public Material? m_rune;

    public Color m_baseColor;
    public float m_lightBaseIntensity;
    public bool m_active = true;
    public float m_intensity;

    private void Awake()
    {
        GameObject portalEffects = Utils.FindChild(transform, "Portal Effects").gameObject;

        m_particles = portalEffects.GetComponentsInChildren<ParticleSystem>();
        m_light = portalEffects.GetComponentInChildren<Light>();
        m_audioSource = portalEffects.GetComponentInChildren<AudioSource>();
        
        Transform? m_runeGlyph = Utils.FindChild(transform, "emissive");
        if (m_runeGlyph)
        {
            m_rune = m_runeGlyph.GetComponent<MeshRenderer>().material;
            m_baseColor = m_rune.color;
            m_rune.color = new Color(m_baseColor.r, m_baseColor.g, m_baseColor.b, 0f);
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
        
        SetActive(false);

        _znv = GetComponent<ZNetView>();
        if (!_znv.IsValid()) return;

        _znv.Register<string>(nameof(RPC_SetStationName), RPC_SetStationName);
        _znv.Register<bool>(nameof(RPC_SetStationNetwork), RPC_SetStationNetwork);
        
        if (!_znv.IsOwner() || !Player.m_localPlayer) return;
        if (_znv.GetZDO().GetString(_prop_station_name).IsNullOrWhiteSpace()) _znv.GetZDO().Set(_prop_station_name, Player.m_localPlayer.GetPlayerName() + " Portal");
    }

    public void Update()
    {
        
        Player closestPlayer = Player.GetClosestPlayer(transform.position, use_distance);
        bool flag = closestPlayer && Teleportation.IsTeleportable(closestPlayer);
        SetActive(flag);
        
        m_intensity = Mathf.MoveTowards(m_intensity, m_active ? 1f : 0.0f, Time.deltaTime / m_fadeDuration);
        if (m_light)
        {
            m_light.intensity = m_intensity * m_lightBaseIntensity;
            m_light.enabled = m_light.intensity > 0.0;
        }

        if (m_audioSource)
        {
            m_audioSource.volume = m_intensity * _PortalVolume.Value;
        }

        if (m_rune != null)
        {
            m_rune.color = new Color(m_baseColor.r, m_baseColor.g, m_baseColor.b, m_intensity * 1f);
        }
    }

    public void SetActive(bool active)
    {
        if (m_active == active) return;
        m_active = active;
        foreach (ParticleSystem particle in m_particles)
        {
            ParticleSystem.EmissionModule particleEmission = particle.emission;
            particleEmission.enabled = active;
        }
    }
    private void OnDestroy() => PortalStationGUI.HidePortalGUI();
    public bool Interact(Humanoid user, bool hold, bool alt)
    {
        if (hold) return false;
        if (alt)
        {
            if (_OnlyAdminRename.Value is Toggle.On && !Player.m_localPlayer.NoCostCheat()) return false;
            TextInput.instance.RequestText(this, _StationRenameText.Value, 40);
            return true;
        }
        
        return InUseDistance(user) && PortalStationGUI.ShowPortalGUI(_znv);
    }
    public void RPC_SetStationName(long sender, string value)
    {
        if (!_znv.IsValid() || !_znv.IsOwner() || GetStationName() == value) return;
        _znv.GetZDO().Set(_prop_station_name, value);
    }
    public void RPC_SetStationNetwork(long sender, bool value)
    {
        if (!_znv.IsValid() || !_znv.IsOwner()) return;
        if (!TryGetComponent(out Piece piece)) return;
        if (!piece.IsCreator()) return;
        
        _znv.GetZDO().Set(_prop_station_code, value);
    }
    private string GetStationName() => _znv.GetZDO().GetString(_prop_station_name);
    private bool InUseDistance(Humanoid human) => Vector3.Distance(human.transform.position, transform.position) <= use_distance;
    public bool UseItem(Humanoid user, ItemDrop.ItemData item) => false;
    public string GetHoverText()
    {
        if (_OnlyAdminRename.Value is PortalStationsPlugin.Toggle.On && !Player.m_localPlayer.NoCostCheat())
        {
            return _znv.GetZDO().GetString(_prop_station_name)
                   + "\n"
                   + Localization.instance.Localize($"[<color=yellow><b>$KEY_Use</b></color>] {_StationUseText.Value}");
        }
        return _znv.GetZDO().GetString(_prop_station_name) 
               + "\n" 
               + Localization.instance.Localize($"[<color=yellow><b>$KEY_Use</b></color>] {_StationUseText.Value}")
               + "\n"
               + Localization.instance.Localize($"[<color=yellow><b>L.Shift + $KEY_Use</b></color>] {_StationSetNameText.Value}");
    } 
    public string GetHoverName() => "";
    public string GetText() => !_znv.IsValid() ? "" : _znv.GetZDO().GetString(_prop_station_name);
    public void SetText(string text)
    {
        if (!_znv.IsValid()) return;
        if (String.IsNullOrWhiteSpace(text)) return;
        _znv.InvokeRPC(nameof(RPC_SetStationName), text);
    }
}