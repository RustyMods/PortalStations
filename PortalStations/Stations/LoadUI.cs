using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static PortalStations.PortalStationsPlugin;
using static PortalStations.Stations.PortalStationGUI;

namespace PortalStations.Stations;

public static class LoadUI
{
    [Header("Vanilla Assets")]
    private static ButtonSfx m_sfx = null!;
    private static Image m_background = null!;
    [Header("Prefabs")]
    public static GameObject PortalGUI = null!;
    public static GameObject PortalGUI_Item = null!;
    public static GameObject ToggleOn = null!;
    public static GameObject ToggleOff = null!;
    public static GameObject m_public_button = null!;
    public static GameObject m_public_text_go = null!;
    public static GameObject m_group_button = null!;
    public static GameObject m_group_text_go = null!;
    public static GameObject GroupToggleOn = null!;
    public static GameObject GroupToggleOff = null!;
    public static GameObject m_guild_button = null!;
    public static GameObject m_guild_text_go = null!;
    public static GameObject GuildToggleOn = null!;
    public static GameObject GuildToggleOff = null!;

    public static RectTransform ItemListRoot = null!;
    [Header("Inputs")]
    public static Text m_title = null!;
    public static Text m_public_text = null!;
    public static Text m_group_text = null!;
    public static Text m_guild_text = null!;
    
    public static void InitGUI(InventoryGui instance)
    {
        if (!instance) return;
        GetAssets(instance);
        SetVariables(instance);
        SetCloseButton();
        AddBackgroundMaterial();
        SetConfigurable();
        SetOtherButtons();
        SetFilterListener();
        AddSFX();
        AddFont();
    }

    private static void AddSFX()
    {
        foreach (Button? button in PortalGUI.GetComponentsInChildren<Button>())
        {
            if (button.GetComponent<ButtonSfx>()) continue;
            button.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = m_sfx.m_sfxPrefab;
        }

        foreach (Button? button in PortalGUI_Item.GetComponentsInChildren<Button>())
        {
            if (button.GetComponent<ButtonSfx>()) continue;
            button.gameObject.AddComponent<ButtonSfx>().m_sfxPrefab = m_sfx.m_sfxPrefab;
        }
    }

    private static void SetOtherButtons()
    {
        Transform ToggleButton = Utils.FindChild(PortalGUI.transform, "$part_toggleButton");
        m_public_button = ToggleButton.parent.gameObject;
        if (!ToggleButton.TryGetComponent(out Button toggleButton)) return;
        toggleButton.onClick.AddListener(TogglePublic);
        ToggleOn = Utils.FindChild(ToggleButton, "On").gameObject;
        ToggleOff = Utils.FindChild(ToggleButton, "Off").gameObject;

        Transform GroupButton = Utils.FindChild(PortalGUI.transform, "$part_groupButton");
        m_group_button = GroupButton.parent.gameObject;
        if (!GroupButton.TryGetComponent(out Button groupButton)) return;
        groupButton.onClick.AddListener(ToggleGroup);
        GroupToggleOn = Utils.FindChild(GroupButton, "On").gameObject;
        GroupToggleOff = Utils.FindChild(GroupButton, "Off").gameObject;

        Transform GuildButton = Utils.FindChild(PortalGUI.transform, "$part_guildButton");
        m_guild_button = GuildButton.parent.gameObject;
        if (!GuildButton.TryGetComponent(out Button guildButton)) return;
        guildButton.onClick.AddListener(ToggleGuild);
        GuildToggleOn = Utils.FindChild(GuildButton, "On").gameObject;
        GuildToggleOff = Utils.FindChild(GuildButton, "Off").gameObject;

        if (!Groups.API.IsLoaded())
        {
            m_group_button.SetActive(false);
            m_group_text_go.SetActive(false);
        }

        if (!Guilds.API.IsLoaded())
        {
            m_guild_button.SetActive(false);
            m_guild_text_go.SetActive(false);
        }
    }

    private static void SetFilterListener()
    {
        if (Utils.FindChild(PortalGUI.transform, "$input_filter").TryGetComponent(out InputField filter))
        {
            filter.onValueChanged.AddListener(OnFilterInput);
        }
    }

    private static void SetConfigurable()
    {
        Utils.FindChild(PortalGUI.transform, "$text_filter").GetComponent<Text>().text = _StationFilterText.Value;
        Utils.FindChild(PortalGUI.transform, "$text_destinations").GetComponent<Text>().text = _StationDestinationText.Value;
        m_title.text = _StationTitle.Value;
        m_public_text.text= _PublicText.Value;
    }

    private static void AddBackgroundMaterial()
    {
        Image[] PortalStationImages = PortalGUI.GetComponentsInChildren<Image>();
        foreach (Image image in PortalStationImages) image.material = m_background.material;
    }

    private static void SetCloseButton()
    {
        Transform button = Utils.FindChild(PortalGUI.transform, "$part_CloseButton");
        Button closeButton = button.GetComponent<Button>();
        closeButton.onClick.AddListener(HidePortalGUI);

        if (!button.GetComponent<ButtonSfx>())
        {
            ButtonSfx closeButtonSfx = button.gameObject.AddComponent<ButtonSfx>();
            closeButtonSfx.m_sfxPrefab = m_sfx.m_sfxPrefab;
        }

        if (button.Find("$text_button_close").TryGetComponent(out Text buttonText))
        {
            buttonText.text = _StationCloseText.Value;
        }
    }

    private static void SetVariables(InventoryGui instance)
    {
        PortalGUI = Object.Instantiate(_asset.LoadAsset<GameObject>("portalstation_gui"), instance.transform, false);
        PortalGUI_Item = _asset.LoadAsset<GameObject>("portalstation_gui_stationitem");
        ItemListRoot = Utils.FindChild(PortalGUI.transform, "$part_Content").GetComponent<RectTransform>();
        
        PortalGUI.SetActive(false);
        
        m_public_text_go = Utils.FindChild(PortalGUI.transform, "$text_public").gameObject;
        m_public_text = m_public_text_go.GetComponent<Text>();
        m_title = Utils.FindChild(PortalGUI.transform, "$text_title").GetComponent<Text>();

        m_group_text_go = Utils.FindChild(PortalGUI.transform, "$text_group").gameObject;
        m_group_text = m_group_text_go.GetComponent<Text>();

        m_guild_text_go = Utils.FindChild(PortalGUI.transform, "$text_guild").gameObject;
        m_guild_text = m_group_text_go.GetComponent<Text>();
    }

    private static void GetAssets(InventoryGui instance)
    {
        m_sfx = instance.m_trophiesPanel.transform.Find("TrophiesFrame/Closebutton").GetComponent<ButtonSfx>();
        m_background = instance.m_trophiesPanel.transform.Find("TrophiesFrame/border (1)").GetComponent<Image>();
    }

    private static void AddFont()
    {
        Font? norseBold = GetFont("Norsebold");
        AddFonts(PortalGUI.GetComponentsInChildren<Text>(), norseBold);
        AddFonts(PortalGUI_Item.GetComponentsInChildren<Text>(), norseBold);
    }

    private static void AddFonts(Text[] array, Font? font)
    {
        foreach (Text text in array) text.font = font;
    }
    
    private static Font? GetFont(string name) => Resources.FindObjectsOfTypeAll<Font>().FirstOrDefault(x => x.name == name);
}