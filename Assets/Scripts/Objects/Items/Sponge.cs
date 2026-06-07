using Unity.Netcode;
using UnityEngine;

public class Sponge : NetworkBehaviour
{
    [Header("Sponge Settings")]
    // [SerializeField] Material spongeMat;
    [SerializeField] NetworkVariable<float> dirtynes = new NetworkVariable<float>(0f);
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
    [SerializeField] Gradient cleanGrad;
    [SerializeField] Gradient dirtyGrad;
    NetworkVariable<bool> isScrubbingSync = new NetworkVariable<bool>(false);
    float lastScrubTime = 0f;
    float scrubTimeout = 0.1f;


    public override void OnNetworkSpawn()
    {
        dirtynes.OnValueChanged += (oldVal, newVal) =>
        {
            UpdateParticleColor(newVal);
            /*if (spongeMat != null)
                spongeMat.color = Color.Lerp(cleanColor, dirtyColor, newVal);*/
        };

        isScrubbingSync.OnValueChanged += (oldVal, newVal) =>
        {
            if (soapParticles == null) return;
            if (newVal && !soapParticles.isPlaying)
                soapParticles.Play();
            else if (!newVal && soapParticles.isPlaying)
                soapParticles.Stop();
        };
        UpdateParticleColor(dirtynes.Value);

        /*if (spongeMat != null)
            spongeMat.color = Color.Lerp(cleanColor, dirtyColor, dirtynes.Value);*/
    }
    void Awake()
    {
        //spongeMat = gameObject.GetComponent<MeshRenderer>().material;
        rb = GetComponent<Rigidbody>();
    }
    void Update()
    {
        if (!IsServer) return;

        bool currentlyScrubbing = Time.time <= lastScrubTime + scrubTimeout;
        if (isScrubbingSync.Value != currentlyScrubbing)
            isScrubbingSync.Value = currentlyScrubbing;
    }
    void OnTriggerStay(Collider other)
    {
        if (!IsServer) return;

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
                bP.ReduceBlood(amountToClean);

                float bloodAbsorbed = amountToClean * dirtAmountPerPuddle;
                dirtynes.Value = Mathf.Clamp01(dirtynes.Value + bloodAbsorbed);
            }
        }
    }
    void OnCollisionStay(Collision collision)
    {
        if (!IsServer) return;

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

                    ScrubPlayerClientRpc(playerNetObj.NetworkObjectId, dirtynes.Value < 1f);

                    if (dirtynes.Value < 1f)
                        dirtynes.Value = Mathf.Clamp01(dirtynes.Value + spongeDirtGainFromPlayer);
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