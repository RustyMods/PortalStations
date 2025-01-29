using System.Collections.Generic;
using System.Linq;
using BepInEx;
using Guilds;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using static PortalStations.PortalStationsPlugin;
using static PortalStations.Stations.LoadUI;
using Object = UnityEngine.Object;

namespace PortalStations.Stations;

public static class PortalStationGUI
{
    private static ZNetView? currentPortalStation;
    private static ItemDrop.ItemData? currentDevice;
    private const float portal_exit_distance = 1.0f;
    public static List<string> Favorites = new();
    private static HashSet<ZDO> SortStations(HashSet<ZDO> list) => new (list.OrderBy(zdo => zdo.GetString(PortalStation._prop_station_name)).ToList());
    public static void OnFilterInput(string input)
    {
        if (currentDevice != null) GetDestinations(Player.m_localPlayer, currentDevice, input);
        if (currentPortalStation != null) GetDestinations(currentPortalStation, input);
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
    public static void TogglePublic()
    {
        if (currentPortalStation == null || !currentPortalStation.IsValid()) return;
        if (Player.m_localPlayer.GetPlayerID() != currentPortalStation.GetZDO().GetLong(ZDOVars.s_creator)) return;
        bool flag = !currentPortalStation.GetZDO().GetBool(PortalStation._prop_station_code);
        currentPortalStation.GetZDO().Set(PortalStation._prop_station_code, flag);
        ToggleOn.SetActive(flag);
        ToggleOff.SetActive(!flag);
        
        m_public_text.text = flag ? _PublicText.Value : _PrivateText.Value;
    }

    public static void ToggleGroup()
    {
        if (currentPortalStation == null || !currentPortalStation.IsValid()) return;
        if (Player.m_localPlayer.GetPlayerID() != currentPortalStation.GetZDO().GetLong(ZDOVars.s_creator)) return;
        bool flag = !currentPortalStation.GetZDO().GetBool(PortalStation._prop_station_group_code);
        currentPortalStation.GetZDO().Set(PortalStation._prop_station_group_code, flag);
        GroupToggleOn.SetActive(flag);
        GroupToggleOff.SetActive(!flag);
    }
    
    public static void ToggleGuild()
    {
        if (currentPortalStation == null || !currentPortalStation.IsValid()) return;
        if (Player.m_localPlayer.GetPlayerID() != currentPortalStation.GetZDO().GetLong(ZDOVars.s_creator)) return;
        bool flag = !currentPortalStation.GetZDO().GetBool(PortalStation._prop_station_guild_code);
        currentPortalStation.GetZDO().Set(PortalStation._prop_station_guild_code, flag);
        GuildToggleOn.SetActive(flag);
        GuildToggleOff.SetActive(!flag);
    }
    private static void SetToggleVisibility(bool toggle)
    {
        m_public_button.SetActive(toggle);
        m_group_button.SetActive(Groups.API.IsLoaded() && toggle);
        m_guild_button.SetActive(Guilds.API.IsLoaded() && toggle);
    }
    public static bool ShowPortalGUI(ZNetView znv)
    {
        if (!znv || !znv.IsValid()) return false;
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
        bool grouped = znv.GetZDO().GetBool(PortalStation._prop_station_group_code);
        bool guild = znv.GetZDO().GetBool(PortalStation._prop_station_guild_code);
        GroupToggleOn.SetActive(grouped);
        GroupToggleOff.SetActive(!grouped);
        GuildToggleOn.SetActive(guild);
        GuildToggleOff.SetActive(!guild);
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
        HashSet<ZDO> Destinations = FindDestinations();
        
        foreach (ZDO zdo in Destinations.Where(x => Favorites.Contains(x.GetString(PortalStation._prop_station_name))))
        {
            CreateDestination(zdo, user, deviceData, filter, fuel, true);
        }

        foreach (ZDO zdo in Destinations.Where(x => !Favorites.Contains(x.GetString(PortalStation._prop_station_name))))
        {
            CreateDestination(zdo, user, deviceData, filter, fuel, false);
        }

        if (_PortalToPlayers.Value is PortalStationsPlugin.Toggle.Off) return;

        foreach (ZNetPeer peer in ZNet.instance.GetPeers())
        {
            CreateDestination(peer, user, deviceData, filter, fuel);
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
    private static void GetDestinations(ZNetView znv, string filter = "")
    {
        if (znv == null || !znv.IsValid()) return;
        DestroyDestinations();
        ItemDrop? fuel = GetFuelItem();
        if (fuel == null) return;
        HashSet<ZDO> Destinations = SortStations(FindDestinations());

        foreach (ZDO zdo in Destinations.Where(x => Favorites.Contains(x.GetString(PortalStation._prop_station_name))))
        {
            GetDestination(zdo, znv, filter, fuel, true);
        }

        foreach (ZDO zdo in Destinations.Where(x => !Favorites.Contains(x.GetString(PortalStation._prop_station_name))))
        {
            GetDestination(zdo, znv, filter, fuel, false);
        }

        if (_PortalToPlayers.Value is PortalStationsPlugin.Toggle.Off || !ZNet.instance) return;

        foreach (ZNetPeer peer in ZNet.instance.GetPeers().Where(peer => peer.IsReady()))
        {
            CreatePlayerDestination(peer, filter, fuel);
        }
    }
    private static HashSet<ZDO> FindDestinations()
    {
        List<ZDO> Destinations = new();
        foreach (string prefab in Stations.PrefabsToSearch)
        {
            int amount = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab, Destinations, ref amount))
            {
            }
        }

        return new HashSet<ZDO>(Destinations);
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
    private static void CreateDestination(ZNetPeer peer, Humanoid user, ItemDrop.ItemData device, string filter, ItemDrop fuel)
    {
        if (peer.m_characterID == user.GetZDOID()) return;
        string name = peer.m_playerName;
        if (name.IsNullOrWhiteSpace() || name.ToLower().Contains("stranger")) return;
        if (!filter.IsNullOrWhiteSpace() && !name.ToLower().Contains(filter.ToLower())) return;
        int cost = Teleportation.CalculateFuelCost(device, Vector3.Distance(peer.GetRefPos(), user.transform.position));

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
            TeleportWithCost(peer.GetRefPos(), cost, fuel);
        });
    }
    private static void CreatePlayerDestination(ZNetPeer peer, string filter, ItemDrop fuel)
    {
        if (peer.m_characterID == Player.m_localPlayer.GetZDOID()) return;
        if (peer.m_playerName == "Stranger") return;
        string name = peer.m_playerName;
        if (name.IsNullOrWhiteSpace()) return;
        if (!filter.IsNullOrWhiteSpace() && !name.ToLower().Contains(filter.ToLower())) return;
        int cost = Teleportation.CalculateFuelCost(Vector3.Distance(peer.m_refPos, Player.m_localPlayer.transform.position));
        
        CreateDestination(cost, name, fuel, out Image image, out Button favoriteButton, out Button teleportButton);

        image.color = Color.black;
        favoriteButton.interactable = false;
        teleportButton.onClick.AddListener(() =>
        {
            TeleportWithCost(peer.m_refPos, cost, fuel);
        });
    }

