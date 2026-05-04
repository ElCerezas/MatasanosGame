using Unity.Netcode;
using UnityEngine;

public class WoundVendaBehaviour : TaraBase
{
    private NetworkVariable<bool> isDesinfected = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isHealed = new NetworkVariable<bool>(false);

    public void DesinfectedByCotton()
    {
        if (!isDesinfected.Value)
        {
            isDesinfected.Value = true;
            Debug.Log("Desinfectado");
        }
    }
    public void HealedByBandage()
    {
        if (isDesinfected.Value)
        {
            isHealed.Value = true;
            MarkAsHealed();
            GetComponent<NetworkObject>().Despawn();
            Destroy(gameObject);
        }
    }
}
