using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PortalStations.UI;

public class StationButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform m_rect = null!;
    public Vector2 m_originalSize;
    public void Awake()
    {
        m_rect = GetComponent<RectTransform>();
        m_originalSize = m_rect.sizeDelta;
    }

    public void OnDisable()
    {
        m_rect.sizeDelta = m_originalSize;
    }

    public void Reset()
    {
        m_rect.sizeDelta = m_originalSize;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        m_rect.sizeDelta *= 1.15f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        m_rect.sizeDelta /= 1.15f;
    }
}