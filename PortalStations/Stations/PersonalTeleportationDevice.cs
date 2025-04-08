using HarmonyLib;
using PortalStations.UI;
using static PortalStations.PortalStationsPlugin;

namespace PortalStations.Stations;

public static class PersonalTeleportationDevice
{
    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UseItem))]
    static class UsePersonalPortalDevice
    {
        private static bool Prefix(Humanoid __instance, ItemDrop.ItemData item)
        {
            if (item.m_shared.m_name != "$item_personal_teleportation_device") return true;
            UseItem(__instance, item);
            return false;
        }
    }
    private static void UseItem(Humanoid user, ItemDrop.ItemData item)
    {
        if (item.m_durability < item.m_shared.m_durabilityDrain) return;
        item.m_durability -= _PersonalPortalDurabilityDrain.Value;
        StationUI.m_instance.Show();
    }
    
}