using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BepInEx.Configuration;
using Groups;
using HarmonyLib;
using JetBrains.Annotations;
using MWL_Ports.Managers;
using PortalStations.Managers;
using PortalStations.Stations;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PortalStations.UI;

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
public static class Load_PortalStation_UI
{
    [UsedImplicitly]
    private static void Postfix(InventoryGui __instance)
    {
        GameObject? panel = AssetBundleManager.LoadAsset<GameObject>("stationui", "PortalStationUI");
        if (panel == null)
        {
            Debug.LogWarning("Couldn't find PortalStationUI");
            return;
        }
        var craftingPanel = __instance.m_crafting.gameObject;
        GameObject? go = Object.Instantiate(panel, __instance.transform.parent.Find("HUD"));
        go.name = "PortalStationUI";
        
        Text[]? panelTexts = go.GetComponentsInChildren<Text>(true);
        Text[]? listItemTexts = PortalStationUI.ListItem.GetComponentsInChildren<Text>(true);
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Selected", "selected_frame/selected (1)");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/bkg", "Bkg");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/TitlePanel/BraidLineHorisontalMedium (1)", "TitlePanel/BraidLineHorisontalMedium (1)");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/TitlePanel/BraidLineHorisontalMedium (2)", "TitlePanel/BraidLineHorisontalMedium (2)");
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Tabs/Stations", "TabsButtons/Craft");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Tabs/Stations/Selected", "TabsButtons/Craft/Selected");
        go.CopyButtonState(craftingPanel, "Panel/Tabs/Stations", "TabsButtons/Craft");
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Tabs/Favorites", "TabsButtons/Craft");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Tabs/Favorites/Selected", "TabsButtons/Craft/Selected");
        go.CopyButtonState(craftingPanel, "Panel/Tabs/Favorites", "TabsButtons/Craft");
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Tabs/Players", "TabsButtons/Craft");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Tabs/Players/Selected", "TabsButtons/Craft/Selected");
        go.CopyButtonState(craftingPanel, "Panel/Tabs/Players", "TabsButtons/Craft");
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Tabs/Settings", "TabsButtons/Craft");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Tabs/Settings/Selected", "TabsButtons/Craft/Selected");
        go.CopyButtonState(craftingPanel, "Panel/Tabs/Settings", "TabsButtons/Craft");
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Separator", "TabsButtons/TabBorder");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/LeftPanel/Viewport", "RecipeList/Recipes");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description", "Decription");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/Icon", "Decription/Icon");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/TeleportButton", "Decription/craft_button_panel/CraftButton");
        go.CopyButtonState(craftingPanel, "Panel/Description/TeleportButton", "Decription/craft_button_panel/CraftButton");
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/Requirements/1", "Decription/requirements/res_bkg");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/Requirements/2", "Decription/requirements/res_bkg");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/Requirements/3", "Decription/requirements/res_bkg");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/Requirements/4", "Decription/requirements/res_bkg");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/Requirements/1/Icon", "Decription/requirements/res_bkg/res_icon");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/Requirements/2/Icon", "Decription/requirements/res_bkg/res_icon");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/Requirements/3/Icon", "Decription/requirements/res_bkg/res_icon");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/Requirements/4/Icon", "Decription/requirements/res_bkg/res_icon");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/Requirements/level", "Decription/requirements/level");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/Requirements/level/MinLevel", "Decription/requirements/level/MinLevel");
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Public", "RepairButton");
        go.CopyButtonState(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Public", "RepairButton");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Public/Glow", "RepairButton/Glow");
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Private", "RepairButton");
        go.CopyButtonState(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Private", "RepairButton");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Private/Glow", "RepairButton/Glow");
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Guild", "RepairButton");
        go.CopyButtonState(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Guild", "RepairButton");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Guild/Glow", "RepairButton/Glow");
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Group", "RepairButton");
        go.CopyButtonState(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Group", "RepairButton");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/State/Group/Glow", "RepairButton/Glow");
        
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/NameField/Background", "Decription/craft_button_panel/CraftButton");
        go.CopySpriteAndMaterial(craftingPanel, "Panel/Description/ListRoot/Viewport/Root/GuildField/Background", "Decription/craft_button_panel/CraftButton");
        
        PortalStationUI.ListItem.CopySpriteAndMaterial(craftingPanel, "Icon", "RecipeList/Recipes/RecipeElement/icon");
        GameObject? sfx = craftingPanel.GetComponentInChildren<ButtonSfx>().m_sfxPrefab;
        foreach (ButtonSfx? component in go.GetComponentsInChildren<ButtonSfx>(true)) component.m_sfxPrefab = sfx;
        FontManager.SetFont(panelTexts);
        FontManager.SetFont(listItemTexts);
        go.transform.position = PortalStationsPlugin.PanelPos.Value;
        
        go.AddComponent<PortalStationUI>();
    }
}

[HarmonyPatch(typeof(PlayerController), nameof(PlayerController.TakeInput))]
public static class PlayerController_TakeInput_Patch
{
    [UsedImplicitly]
    private static void Postfix(ref bool __result)
    {
        __result &= !PortalStationUI.IsVisible();
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.TakeInput))]
public static class PlayerTakeInput_Patch
{
    [UsedImplicitly]
    private static void Postfix(ref bool __result)
    {
        __result &= !PortalStationUI.IsVisible();
    } 
}

[HarmonyPatch(typeof(PlayerController), nameof(PlayerController.InInventoryEtc))]
public static class PlayerController_InInventoryEtc_Patch
{
    [UsedImplicitly]
    private static void Postfix(ref bool __result)
    {
        __result |= PortalStationUI.IsVisible();
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.IsVisible))]
public static class InventoryGui_IsVisible_Patch
{
    [UsedImplicitly]
    private static void Postfix(ref bool __result)
    {
        __result |= PortalStationUI.IsVisible();
    }
}

