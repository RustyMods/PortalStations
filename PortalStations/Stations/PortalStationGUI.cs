using System.Collections.Generic;
using System.Linq;
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
    
    private static ZNetView? currentPortalStation;
    private static ItemDrop.ItemData? currentDevice;
    
    private const float portal_exit_distance = 1.0f;

    private static GameObject ToggleOn = null!;
    private static GameObject ToggleOff = null!;

    private static Text m_title = null!;
    private static Text m_public_text = null!;
    private static GameObject m_public_button = null!;
    private static GameObject m_public_text_go = null!;
    
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

        if (button.Find("$text_button_close").TryGetComponent(out Text buttonText))
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
        
        Utils.FindChild(PortalGUI.transform, "$text_filter").GetComponent<Text>().text = _StationFilterText.Value;
        Utils.FindChild(PortalGUI.transform, "$text_destinations").GetComponent<Text>().text = _StationDestinationText.Value;
        m_public_text_go = Utils.FindChild(PortalGUI.transform, "$text_public").gameObject;
        m_public_text = m_public_text_go.GetComponent<Text>();

        Transform ToggleButton = Utils.FindChild(PortalGUI.transform, "$part_toggleButton");
        m_public_button = ToggleButton.gameObject;
        if (!ToggleButton.TryGetComponent(out Button toggleButton)) return;
        toggleButton.onClick.AddListener(SetToggleValue);

        if (!toggleButton.GetComponent<ButtonSfx>())
        {
            ButtonSfx ToggleSfx = ToggleButton.gameObject.AddComponent<ButtonSfx>();
            ToggleSfx.m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        }

        ToggleOn = Utils.FindChild(ToggleButton, "On").gameObject;
        ToggleOff = Utils.FindChild(ToggleButton, "Off").gameObject;
        
        if (Utils.FindChild(PortalGUI.transform, "$input_filter").TryGetComponent(out InputField filter))
        {
            filter.onValueChanged.AddListener(e =>
            {
                if (currentDevice != null)
                {
                    FilterDeviceDestinations(e);
                }

                if (currentPortalStation != null)
                {
                    FilterDestinations(e);
                }
            });
        }

        Transform favorite = Utils.FindChild(PortalGUI_Item.transform, "$part_FavoriteButton");
        if (!favorite.GetComponent<ButtonSfx>())
        {
            ButtonSfx sfx = favorite.gameObject.AddComponent<ButtonSfx>();
            sfx.m_sfxPrefab = VanillaButtonSFX.m_sfxPrefab;
        }

        m_title = Utils.FindChild(PortalGUI.transform, "$text_title").GetComponent<Text>();
        m_title.text = _StationTitle.Value;
        m_public_text.text= _PublicText.Value;
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
        if (currentPortalStation == null) return;
        if (!currentPortalStation.IsValid()) return;
        long localId = Player.m_localPlayer.GetPlayerID();
        long creatorId = currentPortalStation.GetZDO().GetLong(ZDOVars.s_creator);
        if (localId != creatorId) return;
        bool flag = !currentPortalStation.GetZDO().GetBool(PortalStation._prop_station_code);
        currentPortalStation.GetZDO().Set(PortalStation._prop_station_code, flag);
        ToggleOn.SetActive(flag);
        ToggleOff.SetActive(!flag);
        
        m_public_text.text = flag ? _PublicText.Value : _PrivateText.Value;
    }

    private static void SetToggleVisibility(bool toggle)
    {
        m_public_button.SetActive(toggle);
        m_public_text_go.SetActive(toggle);
    }
    public static bool ShowPortalGUI(ZNetView znv)
    {
        if (!znv) return false;
        if (!znv.IsValid()) return false;
        znv.ClaimOwnership();
        PortalGUI.SetActive(true);
        currentPortalStation = znv;
        currentDevice = null;
        GetDestinations(znv);
        m_title.text = _StationTitle.Value;
        bool flag = znv.GetZDO().GetBool(PortalStation._prop_station_code);
        ToggleOn.SetActive(flag);
        ToggleOff.SetActive(!flag);
        m_public_text.text = flag ? _PublicText.Value : _PrivateText.Value;
        SetToggleVisibility(true);
        return true;
    }

    public static void ShowPortalGUI(Humanoid user, ItemDrop.ItemData item)
    {
        m_title.text = _PortableStationTitle.Value;
        PortalGUI.SetActive(true);
        SetToggleVisibility(false);
        GetDestinations(user, item);
        currentDevice = item;
        currentPortalStation = null;
    }

    private static void GetDestinations(Humanoid user, ItemDrop.ItemData deviceData, string filter = "")
    {
        DestroyDestinations();
        ItemDrop? fuel = GetFuelItem();
        if (fuel == null) return;
        List<ZDO> Destinations = FindDestinations();
        List<ZDO> FavoriteList = Destinations.FindAll(x => Favorites.Contains(x.GetString(PortalStation._prop_station_name)));
        foreach (ZDO zdo in FavoriteList) CreateDestination(zdo, user, deviceData, filter, fuel, true);

        foreach (ZDO zdo in Destinations)
        {
            if (FavoriteList.Contains(zdo)) continue;
            CreateDestination(zdo, user, deviceData, filter, fuel, false);
        }

        if (_PortalToPlayers.Value is PortalStationsPlugin.Toggle.Off) return;
        foreach (Player player in Player.GetAllPlayers())
        {
            CreateDestination(player, user, deviceData, filter, fuel);
        }
    }


    private static ItemDrop? GetFuelItem()
    {
        if (!ZNetScene.instance) return null;
        GameObject fuelItem = ZNetScene.instance.GetPrefab(_DeviceFuel.Value);
        if (!fuelItem)
        {
            GameObject coins = ZNetScene.instance.GetPrefab("Coins");
            return coins.GetComponent<ItemDrop>();
        };
        return !fuelItem.TryGetComponent(out ItemDrop itemDrop) ? null : itemDrop;
    }

    private static void FilterDestinations(string value)
    {
        if (currentPortalStation == null) return;
        GetDestinations(currentPortalStation, value);
    }

    private static void FilterDeviceDestinations(string value)
    {
        if (currentDevice == null) return;
        GetDestinations(Player.m_localPlayer, currentDevice, value);
    }
    
    private static void GetDestinations(ZNetView znv, string filter = "")
    {
        if (znv == null || !znv.IsValid()) return;
        DestroyDestinations();
        ItemDrop? fuel = GetFuelItem();
        if (fuel == null) return;
        List<ZDO> Destinations = FindDestinations();
        List<ZDO> FavoriteList = Destinations.FindAll(x => Favorites.Contains(x.GetString(PortalStation._prop_station_name)));
        foreach (ZDO zdo in FavoriteList) CreateDestination(zdo, znv, filter, fuel, true);

        foreach (ZDO zdo in Destinations)
        {
            if (FavoriteList.Contains(zdo)) continue;
            CreateDestination(zdo, znv, filter, fuel, false);
        }

        if (_PortalToPlayers.Value is PortalStationsPlugin.Toggle.Off) return;
        foreach (Player player in Player.GetAllPlayers())
        {
            CreateDestination(player, filter, fuel);
        }
    }

    private static List<ZDO> FindDestinations()
    {
        List<ZDO> Destinations = new();
        foreach (string prefab in Stations.PrefabsToSearch)
        {
            int amount = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab, Destinations, ref amount))
            {
            }
        }

        return Destinations;
    }

    private static void DestroyDestinations()
    {
        foreach(Transform item in ItemListRoot) Object.Destroy(item.gameObject);
    }

    private static void CreateDestination(ZDO zdo, Humanoid user, ItemDrop.ItemData device, string filter, ItemDrop fuel, bool isFavorite)
    {
        if (!zdo.IsValid() || zdo.m_uid == user.GetZDOID()) return;
        string name = zdo.GetString(PortalStation._prop_station_name);
        if (name.IsNullOrWhiteSpace()) return;
        if (!filter.IsNullOrWhiteSpace() && !name.ToLower().Contains(filter.ToLower())) return;
        int cost = Teleportation.CalculateFuelCost(device, Vector3.Distance(zdo.GetPosition(), user.transform.position));
        
        GameObject item = Object.Instantiate(PortalGUI_Item, ItemListRoot);
        Utils.FindChild(item.transform, "$part_StationName").GetComponent<Text>().text = name;
        Utils.FindChild(item.transform, "$part_FuelImage").GetComponent<Image>().sprite = fuel.m_itemData.GetIcon();
        Utils.FindChild(item.transform, "$part_FuelCount").GetComponent<Text>().text = cost.ToString();

        if (_DeviceUseFuel.Value is PortalStationsPlugin.Toggle.Off)
        {
            Utils.FindChild(item.transform, "$part_FuelElement").gameObject.SetActive(false);
        }
        
        Transform favorite = Utils.FindChild(item.transform, "$part_FavoriteButton");
        favorite.GetChild(0).GetComponent<Image>().color = isFavorite ? Color.white : Color.black;
        favorite.GetComponent<Button>().onClick.AddListener(() =>
        {
            SetFavorite(zdo);
            GetDestinations(user, device, filter);
        });
        Utils.FindChild(item.transform, "$part_TeleportButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            TeleportWithCost(zdo, cost, fuel);
        });    
    }
    
    private static void CreateDestination(Player player, Humanoid user, ItemDrop.ItemData device, string filter, ItemDrop fuel)
    {
        if (player.GetZDOID() == user.GetZDOID()) return;
        string name = player.GetPlayerName();
        if (name.IsNullOrWhiteSpace()) return;
        if (!filter.IsNullOrWhiteSpace() && !name.ToLower().Contains(filter.ToLower())) return;
        int cost = Teleportation.CalculateFuelCost(device, Vector3.Distance(player.transform.position, user.transform.position));

        GameObject item = Object.Instantiate(PortalGUI_Item, ItemListRoot);
        Utils.FindChild(item.transform, "$part_StationName").GetComponent<Text>().text = name;
        Utils.FindChild(item.transform, "$part_FuelImage").GetComponent<Image>().sprite = fuel.m_itemData.GetIcon();
        Utils.FindChild(item.transform, "$part_FuelCount").GetComponent<Text>().text = cost.ToString();

        if (_PortalUseFuel.Value is PortalStationsPlugin.Toggle.Off)
        {
            Utils.FindChild(item.transform, "$part_FuelElement").gameObject.SetActive(false);
        }
        
        Transform favorite = Utils.FindChild(item.transform, "$part_FavoriteButton");
        favorite.gameObject.SetActive(false);
        favorite.GetChild(0).GetComponent<Image>().color = Color.black;
        favorite.GetComponent<Button>().interactable = false;
        Utils.FindChild(item.transform, "$part_TeleportButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            TeleportWithCost(player.transform.position, cost, fuel);
        });
    }

    private static void CreateDestination(Player player, string filter, ItemDrop fuel)
    {
        if (player.GetZDOID() == Player.m_localPlayer.GetZDOID()) return;
        string name = player.GetPlayerName();
        if (name.IsNullOrWhiteSpace()) return;
        if (!filter.IsNullOrWhiteSpace() && !name.ToLower().Contains(filter.ToLower())) return;
        int cost = Teleportation.CalculateFuelCost(Vector3.Distance(player.transform.position,
            Player.m_localPlayer.transform.position));
        GameObject item = Object.Instantiate(PortalGUI_Item, ItemListRoot);
        Utils.FindChild(item.transform, "$part_StationName").GetComponent<Text>().text = name;
        Utils.FindChild(item.transform, "$part_FuelImage").GetComponent<Image>().sprite = fuel.m_itemData.GetIcon();
        Utils.FindChild(item.transform, "$part_FuelCount").GetComponent<Text>().text = cost.ToString();

        if (_PortalUseFuel.Value is PortalStationsPlugin.Toggle.Off)
        {
            Utils.FindChild(item.transform, "$part_FuelElement").gameObject.SetActive(false);
        }
        
        Transform favorite = Utils.FindChild(item.transform, "$part_FavoriteButton");
        favorite.GetChild(0).GetComponent<Image>().color = Color.black;
        favorite.GetComponent<Button>().interactable = false;
        Utils.FindChild(item.transform, "$part_TeleportButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            TeleportWithCost(player.transform.position, cost, fuel);
        });
    }
    private static void CreateDestination(ZDO zdo, ZNetView znv, string filter, ItemDrop fuel, bool isFavorite)
    {
        if (!zdo.IsValid() || zdo.m_uid == znv.GetZDO().m_uid) return;
        if (!zdo.GetBool(PortalStation._prop_station_code) && zdo.GetLong(ZDOVars.s_creator) != Player.m_localPlayer.GetPlayerID()) return;
        string name = zdo.GetString(PortalStation._prop_station_name);
        if (name.IsNullOrWhiteSpace()) return;
        if (!filter.IsNullOrWhiteSpace() && !name.ToLower().Contains(filter.ToLower())) return;

        int cost = Teleportation.CalculateFuelCost(Vector3.Distance(zdo.GetPosition(), Player.m_localPlayer.transform.position));
        
        GameObject item = Object.Instantiate(PortalGUI_Item, ItemListRoot);
        Utils.FindChild(item.transform, "$part_StationName").GetComponent<Text>().text = name;
        Utils.FindChild(item.transform, "$part_FuelImage").GetComponent<Image>().sprite = fuel.m_itemData.GetIcon();
        Utils.FindChild(item.transform, "$part_FuelCount").GetComponent<Text>().text = cost.ToString();

        if (_PortalUseFuel.Value is PortalStationsPlugin.Toggle.Off)
        {
            Utils.FindChild(item.transform, "$part_FuelElement").gameObject.SetActive(false);
        }
        
        Transform favorite = Utils.FindChild(item.transform, "$part_FavoriteButton");
        favorite.GetChild(0).GetComponent<Image>().color = isFavorite ? Color.white : Color.black;
        favorite.GetComponent<Button>().onClick.AddListener(() =>
        {
            SetFavorite(zdo);
            GetDestinations(znv, filter);
        });
        Utils.FindChild(item.transform, "$part_TeleportButton").GetComponent<Button>().onClick.AddListener(() =>
        {
            TeleportWithCost(zdo, cost, fuel);
        });
    }

    private static void TeleportWithCost(Vector3 location, int cost, ItemDrop fuel)
    {
        if (!Teleportation.IsTeleportable(Player.m_localPlayer))
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return;
        }
        int inventoryFuel = Teleportation.GetFuelAmount(Player.m_localPlayer, fuel);
        if (inventoryFuel < cost && !Player.m_localPlayer.NoCostCheat())
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, _NotEnoughFuelText.Value);
            return;
        }
        if (!Player.m_localPlayer.NoCostCheat()) Teleportation.ConsumeFuel(Player.m_localPlayer, fuel, cost);
        Player.m_localPlayer.TeleportTo(location + new Vector3(0f, portal_exit_distance, 0f),
            Player.m_localPlayer.transform.rotation, true);
        HidePortalGUI();
    }
    private static void TeleportWithCost(ZDO zdo, int cost, ItemDrop fuel)
    {
        if (!Teleportation.IsTeleportable(Player.m_localPlayer))
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return;
        }

        int inventoryFuel = Teleportation.GetFuelAmount(Player.m_localPlayer, fuel);
        if (inventoryFuel < cost && !Player.m_localPlayer.NoCostCheat())
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, _NotEnoughFuelText.Value);
            return;
        }
        if (!Player.m_localPlayer.NoCostCheat()) Teleportation.ConsumeFuel(Player.m_localPlayer, fuel, cost);
        Player.m_localPlayer.TeleportTo(zdo.GetPosition() + new Vector3(0f, portal_exit_distance, 0f), zdo.GetRotation(), true);
        HidePortalGUI();
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