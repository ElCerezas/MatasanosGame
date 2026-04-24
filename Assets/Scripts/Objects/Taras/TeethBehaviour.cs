// TeethBehaviour.cs
using System;
using UnityEngine;
using Unity.Netcode;

public class TeethBehaviour : TaraBase
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsServer) return;
        EventBus.Subscribe<OnDienteSnap>(OnDienteSnapHandler);

        SnapZone snapZone = GetComponent<SnapZone>();
        //Debug.Log($"TeethBehaviour {NetworkObjectId} - SnapZone: {snapZone != null}, currentItem: {snapZone?.currentItem != null}, tag: {snapZone?.currentItem?.gameObject.tag}");

        if (snapZone != null && snapZone.currentItem != null)
        {
            OnDienteSnapHandler(new OnDienteSnap
            {
                ID = NetworkObjectId,
                currentItem = snapZone.currentItem
            });
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;
        EventBus.Unsubscribe<OnDienteSnap>(OnDienteSnapHandler);
    }

    private void OnDienteSnapHandler(OnDienteSnap snap)
    {
        if (snap.ID != NetworkObjectId) return;
        if (snap.currentItem.gameObject.CompareTag("GoodTeeth"))
            MarkAsHealed();
    }
}