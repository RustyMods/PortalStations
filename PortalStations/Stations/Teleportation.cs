using System.Collections.Generic;
using static PortalStations.PortalStationsPlugin;

namespace PortalStations.Stations;

public static class Teleportation
{
    private static readonly Dictionary<string, string> OreKeys = new()
    {
        {"$item_tinore", _TinKey.Value},
        {"$item_tin",_TinKey.Value},
        {"$item_copperore", _CopperKey.Value},
        {"$item_copperscrap",_CopperKey.Value},
        {"$item_copper",_CopperKey.Value},
        {"$item_bronze",_BronzeKey.Value},
        {"$item_bronzescrap",_BronzeKey.Value},
        {"$item_iron",_IronKey.Value},
        {"$item_ironore",_IronKey.Value},
        {"$item_ironscrap",_IronKey.Value},
        {"$item_silver",_SilverKey.Value},
        {"$item_silverore",_SilverKey.Value},
        {"$item_blackmetalscrap",_BlackMetalKey.Value},
        {"$item_blackmetal",_BlackMetalKey.Value},
        {"$item_dragonegg",_DragonEggKey.Value},
        {"$item_dvergrneedle",_DvergerNeedleKey.Value},
        {"$item_flametal",_FlameMetalKey.Value},
        {"$item_flametalore",_FlameMetalKey.Value}
    };
    public static bool IsTeleportable(Player player)
    {
        if (_TeleportAnything.Value is Toggle.On) return true;
        if (!player.IsTeleportable())
        {
            if (_UsePortalKeys.Value is Toggle.Off) return false;
            Inventory inventory = player.GetInventory();
            List<string> keys = ZoneSystem.instance.GetGlobalKeys();
            foreach (ItemDrop.ItemData itemData in inventory.m_inventory)
            {
                if (!OreKeys.TryGetValue(itemData.m_shared.m_name, out string key)) continue;
                if (!keys.Contains(key)) return false ;
            }
            return true;
        }
        return true;
    }
}