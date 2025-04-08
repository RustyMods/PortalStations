using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using PortalStations.Stations;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PortalStations.UI;

public class StationElement : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public static readonly List<StationElement> m_instances = new();
    private readonly Vector3 m_portalExitModifier = new Vector3(0f, 1f, 0f);
    public Text m_title = null!;
    public Text m_fuelCount = null!;
    public Image m_fuelIcon = null!;
    public Image m_background = null!;
    public Image m_star = null!;
    public Scrollbar m_scrollRect = null!;

    public string m_name = string.Empty;
    public ItemDrop.ItemData? m_fuelItem;
    public int m_cost;
    public Vector3 m_destination = Vector3.zero;
    public bool m_shouldPan;
    public bool m_pan;
    public float m_panTimer;
    public float m_scrollSpeed = 0.2f;
    public string m_truncatedTitle = "";
    public string m_fullTitle = "";
    public void Awake()
    {
        m_scrollRect = transform.Find("ScrollRect/Scrollbar").GetComponent<Scrollbar>();
        m_title = transform.Find("ScrollRect/Viewport/Title").GetComponent<Text>();
        m_fuelCount = transform.Find("Fuel/Count").GetComponent<Text>();
        m_fuelIcon = transform.Find("Fuel/Icon").GetComponent<Image>();
        m_background = GetComponent<Image>();
        m_star = transform.Find("FavoriteButton/Icon").GetComponent<Image>();
        transform.Find("FavoriteButton").GetComponent<Button>().onClick.AddListener(OnFavorite);
        transform.Find("TeleportButton").GetComponent<Button>().onClick.AddListener(OnTeleport);
        m_scrollRect.value = 1f;
        m_instances.Add(this);
    }

    public void Update()
    {
        if (!m_pan) return;
        var dt = Time.deltaTime;
        m_panTimer += dt;
        if (m_panTimer <= 0.05f) return;
        m_panTimer = 0.0f;
        m_scrollRect.value = Mathf.Lerp(m_scrollRect.value, -0.1f, dt * m_scrollSpeed);
        if (m_scrollRect.value <= 0)
        {
            m_scrollRect.value = 1.0f;
        }
    }

    public void OnDestroy()
    {
        m_instances.Remove(this);
    }

    public void Setup(StationUI.Destination data, ItemDrop.ItemData fuel)
    {
        m_cost = data.m_cost;
        m_destination = data.m_pos;
        m_fuelItem = fuel;
        m_name = data.m_name;
        SetName(FormatName(data));
        m_fuelIcon.sprite = fuel.GetIcon();
        m_fuelCount.text = data.m_isFree ? Localization.instance.Localize("$text_free") : data.m_cost.ToString();
        m_star.color = data.m_isFavorite ? Color.white : Color.black;
    }

    private static string FormatName(StationUI.Destination data)
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append(data.m_name);
        if (PortalStationsPlugin._DisplayBiome.Value is PortalStationsPlugin.Toggle.On)
        {
            stringBuilder.Append($" - {SplitCamelCase(WorldGenerator.instance.GetBiome(data.m_pos).ToString())}");
        }
        if (PortalStationsPlugin._DisplayDistance.Value is PortalStationsPlugin.Toggle.On)
        {
            stringBuilder.Append($" ({data.m_distance:F1}m)");
        }

        return stringBuilder.ToString();
    }

    private static string SplitCamelCase(string input)
    {
        string result = Regex.Replace(input, "([A-Z])", " $1");
        result = Regex.Replace(result, "([A-Z]+)([A-Z][a-z])", "$1 $2");
        return result.TrimStart();
    }
    
    public void SetName(string title)
    {
        m_fullTitle = title;
        if (title.Length > 25)
        {
            m_truncatedTitle = title.Substring(0, 25) + "...";
            m_shouldPan = true;
            m_title.text = m_truncatedTitle;
        }
        else
        {
            m_truncatedTitle = m_fullTitle;
            m_title.text = m_fullTitle;
        }
    }
    
    public void OnFavorite()
    {
        if (m_name.IsNullOrWhiteSpace()) return;
        if (StationUI.m_instance.IsFavorite(m_name))
        {
            StationUI.m_instance.RemoveFavorite(m_name);
            m_star.color = Color.black;
        }
        else
        {
            StationUI.m_instance.AddFavorite(m_name);
            m_star.color = Color.white;
        }
    }

    public void OnTeleport()
    {
        if (!Player.m_localPlayer) return;
        if (m_destination == Vector3.zero || m_fuelItem == null) return;
        if (!Teleportation.IsTeleportable(Player.m_localPlayer))
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return;
        }

        int resources = Player.m_localPlayer.GetInventory().CountItems(m_fuelItem.m_shared.m_name);
        if (resources < m_cost && !Player.m_localPlayer.NoCostCheat())
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_not_enough_fuel");
            return;
        }

        if (!Player.m_localPlayer.NoCostCheat())
        {
            Player.m_localPlayer.GetInventory().RemoveItem(m_fuelItem.m_shared.m_name, m_cost);
        }

        Player.m_localPlayer.TeleportTo(m_destination + m_portalExitModifier, Player.m_localPlayer.transform.rotation, true);
        StationUI.m_instance.OnClose();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_shouldPan)
        {
            m_title.text = m_fullTitle;
            m_pan = true;
        }
        m_background.color = new Color(1f, 1f, 1f, 0.1f);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_pan = false;
        m_title.text = m_truncatedTitle;
        m_scrollRect.value = 1f;
        m_background.color = Color.clear;
    }

    public static void OnFontChange(Font? font)
    {
        foreach (var instance in m_instances)
        {
            instance.m_title.font = font;
            instance.m_fuelCount.font = font;
        }
    }
}