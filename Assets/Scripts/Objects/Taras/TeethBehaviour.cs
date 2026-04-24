using System;
using UnityEngine;

public class TeethBehaviour : TaraBase
{    
    public override void OnNetworkSpawn()
    {
        EventBus.Subscribe<OnDienteSnap>(OnDienteSnapHandler);
        base.OnNetworkSpawn();
    }

    private void OnDienteSnapHandler(OnDienteSnap snap)
    {
        if (snap.currentItem.gameObject.CompareTag("GoodTheet"))
        {
            MarkAsHealed();
        }
    }

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
