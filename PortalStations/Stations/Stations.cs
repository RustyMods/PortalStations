using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PortalStations.Stations;

public static class Stations
{
    private static readonly List<ZDO> TempZDOs = new();
    public static readonly string PrefabToSearch = "portalstation";
    public static string PlayerToSearch = "Player";

    public static void InitCoroutine() => PortalStationsPlugin._plugin.StartCoroutine(SendStationsToClient());
    private static IEnumerator SendStationsToClient()
    {
        for (;;)
        {
            if (Game.instance && ZDOMan.instance != null && ZNet.instance && ZNet.instance.IsServer())
            {
                TempZDOs.Clear();
                int index = 0;
                while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(PrefabToSearch, TempZDOs, ref index))
                {
                    yield return null;
                }

                int playerIndex = 0;
                while (!ZDOMan.instance.GetAllZDOsWithPrefabIterative(PlayerToSearch, TempZDOs, ref playerIndex))
                {
                    yield return null;
                }
                foreach (ZDO zdo in TempZDOs) ZDOMan.instance.ForceSendZDO(zdo.m_uid);
            }
            yield return new WaitForSeconds(10f);
        }
    }
    
}