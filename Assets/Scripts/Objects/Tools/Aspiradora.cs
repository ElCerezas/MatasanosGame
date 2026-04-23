using Unity.Netcode;
using UnityEngine;

public class Aspiradora : PoweredItem
{
    [SerializeField] GameObject conoAsp;
    public override void Interact(ulong clientID)
    {
        base.Interact(clientID);
    }
    public virtual void Update()
    {
        if (!IsServer) return;
        conoAsp.GetComponent<Renderer>().enabled = isTurnedOn.Value;

        if (!isTurnedOn.Value) return;
        //Acción

    }

}
