using HarmonyLib;
using static PortalStations.Stations.PersonalTeleportationGUI;
using static PortalStations.Stations.PortalStationGUI;

namespace PortalStations.Stations;

public static class Patches
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    static class AttachPortalStationGUI
    {
        private static void Postfix(InventoryGui __instance)
        {
            PersonalTeleportationGUI.InitGUI(__instance);
            PortalStationGUI.InitGUI(__instance);
        }
    }
    
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.IsVisible))]
    static class IsStationVisible2
    {
        private static void Postfix(ref bool __result)
        {
            __result |= IsPersonalPortalGUIVisible() || IsPortalGUIVisible();
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    static class IsStationVisible
    {
        private static bool Prefix() => !IsPersonalPortalGUIVisible() || !IsPortalGUIVisible();
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FixedUpdate))]
    static class StationPlayerControllerOverride
    {
        private static bool Prefix() => !IsPersonalPortalGUIVisible() || !IsPortalGUIVisible();
    }
}