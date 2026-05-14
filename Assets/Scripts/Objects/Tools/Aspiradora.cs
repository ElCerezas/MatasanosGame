using Unity.Netcode;
using UnityEngine;

public class Aspiradora : PoweredItem
{
    public NetworkVariable<float> fillAmount = new NetworkVariable<float>();
    public float maxCapacity = 5f;

    public void TryAddCapacity(BloodPuddle puddle)
    {
        if(fillAmount.Value < maxCapacity)
        {
            fillAmount.Value++;
            puddle.Clean();
        }
    }
    public void ClearStorage()
    {
        fillAmount.Value = 0f;
    }
}
