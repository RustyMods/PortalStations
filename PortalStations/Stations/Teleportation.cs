using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using static PortalStations.PortalStationsPlugin;

namespace PortalStations.Stations;

public static class Teleportation
{
    private static readonly Dictionary<string, ConfigEntry<string>> m_teleportKeys = new();

    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
    private static class CreateConfigsForRestrictiveItems
    {
        private static void Postfix(ObjectDB __instance)
        {
            if (!ZNetScene.instance) return;
            CreateOreConfigs(__instance);
        }
    }
    private static void CreateOreConfigs(ObjectDB __instance)
    {
        foreach (GameObject prefab in __instance.m_items)
        {
            if (!prefab.TryGetComponent(out ItemDrop component)) continue;
            if (component.m_itemData.m_shared.m_teleportable) continue;
            var config = _plugin.config("Teleport Keys", $"{prefab.name}", "",
                $"Set the defeat key to enable teleporting {prefab.name}");
            m_teleportKeys[component.m_itemData.m_shared.m_name] = config;
        }
    }

    private static string GetKey(string itemName)
    {
        return m_teleportKeys.TryGetValue(itemName, out ConfigEntry<string> config) ? config.Value : "none";
    }

    public static bool IsTeleportable(Player player)
    {
        if (_TeleportAnything.Value is Toggle.On) return true;
        if (!player.IsTeleportable())
        {
            return _UsePortalKeys.Value is Toggle.On && CheckInventory(player.GetInventory());
        }
        return true;
    }

    private static bool CheckInventory(Inventory inventory)
    {
        List<string> keys = ZoneSystem.instance.GetGlobalKeys();
        foreach (ItemDrop.ItemData itemData in inventory.m_inventory)
        {
            string key = GetKey(itemData.m_shared.m_name);
            if (key == "none")
            {
                if (!itemData.m_shared.m_teleportable) return false;
                continue;
            }

            if (!keys.Contains(key) && !ZoneSystem.instance.GetGlobalKey(key)) return false;
        }
        return true;
    }
    
    public static int GetFuelAmount(Humanoid user, ItemDrop fuelItem) => user.GetInventory().CountItems(fuelItem.m_itemData.m_shared.m_name);
    public static void ConsumeFuel(Player user, ItemDrop fuelItem, int amount) => user.GetInventory().RemoveItem(fuelItem.m_itemData.m_shared.m_name, amount, -1, false);
    public static int CalculateFuelCost(ItemDrop.ItemData deviceData, float distance)
    {
        if (_DeviceUseFuel.Value is Toggle.Off) return 0;
    
        float travelDistancePerFuelItem = _DevicePerFuelAmount.Value + (Math.Max(0, deviceData.m_quality - 1) * _DeviceAdditionalDistancePerUpgrade.Value);
    
        return Mathf.Max(1, Mathf.CeilToInt((distance / 2) / travelDistancePerFuelItem));
    }

    public static int CalculateFuelCost(float distance)
    {
        return _PortalUseFuel.Value is Toggle.Off ? 0 : Mathf.Max(1, Mathf.CeilToInt(distance / 2) / _PortalPerFuelAmount.Value );
    }
}