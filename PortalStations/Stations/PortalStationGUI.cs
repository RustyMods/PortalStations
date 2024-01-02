using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static PortalStations.PortalStationsPlugin;
using Object = UnityEngine.Object;

namespace PortalStations.Stations;

public static class PortalStationGUI
{
    private static GameObject PortalGUI = null!;
    private static GameObject PortalGUI_Item = null!;
    private static RectTransform ItemListRoot = null!;
    private static ZNetView currentPortalStation = null!;
    public const float portal_exit_distance = 1.0f;
    public static void InitGUI(InventoryGui instance)
    {
        if (!instance) return;
        PortalGUI = Object.Instantiate(_asset.LoadAsset<GameObject>("portalstation_gui"));
        PortalGUI_Item = Object.Instantiate(_asset.LoadAsset<GameObject>("portalstation_gui_stationitem"));
        ItemListRoot = Utils.FindChild(PortalGUI.transform, "$part_Content").GetComponent<RectTransform>();
        
        Object.DontDestroyOnLoad(PortalGUI);
        Object.DontDestroyOnLoad(PortalGUI_Item);

        if (!PortalGUI.TryGetComponent(out RectTransform rootTransform)) return;
        rootTransform.SetParent(instance.transform, false);
        PortalGUI.SetActive(false);

        ButtonSfx VanillaButtonSFX = instance.m_trophiesPanel.transform.Find("TrophiesFrame/Closebutton").GetComponent<ButtonSfx>();
        Transform button = Utils.FindChild(PortalGUI.transform, "$part_CloseButton");
        Button closeButton = button.GetComponent<Button>();
        closeButton.onClick.AddListener(HidePortalGUI);
        ButtonSfx closeButtonSfx = button.gameObject.AddComponent<ButtonSfx>();
        closeButtonSfx.m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        
        Image vanillaBackground = instance.m_trophiesPanel.transform.Find("TrophiesFrame/border (1)").GetComponent<Image>();
        Image[] PortalStationImages = PortalGUI.GetComponentsInChildren<Image>();
        foreach (Image image in PortalStationImages) image.material = vanillaBackground.material;

        Transform teleportButton = Utils.FindChild(PortalGUI_Item.transform, "$part_TeleportButton");
        ButtonSfx teleportButtonSfx = teleportButton.gameObject.AddComponent<ButtonSfx>();
        teleportButtonSfx.m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
    }
    public static bool ShowPortalGUI(ZNetView znv)
    {
        znv.ClaimOwnership();
        if (!znv.IsValid()) return false;
        PortalGUI.SetActive(true);
        currentPortalStation = znv;
        GetDestinations(znv);
        InputField stationName = Utils.FindChild(PortalGUI.transform, "$part_PortalStationName").GetComponent<InputField>();
        stationName.onValueChanged.RemoveAllListeners();
        stationName.onValueChanged.AddListener(FilterDestinations);

        return true;
    }
    private static void FilterDestinations(string value) => GetDestinations(currentPortalStation, value);
    private static void GetDestinations(ZNetView znv, string filter = "")
    {
        if (znv == null || !znv.IsValid()) return;
        foreach (Transform item in ItemListRoot) Object.Destroy(item.gameObject);
        // Get all portal stations
        List<ZDO> Destinations = new();
        foreach (string prefab in Stations.PrefabsToSearch)
        {
            if (prefab == "Player") continue;
            int amount = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab, Destinations, ref amount))
            {
            }
        }
        foreach (ZDO zdo in Destinations)
        {
            if (!zdo.IsValid() || zdo.m_uid == znv.GetZDO().m_uid) continue;
            string name = zdo.GetString(PortalStation._prop_station_name);
            if (name.IsNullOrWhiteSpace()) continue;
            if (filter.IsNullOrWhiteSpace() || name.Contains(filter))
            {
                GameObject item = Object.Instantiate(PortalGUI_Item, ItemListRoot);
                Text stationName = Utils.FindChild(item.transform, "$part_StationName").GetComponent<Text>();
                stationName.text = name;
                Button teleportButton = Utils.FindChild(item.transform, "$part_TeleportButton").GetComponent<Button>();
                teleportButton.onClick.AddListener(() => { TeleportToDestination(zdo); });
            }
        }
    }
    private static void TeleportToDestination(ZDO zdo)
    {
        if (!Player.m_localPlayer.IsTeleportable() && _TeleportAnything.Value is PortalStationsPlugin.Toggle.Off)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return;
        }
        Player.m_localPlayer.TeleportTo(zdo.GetPosition() + new Vector3(0f, portal_exit_distance, 0f), zdo.GetRotation(), true);
        HidePortalGUI();
        PersonalTeleportationGUI.HidePersonalPortalGUI();
    }

    public static void HidePortalGUI()
    {
        if (PortalGUI) PortalGUI.SetActive(false);
    } 
    public static bool IsPortalGUIVisible() => PortalGUI && PortalGUI.activeSelf;
    public static void UpdateGUI()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || !IsPortalGUIVisible()) return;
        HidePortalGUI();
        Menu.instance.OnClose();
    }
}