using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx;
using HarmonyLib;
using PortalStations.Stations;
using UnityEngine;
using UnityEngine.UI;
using Valheim.SettingsGui;
using YamlDotNet.Serialization;
using Toggle = UnityEngine.UI.Toggle;

namespace PortalStations.UI;

public class StationUI : MonoBehaviour
{
    private static readonly Sprite m_on = PortalStationsPlugin.UIAssets.LoadAsset<Sprite>("toggleOn");
    private static readonly Sprite m_off = PortalStationsPlugin.UIAssets.LoadAsset<Sprite>("toggleOff");
    private static GameObject m_item = null!;
    public static StationUI m_instance = null!;
    private static List<string> m_favorites = new();
    private static PortalStation? m_currentStation;
    private static List<Destination> m_destinations = new();

    public RectTransform m_panel = null!;
    public RectTransform m_settingsPanel = null!;
    public Button m_settings = null!;
    public Text m_title = null!;
    public Text m_destination = null!;
    public RectTransform m_content = null!;
    public Dropdown m_dropdown = null!;
    public float m_itemSize;
    public float m_contentSize;
    public InputField m_rename = null!;
    public InputField m_guild = null!;
    public GameObject m_noCostToggle = null!;
    public Text m_noCostText = null!;
    public Image m_freeToggle = null!;
    public Text m_data = null!;
    public enum FilterOptions
    {
        Public, Private, GuildOnly, GroupOnly, GuildGroupOnly
    }

    public static FilterOptions GetFilterOption(int index)
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
    
    public void Init()
    {
        List<Text> texts = new();
        m_item = PortalStationsPlugin.UIAssets.LoadAsset<GameObject>("StationElement");
        m_item.AddComponent<StationElement>();
        texts.AddRange(m_item.GetComponentsInChildren<Text>());
        texts.AddRange(GetComponentsInChildren<Text>(true));
        FontManager.SetFont(texts.ToArray());
        m_panel = transform.Find("Panel").GetComponent<RectTransform>();
        m_settingsPanel = transform.Find("Settings").GetComponent<RectTransform>();
        m_title = transform.Find("Text_Title").GetComponent<Text>();
        transform.Find("CloseButton").GetComponent<Button>().onClick.AddListener(OnClose);
        var searchField = transform.Find("Panel/SearchField").GetComponent<InputField>();
        searchField.onValueChanged.AddListener(OnSearch);
        m_destination = transform.Find("Panel/Text_Destinations").GetComponent<Text>();
        m_content = transform.Find("Panel/ContentFrame/ScrollView/Viewport/ContentList").GetComponent<RectTransform>();
        m_dropdown = transform.Find("Settings/Dropdown").GetComponent<Dropdown>();
        m_dropdown.onValueChanged.AddListener(OnSelectChoice);
        m_contentSize = transform.Find("Panel/ContentFrame").GetComponent<RectTransform>().sizeDelta.y;
        m_itemSize = m_item.GetComponent<RectTransform>().sizeDelta.y + m_content.GetComponent<VerticalLayoutGroup>().spacing;
        m_instance = this;
        searchField.transform.Find("Placeholder").GetComponent<Text>().text = Localization.instance.Localize("$text_search");
        m_dropdown.transform.Find("Template/Item").gameObject.AddComponent<DropdownItem>();
        m_dropdown.AddOptions(new List<string>()
        {
            Localization.instance.Localize("$text_public"), 
            Localization.instance.Localize("$text_private"), 
            Localization.instance.Localize("$text_guild_only"), 
            Localization.instance.Localize("$text_group_only"), 
            Localization.instance.Localize("$text_guild_and_group_only")
        });
        
        m_settings = transform.Find("SettingButton").GetComponent<Button>();
        m_settings.onClick.AddListener(OnSettings);
        m_rename = transform.Find("Settings/Rename").GetComponent<InputField>();
        m_rename.onValueChanged.AddListener(OnRename);
        m_guild = transform.Find("Settings/GuildName").GetComponent<InputField>();
        m_guild.onValueChanged.AddListener(OnGuild);
        transform.Find("Settings/Text_Rename").GetComponent<Text>().text = Localization.instance.Localize("$text_rename");
        transform.Find("Settings/Text_Guild").GetComponent<Text>().text = Localization.instance.Localize("$text_guild");
        transform.Find("Settings/Text_Filter").GetComponent<Text>().text = Localization.instance.Localize("$text_privacy");
        transform.Find("Panel/Text_Search").GetComponent<Text>().text = Localization.instance.Localize("$text_search");
        List<Button> buttons = new();
        var sfx = InventoryGui.instance.transform.Find("root/Trophies/TrophiesFrame/Closebutton").GetComponent<ButtonSfx>().m_sfxPrefab;
        buttons.AddRange(GetComponentsInChildren<Button>(true));
        buttons.AddRange(m_item.GetComponentsInChildren<Button>(true));
        foreach (var button in buttons)
        {
            button.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = sfx;
            button.gameObject.AddComponent<StationButton>();
        }

        var freeToggle = transform.Find("Settings/FreeToggle").GetComponent<Toggle>();
        freeToggle.onValueChanged.AddListener(OnFreeToggle);
        freeToggle.gameObject.AddComponent<UIHover>().m_tooltip = "Admin Only";
        m_freeToggle = transform.Find("Settings/FreeToggle/Graphic").GetComponent<Image>();
        m_noCostText = transform.Find("Settings/Text_NoCost").GetComponent<Text>();
        m_noCostToggle = freeToggle.gameObject;
        m_noCostText.text = Localization.instance.Localize("$text_nocost");
        
        m_data = transform.Find("Settings/Text_Data").GetComponent<Text>();
        SetDestinationTitle("$text_destinations");
        OnClose();
    }

