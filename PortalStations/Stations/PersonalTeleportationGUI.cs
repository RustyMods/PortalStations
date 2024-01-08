using System.Collections.Generic;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static PortalStations.PortalStationsPlugin;

namespace PortalStations.Stations;

public static class PersonalTeleportationGUI
{
    private static GameObject PersonalGUI = null!;
    private static GameObject PersonalGUI_Item = null!;
    private static RectTransform ItemListRoot = null!;
    public static void InitGUI(InventoryGui instance)
    {
        if (!instance) return;
        PersonalGUI = Object.Instantiate(_asset.LoadAsset<GameObject>("personalteleportationdevice_gui"));
        PersonalGUI_Item = Object.Instantiate(_asset.LoadAsset<GameObject>("personalteleportationdevice_gui_stationitem"));
        ItemListRoot = Utils.FindChild(PersonalGUI.transform, "$part_Content").GetComponent<RectTransform>();
        
        Object.DontDestroyOnLoad(PersonalGUI);
        Object.DontDestroyOnLoad(PersonalGUI_Item);

        if (!PersonalGUI.TryGetComponent(out RectTransform rootTransform)) return;
        rootTransform.SetParent(instance.transform, false);
        PersonalGUI.SetActive(false);

        ButtonSfx VanillaButtonSFX = instance.m_trophiesPanel.transform.Find("TrophiesFrame/Closebutton").GetComponent<ButtonSfx>();

        Transform button = Utils.FindChild(PersonalGUI.transform, "$part_CloseButton");
        Button closeButton = button.GetComponent<Button>();
        closeButton.onClick.AddListener(HidePersonalPortalGUI);
        ButtonSfx closeButtonSfx = button.gameObject.AddComponent<ButtonSfx>();
        closeButtonSfx.m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        button.Find("Text").GetComponent<Text>().text = _StationCloseText.Value;
        
        Image vanillaBackground = instance.m_trophiesPanel.transform.Find("TrophiesFrame/border (1)").GetComponent<Image>();
        Image[] PortalStationImages = PersonalGUI.GetComponentsInChildren<Image>();
        foreach (Image image in PortalStationImages) image.material = vanillaBackground.material;

        Transform teleportButton = Utils.FindChild(PersonalGUI_Item.transform, "$part_TeleportButton");
        ButtonSfx teleportButtonSfx = teleportButton.gameObject.AddComponent<ButtonSfx>();
        teleportButtonSfx.m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;

        Utils.FindChild(PersonalGUI.transform, "Header").Find("Text").GetComponent<Text>().text = _PortableStationTitle.Value;
        Utils.FindChild(PersonalGUI.transform, "Header (1)").Find("Text").GetComponent<Text>().text = _StationDestinationText.Value;
    }

    public static void ShowPersonalPortalGUI(Humanoid user, ItemDrop.ItemData item)
    {
        PersonalGUI.SetActive(true);
        GetDestinations(user, item);
    }
    private static void GetDestinations(Humanoid user, ItemDrop.ItemData deviceData)
    {
        foreach (Transform item in ItemListRoot) Object.Destroy(item.gameObject);
        if (!ZNetScene.instance) return;
        GameObject fuelItem = ZNetScene.instance.GetPrefab(_DeviceFuel.Value);
        if (!fuelItem) return;
        if (!fuelItem.TryGetComponent(out ItemDrop itemDrop)) return;
        
        // Get all portal stations
        List<ZDO> Destinations = new();
        foreach (string prefab in Stations.PrefabsToSearch)
        {
            int amount = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab, Destinations, ref amount))
            {
            }
        }

        if (_PortalToPlayers.Value is PortalStationsPlugin.Toggle.On)
        {
            List<ZDO> Players = new();
            int playerCount = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative("Player", Players, ref playerCount))
            {
            }

            HashSet<string> uniquePlayerNames = new();

            foreach (ZDO zdo in Players)
            {
                if (!zdo.IsValid() || zdo.m_uid == user.GetZDOID()) continue;
                string name = zdo.GetString(ZDOVars.s_playerName);
                if (name.IsNullOrWhiteSpace()) continue;
                if (name == Player.m_localPlayer.GetPlayerName()) continue;
                if (uniquePlayerNames.Contains(name)) continue;
                uniquePlayerNames.Add(name);
                int cost = PersonalTeleportationDevice.CalculateFuelCost(deviceData, Vector3.Distance( zdo.GetPosition(), user.transform.position));
                GameObject item = Object.Instantiate(PersonalGUI_Item, ItemListRoot);
                Utils.FindChild(item.transform, "$part_StationName").GetComponent<Text>().text = name;
                Utils.FindChild(item.transform, "$part_FuelImage").GetComponent<Image>().sprite = itemDrop.m_itemData.GetIcon();
                Utils.FindChild(item.transform, "$part_FuelCount").GetComponent<Text>().text = cost.ToString();
                Utils.FindChild(item.transform, "$part_TeleportButton").GetComponent<Button>().onClick.AddListener(() =>
                {
                    TeleportToDestinationWithCost(zdo, cost, user, itemDrop);
                });
            }
        }
        
        foreach (ZDO zdo in Destinations)
        {
            if (!zdo.IsValid()) continue;
            string name = zdo.GetString(PortalStation._prop_station_name);
            if (name.IsNullOrWhiteSpace()) continue;
            int cost = PersonalTeleportationDevice.CalculateFuelCost(deviceData, Vector3.Distance(zdo.GetPosition(), user.transform.position));
            GameObject item = Object.Instantiate(PersonalGUI_Item, ItemListRoot);
            Utils.FindChild(item.transform, "$part_StationName").GetComponent<Text>().text = name;
            Utils.FindChild(item.transform, "$part_FuelImage").GetComponent<Image>().sprite = itemDrop.m_itemData.GetIcon();
            Utils.FindChild(item.transform, "$part_FuelCount").GetComponent<Text>().text = cost.ToString();
            Utils.FindChild(item.transform, "$part_TeleportButton").GetComponent<Button>().onClick.AddListener(() =>
            {
                TeleportToDestinationWithCost(zdo, cost, user, itemDrop);
            });
        }
    }
    private static void TeleportToDestinationWithCost(ZDO zdo, int cost, Humanoid user, ItemDrop fuelItem)
    {
        if (!Player.m_localPlayer.IsTeleportable() && _TeleportAnything.Value is PortalStationsPlugin.Toggle.Off)
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return;
        }

        int inventoryFuel = PersonalTeleportationDevice.GetFuelAmount(user, fuelItem);
        if (inventoryFuel < cost && !Player.m_localPlayer.NoCostCheat())
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Not enough fuel");
            return;
        }

        if(!Player.m_localPlayer.NoCostCheat()) PersonalTeleportationDevice.ConsumeFuel(Player.m_localPlayer, fuelItem, cost);
        Player.m_localPlayer.TeleportTo(zdo.GetPosition() + new Vector3(0f,  PortalStationGUI.portal_exit_distance, 0f), zdo.GetRotation(), true);
        HidePersonalPortalGUI();
        
    }
    public static void HidePersonalPortalGUI() => PersonalGUI.SetActive(false);
    public static bool IsPersonalPortalGUIVisible() => PersonalGUI && PersonalGUI.activeSelf;
    public static void UpdatePersonalGUI()
    {
        if (!Input.GetKeyDown(KeyCode.Escape) || !IsPersonalPortalGUIVisible()) return;
        HidePersonalPortalGUI();
        Menu.instance.OnClose();
    }
}