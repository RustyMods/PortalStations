using System;
using HarmonyLib;
using UnityEngine;
using static PortalStations.PortalStationsPlugin;

namespace PortalStations.Stations;

public static class PersonalTeleportationDevice
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    static class UsePersonalPortalDevice
    {
        private static bool Prefix(Humanoid __instance, Inventory inventory, ItemDrop.ItemData item, bool fromInventoryGui)
        {
            if (item.m_shared.m_name != "$item_personal_teleportation_device") return true;
            UseItem(__instance, item);
            return false;
        }
    }
    public static int GetFuelAmount(Humanoid user, ItemDrop fuelItem) => user.GetInventory().CountItems(fuelItem.m_itemData.m_shared.m_name);
    public static void ConsumeFuel(Player user, ItemDrop fuelItem, int amount) => user.GetInventory().RemoveItem(fuelItem.m_itemData.m_shared.m_name, amount, -1, false);
    public static int CalculateFuelCost(ItemDrop.ItemData deviceData, float distance)
    {
        if (_DeviceUseFuel.Value is Toggle.Off) return 0;

        float travelDistancePerFuelItem = _DevicePerFuelAmount.Value + (Math.Max(0, deviceData.m_quality - 1) * _DeviceAdditionalDistancePerUpgrade.Value);

        return Mathf.Max(1, Mathf.CeilToInt((distance / 2) / travelDistancePerFuelItem));
    }

    private static void UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (item.m_durability < item.m_shared.m_durabilityDrain) return;
        item.m_durability -= _PersonalPortalDurabilityDrain.Value;
        PersonalTeleportationGUI.ShowPersonalPortalGUI(user, item);
    }
    
}