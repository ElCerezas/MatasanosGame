using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(InteractableOutline))]
[RequireComponent(typeof(NetworkObject))]
public class ColorButton : InteractableItem
{
    UnityEvent<String> onInteractButton;
    public override void Interact(ulong clientID)
    {
        string clientString = clientID.ToString();
        onInteractButton.Invoke(clientString);
    }
}