public class PortalStationUI : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public static readonly GameObject ListItem = AssetBundleManager.LoadAsset<GameObject>("stationui", "PortalStationItem")!;
    public enum BackgroundOption { Opaque, Transparent }
    
    public static PortalStationUI? instance;
    private static Minimap.PinData? m_tempPin; 
    private static Sprite? m_defaultIcon;
    private static Sprite? m_starIcon;

    private RectTransform m_rect = null!;
    private Image Selected = null!;
    private Image Background = null!;
    private GameObject Darken = null!;
    private Text Topic = null!;
    private Tab StationTab = null!;
    private Tab SettingTab = null!;
    private Tab PlayerTab = null!;
    private Tab FavoriteTab = null!;
    private VerticalLayoutGroup LeftPanelLayout = null!;
    private RectTransform LeftPanelRoot = null!;
    private Image Icon = null!;
    private Button MainButton = null!;
    private Text MainButtonText = null!;
    private RightPanel Description = null!;
    private Requirement Requirements = null!;
    private Text MapButtonText = null!;
    
    private readonly List<TempListItem> StationItems = new();

    public void OnFontChange(Font? font)
    {
        foreach (var item in StationItems)
        {
            item.SetFont(font);
        }
    }
    private readonly List<Tab> Tabs = new();

    public DeviceType m_deviceType = DeviceType.None;
    public PortalStation? m_currentStation;
    private StationInfo? m_currentStationInfo;
    private TempListItem? m_currentStationItem;
    public ItemDrop.ItemData? m_currentItem;
    private StationInfo? m_destination;
    private float m_listItemHeight;
    private float m_leftListMinHeight;
    private float m_pinTimer;
    private Vector3 mouseDifference = Vector3.zero;
    private Action<float>? OnUpdate;
    
    public enum DeviceType { None, Station, Item }

    public void Awake()
    {
        instance = this;
        m_listItemHeight = ListItem.GetComponent<RectTransform>().sizeDelta.y;
        m_rect = GetComponent<RectTransform>();
        Selected = transform.Find("Panel/Selected").GetComponent<Image>();
        Background = transform.Find("Panel/bkg").GetComponent<Image>();
        Darken = transform.Find("Panel/darken").gameObject;
        Topic = transform.Find("Panel/TitlePanel/topic").GetComponent<Text>();
        Icon  = transform.Find("Panel/Description/Icon").GetComponent<Image>();
        MainButton = transform.Find("Panel/Description/TeleportButton").GetComponent<Button>();
        MainButtonText = transform.Find("Panel/Description/TeleportButton/Text").GetComponent<Text>();
        StationTab = new Tab(transform.Find("Panel/Tabs/Stations"));
        SettingTab = new Tab(transform.Find("Panel/Tabs/Settings"));
        PlayerTab = new Tab(transform.Find("Panel/Tabs/Players"));
        FavoriteTab = new Tab(transform.Find("Panel/Tabs/Favorites"));
        LeftPanelRoot =  transform.Find("Panel/LeftPanel/Viewport/ListRoot").GetComponent<RectTransform>();
        m_leftListMinHeight = LeftPanelRoot.sizeDelta.y;
        LeftPanelLayout = LeftPanelRoot.GetComponent<VerticalLayoutGroup>();
        Description = new RightPanel(transform.Find("Panel/Description"));
        Requirements = new Requirement(transform.Find("Panel/Description/Requirements"));
        Requirements.Add(transform.Find("Panel/Description/Requirements/1"));
        Requirements.Add(transform.Find("Panel/Description/Requirements/2"));
        Requirements.Add(transform.Find("Panel/Description/Requirements/3"));
        Requirements.Add(transform.Find("Panel/Description/Requirements/4"));
        MapButtonText = transform.Find("Panel/Description/MapButton/Text").GetComponent<Text>();
        m_defaultIcon = Icon.sprite;
        m_starIcon = Requirements.favorite.Icon.sprite;
    }

    public void Start()
    {
        MainButton.onClick.AddListener(OnTeleport);
        Description.SetMapButton(OnMapButton);
        StationTab.SetButton(OnStationTab);
        SettingTab.SetButton(OnSettingTab);
        PlayerTab.SetButton(OnPlayerTab);
        FavoriteTab.SetButton(OnFavoriteTab);
        SetMainButtonText("$text_teleport");
        StationTab.SetLabel("$text_stations");
        PlayerTab.SetLabel("$text_players");
        FavoriteTab.SetLabel("$text_favorites");
        SettingTab.SetLabel("$text_settings");
        Description.SetStateLabel("$text_state");
        Description.NameInput.SetLabel("$text_name");
        Description.GuildInput.SetLabel("$text_guild");
        Description.Group.SetText("$text_group");
        Description.Guild.SetText("$text_guild");
        Description.Public.SetText("$text_public");
        Description.Private.SetText("$text_private");
        MapButtonText.text = Localization.instance.Localize("$text_open_map");
        SetBackground(PortalStationsPlugin.BkgOption.Value);
        Hide();
    }

    public void Update()
    {
        float dt = Time.deltaTime;
        OnUpdate?.Invoke(dt);
        UpdatePin(dt);
        if (!ZInput.GetKeyDown(KeyCode.Escape) && !ZInput.GetKeyDown(KeyCode.Tab)) return;
        Hide();
    }
    
    public void OnDestroy()
    {
        instance = null;
    }

    public void Show(PortalStation station, Player player)
    {
        m_deviceType = DeviceType.Station;
        m_currentStation = station;
        m_currentStationInfo = new StationInfo(station.m_nview.GetZDO());
        gameObject.SetActive(true);
        StationTab.SetSelected(true);
        SetTopic(station.GetText());
        Description.ResetDescription();
        LoadStations();
        Description.ShowMapButton(null);
        SetTopic(m_currentStationInfo.Name);
        OnUpdate = null;
        Requirements.SetActive(false);
        m_destination = null;
        SettingTab.Enable(true);
        PlayerTab.Enable(PortalStationsPlugin._PortalToPlayers.Value is PortalStationsPlugin.Toggle.On);
    }

    public void Show(Player user, ItemDrop.ItemData item)
    {
        m_deviceType = DeviceType.Item;
        m_currentItem = item;
        m_currentStationInfo = new StationInfo(user);
        gameObject.SetActive(true);
        StationTab.SetSelected(true);
        SettingTab.Enable(false);
        Description.ResetDescription();
        SetTopic(user.GetPlayerName());
        Description.ShowMapButton(null);
        LoadStations();
        OnUpdate = null;
        MainButton.gameObject.SetActive(false);
        Requirements.SetActive(false);
        m_destination = null;
        PlayerTab.Enable(PortalStationsPlugin._PortalToPlayers.Value is PortalStationsPlugin.Toggle.On);
        SettingTab.Enable(m_currentStationInfo.CreatorName == Player.m_localPlayer.GetPlayerName());
    }

    public void OnStationTab()
    {
        StationTab.SetSelected(true);
        MainButton.gameObject.SetActive(false);
        DestroyTempItems();
        LoadStations();
        Description.ResetDescription();
        Description.SetView(RightPanel.PanelView.Body);
        OnUpdate = null;
        Requirements.SetActive(false);
        m_destination = null;
    }

    public void OnFavoriteTab()
    {
        FavoriteTab.SetSelected(true);
        MainButton.gameObject.SetActive(false);
        DestroyTempItems();
        LoadStations(true);
        Description.ResetDescription();
        Description.SetView(RightPanel.PanelView.Body);
        OnUpdate = null;
        Requirements.SetActive(false);
        m_destination = null;
    }

    public void OnPlayerTab()
    {
        PlayerTab.SetSelected(true);
        MainButton.gameObject.SetActive(true);
        DestroyTempItems();
        LoadPlayers();
        Description.ResetDescription();
        Description.SetView(RightPanel.PanelView.Body);
        OnUpdate = null;
        Requirements.SetActive(false);
        m_destination = null;
    }

    public void OnSettingTab()
    {
        if (m_currentStation == null || m_currentStationInfo == null) return;
        DestroyTempItems();
        ResizeLeftList();
        MainButton.gameObject.SetActive(false);
        SettingTab.SetSelected(true);
        Description.ResetDescription();
        Description.SetName(m_currentStationInfo.Name);
        Description.SetView(RightPanel.PanelView.Settings);
        Description.NameInput.SetPlaceholder(m_currentStationInfo.Name);
        Description.GuildInput.SetPlaceholder(m_currentStationInfo.Guild);
        Description.ResetState();
        Description.NameInput.ResetField();
        Description.GuildInput.ResetField();
        switch (m_currentStation.GetFilter())
        {
            case PortalStation.FilterOptions.Public:
                Description.Public.SetGlow(true);
                break;
            case  PortalStation.FilterOptions.Private:
                Description.Private.SetGlow(true);
                break;
            case PortalStation.FilterOptions.GroupOnly:
                Description.Group.SetGlow(true);
                break;
            case PortalStation.FilterOptions.GuildOnly:
                Description.Guild.SetGlow(true);
                break;
            case PortalStation.FilterOptions.GuildGroupOnly:
                Description.Guild.SetGlow(true);
                Description.Group.SetGlow(true);
                break;
        }
        OnUpdate = null;
        Requirements.SetActive(false);
        m_destination = null;
    }

    public void DestroyTempItems()
    {
        foreach(var item in StationItems) item.Destroy();
        StationItems.Clear();
    }
    
    public void ResizeLeftList()
    {
        int count = StationItems.Count;
        float padding = LeftPanelLayout.spacing;
        float totalItemHeight = count * m_listItemHeight;
        float totalSpacingHeight = Mathf.Max(0, count - 1) * padding;
        float newHeight = totalItemHeight + totalSpacingHeight;
        LeftPanelRoot.sizeDelta = new Vector2(LeftPanelRoot.sizeDelta.x, Mathf.Max(newHeight, m_leftListMinHeight));
    }

    public void LoadStations(bool favoriteOnly = false)
    {
        DestroyTempItems();
        foreach (ZDO? zdo in StationManager.GetStations())
        {
            if (zdo == m_currentStation?.m_nview.GetZDO()) continue;
            var info = new StationInfo(zdo);
            if (!info.IsValid) continue;
            if (favoriteOnly && !info.IsFavorite) continue;
            var item = new TempListItem();
            item.SetLabel(info.Name);
            item.SetIcon(info.IsFavorite ? m_starIcon : Minimap.instance.GetSprite(Minimap.PinType.Icon4));
            item.SetButton(() =>
            {
                item.SetSelected(true);
                Description.SetName(info.Name);
                Description.SetBodyText(info.GetTooltip());
                Description.ShowMapButton(info);
                Requirements.LoadTeleportCost(info);
                m_destination = info;
                Requirements.favorite.SetFavorite(info.IsFavorite);
                MainButton.gameObject.SetActive(true);
                MainButton.interactable = info.CanTeleport(false);
            });
        }
        ResizeLeftList();
    }

    public void LoadPlayers()
    {
        if (m_currentStation == null) return;
        DestroyTempItems();
        foreach (ZNetPeer? peer in ZNet.instance.GetPeers())
        {
            var info = new StationInfo(peer);
            if (!info.IsValid) continue;
            var item = new TempListItem();
            item.SetLabel(info.Name);
            item.SetIcon(Minimap.instance.GetSprite(Minimap.PinType.Icon1));
            item.SetButton(() =>
            {
                item.SetSelected(true);
                Description.SetName(info.Name);
                Description.SetBodyText(info.GetTooltip());
                Description.ShowMapButton(info);
                Requirements.LoadTeleportCost(info);
                Requirements.favorite.SetFavorite(info.IsFavorite);
                MainButton.gameObject.SetActive(true);
                m_destination = info;
            });
        }
        ResizeLeftList();
    }

    public void OnTeleport()
    {
        if (m_destination == null) return;
        if (PortalStationsPlugin._UsePortalKeys.Value is PortalStationsPlugin.Toggle.On)
        {
            Dictionary<string, string> keys = new PortalStationsPlugin.SerializedKeys(PortalStationsPlugin._PortalKeys.Value).Keys;
            Dictionary<string, string> sharedNames = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> key in keys)
            {
                if (ObjectDB.instance.GetItemPrefab(key.Key) is not { } itemPrefab) continue;
                var item = itemPrefab.GetComponent<ItemDrop>();
                sharedNames.Add(item.m_itemData.m_shared.m_name, key.Value);
            }
            foreach (ItemDrop.ItemData? item in Player.m_localPlayer.GetInventory().GetAllItems())
            {
                if (item.m_shared.m_teleportable) continue;
                if (!sharedNames.TryGetValue(item.m_shared.m_name, out var key))
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                    return;
                }

                if (!ZoneSystem.instance.GetGlobalKey(key) && !Player.m_localPlayer.GetUniqueKeys().Contains(key))
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                    return;
                }
            }
        }
        else if (PortalStationsPlugin._TeleportAnything.Value is PortalStationsPlugin.Toggle.Off && !Player.m_localPlayer.IsTeleportable())
        {
            Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return;
        }

        switch (m_deviceType)
        {
            case DeviceType.None:
                return;
            case DeviceType.Station:
                if (PortalStationsPlugin._PortalUseFuel.Value is PortalStationsPlugin.Toggle.On &&
                    !Player.m_localPlayer.NoCostCheat())
                {
                    var cost = m_currentStationInfo?.GetCost(Player.m_localPlayer) ?? 0;
                    var fuelItem = Requirement.GetFuelItem();
                    Player.m_localPlayer.GetInventory().RemoveItem(fuelItem.m_itemData.m_shared.m_name, cost);
                }
                break;
            case  DeviceType.Item:
                if (PortalStationsPlugin._DeviceUseFuel.Value is PortalStationsPlugin.Toggle.On &&
                    !Player.m_localPlayer.NoCostCheat()  && m_currentItem != null)
                {
                    var cost = m_currentStationInfo?.GetCost(Player.m_localPlayer) ?? 0;
                    var fuelItem = Requirement.GetFuelItem();
                    Player.m_localPlayer.GetInventory().RemoveItem(fuelItem.m_itemData.m_shared.m_name, cost);
                    m_currentItem.m_durability -= PortalStationsPlugin._PersonalPortalDurabilityDrain.Value;
                }
                break;
        }
        
        Player.m_localPlayer.TeleportTo(m_destination.Position + m_destination.Offset, Quaternion.identity, true);
        Hide();
    }
    public void Hide()
    {
        gameObject.SetActive(false);
        OnUpdate = null;
        m_currentStation = null;
        m_currentStationInfo = null;
        m_currentItem = null;
    }
    
    public static void OnBackgroundOptionChange(object sender, EventArgs args)
    {
        if (instance == null) return;
        if (sender is not ConfigEntry<BackgroundOption> option) return;
        instance.SetBackground(option.Value);
    }

    public void SetBackground(BackgroundOption option)
    {
        switch (option)
        {
            case BackgroundOption.Opaque:
                Background.gameObject.SetActive(true);
                Darken.SetActive(false);
                break;
            case BackgroundOption.Transparent:
                Background.gameObject.SetActive(false);
                Darken.SetActive(true);
                break;
        }
    }
    
    public static void OnPanelPositionConfigChange(object sender, EventArgs args)
    {
        if (instance == null) return;
        if (sender is not ConfigEntry<Vector3> config) return;
        instance.SetPanelPosition(config.Value);
    }

    public void SetPanelPosition(Vector3 pos) => transform.position = pos;
    public void UpdatePin(float dt)
    {
        if (m_tempPin == null) return;
        m_pinTimer -= dt;
        if (m_pinTimer > 0.0f) return;
        Minimap.instance.RemovePin(m_tempPin);
    }
    
    public void OnMapButton()
    {
        if (Description.SelectedStation == null || !Minimap.instance) return;
        Vector3 pos = Description.SelectedStation.Position;
        Hide();
        if (m_tempPin != null) Minimap.instance.RemovePin(m_tempPin);
        Minimap.PinData? pin = Minimap.instance.AddPin(pos, Minimap.PinType.Icon2, Description.SelectedStation.Name, false, false);
        Minimap.instance.ShowPointOnMap(pos);
        m_tempPin = pin;
        m_pinTimer = 100f;
    }
    
    public static bool IsVisible() => instance != null && instance.gameObject.activeInHierarchy;
    
    public void SetTopic(string topic) => Topic.text = Localization.instance.Localize(topic);
    
    public void SetMapButton(string text) => MapButtonText.text = Localization.instance.Localize(text);
    
    public void SetMainButtonText(string text) => MainButtonText.text = Localization.instance.Localize(text);

    public void OnDrag(PointerEventData eventData)
    {
        if (!Input.GetKey(KeyCode.LeftAlt)) return;
        m_rect.position = Input.mousePosition + mouseDifference;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Vector2 pos = eventData.position;
        mouseDifference = m_rect.position - new Vector3(pos.x, pos.y, 0);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        PortalStationsPlugin.PanelPos.Value = m_rect.position;
    }

    private class TempListItem
    {
        private readonly GameObject? Prefab;
        private readonly Button? Button;
        private readonly Image? Icon;
        private readonly Text? Label;
        private readonly GameObject? Selected;
        private bool IsSelected => Selected != null && Selected.activeInHierarchy;

        public TempListItem()
        {
            if (instance == null) return;
            Prefab = Instantiate(ListItem, instance.LeftPanelRoot);
            Button = Prefab.GetComponent<Button>();
            Icon = Prefab.transform.Find("Icon").GetComponent<Image>();
            Label = Prefab.transform.Find("Label").GetComponent<Text>();
            Selected = Prefab.transform.Find("selected").gameObject;
            instance.StationItems.Add(this);
        }

        public void SetLabel(string label)
        {
            if (Label == null) return;
            Label.text = Localization.instance.Localize(label);
        }

        public void SetFont(Font? font)
        {
            if (Label == null) return;
            Label.font = font;
        }

        public void SetIcon(Sprite? sprite)
        {
            if (Icon == null) return;
            Icon.sprite = sprite;
        }
        
        public void SetIcon(Minimap.PinType pinType) => SetIcon(Minimap.instance.GetSprite(pinType));

        public void SetIcon(string prefabName)
        {
            if (ZNetScene.instance.GetPrefab(prefabName) is not {} prefab) return;
            if (prefab.TryGetComponent(out ItemDrop itemDrop))
            {
                SetIcon(itemDrop.m_itemData);
            }
            else if (prefab.TryGetComponent(out Piece piece))
            {
                SetIcon(piece);
            }
        }
        
        private void SetIcon(Piece piece) => SetIcon(piece.m_icon);

        private void SetIcon(ItemDrop.ItemData item)
        {
            if (!item.IsValid()) return;
            SetIcon(item.GetIcon());
        }

        public void ShowIcon(bool enable)
        {
            if (Icon == null) return;
            Icon.gameObject.SetActive(enable);
        }
        
        public void SetSelected(bool selected)
        {
            if (IsSelected) return;
            if (Selected == null || instance == null) return;
            foreach (TempListItem? temp in instance.StationItems)
            {
                if (temp.Selected == null) continue;
                temp.Selected.SetActive(false);
            }
            Selected.SetActive(selected);
            if (selected) instance.m_currentStationItem = this;
        }

        public void SetButton(UnityAction action)
        {
            if (Button == null) return;
            Button.onClick.AddListener(action);
        }

        public void Destroy()
        {
            Object.Destroy(Prefab);
        }
    }

    private class Tab
    {
        private readonly GameObject Prefab;
        private readonly Button Button;
        private readonly Text Label;
        private readonly GameObject Selected;
        private readonly Text SelectedLabel;
        public bool IsSelected => Selected.activeInHierarchy;

        public Tab(Transform transform)
        {
            Prefab = transform.gameObject;
            Button = transform.GetComponent<Button>();
            Label = transform.Find("Text").GetComponent<Text>();
            Selected = transform.Find("Selected").gameObject;
            SelectedLabel = transform.Find("Selected/SelectedText").GetComponent<Text>();
            instance?.Tabs.Add(this);
        }

        public void Enable(bool enable) => Prefab.SetActive(enable);
        public void SetButton(UnityAction action) => Button.onClick.AddListener(action);
        public static void SetAllSelected(bool enable)
        {
            if (instance == null) return;
            foreach (Tab? tab in instance.Tabs)
            {
                if (tab.Selected == null) continue;
                tab.SetSelected(enable);
            }
        }
        public void SetLabel(string label)
        {
            Label.text = Localization.instance.Localize(label);
            SelectedLabel.text = Localization.instance.Localize(label);
        }

        public void SetSelected(bool selected)
        {
            if (instance == null) return;
            foreach(Tab tab in instance.Tabs) tab.Selected.SetActive(false);
            Selected.SetActive(selected);
        }
    }
    private class RightPanel
    {
        private readonly GameObject Body;
        private readonly GameObject Settings;
        private readonly Text Name;
        private readonly Text BodyText;
        private readonly RectTransform BodyRect;
        private readonly Button MapButton;
        private readonly Image MapIcon;
        public readonly Input NameInput;
        public readonly Input GuildInput;
        private readonly Text State;
        public readonly StateButton Public;
        public readonly StateButton Private;
        public readonly StateButton Guild;
        public readonly StateButton Group;
        public StationInfo? SelectedStation;
        
        private readonly float BodyMinHeight;
        public enum PanelView {Body, Settings}

        public RightPanel(Transform transform)
        {
            Name = transform.Find("Name").GetComponent<Text>();
            Body = transform.Find("Body").gameObject;
            Settings = transform.Find("ListRoot").gameObject;
            BodyText = transform.Find("Body/Viewport/Text").GetComponent<Text>();
            BodyRect = transform.Find("Body").GetComponent<RectTransform>();
            MapButton = transform.Find("MapButton").GetComponent<Button>();
            MapIcon = transform.Find("MapButton").GetComponent<Image>();
            NameInput = new Input(transform.Find("ListRoot/Viewport/Root/NameField"));
            GuildInput = new Input(transform.Find("ListRoot/Viewport/Root/GuildField"));
            State = transform.Find("ListRoot/Viewport/Root/StateLabel").GetComponent<Text>();
            Public = new StateButton(transform.Find("ListRoot/Viewport/Root/State/Public"));
            Private = new StateButton(transform.Find("ListRoot/Viewport/Root/State/Private"));
            Guild = new StateButton(transform.Find("ListRoot/Viewport/Root/State/Guild"));
            Group = new StateButton(transform.Find("ListRoot/Viewport/Root/State/Group"));
            BodyMinHeight = BodyRect.sizeDelta.y;
            
            NameInput.SetField(input =>
            {
                if (instance == null || instance.m_currentStation == null || instance.m_currentStationInfo == null) return;
                instance.m_currentStation.SetName(input);
                instance.SetTopic(input);
                SetName(input);
                instance.m_currentStationInfo.Reload();
            });
            
            GuildInput.SetField(input =>
            {
                if (instance == null || instance.m_currentStation == null || instance.m_currentStationInfo == null) return;
                instance.m_currentStation.SetGuild(input);
                instance.m_currentStationInfo.Reload();
            });
            
            Public.SetButton(() =>
            {
                if (instance == null || instance.m_currentStation == null) return;
                instance.m_currentStation.SetFilter(PortalStation.FilterOptions.Public);
                ResetState();
                Public.SetGlow(true);
            });
            Private.SetButton(() =>
            {
                if (instance == null || instance.m_currentStation == null || instance.m_currentStationInfo == null) return;
                instance.m_currentStation.SetFilter(PortalStation.FilterOptions.Private);
                ResetState();
                Private.SetGlow(true);
                instance.m_currentStationInfo.Reload();
            });
            Guild.SetButton(() =>
            {
                if (instance == null || instance.m_currentStation == null || instance.m_currentStationInfo == null) return;
                var currentOption = instance.m_currentStation.GetFilter();
                ResetState();
                Guild.SetGlow(true);
                switch (currentOption)
                {
                    case PortalStation.FilterOptions.GroupOnly:
                        instance.m_currentStation.SetFilter(PortalStation.FilterOptions.GuildGroupOnly);
                        Group.SetGlow(true);
                        break;
                    default :
                        instance.m_currentStation.SetFilter(PortalStation.FilterOptions.GuildOnly);
                        break;
                }
                instance.m_currentStationInfo.Reload();
            });
            Group.SetButton(() =>
            {
                if (instance == null || instance.m_currentStation == null || instance.m_currentStationInfo == null) return;
                var currentOption = instance.m_currentStation.GetFilter();
                ResetState();
                Group.SetGlow(true);
                switch (currentOption)
                {
                    case PortalStation.FilterOptions.GuildOnly:
                        instance.m_currentStation.SetFilter(PortalStation.FilterOptions.GuildGroupOnly);
                        Guild.SetGlow(true);
                        break;
                    default :
                        instance.m_currentStation.SetFilter(PortalStation.FilterOptions.GroupOnly);
                        break;
                }
                instance.m_currentStationInfo.Reload();
            });
        }

        public void ResetState()
        {
            Public.SetGlow(false);
            Private.SetGlow(false);
            Guild.SetGlow(false);
            Group.SetGlow(false);
        }

        public void SetMapButton(UnityAction action) => MapButton.onClick.AddListener(action);
        public void SetName(string name)
        {
            Name.text = Localization.instance.Localize(name);
        }
        public void SetBodyText(string body)
        {
            BodyText.text = Localization.instance.Localize(body);
            ResizePanel();
        }
        public void ResetDescription()
        {
            SetName("");
            SetBodyText("");
            ShowMapButton(null);
            SetView(PanelView.Body);
        }
        public void SetStateLabel(string label) => State.text = Localization.instance.Localize(label);

        public void SetView(PanelView view)
        {
            switch (view)
            {
                case PanelView.Body:
                    Body.SetActive(true);
                    Settings.SetActive(false);
                    break;
                case PanelView.Settings:
                    Settings.SetActive(true);
                    Body.SetActive(false);
                    break;
            }
        }
        
        public void ShowMapButton(StationInfo? info)
        {
            MapButton.gameObject.SetActive(info != null);
            SelectedStation = info;
            if (Minimap.instance && MapIcon.sprite == null)
            {
                MapIcon.sprite = Minimap.instance.GetSprite(Minimap.PinType.Icon4);
            }
        }

        private void ResizePanel()
        {
            float height = GetTextPreferredHeight(BodyText, BodyRect);
            float finalHeight = Mathf.Max(height, BodyMinHeight);
            BodyRect.sizeDelta = new Vector2(BodyRect.sizeDelta.x, finalHeight);
        }
    
        private static float GetTextPreferredHeight(Text text, RectTransform rect)
        {
            if (string.IsNullOrEmpty(text.text)) return 0f;
        
            TextGenerator textGen = text.cachedTextGenerator;
        
            TextGenerationSettings settings = text.GetGenerationSettings(rect.rect.size);
            float preferredHeight = textGen.GetPreferredHeight(text.text, settings);
        
            return preferredHeight;
        }

        public class Input
        {
            private readonly InputField Field;
            private readonly Text Label;
            private readonly Text Placeholder;

            public Input(Transform transform)
            {
                Field = transform.GetComponent<InputField>();
                Label = transform.Find("Label").GetComponent<Text>();
                Placeholder = transform.Find("Background/Field/Placeholder").GetComponent<Text>();
            }
            
            public void SetField(UnityAction<string> action) => Field.onValueChanged.AddListener(action);
            public void ResetField() => Field.SetTextWithoutNotify(null);
            public void SetPlaceholder(string placeholder) => Placeholder.text = placeholder;
            public void SetLabel(string label) => Label.text = Localization.instance.Localize(label);
        }

        public class StateButton
        {
            private readonly Button button;
            private readonly GameObject Glow;
            private readonly Text Text;

            public StateButton(Transform transform)
            {
                button = transform.GetComponent<Button>();
                Glow = transform.Find("Glow").gameObject;
                Text = transform.Find("Text").GetComponent<Text>();
            }
            
            public void SetButton(UnityAction action) => button.onClick.AddListener(action);
            public void SetText(string text) => Text.text = Localization.instance.Localize(text);
            public void SetGlow(bool enable) =>  Glow.SetActive(enable);
        }
    }
    public class Requirement
    {
        private readonly GameObject Prefab;
        private readonly List<RequirementItem> items = new List<RequirementItem>();
        public readonly Favorite favorite;
        public Requirement(Transform transform)
        {
            Prefab = transform.gameObject;
            favorite = new Favorite(transform.Find("level"));
        }

        public void SetLevel(string text)
        {
            if (favorite.Label == null) return;
            favorite.Label.text = Localization.instance.Localize(text);
        }
        
        public void Add(Transform parent, string icon = "Icon", string name = "Name", string amount = "Amount")
        {
            Image Icon = parent.Find(icon).GetComponent<Image>();
            Text Label = parent.Find(name).GetComponent<Text>();
            Text Amount = parent.Find(amount).GetComponent<Text>();
            items.Add(new RequirementItem(Icon, Label, Amount));
        }
        
        public static ItemDrop GetFuelItem() => ObjectDB.instance.GetItemPrefab(PortalStationsPlugin._DeviceFuel.Value)?.GetComponent<ItemDrop>() ?? ObjectDB.instance.GetItemPrefab("Coins").GetComponent<ItemDrop>();
        
        public void LoadTeleportCost(StationInfo destination)
        {
            if (instance == null) return;
            SetActive(true);
            var fuelItem = GetFuelItem();
            if (!fuelItem.m_itemData.IsValid()) return;
            var total = destination.GetCost(Player.m_localPlayer);
            var maxStack = fuelItem.m_itemData.m_shared.m_maxStackSize;

            foreach (var item in items)
            {
                if (total <= 0)
                {
                    item.Hide();
                    continue;
                }
                int amount = Mathf.Min(total, maxStack);
                total -= amount;
                item.Set(fuelItem.m_itemData.GetIcon(), fuelItem.m_itemData.m_shared.m_name, amount);
            }

            instance.OnUpdate = dt => Update(dt, Player.m_localPlayer);
        }

        private void Update(float dt, Player? player)
        {
            if (player is null) return;
            foreach (RequirementItem? item in items) item.Update(dt, player);
        }
        
        public void SetActive(bool active) => Prefab.SetActive(active);

        private class RequirementItem
        {
            private readonly Image Icon;
            private readonly Text Name;
            private readonly Text Amount;

            private string? SharedName;
            private int Count;

            public RequirementItem(Image icon, Text name, Text amount)
            {
                Icon = icon;
                Name = name;
                Amount = amount;
            }

            public void Set(Sprite icon, string sharedName, int amount)
            {
                SharedName = sharedName;
                Count = amount;
                Icon.sprite = icon;
                Icon.color = Color.white;
                Name.text = Localization.instance.Localize(sharedName);
                Amount.text = amount.ToString();
            }

            public void Update(float dt, Player player)
            {
                if (SharedName == null) return;
                Inventory inventory = player.GetInventory();
                int count = inventory.CountItems(SharedName);
                bool hasRequirement = Count <= count;

                if (!hasRequirement)
                {
                    Amount.color = Mathf.Sin(Time.time * 10f) > 0.0 ? Color.red : Color.white;
                }
                else
                {
                    Amount.color = Color.white;
                }
            }

            public void Hide()
            {
                Icon.color = Color.clear;
                Name.text = "";
                Amount.text = "";
                Count = 0;
                SharedName = null;
            }
        }

        public class Favorite
        {
            private readonly Button button;
            public readonly Image Icon;
            public readonly Text Label;

            public Favorite(Transform transform)
            {
                button = transform.Find("MinLevel").GetComponent<Button>();
                Icon = transform.Find("MinLevel").GetComponent<Image>();
                Label = transform.Find("MinLevel/Text").GetComponent<Text>();
                
                SetButton(() =>
                {
                    if (instance == null || instance.m_destination?.GUID == null) return;
                    if (instance.m_destination.IsFavorite)
                    {
                        Player.m_localPlayer.RemoveFavorite(instance.m_destination.GUID);
                        instance.m_destination.IsFavorite = false;
                        SetFavorite(false);
                        instance.m_currentStationItem?.SetIcon(Minimap.instance.GetSprite(Minimap.PinType.Icon4));
                    }
                    else
                    {
                        Player.m_localPlayer.AddFavorite(instance.m_destination.GUID);
                        instance.m_destination.IsFavorite = true;
                        SetFavorite(true);
                        instance.m_currentStationItem?.SetIcon(m_starIcon);
                    }
                });
                
                Label.gameObject.SetActive(false);
            }
            
            public void SetButton(UnityAction action) => button.onClick.AddListener(action);
            public void SetFavorite(bool favorite) => Icon.color = favorite ? new Color(1f, 0.5f, 0f, 1f) : Color.black;
            public void SetLabel(string text) => Label.text = Localization.instance.Localize(text);
        }
    }
    public class StationInfo
    {
        public Vector3 Position;
        public Vector3 Offset = new Vector3(0f, 1f, 0f);
        public string Name;
        public readonly string CreatorName = string.Empty;
        public readonly long CreatorID = 0L;
        public string Guild;
        public int Filter;
        public bool IsFavorite;
        public bool IsFree;
        public readonly string? GUID;
        public bool IsValid => !string.IsNullOrEmpty(Name) && Name != "Stranger" && Position != Vector3.zero;
        private readonly ZDO? zdo;
        public static readonly StringBuilder sb = new StringBuilder();
        
        private enum FilterOptions
        {
            Public, Private, GuildOnly, GroupOnly, GuildGroupOnly
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

        private static string GetFilterLocalized(FilterOptions option)
        {
            return option switch
            {
                FilterOptions.Public => "$text_public",
                FilterOptions.Private => "$text_private",
                FilterOptions.GuildOnly => "$text_guild_only",
                FilterOptions.GroupOnly => "$text_group_only",
                FilterOptions.GuildGroupOnly => "$text_guild_and_group_only",
                _ => ""
            };
        }

        public StationInfo(ZDO zdo)
        {
            this.zdo = zdo;
            Position = zdo.GetPosition();
            Name = zdo.GetString(StationVars.Name);
            GUID = zdo.GetString(StationVars.GUID);
            CreatorName = zdo.GetString(StationVars.CreatorName);
            CreatorID = zdo.GetLong(ZDOVars.s_creator);
            Guild = zdo.GetString(StationVars.Guild);
            IsFree = zdo.GetBool(StationVars.Free);
            Filter = zdo.GetInt(StationVars.Filter);
            IsFavorite = Player.m_localPlayer.IsFavoriteStation(GUID);
        }

        public StationInfo(ZNetPeer peer)
        {
            Position = peer.GetRefPos();
            Name = peer.m_playerName;
            Guild = peer.GetGuild();
        }

        public StationInfo(Player user)
        {
            Position = user.transform.position;
            Name = user.GetPlayerName();
            Guild = user.GetGuild();
        }

        public void Reload()
        {
            if (zdo is null) return;
            Name = zdo.GetString(StationVars.Name);
            Guild = zdo.GetString(StationVars.Guild);
            Filter = zdo.GetInt(StationVars.Filter);
        }

        private float GetDistance(Player player) => Utils.DistanceXZ(player.transform.position, Position);

        public int GetCost(Player player)
        {
            if (instance == null) return 0;
            switch (instance.m_deviceType)
            {
                case DeviceType.Station:
                    return PortalStationsPlugin._PortalUseFuel.Value is PortalStationsPlugin.Toggle.Off ? 0 : Mathf.Max(1, Mathf.FloorToInt(Mathf.CeilToInt(GetDistance(player)) * PortalStationsPlugin._PortalPerFuelAmount.Value));
                case DeviceType.Item:
                    if (PortalStationsPlugin._DeviceUseFuel.Value is PortalStationsPlugin.Toggle.Off) return 0;
                    var quality = instance.m_currentItem?.m_quality ?? 1;
                    var cost = Mathf.Max(1, Mathf.FloorToInt(Mathf.CeilToInt(GetDistance(player)) * PortalStationsPlugin._DevicePerFuelAmount.Value));
                    var modifiedCost = Mathf.FloorToInt(cost / (quality * PortalStationsPlugin._DeviceAdditionalDistancePerUpgrade.Value));
                    return modifiedCost;
            }
            return 0;
        }

        public string GetTooltip()
        {
            sb.Clear();
            sb.Append($"$text_distance: <color=yellow>{GetDistance(Player.m_localPlayer):0.0}m</color>");
            sb.Append($"\n$text_cost: <color=yellow>{GetCost(Player.m_localPlayer)}</color>");
            if (!string.IsNullOrEmpty(CreatorName)) sb.Append($"\nCreator: <color=yellow>{CreatorName}</color>");
            sb.Append($"\n$text_privacy: <color=yellow>{GetFilterLocalized(GetFilterOption(Filter))}</color>");
            if (!string.IsNullOrEmpty(Guild)) sb.Append($"\n$text_guild: <color=yellow>{Guild}</color>");
            return sb.ToString();
        }

        public bool CanTeleport(bool message)
        {
            if (instance == null) return false;
            switch (instance.m_deviceType)
            {
                case  DeviceType.Station:
                    if (PortalStationsPlugin._PortalUseFuel.Value is PortalStationsPlugin.Toggle.On && instance?.m_currentStation != null && !Player.m_localPlayer.NoCostCheat())
                    {
                        var cost = GetCost(Player.m_localPlayer);
                        var fuelItem = Requirement.GetFuelItem();
                        var inventoryCount = Player.m_localPlayer.GetInventory().CountItems(fuelItem.m_itemData.m_shared.m_name);
                        if (cost > inventoryCount) return false;
                    }
                    break;
                case DeviceType.Item:
                    if (PortalStationsPlugin._DeviceUseFuel.Value is PortalStationsPlugin.Toggle.On &&
                        instance.m_currentItem != null && !Player.m_localPlayer.NoCostCheat())
                    {
                        var cost = GetCost(Player.m_localPlayer);
                        var modified = Mathf.FloorToInt(cost / (instance.m_currentItem?.m_quality ?? 1 / PortalStationsPlugin._DeviceAdditionalDistancePerUpgrade.Value));
                        var fuelItem = Requirement.GetFuelItem();
                        var inventoryCount = Player.m_localPlayer.GetInventory().CountItems(fuelItem.m_itemData.m_shared.m_name);
                        if (modified > inventoryCount) return false;
                    }
                    break;
                
            }
            bool result;
            switch (GetFilterOption(Filter))
            {
                case  FilterOptions.Public:
                    result = true;
                    break;
                case FilterOptions.Private:
                    result = Player.m_localPlayer.GetPlayerID() == CreatorID;
                    if (!result && message) Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_private");
                    break;
                case FilterOptions.GuildOnly:
                    result = Player.m_localPlayer.IsInGuild(Guild);
                    if (!result && message) Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_invalid_guild");
                    break;
                case  FilterOptions.GroupOnly:
                    result = Player.m_localPlayer.IsInGroup(CreatorID);
                    if (!result && message) Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_invalid_group");
                    break;
                case  FilterOptions.GuildGroupOnly:
                    result = Player.m_localPlayer.IsInGuild(Guild) || Player.m_localPlayer.IsInGroup(CreatorID);
                    if (!result && message) Player.m_localPlayer.Message(MessageHud.MessageType.Center, "$msg_invalid_guild_or_group");
                    break;
                default:
                    result = false;
                    break;
            }
            return result;
        }
    }
}