    public void ShowFreeToggle(bool enable)
    {
        m_noCostToggle.SetActive(enable);
        m_noCostText.gameObject.SetActive(enable);
    }

    public void SetTitle(string text) => m_title.text = Localization.instance.Localize(text);
    public void SetDestinationTitle(string text) => m_destination.text = Localization.instance.Localize(text);

    public void Add(Destination destination)
    {
        if (!Player.m_localPlayer || GetFuelItem() is not {} fuelItem) return;
        Instantiate(m_item, m_content).GetComponent<StationElement>()
            .Setup(destination, fuelItem.m_itemData);
    }

    private static ItemDrop? GetFuelItem()
    {
        if (!ZNetScene.instance) return null;
        GameObject fuelItem = ZNetScene.instance.GetPrefab(PortalStationsPlugin._DeviceFuel.Value);
        if (!fuelItem)
        {
            GameObject coins = ZNetScene.instance.GetPrefab("Coins");
            return coins.GetComponent<ItemDrop>();
        };
        return !fuelItem.TryGetComponent(out ItemDrop itemDrop) ? null : itemDrop;
    }

    public void Resize()
    {
        int count = 0;
        foreach (Transform child in m_content)
        {
            if (child.gameObject.activeSelf) ++count;
        }

        var newHeight = Mathf.CeilToInt(count * m_itemSize);
        m_content.offsetMin = newHeight < m_contentSize ? Vector2.zero : new Vector2(0f, -(newHeight - m_contentSize));
    }

    public void OnSelectChoice(int option)
    {
        if (m_currentStation == null) return;
        if (Player.m_localPlayer.GetPlayerID() != m_currentStation.GetCreator()) return;
        m_currentStation.SetFilter(option);
    }

    public void OnSearch(string input)
    {
        foreach (var element in StationElement.m_instances)
        {
            element.gameObject.SetActive(element.m_name.ToLower().Contains(input.ToLower()));
        }
        Resize();
    }

    public void OnRename(string input)
    {
        if (input.IsNullOrWhiteSpace()) return;
        if (m_currentStation == null)
        {
            m_rename.text = "";
            return;
        }

        if (PortalStationsPlugin._OnlyAdminRename.Value is PortalStationsPlugin.Toggle.On &&
            !ZNet.instance.LocalPlayerIsAdminOrHost())
        {
            m_rename.text = "";
            return;
        }
        m_currentStation.SetName(input);
        SetTitle(input);
    }

    public void OnGuild(string input)
    {
        if (input.IsNullOrWhiteSpace()) return;
        if (m_currentStation == null)
        {
            m_guild.text = "";
            return;
        }

        if (Player.m_localPlayer.GetPlayerID() != m_currentStation.GetCreator())
        {
            m_guild.text = "";
            return;
        }

        m_currentStation.SetGuild(input);
    }

    private bool IsSettingsOpen() => m_settingsPanel.gameObject.activeSelf;

    public void OnSettings()
    {
        if (m_currentStation == null) return;
        if (IsSettingsOpen())
        {
            ShowPanel();
        }
        else
        {
            if (m_currentStation.GetCreator() != Player.m_localPlayer.GetPlayerID()) return;
            ShowSettings();
        }
    }

    public void ShowSettings()
    {
        m_panel.gameObject.SetActive(false);
        m_settingsPanel.gameObject.SetActive(true);
        if (ZNet.m_instance)
        {
            ShowFreeToggle(ZNet.m_instance.LocalPlayerIsAdminOrHost());
        }
    }

