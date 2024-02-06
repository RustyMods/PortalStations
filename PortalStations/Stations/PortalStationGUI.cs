using System.Collections.Generic;
using BepInEx;
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

    private static GameObject ToggleOn = null!;
    private static GameObject ToggleOff = null!;
    
    public static List<string> Favorites = new();
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
        if (!button.GetComponent<ButtonSfx>())
        {
            ButtonSfx closeButtonSfx = button.gameObject.AddComponent<ButtonSfx>();
            closeButtonSfx.m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        }

        if (button.Find("Text").TryGetComponent(out Text buttonText))
        {
            buttonText.text = _StationCloseText.Value;
        }
        
        Image vanillaBackground = instance.m_trophiesPanel.transform.Find("TrophiesFrame/border (1)").GetComponent<Image>();
        Image[] PortalStationImages = PortalGUI.GetComponentsInChildren<Image>();
        foreach (Image image in PortalStationImages) image.material = vanillaBackground.material;

        Transform teleportButton = Utils.FindChild(PortalGUI_Item.transform, "$part_TeleportButton");
        if (!teleportButton.GetComponent<ButtonSfx>())
        {
            ButtonSfx teleportButtonSfx = teleportButton.gameObject.AddComponent<ButtonSfx>();
            teleportButtonSfx.m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        }
        
        Utils.FindChild(PortalGUI.transform, "Header").Find("Text").GetComponent<Text>().text = _StationTitle.Value;
        Utils.FindChild(PortalGUI.transform, "Header (2)").Find("Text").GetComponent<Text>().text = _StationFilterText.Value;
        Utils.FindChild(PortalGUI.transform, "Header (1)").Find("Text").GetComponent<Text>().text = _StationDestinationText.Value;
        Utils.FindChild(PortalGUI.transform, "Header (3)").Find("Text").GetComponent<Text>().text = _PublicText.Value;

        Transform ToggleButton = Utils.FindChild(PortalGUI.transform, "$part_toggleButton");
        if (!ToggleButton.TryGetComponent(out Button toggleButton)) return;
        toggleButton.onClick.AddListener(SetToggleValue);

        if (!toggleButton.GetComponent<ButtonSfx>())
        {
            ButtonSfx ToggleSfx = ToggleButton.gameObject.AddComponent<ButtonSfx>();
            ToggleSfx.m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        }

        ToggleOn = Utils.FindChild(ToggleButton, "On").gameObject;
        ToggleOff = Utils.FindChild(ToggleButton, "Off").gameObject;
        
        if (Utils.FindChild(PortalGUI.transform, "$part_PortalStationName").TryGetComponent(out InputField filter))
        {
            filter.onValueChanged.AddListener(FilterDestinations);
        }

        Transform favorite = Utils.FindChild(PortalGUI_Item.transform, "$part_FavoriteButton");
        if (!favorite.GetComponent<ButtonSfx>())
        {
            ButtonSfx sfx = favorite.gameObject.AddComponent<ButtonSfx>();
            sfx.m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        }
    }

    private static void SetFavorite(ZDO zdo)
    {
        if (Favorites.Contains(zdo.GetString(PortalStation._prop_station_name)))
        {
            Favorites.Remove(zdo.GetString(PortalStation._prop_station_name));
        }
        else
        {
            Favorites.Add(zdo.GetString(PortalStation._prop_station_name));
        }
        Patches.SaveFavorites();
    }

    private static void SetToggleValue()
    {
        if (!currentPortalStation.IsValid()) return;
        long localId = Player.m_localPlayer.GetPlayerID();
        long creatorId = currentPortalStation.GetZDO().GetLong(ZDOVars.s_creator);
        if (localId != creatorId) return;
        bool flag = !currentPortalStation.GetZDO().GetBool(PortalStation._prop_station_code);
        currentPortalStation.GetZDO().Set(PortalStation._prop_station_code, flag);
        ToggleOn.SetActive(flag);
        ToggleOff.SetActive(!flag);
        
        Utils.FindChild(PortalGUI.transform, "Header (3)").Find("Text").GetComponent<Text>().text = flag ? _PublicText.Value : _PrivateText.Value;

    }
    public static bool ShowPortalGUI(ZNetView znv)
    {
        if (!znv) return false;
        if (!znv.IsValid()) return false;
        znv.ClaimOwnership();
        PortalGUI.SetActive(true);
        currentPortalStation = znv;
        GetDestinations(znv);

        bool flag = znv.GetZDO().GetBool(PortalStation._prop_station_code);
        ToggleOn.SetActive(flag);
        ToggleOff.SetActive(!flag);

        if (Utils.FindChild(PortalGUI.transform, "Header (3)").Find("Text").TryGetComponent(out Text public_private))
        {
            public_private.text = flag ? _PublicText.Value : _PrivateText.Value;
        }
        
        return true;
    }
    private static void FilterDestinations(string value) => GetDestinations(currentPortalStation, value);
    private static void GetDestinations(ZNetView znv, string filter = "")
    {
        if (znv == null || !znv.IsValid()) return;
        foreach (Transform item in ItemListRoot) Object.Destroy(item.gameObject);
        
        long localId = Player.m_localPlayer.GetPlayerID();
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

        List<ZDO> FavoriteList = Destinations.FindAll(x => Favorites.Contains(x.GetString(PortalStation._prop_station_name)));

        foreach (ZDO zdo in FavoriteList)
        {
            // If Portal Station is itself, ignore
            if (!zdo.IsValid() || zdo.m_uid == znv.GetZDO().m_uid) continue;
            // If Portal Station is private and user is not creator, ignore
            long creatorId = zdo.GetLong(ZDOVars.s_creator);
            if (!zdo.GetBool(PortalStation._prop_station_code) && creatorId != localId) continue;
            // Create GUI item
            string name = zdo.GetString(PortalStation._prop_station_name);
            if (name.IsNullOrWhiteSpace()) continue;
            if (filter.IsNullOrWhiteSpace() || name.ToLower().Contains(filter.ToLower()))
            {
                GameObject item = Object.Instantiate(PortalGUI_Item, ItemListRoot);
                Utils.FindChild(item.transform, "$part_StationName").GetComponent<Text>().text = name;
                Button teleportButton = Utils.FindChild(item.transform, "$part_TeleportButton").GetComponent<Button>();
                teleportButton.onClick.AddListener(() => { TeleportToDestination(zdo); });
                
                Transform favorite = Utils.FindChild(item.transform, "$part_FavoriteButton");
                Image favoriteImage = favorite.GetChild(0).GetComponent<Image>();
                favoriteImage.color = Color.white;
                if (favorite.TryGetComponent(out Button favoriteButton))
                {
                    favoriteButton.onClick.AddListener(() =>
                    {
                        SetFavorite(zdo);
                        GetDestinations(znv, filter);
                    });
                }
            }
        }
        
        foreach (ZDO zdo in Destinations)
        {
            if (FavoriteList.Contains(zdo)) continue;
            // If Portal Station is itself, ignore
            if (!zdo.IsValid() || zdo.m_uid == znv.GetZDO().m_uid) continue;
            // If Portal Station is private and user is not creator, ignore
            long creatorId = zdo.GetLong(ZDOVars.s_creator);
            if (!zdo.GetBool(PortalStation._prop_station_code) && creatorId != localId) continue;
            // Create GUI item
            string name = zdo.GetString(PortalStation._prop_station_name);
            if (name.IsNullOrWhiteSpace()) continue;
            if (filter.IsNullOrWhiteSpace() || name.ToLower().Contains(filter.ToLower()))
            {
                GameObject item = Object.Instantiate(PortalGUI_Item, ItemListRoot);
                Utils.FindChild(item.transform, "$part_StationName").GetComponent<Text>().text = name;
                Button teleportButton = Utils.FindChild(item.transform, "$part_TeleportButton").GetComponent<Button>();
                teleportButton.onClick.AddListener(() => { TeleportToDestination(zdo); });
                
                Transform favorite = Utils.FindChild(item.transform, "$part_FavoriteButton");
                Image favoriteImage = favorite.GetChild(0).GetComponent<Image>();
                favoriteImage.color = Color.black;
                if (favorite.TryGetComponent(out Button favoriteButton))
                {
                    favoriteButton.onClick.AddListener(() =>
                    {
                        SetFavorite(zdo);
                        GetDestinations(znv, filter);
                    });
                }
            }
        }
    }
    private static void TeleportToDestination(ZDO zdo)
    {
        if (!Teleportation.IsTeleportable(Player.m_localPlayer))
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