public static class StationHelpers
{
    public static bool IsInGuild(this Player player, string guildName)
    {
        if (!Guilds.API.IsLoaded()) return true;
        return player.GetGuild() == guildName;
    }

    public static bool IsInGroup(this Player player, long creatorID)
    {
        return !API.IsLoaded() || API.GroupPlayers().ConvertAll(p => p.peerId).Contains(creatorID);
    }
    public static string GetGuild(this ZNetPeer peer)
    {
        if (!ZNet.instance) return "";
        if (!Guilds.API.IsLoaded()) return "";
        ZNet.PlayerInfo info = ZNet.instance.GetPlayerList().FirstOrDefault(x => x.m_name == peer.m_playerName);
        Guilds.PlayerReference reference = Guilds.PlayerReference.fromPlayerInfo(info);
        return Guilds.API.GetPlayerGuild(reference)?.Name ?? "";
    }

    public static string GetGuild(this Player player)
    {
        if (!ZNet.instance) return "";
        if (!Guilds.API.IsLoaded()) return "";
        Guilds.PlayerReference reference = Guilds.PlayerReference.fromPlayer(player);
        return Guilds.API.GetPlayerGuild(reference)?.Name ?? "";
    }

    public static List<string> GetGroupMembers()
    {
        List<PlayerReference> players = API.GroupPlayers();
        return players.ConvertAll(p => p.name);
    }
}