    private static bool isInGroup(long creator, bool enable)
    {
        if (!enable) return false;
        return Groups.API.FindGroupMemberByPlayerId(creator) != null;
    }
    private static bool isInGuild(string guildName, bool enable)
    {
        if (!enable) return false;
        if (Guilds.API.GetOwnGuild() is not { } guild) return false;

        return guild.Name == guildName;
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.SetCreator))]
    private static class Piece_SetCreator_Patch
    {
        private static void Postfix(Piece __instance)
        {
            if (__instance.m_nview == null || !__instance.m_nview.IsOwner() || !__instance.m_nview.GetZDO().GetString("CreatorName").IsNullOrWhiteSpace()) return;
            __instance.m_nview.GetZDO().Set("CreatorName".GetStableHashCode(), Game.instance.GetPlayerProfile().GetName());
            if (Guilds.API.IsLoaded() && Guilds.API.GetOwnGuild() is { } guild)
            {
                __instance.m_nview.GetZDO().Set("Guild".GetStableHashCode(), guild.Name);
            }
        }
    }

    [HarmonyPatch(typeof(Piece), nameof(Piece.Awake))]
    private static class Piece_Awake_Patch
    {
        private static void Postfix(Piece __instance)
        {
            if (!__instance.GetComponent<PortalStation>()) return;
            if (!__instance.m_nview.IsValid() || !__instance.m_nview.IsOwner()) return;
            if (Player.m_localPlayer && Player.m_localPlayer.GetPlayerID() == __instance.GetCreator() 
                                     && __instance.m_nview.GetZDO().GetString("Guild".GetStableHashCode()).IsNullOrWhiteSpace() 
                                     && Guilds.API.IsLoaded() && Guilds.API.GetOwnGuild() is { } guild)
            {
                __instance.m_nview.GetZDO().Set("Guild".GetStableHashCode(), guild.Name);
            }
        }
    }
    private static bool isPublic(ZDO zdo) => zdo.GetBool(PortalStation._prop_station_code);
    private static bool isCreator(long creator) => creator == Player.m_localPlayer.GetPlayerID();
    private static void GetDestination(ZDO zdo, ZNetView znv, string filter, ItemDrop fuel, bool isFavorite)
    {
        if (!zdo.IsValid() || zdo.m_uid == znv.GetZDO().m_uid) return;
        long creator = zdo.GetLong(ZDOVars.s_creator);
        string creatorGuild = zdo.GetString("Guild".GetStableHashCode());
        if (!isPublic(zdo) && !isCreator(creator) 
                           && !isInGroup(creator, znv.GetZDO().GetBool(PortalStation._prop_station_group_code)) 
                           && !isInGuild(creatorGuild, znv.GetZDO().GetBool(PortalStation._prop_station_guild_code))) return;
        string name = zdo.GetString(PortalStation._prop_station_name);
        if (name.IsNullOrWhiteSpace()) return;
        if (!filter.IsNullOrWhiteSpace() && !name.ToLower().Contains(filter.ToLower())) return;
        int cost = Teleportation.CalculateFuelCost(Vector3.Distance(zdo.GetPosition(), Player.m_localPlayer.transform.position));

        CreateDestination(cost, name, fuel, out Image favoriteImage, out Button favoriteButton, out Button teleportButton);
        favoriteImage.color = isFavorite ? Color.white : Color.black;
        favoriteButton.onClick.AddListener(() =>
        {
            SetFavorite(zdo);
            GetDestinations(znv, filter);
        });
        teleportButton.onClick.AddListener(() =>
        {
            TeleportWithCost(zdo, cost, fuel);

        });
    }
    private static void CreateDestination(int cost, string name, ItemDrop fuel, out Image image, out Button favoriteButton, out Button teleportButton)
    {
        GameObject item = Object.Instantiate(PortalGUI_Item, ItemListRoot);
        Utils.FindChild(item.transform, "$part_StationName").GetComponent<Text>().text = name;
        Utils.FindChild(item.transform, "$part_FuelImage").GetComponent<Image>().sprite = fuel.m_itemData.GetIcon();
        Utils.FindChild(item.transform, "$part_FuelCount").GetComponent<Text>().text = cost.ToString();

        if (_PortalUseFuel.Value is PortalStationsPlugin.Toggle.Off)
        {
            Utils.FindChild(item.transform, "$part_FuelElement").gameObject.SetActive(false);
        }
        
        Transform favorite = Utils.FindChild(item.transform, "$part_FavoriteButton");

        image = favorite.GetChild(0).GetComponent<Image>();
        favoriteButton = favorite.GetComponent<Button>();
        teleportButton = Utils.FindChild(item.transform, "$part_TeleportButton").GetComponent<Button>();
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
        Player.m_localPlayer.TeleportTo(location + new Vector3(0f, portal_exit_distance, 0f), Player.m_localPlayer.transform.rotation, true);
        HidePortalGUI();
    }
    private static void TeleportWithCost(ZDO zdo, int cost, ItemDrop fuel) => TeleportWithCost(zdo.GetPosition(), cost, fuel);
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