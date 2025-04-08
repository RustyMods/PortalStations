using UnityEngine;
using UnityEngine.UI;

namespace PortalStations.UI;

public class DropdownItem : MonoBehaviour
{
    public Text m_text = null!;
    public Image m_image = null!;
    public Color m_baseColor;
    public void Awake()
    {
        m_text = GetComponentInChildren<Text>();
        m_image = GetComponentInChildren<Image>();
        m_baseColor = m_image.color;
        m_text.font = FontManager.GetFont(PortalStationsPlugin._Font.Value);
    }
}