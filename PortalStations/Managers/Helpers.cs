using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PortalStations.Managers;

[PublicAPI]
public static class Helpers
{
    internal static ZNetScene? _ZNetScene;
    internal static ObjectDB? _ObjectDB;

    internal static GameObject? GetPrefab(string prefabName)
    {
        if (ZNetScene.instance != null) return ZNetScene.instance.GetPrefab(prefabName);
        if (_ZNetScene == null) return null;
        GameObject? result = _ZNetScene.m_prefabs.Find(prefab => prefab.name == prefabName);
        if (result != null) return result;
        return Clone.registeredPrefabs.TryGetValue(prefabName, out GameObject clone) ? clone : result;
    }
    
    public static string GetNormalizedName(string name) => Regex.Replace(name, @"\s*\(.*?\)", "").Trim();

    public static bool HasComponent<T>(this GameObject go) where T : Component => go.GetComponent<T>();

    public static void RemoveComponent<T>(this GameObject go) where T : Component
    {
        if (!go.TryGetComponent(out T component)) return;
        Object.DestroyImmediate(component);
    }

    public static void RemoveAllComponents<T>(this GameObject go, bool includeChildren = false, params Type[] ignoreComponents) where T : MonoBehaviour
    {
        List<T> components = go.GetComponents<T>().ToList();
        if (includeChildren)
        {
            components.AddRange(go.GetComponentsInChildren<T>(true));
        }
        foreach (T component in components)
        {
            if (ignoreComponents.Contains(component.GetType())) continue;
            Object.DestroyImmediate(component);
        }
    }

    public static List<Transform> FindAll(this Transform parent, string name)
    {
        List<Transform> result = new List<Transform>();
        foreach (Transform transform in parent)
        {
            if (transform.name == name) result.Add(transform);
        }

        return result;
    }
    
    public static void CopySpriteAndMaterial(this GameObject prefab, GameObject source, string childName, string sourceChildName = "")
    {
        Transform to = prefab.transform.Find(childName);
        if (to == null)
        {
            Debug.LogError($"CopySpriteAndMaterial: couldn't find child {childName} on {prefab.name}");
            return;
        }

        if (!to.TryGetComponent(out Image toImage))
        {
            Debug.LogError($"CopySpriteAndMaterial: couldn't find image on {to.name}");
            return;
        }
        
        Transform from = string.IsNullOrWhiteSpace(sourceChildName) ? source.transform : source.transform.Find(sourceChildName);
        if (from == null)
        {
            Debug.LogError($"CopySpriteAndMaterial: couldn't find child {sourceChildName} on {source.name}");
            return;
        }

        if (!from.TryGetComponent(out Image fromImage))
        {
            Debug.LogError($"CopySpriteAndMaterial: couldn't find image on {from.name}");
            return;
        }
        toImage.sprite = fromImage.sprite;
        toImage.material = fromImage.material;
        toImage.color = fromImage.color;
        toImage.type = fromImage.type;
    }
    
    public static void CopyButtonState(this GameObject prefab, GameObject source, string childName, string sourceChildName = "")
    {
        Transform? target = prefab.transform.Find(childName);
        if (target == null)
        {
            Debug.LogError($"CopyButtonState failed to find {childName} on {prefab.name}");
            return;
        }

        if (!target.TryGetComponent(out Button button))
        {
            Debug.LogError($"CopyButtonState failed to find Button component on {target.name}");
            return;
        }

        Transform sourceChild;
        if (!string.IsNullOrWhiteSpace(sourceChildName))
        {
            sourceChild = source.transform.Find(sourceChildName);
            if (sourceChild == null)
            {
                Debug.LogError($"CopyButtonState failed to find {sourceChildName} on {source.name}");
                return;
            }
        }
        else
        {
            sourceChild = source.transform;
        }

        if (!sourceChild.TryGetComponent(out Button sourceButton))
        {
            Debug.LogError($"CopyButtonSprite {sourceChild.name} missing Button component");
            return;
        }
        button.spriteState = sourceButton.spriteState;
    }
    
    public static bool IsValid(this ItemDrop.ItemData item) => item.m_shared.m_icons.Length > 0;

}