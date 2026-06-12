using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(InteractableOutline))]
[RequireComponent(typeof(NetworkObject))]
public class ColorButton : InteractableItem
{
    UnityEvent<String> onInteractButton;
    [SerializeField]CharacterCustomitationMachine CCM;
    [SerializeField]bool nextColor = false;
    public override void Interact(ulong clientID)
    {
        base.Interact(clientID);
    }
}
