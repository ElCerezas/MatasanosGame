using UnityEngine;
using Unity.Netcode;

public class EnergyProvider : NetworkBehaviour
{
    private void Start()
    {
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("enteredCollider");
        PowerableItem powerableItem;
        Debug.Log(powerableItem = other.gameObject.GetComponent<PowerableItem>());
        if (powerableItem = other.gameObject.GetComponent<PowerableItem>())
        {
            Debug.Log("Powered");
            powerableItem.EnablePower();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        PowerableItem powerableItem;
        if (powerableItem = other.gameObject.GetComponent<PowerableItem>())
        {
            powerableItem.DisablePower();
        }
    }
}
