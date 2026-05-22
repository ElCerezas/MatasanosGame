using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class WoundVendaBehaviour : TaraBase
{
    private NetworkVariable<bool> isDesinfected = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> isHealed = new NetworkVariable<bool>(false);
    private ColliderDetector colliderInteracttable;
    public DecalProjector woundProjector;
    public Material tiritaMaterial;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        EventBus.Publish(new TaraCreated { TaraID = NetworkObjectId, Type = type });
    }
    private void Awake()
    {
        colliderInteracttable = gameObject.GetComponent<ColliderDetector>();
    }
    public void DesinfectedByCotton()
    {
        if (!isDesinfected.Value)
        {
            colliderInteracttable.detectorType = ColliderDetectorType.HeridaDesinfectada;
            isDesinfected.Value = true;
            Debug.Log("Desinfectada herida");
        }
    }
    public void HealedByBandage()
    {
        if (isDesinfected.Value)
        {
            isHealed.Value = true;
            MarkAsHealed();
            woundProjector.material = tiritaMaterial;
        }
    }
}
