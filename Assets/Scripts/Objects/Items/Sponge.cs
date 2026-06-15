using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Sponge : NetworkBehaviour
{
    [Header("Sponge Settings")]
    [SerializeField] NetworkVariable<float> dirtynes = new NetworkVariable<float>(0f);
    [SerializeField] Renderer sponjeRenderer;
    Material mat;
    Rigidbody rb;

    [Header("Puddle Settings")]
    [SerializeField] float cleanPercent = 0.4f;
    [SerializeField] float dirtAmountPerPuddle = 0.25f;

    [Header("Player Interaction Settings")]
    [SerializeField] float playerInteractInterval = 0.25f;
    [SerializeField] float spongeDirtGainFromPlayer = 0.05f;
    float nextPlayerInteractTime = 0f;

    [Header("Particles")]
    [SerializeField] ParticleSystem soapParticles;
    [SerializeField] ParticleSystem cleanParticles;
    [SerializeField] Gradient cleanGrad;
    [SerializeField] Gradient dirtyGrad;
    NetworkVariable<bool> isScrubbingSync = new NetworkVariable<bool>(false);
    float lastScrubTime = 0f;
    float scrubTimeout = 0.1f;

    [Header("Audio")]
    [SerializeField] private FMODUnity.EventReference scrubSound;
    [SerializeField] private float soundInterval = 0.35f;
    private Coroutine scrubSoundCoroutine;

    private void Start()
    {
        mat = sponjeRenderer.material;
    }
    public override void OnNetworkSpawn()
    {
        dirtynes.OnValueChanged += (oldVal, newVal) =>
        {
            UpdateParticleColor(newVal);
        };

        isScrubbingSync.OnValueChanged += (oldVal, newVal) =>
        {
            if (soapParticles != null)
            {
                if (newVal && !soapParticles.isPlaying)
                    soapParticles.Play();
                else if (!newVal && soapParticles.isPlaying)
                    soapParticles.Stop();
            }

            if (newVal)
            {
                if (scrubSoundCoroutine != null) StopCoroutine(scrubSoundCoroutine);
                scrubSoundCoroutine = StartCoroutine(PlayScrubSoundRoutine());
            }
            else
            {
                if (scrubSoundCoroutine != null)
                {
                    StopCoroutine(scrubSoundCoroutine);
                    scrubSoundCoroutine = null;
                }
            }
        };

        UpdateParticleColor(dirtynes.Value);
    }

    private IEnumerator PlayScrubSoundRoutine()
    {
        while (isScrubbingSync.Value)
        {
            if (!scrubSound.IsNull)
            {
                FMODUnity.RuntimeManager.PlayOneShotAttached(scrubSound, gameObject);
            }
            
            yield return new WaitForSeconds(soundInterval);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (scrubSoundCoroutine != null) StopCoroutine(scrubSoundCoroutine);
        base.OnNetworkDespawn();
    }

    private void OnDestroy()
    {
        if (scrubSoundCoroutine != null) StopCoroutine(scrubSoundCoroutine);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!IsOwner) return;

        bool currentlyScrubbing = Time.time <= lastScrubTime + scrubTimeout;
        if (isScrubbingSync.Value != currentlyScrubbing)
            UpdateScrubbingRpc(currentlyScrubbing);
    }
    void OnTriggerStay(Collider other)
    {
        if (!IsOwner) return;

        bool isScrubbingMovement = (rb != null) && (rb.linearVelocity.magnitude > 0.1f);
        if (!isScrubbingMovement) return;

        if (other.CompareTag("Puddle"))
        {

            BloodPuddle bP = other.GetComponent<BloodPuddle>();
            if (bP != null && bP.IsActive())
            {
                lastScrubTime = Time.time;

                if (dirtynes.Value >= 1f) return;

                float amountToClean = cleanPercent * Time.deltaTime;
                CleanPuddleRpc(bP.GetNetworkObjectID(), amountToClean);
            }
        }
    }
    void OnCollisionStay(Collision collision)
    {
        if (!IsOwner) return;

        bool isScrubbingMovement = (rb != null) && (rb.linearVelocity.magnitude > 0.1f);
        if (!isScrubbingMovement) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            lastScrubTime = Time.time;

            if (Time.time >= nextPlayerInteractTime)
            {
                if (collision.gameObject.TryGetComponent(out NetworkObject playerNetObj))
                {
                    nextPlayerInteractTime = Time.time + playerInteractInterval;

                    ScrubPlayerRpc(playerNetObj.NetworkObjectId, dirtynes.Value < 1f);
                }
            }
        }
    }
    void UpdateParticleColor(float dirtLevel)
    {
        Color currentStart = Color.Lerp(cleanGrad.Evaluate(0f), dirtyGrad.Evaluate(0f), dirtLevel);
        Color currentEnd = Color.Lerp(cleanGrad.Evaluate(1f), dirtyGrad.Evaluate(1f), dirtLevel);

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(currentStart, 0.0f), new GradientColorKey(currentEnd, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, 1.0f) }
        );

        ParticleSystem.MainModule mainModule = soapParticles.main;
        mainModule.startColor = new ParticleSystem.MinMaxGradient(gradient);
        mat.SetFloat("_DetailAlbedoMapScale", dirtynes.Value);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void CleanPuddleRpc(ulong puddleNetworkObjectId, float amountToClean)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(puddleNetworkObjectId, out NetworkObject puddleNetObj)) return;

        if (puddleNetObj.TryGetComponent(out BloodPuddle bloodPuddle))
        {
            bloodPuddle.ReduceBlood(amountToClean);
            
            if (dirtynes.Value < 1f)
            {
                float bloodAbsorbed = amountToClean * dirtAmountPerPuddle;
                dirtynes.Value = Mathf.Clamp01(dirtynes.Value + bloodAbsorbed);
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ScrubPlayerRpc(ulong playerNetObjectId, bool shouldClean)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(playerNetObjectId, out NetworkObject netObj)) return;

        ScrubPlayerClientRpc(playerNetObjectId, shouldClean);

        if (shouldClean && dirtynes.Value < 1f)
            dirtynes.Value = Mathf.Clamp01(dirtynes.Value + spongeDirtGainFromPlayer);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void UpdateScrubbingRpc(bool isScrubbing)
    {
        isScrubbingSync.Value = isScrubbing;
    }

    [Rpc(SendTo.ClientsAndHost)]
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
    public void FullyCleanSponge()
    {
        if (!IsOwner) return;
        cleanParticles.Play();
        FullyCleanSpongeRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void FullyCleanSpongeRpc()
    {
        dirtynes.Value = 0f;
    }
}