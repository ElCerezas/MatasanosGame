using Unity.Netcode;
using UnityEngine;
[RequireComponent(typeof(InteractableOutline))]
[RequireComponent(typeof(NetworkObject))]
public class CustomitzationMachineButton : InteractableItem
{
    [SerializeField]CharacterCustomitationMachine CCM;
    [SerializeField]bool isColor, isEyes, isMouth, isHat;
    [SerializeField] bool plusIndex;
    public override void Interact(ulong clientID)
    {
        base.Interact(clientID);
        if (isColor)
            CCM.ChangeColor(clientID, plusIndex ? 1 : -1);
        if (isEyes)
            CCM.ChangEyes(clientID, plusIndex ? 1 : -1);
        if (isMouth)
            CCM.ChangMouth(clientID, plusIndex? 1 : -1);
        if (isHat)
            CCM.ChangeHat(clientID, plusIndex ? 1 : -1);
    }
}
