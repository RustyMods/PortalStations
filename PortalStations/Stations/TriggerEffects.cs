// using System;
// using UnityEngine;
//
// namespace PortalStations.Stations;
//
// public class TriggerEffects : MonoBehaviour
// {
//     private ZNetView _znv = null!;
//     private static readonly int _prop_station_emission = "stationEmission".GetStableHashCode();
//     private GameObject emissive = null!;
//     public void Awake()
//     {
//         _znv = GetComponentInParent<ZNetView>();
//         if (!_znv.IsValid()) return;
//         
//         emissive = Utils.FindChild(transform, "emissive").gameObject;
//         _znv.Register<bool>(nameof(RPC_SetEmission), RPC_SetEmission);
//
//         if (!_znv.IsOwner()) return;
//         
//         _znv.GetZDO().Set(_prop_station_emission, false);
//     }
//
//     public void Update()
//     {
//         if (!_znv.IsValid()) return;
//         emissive.SetActive(_znv.GetZDO().GetBool(_prop_station_emission));
//     }
//
//     public void OnTriggerEnter(Collider other)
//     {
//         if (!_znv.IsValid()) return;
//         _znv.GetZDO().Set(_prop_station_emission, true);
//     }
//
//     public void OnTriggerExit(Collider other)
//     {
//         if (!_znv.IsValid()) return;
//         _znv.GetZDO().Set(_prop_station_emission, false);
//     }
//
//     public void RPC_SetEmission(long sender, bool value)
//     {
//         if (!_znv.IsValid() || !_znv.IsOwner()) return;
//         _znv.GetZDO().Set(_prop_station_emission, value);
//     }
// }