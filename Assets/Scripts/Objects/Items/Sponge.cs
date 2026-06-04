using Unity.Netcode;
using UnityEngine;

public class Sponge : NetworkBehaviour
{
    // [SerializeField] Material spongeMat;

    [Header("Puddle Settings")]
    [SerializeField] float cleanPercent = 0.4f;
    [SerializeField] float dirtAmountPerSponge = 0.25f;
    [SerializeField] NetworkVariable<float> dirtynes = new NetworkVariable<float>(0f);

    [Header("Player Interaction Settings")]
    [SerializeField] float playerInteractInterval = 0.25f;
    [SerializeField] float spongeDirtGainFromPlayer = 0.05f;

    [Header("Visual Colors")]
    [SerializeField] Color cleanColor = Color.white;
    [SerializeField] Color dirtyColor = new Color(0.35f, 0.2f, 0.15f);

    private Rigidbody rb;
    private float nextPlayerInteractTime = 0f;

    public override void OnNetworkSpawn()
    {
        /*dirtynes.OnValueChanged += (oldVal, newVal) =>
        {
            if (spongeMat != null)
                spongeMat.color = Color.Lerp(cleanColor, dirtyColor, newVal);
        };

        if (spongeMat != null)
            spongeMat.color = Color.Lerp(cleanColor, dirtyColor, dirtynes.Value);*/
    }

    private void Awake()
    {
        //spongeMat = gameObject.GetComponent<MeshRenderer>().material;
        rb = GetComponent<Rigidbody>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;

        bool isScrubbing = (rb != null) && (rb.linearVelocity.magnitude > 0.1f);
        if (!isScrubbing) return;

        if (other.CompareTag("Puddle"))
        {
            BloodPuddle bP = other.GetComponent<BloodPuddle>();
            if (bP != null && bP.IsActive())
            {
                if (dirtynes.Value >= 1f) return;

                float amountToClean = cleanPercent * Time.deltaTime;
                bP.ReduceBlood(amountToClean);

                float bloodAbsorbed = amountToClean * dirtAmountPerSponge;
                dirtynes.Value = Mathf.Clamp01(dirtynes.Value + bloodAbsorbed);
            }
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (!IsServer) return;

        bool isScrubbing = (rb != null) && (rb.linearVelocity.magnitude > 0.1f);
        if (!isScrubbing) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time >= nextPlayerInteractTime)
            {
                if (collision.gameObject.TryGetComponent(out NetworkObject playerNetObj))
                {
                    nextPlayerInteractTime = Time.time + playerInteractInterval;

                    ScrubPlayerClientRpc(playerNetObj.NetworkObjectId, dirtynes.Value < 1f);

                    if (dirtynes.Value < 1f)
                        dirtynes.Value = Mathf.Clamp01(dirtynes.Value + spongeDirtGainFromPlayer);
                }
            }
        }
    }

    [ClientRpc]
    private void ScrubPlayerClientRpc(ulong playerNetObjectId, bool shouldClean)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(playerNetObjectId, out NetworkObject netObj))
        {
            if (netObj.IsOwner && netObj.TryGetComponent(out PlayerHUDEffects hudEffects))
            {
                if (shouldClean)
                {
                    hudEffects.CleanProgressive();
                }
                else
                {
                    if (Random.value > 0.5f) hudEffects.TriggerBlood();
                    else hudEffects.TriggerMoco();
                }
            }
        }
    }
}