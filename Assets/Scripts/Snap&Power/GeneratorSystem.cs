using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class GeneratorSystem : NetworkBehaviour
{
    [Header("Generator Settings")]
    public int maxPowerLoad = 10;
    public NetworkVariable<int> currentLoad = new NetworkVariable<int>(0);
    public NetworkVariable<bool> isGeneratorOn = new NetworkVariable<bool>(true);
    [SerializeField] float maxChanceOfFailure = 50f;
    [SerializeField] GameObject generatorSoundEmitter;
    [SerializeField] EventReference generatorGoneSound;

    [Header("PowerLoad System")]
    [SerializeField] PowerEmitter[] mainEmitters;

    [Header("Effects")]
    [SerializeField] ParticleSystem[] particleSystemsOnFailure;

    [Header("Objetos Emisivos")]
    [SerializeField] private Renderer[] objetosEmisivos;
    private LightmapData[] originalLightmaps;
    private LightmapData[] darkLightmaps;
    private float originalAmbientIntensity;
    private Color originalAmbientColor;
    private MaterialPropertyBlock apagadorEmision;

    private void Start()
    {
        originalLightmaps = LightmapSettings.lightmaps;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalAmbientColor = RenderSettings.ambientLight;

        darkLightmaps = new LightmapData[originalLightmaps.Length];
        Texture2D blackTex = Texture2D.blackTexture;

        for (int i = 0; i < originalLightmaps.Length; i++)
        {
            darkLightmaps[i] = new LightmapData { lightmapColor = blackTex };
        }

        apagadorEmision = new MaterialPropertyBlock();
    }

    public override void OnNetworkSpawn()
    {
        isGeneratorOn.OnValueChanged += (oldVal, newVal) =>
        {
            EventBus.Publish(new GeneratorEvent { IsGeneratorOn = newVal });

            if (newVal == false)
            {
                AudioManager.instance.PlayOneShot(generatorGoneSound);
                generatorSoundEmitter.SetActive(true);
                foreach (var emitter in mainEmitters) { 
                    emitter.SetEmitting(false); 
                    emitter.soundEmitter.SetActive(true);
                }
                foreach (var pS in particleSystemsOnFailure) pS.Play();

                LightmapSettings.lightmaps = darkLightmaps;
                
                RenderSettings.ambientIntensity = 0f;
                RenderSettings.ambientLight = Color.black;

                apagadorEmision.SetColor("_EmissionColor", Color.black);
                foreach (var rendererEmisivo in objetosEmisivos)
                {
                    if (rendererEmisivo != null)
                        rendererEmisivo.SetPropertyBlock(apagadorEmision);
                }
            }
            else
            {
                generatorSoundEmitter.SetActive(false);
                foreach (var emitter in mainEmitters) { 
                    emitter.SetEmitting(true); 
                    emitter.soundEmitter.SetActive(false);
                }
                foreach (var pS in particleSystemsOnFailure) pS.Stop();

                LightmapSettings.lightmaps = originalLightmaps;
                RenderSettings.ambientIntensity = originalAmbientIntensity;
                RenderSettings.ambientLight = originalAmbientColor;

                foreach (var rendererEmisivo in objetosEmisivos)
                {
                    if (rendererEmisivo != null)
                        rendererEmisivo.SetPropertyBlock(null);
                }
            }
        };

        currentLoad.OnValueChanged += (oldVal, newVal) =>
        {
            EventBus.Publish(new OnGeneratorChargeChanged { CurrentCharge = newVal, MaxCharge = maxPowerLoad });
        };
        
        if (IsServer)
        {
            EvaluateLoad();
            EventBus.Subscribe<AddEnergyLoad>(OnLoadUpdated);

        }

    }
    public override void OnNetworkDespawn()
    {
        if (IsServer)
            EventBus.Unsubscribe<AddEnergyLoad>(OnLoadUpdated);
    }
    void OnLoadUpdated(AddEnergyLoad s)
    {
        if(IsServer)
            currentLoad.Value += s.energyLoad;
    }
    public void EvaluateLoad()
    {
        if (!IsServer) return;
        if (!isGeneratorOn.Value) return;

        int calculatedLoad = 0;
        foreach (var emitter in mainEmitters)
        {
            calculatedLoad += emitter.GetLoad();
        }
        
        currentLoad.Value = calculatedLoad;

        if (currentLoad.Value >= maxPowerLoad)
        {
            OverloadGenerator();
            return;
        }

        float loadPercentage = Mathf.Clamp01((float)currentLoad.Value / maxPowerLoad);
        float currentChanceToFail = maxChanceOfFailure * loadPercentage;

        if (D100() < currentChanceToFail)
        {
            OverloadGenerator();
        }
    }

    private void OverloadGenerator()
    {
        isGeneratorOn.Value = false;
        currentLoad.Value = 0; 
    }

    public void TurnOnGenerator()
    {
        if (!IsServer) return;
        isGeneratorOn.Value = true;
        EvaluateLoad();
    }

    private int D100()
    {
        return Random.Range(0, 101);
    }
}