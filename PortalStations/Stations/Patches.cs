﻿using System.Collections.Generic;
using HarmonyLib;
using YamlDotNet.Serialization;
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
    
    [HarmonyPatch(typeof(Game), nameof(Game.Logout))]
    private static class LogoutPatch
    {
        private static void Postfix() => SaveFavorites();
    }

    public static void SaveFavorites()
    {
        ISerializer serializer = new SerializerBuilder().Build();
        string data = serializer.Serialize(Favorites);

        Player.m_localPlayer.m_customData[PortalStation._FavoriteKey] = data;
    }

    [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
    private static class SetLocalPlayerPatch
    {
        private static void Postfix(Player __instance)
        {
            if (!__instance) return;
            if (!__instance.m_customData.TryGetValue(PortalStation._FavoriteKey, out string data)) return;
            IDeserializer deserializer = new DeserializerBuilder().Build();
            Favorites = deserializer.Deserialize<List<string>>(data);
        }
    }
}