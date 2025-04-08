using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace PortalStations.UI;

public class UIHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject m_go = null!;
    public Text m_text = null!;
    public string m_tooltip = "";
    public void Awake()
    {
        var hover = new GameObject("hover");
        m_go = hover;
        var rect = hover.AddComponent<RectTransform>();
        rect.SetParent(transform);
        m_text = hover.AddComponent<Text>();
        m_text.horizontalOverflow = HorizontalWrapMode.Overflow;
        m_text.fontSize = 14;
        m_text.font = FontManager.GetFont(FontManager.FontOptions.AveriaSerifLibre);
        m_text.text = m_tooltip;
        m_go.SetActive(false);
    }

    public void Update()
    {
        m_go.transform.position = Input.mousePosition + new Vector3(5f, 0f);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_go.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_go.SetActive(false);
    }
}