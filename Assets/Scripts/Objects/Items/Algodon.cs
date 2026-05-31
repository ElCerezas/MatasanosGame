using Unity.Netcode;
using UnityEngine;

public class Algodon : NetworkBehaviour
{
    private ColliderInteractItem colliderItem;
    private NetworkVariable<bool> isWithBetadine= new NetworkVariable<bool>(false);
    private MeshRenderer meshRenderer;
    [SerializeField] Material algodon;
    [SerializeField] Material algodonBetadine;
    public override void OnNetworkSpawn()
    {
        isWithBetadine.OnValueChanged += (oldVal, newVal) =>
        {
            if (newVal)
                meshRenderer.material = algodonBetadine;
            else
                meshRenderer.material = algodon;
                
        };
    }

    private void Awake()
    {
        colliderItem = gameObject.GetComponent<ColliderInteractItem>();
        meshRenderer = gameObject.GetComponent<MeshRenderer>();
    }

    public void Desinfectar()
    {
        colliderItem.itemType = ColliderItemType.AlgodonEstirilizado;
        isWithBetadine.Value = true;
        //AlgodonChangeMeshClientRpc();
    }
    public void AlgodonInfectado()
    {
        colliderItem.itemType = ColliderItemType.Algodon;
        isWithBetadine.Value = false;
        //AlgodonChangeMeshClientRpc();
    }

    [ClientRpc]
    private void AlgodonChangeMeshClientRpc()
    {
        if (isWithBetadine.Value)
        {
            meshRenderer.material = algodonBetadine;
        }
        else
        {
            meshRenderer.material = algodon;
        }
    }
}
