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

            if (_UsePrivateKeys.Value is Toggle.On)
            {
                return ZoneSystem.instance.GetGlobalKey(key);
            }
            
            if (!keys.Contains(key)) return false;
        }
        return true;
    }
}