    public void ShowPanel()
    {
        m_panel.gameObject.SetActive(true);
        m_settingsPanel.gameObject.SetActive(false);
    }
    public void OnClose()
    {
        gameObject.SetActive(false);
        m_currentStation = null;
        Clear();
    }

    public void OnFreeToggle(bool enable)
    {
        if (m_currentStation == null) return;
        if (!ZNet.instance.LocalPlayerIsAdminOrHost()) return;
        m_freeToggle.sprite = enable ? m_on : m_off;
        m_freeToggle.color = enable ? new Color32(32, 187, 69, 255) : new Color32(185, 69, 34, 255);
        m_currentStation.SetFree(enable);
    }

    public void SetData(string text) => m_data.text = Localization.instance.Localize(text);

    public void Show(PortalStation? station = null)
    {
        ShowPanel();
        gameObject.SetActive(true);
        m_currentStation = station;
        SetData("");
        if (m_currentStation != null)
        {
            SetTitle(m_currentStation.GetText());
            m_dropdown.value = m_currentStation.GetFilter();
            m_rename.text = m_currentStation.GetText();
            m_guild.text = m_currentStation.GetGuild();
            var free = m_currentStation.IsFree();
            m_freeToggle.sprite = free ? m_on : m_off;
            m_freeToggle.color = free ? new Color32(32, 187, 69, 255) : new Color32(185, 69, 34, 255);
            SetData(GetStationData());
        }
        LoadDestinations();
    }

    private string GetStationData()
    {
        return "";
        if (m_currentStation == null) return "";
        var portalName = m_currentStation.m_nview.GetZDO().GetString(PortalStation.m_stationName);
        var creator = m_currentStation.m_nview.GetZDO().GetLong(ZDOVars.s_creator);
        var creatorName = "Not available";
        if (creator == Player.m_localPlayer.GetPlayerID()) creatorName = Player.m_localPlayer.GetPlayerName();
        else
        {
            foreach (var player in ZNet.instance.GetPlayerList())
            {
                if (player.m_characterID.UserID != creator) continue;
                creatorName = player.m_name;
                break;
            }
        }
        var isFavorite = m_instance.IsFavorite(portalName);
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append($"Creator: {creatorName}");
        stringBuilder.Append($"\nIs Favorite: {(isFavorite ? "TRUE" : "FALSE")}");
        return stringBuilder.ToString();
    }

    public void LoadDestinations()
    {
        m_destinations.Clear();
        foreach (var zdo in GetDestinations())
        {
            var _ = new Destination(zdo);
        }

        if (PortalStationsPlugin._PortalToPlayers.Value is PortalStationsPlugin.Toggle.On && ZNet.instance)
        {
            foreach (var peer in ZNet.instance.GetPeers().Where(peer => peer.IsReady()))
            {
                var _ = new Destination(peer);
            }
        }

        if (m_destinations.Count <= 0) return;
        m_destinations = m_destinations
            .OrderBy(x => !x.m_isFavorite)
            .ThenBy(x => x.m_name)     
            .ToList();
        
        foreach (var destination in m_destinations)
        {
            Add(destination);
        }
        Resize();
    }
    public void Clear()
    {
        foreach (Transform child in m_content)
        {
            Destroy(child.gameObject);
        }
    }
    
