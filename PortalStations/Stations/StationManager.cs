using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;
using YamlDotNet.Serialization;

namespace PortalStations.Stations;

[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
public static class ZNetScene_Awake_Patch
{
    [UsedImplicitly]
    private static void Postfix()
    {
        if (StationManager.instance == null) return;
        if (!ZNet.instance || !ZNet.instance.IsServer()) return;
        StationManager.instance.InitCoroutine();
    }
}

public class StationManager : MonoBehaviour
{
    private static readonly List<ZDO> TempZDOs = new();
    public static readonly List<string> PrefabsToSearch = new();

    public static StationManager instance = null!;

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

                foreach (ZDO zdo in TempZDOs)
                {
                    ZDOMan.instance.ForceSendZDO(zdo.m_uid);
                }
            }
            yield return new WaitForSeconds(10f);
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
    public static readonly ISerializer serializer = new SerializerBuilder().Build();
    public static readonly IDeserializer deserializer = new DeserializerBuilder().Build();
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
    
    
}