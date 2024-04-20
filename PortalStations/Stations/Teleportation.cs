using System;
using System.Collections.Generic;
using UnityEngine;
using static PortalStations.PortalStationsPlugin;

namespace PortalStations.Stations;

public static class Teleportation
{
    private static string GetTeleportKey(string itemName)
    {
        switch (itemName)
        {
            case "$item_tinore" or "$item_tin": return _TinKey.Value;
            case "$item_copperore" or "$item_copperscrap" or "$item_copper": return _CopperKey.Value;
            case "$item_bronze" or "$item_bronzescrap": return _BronzeKey.Value;
            case "$item_iron" or "$item_ironore" or "$item_ironscrap": return _IronKey.Value;
            case "$item_silver" or "$item_silverore": return _SilverKey.Value;
            case "$item_blackmetalscrap" or "$item_blackmetal": return _BlackMetalKey.Value;
            case "$item_dragonegg": return _DragonEggKey.Value;
            case "$item_dvergrneedle": return _DvergerNeedleKey.Value;
            case "$item_flametal" or "$item_flametalore": return _FlameMetalKey.Value;
            default: return "none";
        }
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
            string key = GetTeleportKey(itemData.m_shared.m_name);
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