    private static HashSet<ZDO> GetDestinations()
    {
        List<ZDO> Destinations = new();
        foreach (string prefab in Stations.Stations.PrefabsToSearch)
        {
            int amount = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab, Destinations, ref amount))
            {
            }
        }

        return new HashSet<ZDO>(Destinations);
    }

    public bool IsFavorite(string title) => m_favorites.Contains(title);
    public void AddFavorite(string title) => m_favorites.Add(title);
    public void RemoveFavorite(string title) => m_favorites.Remove(title);
    public static bool IsVisible() => m_instance && m_instance.gameObject.activeInHierarchy;
    
    public class Destination
    {
        public Vector3 m_pos = Vector3.zero;
        public readonly string m_name = "";
        public readonly long m_creator = 0L;
        public readonly string m_creatorName = "";
        public readonly string m_guild = "";
        public readonly int m_filter;
        public readonly int m_cost;
        public readonly bool m_isFavorite;
        public readonly float m_distance;
        public readonly bool m_isFree;

        public Destination(ZDO zdo)
        {
            if (!zdo.IsValid() || !Player.m_localPlayer) return;
            if (m_currentStation == null || !m_currentStation.m_nview.IsValid()) return;
            if (m_currentStation.m_nview.GetZDO().m_uid == zdo.m_uid) return;
            m_name = zdo.GetString(PortalStation.m_stationName);
            m_creator = zdo.GetLong(ZDOVars.s_creator);
            m_creatorName = ZNet.instance.GetPlayerList().Find(x => x.m_characterID.ID == m_creator) is { } info
                ? info.m_name
                : "";
            m_guild = zdo.GetString(PortalStation.m_stationGuild);
            m_pos = zdo.GetPosition();
            m_filter = zdo.GetInt(PortalStation.m_stationFilter);
            m_isFree = zdo.GetBool(PortalStation.m_free);
            m_distance = Vector3.Distance(m_pos, Player.m_localPlayer.transform.position);
            m_cost = m_isFree ? 0 : Teleportation.CalculateFuelCost(m_distance);
            m_isFavorite = m_instance.IsFavorite(m_name);
            var option = GetFilterOption(m_filter);
            switch (option)
            {
                case FilterOptions.GuildOnly when !IsInGuild():
                case FilterOptions.GroupOnly when !IsInGroup():
                case FilterOptions.Private when m_creator != Player.m_localPlayer.GetPlayerID():
                case FilterOptions.GuildGroupOnly when !IsInGuild() && !IsInGroup():
                    return;
            }
            m_destinations.Add(this);
        }

        public Destination(ZNetPeer peer)
        {
            if (!Player.m_localPlayer) return;
            if (peer.m_characterID == Player.m_localPlayer.GetZDOID()) return;
            if (peer.m_playerName.IsNullOrWhiteSpace()) return;
            if (peer.m_playerName == "Stranger") return;
            m_name = peer.m_playerName;
            m_distance = Vector3.Distance(peer.m_refPos, Player.m_localPlayer.transform.position);
            m_cost = Teleportation.CalculateFuelCost(m_distance);
            m_isFavorite = m_instance.IsFavorite(m_name);
            m_creator = peer.m_uid;
            m_guild = GetGuild(m_name);
            if (!IsInGroup() && !IsInGuild()) return;
            m_destinations.Add(this);
        }

        private static string GetGuild(string playerName)
        {
            if (!ZNet.instance) return "";
            ZNet.PlayerInfo info = ZNet.instance.GetPlayerList().FirstOrDefault(x => x.m_name == playerName);
            Guilds.PlayerReference reference = Guilds.PlayerReference.fromPlayerInfo(info);
            return Guilds.API.GetPlayerGuild(reference)?.Name ?? "";
        }
        
        private bool IsInGuild()
        {
            if (!Guilds.API.IsLoaded()) return true;
            if (m_guild.IsNullOrWhiteSpace()) return true;
            if (Guilds.API.GetOwnGuild() is not { } guild) return false;
            return guild.Name == m_guild;
        }

        private bool IsInGroup()
        {
            if (!Groups.API.IsLoaded()) return true;
            return Groups.API.FindGroupMemberByPlayerId(m_creator) != null;
        }
    }
    
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    static class AttachPortalStationGUI
    {
        private static void Postfix(InventoryGui __instance)
        {
            Instantiate(PortalStationsPlugin.UIAssets.LoadAsset<GameObject>("StationUI"), __instance.transform).AddComponent<StationUI>().Init();
        }
    }
    
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.IsVisible))]
    static class IsStationVisible2
    {
        private static void Postfix(ref bool __result) => __result |= IsVisible();

    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    static class IsStationVisible
    {
        private static bool Prefix() => !IsVisible();
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FixedUpdate))]
    static class StationPlayerControllerOverride
    {
        private static bool Prefix() => !IsVisible();
    }
    
    [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
    private static class LogoutPatch
    {
        private static void Postfix() => SaveFavorites();
    }

    private static void SaveFavorites()
    {
        if (!Player.m_localPlayer) return;
        try
        {
            ISerializer serializer = new SerializerBuilder().Build();
            string data = serializer.Serialize(m_favorites);

            Player.m_localPlayer.m_customData[PortalStation._FavoriteKey] = data;
        }
        catch
        {
            PortalStationsPlugin.PortalStationsLogger.LogDebug("Failed to save favorite portals");
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    private static class SetLocalPlayerPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!__instance || __instance != Player.m_localPlayer) return;
            if (!__instance.m_customData.TryGetValue(PortalStation._FavoriteKey, out string data)) return;
            IDeserializer deserializer = new DeserializerBuilder().Build();
            m_favorites = deserializer.Deserialize<List<string>>(data);
        }
    }

    [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.GetPiecesInSelectedCategory))]
    private static class PieceTableGetPiecesPatch
    {
        private static void Postfix(ref List<Piece> __result)
        {
            if (PortalStationsPlugin._OnlyAdminBuilds.Value is PortalStationsPlugin.Toggle.Off) return;
            if (ZNet.instance.LocalPlayerIsAdminOrHost()) return;
            __result.RemoveAll(x => x.GetComponent<PortalStation>());
        }
    }
}