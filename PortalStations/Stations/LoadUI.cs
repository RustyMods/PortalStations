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
    public static RectTransform ItemListRoot = null!;
    [Header("Inputs")]
    public static Text m_title = null!;
    public static Text m_public_text = null!;
    
    public static void InitGUI(InventoryGui instance)
    {
        if (!instance) return;
        GetAssets(instance);
        SetVariables(instance);
        SetCloseButton();
        AddBackgroundMaterial();
        SetConfigurable();
        SetToggleButton();
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

    private static void SetToggleButton()
    {
        Transform ToggleButton = Utils.FindChild(PortalGUI.transform, "$part_toggleButton");
        m_public_button = ToggleButton.gameObject;
        if (!ToggleButton.TryGetComponent(out Button toggleButton)) return;
        toggleButton.onClick.AddListener(SetToggleValue);
        ToggleOn = Utils.FindChild(ToggleButton, "On").gameObject;
        ToggleOff = Utils.FindChild(ToggleButton, "Off").gameObject;
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
        PortalGUI_Item = Object.Instantiate(_asset.LoadAsset<GameObject>("portalstation_gui_stationitem"));
        ItemListRoot = Utils.FindChild(PortalGUI.transform, "$part_Content").GetComponent<RectTransform>();
        
        Object.DontDestroyOnLoad(PortalGUI);
        PortalGUI.SetActive(false);
        
        m_public_text_go = Utils.FindChild(PortalGUI.transform, "$text_public").gameObject;
        m_public_text = m_public_text_go.GetComponent<Text>();
        m_title = Utils.FindChild(PortalGUI.transform, "$text_title").GetComponent<Text>();
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