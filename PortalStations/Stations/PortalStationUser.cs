using UnityEngine;

namespace PortalStations.Stations;

public class PortalStationUser : MonoBehaviour
{
    private Humanoid player = null!;
    private ZNetView znv = null!;

    public void Use(ItemDrop.ItemData device)
    {
        if (!znv || !znv.IsValid()) return;
        UsePortalStation(device);
    }
    private void Awake()
    {
        znv = GetComponent<ZNetView>();
        player = GetComponent<Humanoid>();
    }

    private void UsePortalStation(ItemDrop.ItemData device)
    {
        if (!player || !znv) return;
        ZDO? playerZDO = znv.IsValid() ? znv.GetZDO() : null;
        if (playerZDO == null) return;
        PersonalTeleportationGUI.ShowPersonalPortalGUI(player, znv, device);
    }
}