using System;
using UnityEngine;
using Unity.Netcode;

public class TeethBehaviour : TaraBase
{
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        SnapZone snapZone = GetComponent<SnapZone>();
        bool preSnappedGood = snapZone?.currentItem != null &&
                              snapZone.currentItem.gameObject.CompareTag("GoodTeeth");

        if (preSnappedGood)
        {
            MarkAsHealed();
            EventBus.Subscribe<OnDienteUnSnap>(OnDienteUnSnapHandler);
            return;
        }

        EventBus.Subscribe<OnDienteSnap>(OnDienteSnapHandler);
    }



    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        EventBus.Unsubscribe<OnDienteSnap>(OnDienteSnapHandler);
        EventBus.Unsubscribe<OnDienteUnSnap>(OnDienteUnSnapHandler);
    }

    private void OnDienteSnapHandler(OnDienteSnap snap)
    {
        if (snap.ID != NetworkObjectId) return;
        if (snap.currentItem == null) return;

        if (snap.currentItem.gameObject.CompareTag("GoodTeeth"))
        {
            MarkAsHealed();
            EventBus.Unsubscribe<OnDienteSnap>(OnDienteSnapHandler);
            EventBus.Subscribe<OnDienteUnSnap>(OnDienteUnSnapHandler);
        }
    }

    private void OnDienteUnSnapHandler(OnDienteUnSnap snap)
    {
        if (snap.SnapZoneID != NetworkObjectId) return;
        UnmarkAsHealed();
        EventBus.Unsubscribe<OnDienteUnSnap>(OnDienteUnSnapHandler);
        EventBus.Subscribe<OnDienteSnap>(OnDienteSnapHandler);
    }
}