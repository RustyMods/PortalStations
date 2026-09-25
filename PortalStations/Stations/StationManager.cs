using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using YamlDotNet.Serialization;

namespace PortalStations.Stations;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
internal static class ZNetScene_Awake_Patch
{
    [UsedImplicitly]
    private static void Postfix()
    {
        if (StationManager.instance == null) return;
        if (!ZNet.instance || !ZNet.instance.IsServer()) return;
        StationManager.instance.InitCoroutine();
    }
}

[HarmonyPatch(typeof(ObjectDB), nameof(ObjectDB.Awake))]
internal static class ObjectDB_Awake_Patch
{
    [HarmonyPriority(Priority.Last)]
    private static void Postfix(ObjectDB __instance)
    {
        foreach (var item in __instance.m_items)
        {
            if (item == null || !item.TryGetComponent(out ItemDrop component)) continue;
            StationManagerHelpers.itemSharedNamesMap[item.name] = component.m_itemData.m_shared.m_name;
        }
    }
}

public class StationManager : MonoBehaviour
{
    private static readonly List<ZDO> TempZDOs = new();
    public static readonly List<string> PrefabsToSearch = new();

    public static StationManager instance = null!;

    private static readonly WaitForSeconds interval = new (10f);

    public void Awake()
    {
        instance = this;
    }

    public void InitCoroutine() => StartCoroutine(SendStationsToClient());
    
    private static IEnumerator SendStationsToClient()
    {
        while (true)
        {
            if (Game.instance && ZDOMan.instance != null && ZNet.instance && ZNet.instance.IsServer())
            {
                TempZDOs.Clear();
                foreach (string prefab in PrefabsToSearch)
                {
                    int index = 0;
                    while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab, TempZDOs, ref index))
                    {
                        yield return null;
                    }
                }

                foreach (ZDOMan.ZDOPeer? peer in ZDOMan.instance.m_peers)
                {
                    peer.m_forceSend.UnionWith(TempZDOs.Select(zdo => zdo.m_uid));
                }
            }

            yield return interval;
        }
    }

    public static HashSet<ZDO> GetStations()
    {
        List<ZDO> Destinations = new();
        foreach (string prefab in PrefabsToSearch)
        {
            int amount = 0;
            while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(prefab, Destinations, ref amount))
            {
            }
        }

        return new HashSet<ZDO>(Destinations);
    }
}

public static class StationManagerHelpers
{
    public static readonly Dictionary<string, string> itemSharedNamesMap = new();
    private static readonly ISerializer serializer = new SerializerBuilder().Build();
    private static readonly IDeserializer deserializer = new DeserializerBuilder().Build();
    public static void AddFavorite(this Player player, string stationGUID)
    {
        List<string> favorites = player.GetFavoriteStations();
        favorites.Add(stationGUID);
        string data = serializer.Serialize(favorites);
        player.m_customData[StationVars.FavoriteKey] = data;
    }

    public static void RemoveFavorite(this Player player, string stationGUID)
    {
        List<string> favorites = player.GetFavoriteStations();
        favorites.Remove(stationGUID);
        string data = serializer.Serialize(favorites);
        player.m_customData[StationVars.FavoriteKey] = data;
    }

    public static List<string> GetFavoriteStations(this Player player)
    {
        if (!player.m_customData.TryGetValue(StationVars.FavoriteKey, out string data)) return new();
        return deserializer.Deserialize<List<string>>(data);
    }
    
    public static bool IsFavoriteStation(this Player player, string stationGUID) => player.GetFavoriteStations().Contains(stationGUID);

    public static bool CanUsePortableStation(this Player player, bool msg = false)
    {
        if (PortalStationsPlugin.TeleportAnything) return true;
        if (!PortalStationsPlugin.UsePortalKeys)
        {
            if (player.IsTeleportable(false)) return true;
            if (msg) player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return false;
        }
        Dictionary<string, string> prefabToKeyMap = new PortalStationsPlugin.SerializedKeys(PortalStationsPlugin.PortalKeys).Keys;;
        Dictionary<string, string> sharedNames = new Dictionary<string, string>();
        foreach (KeyValuePair<string, string> kvp in prefabToKeyMap)
        {
            if (!itemSharedNamesMap.TryGetValue(kvp.Key, out string sharedName)) continue;
            sharedNames.Add(sharedName, kvp.Value);
        }
        foreach (ItemDrop.ItemData? item in Player.m_localPlayer.GetInventory().GetAllItems())
        {
            if (item.m_shared.m_teleportable) continue;
            if (!sharedNames.TryGetValue(item.m_shared.m_name, out string? key))
            {
                if (msg) player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return false;
            }

            if (!ZoneSystem.instance.GetGlobalKey(key) && !Player.m_localPlayer.GetUniqueKeys().Contains(key))
            {
                if (msg) player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return false;
            }
        }
        return true;
    }

    public static bool CanUsePortalStation(this Player player, PortalStation station, bool msg = false)
    {
        if (PortalStationsPlugin.TeleportAnything) return true;

        if (!PortalStationsPlugin.UsePortalKeys)
        {
            if (player.IsTeleportable(false)) return true;
            if (msg) player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
            return false;
        }
        
        Dictionary<string, string> prefabToKeyMap;
        string normalizedName = station.name.Replace("(Clone)", string.Empty);
        if (PortalStationsPlugin.localPortalKeysConfigs.TryGetValue(normalizedName, out var pieceConfig))
        {
            prefabToKeyMap = new PortalStationsPlugin.SerializedKeys(pieceConfig.Value).Keys;
            if (prefabToKeyMap.Count == 0)
            {
                prefabToKeyMap = new PortalStationsPlugin.SerializedKeys(PortalStationsPlugin.PortalKeys).Keys;
            }
        }
        else
        {
            prefabToKeyMap = new PortalStationsPlugin.SerializedKeys(PortalStationsPlugin.PortalKeys).Keys;
        }
            
        Dictionary<string, string> sharedNames = new Dictionary<string, string>();
        foreach (KeyValuePair<string, string> kvp in prefabToKeyMap)
        {
            if (!itemSharedNamesMap.TryGetValue(kvp.Key, out string sharedName)) continue;
            sharedNames.Add(sharedName, kvp.Value);
        }
            
        foreach (ItemDrop.ItemData? item in Player.m_localPlayer.GetInventory().GetAllItems())
        {
            if (item.m_shared.m_teleportable) continue;
            if (!sharedNames.TryGetValue(item.m_shared.m_name, out string? key))
            {
                if (msg) player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return false;
            }

            if (!ZoneSystem.instance.GetGlobalKey(key) && !Player.m_localPlayer.GetUniqueKeys().Contains(key))
            {
                if (msg) player.Message(MessageHud.MessageType.Center, "$msg_noteleport");
                return false;
            }
        }

        return true;
    }
}