using System.Collections.Generic;
using HarmonyLib;
using YamlDotNet.Serialization;
using static PortalStations.Stations.PortalStationGUI;

namespace PortalStations.Stations;

public static class Patches
{
    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    static class AttachPortalStationGUI
    {
        private static void Postfix(InventoryGui __instance) => LoadUI.InitGUI(__instance);
        
    }
    
    [HarmonyPatch(typeof(StoreGui), nameof(StoreGui.IsVisible))]
    static class IsStationVisible2
    {
        private static void Postfix(ref bool __result) => __result |=  IsPortalGUIVisible();
        
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Hide))]
    static class IsStationVisible
    {
        private static bool Prefix() => !IsPortalGUIVisible();
    }

    [HarmonyPatch(typeof(PlayerController), nameof(PlayerController.FixedUpdate))]
    static class StationPlayerControllerOverride
    {
        private static bool Prefix() => !IsPortalGUIVisible();
    }
    
    [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
    private static class LogoutPatch
    {
        private static void Postfix() => SaveFavorites();
    }

    public static void SaveFavorites()
    {
        if (!Player.m_localPlayer) return;
        try
        {
            ISerializer serializer = new SerializerBuilder().Build();
            string data = serializer.Serialize(Favorites);

            Player.m_localPlayer.m_customData[PortalStation._FavoriteKey] = data;
        }
        catch
        {
            PortalStationsPlugin.PortalStationsLogger.LogDebug("Failed to save favorite portals");
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    private static class SetLocalPlayerPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!__instance || __instance != Player.m_localPlayer) return;
            if (!__instance.m_customData.TryGetValue(PortalStation._FavoriteKey, out string data)) return;
            IDeserializer deserializer = new DeserializerBuilder().Build();
            Favorites = deserializer.Deserialize<List<string>>(data);
        }
    }

    [HarmonyPatch(typeof(PieceTable), nameof(PieceTable.GetPiecesInSelectedCategory))]
    private static class PieceTableGetPiecesPatch
    {
        private static void Postfix(ref List<Piece> __result)
        {
            if (PortalStationsPlugin._OnlyAdminBuilds.Value is PortalStationsPlugin.Toggle.Off) return;
            if (ZNet.instance.LocalPlayerIsAdminOrHost()) return;
            __result.RemoveAll(x => x.GetComponent<PortalStation>());
        }
    }
}