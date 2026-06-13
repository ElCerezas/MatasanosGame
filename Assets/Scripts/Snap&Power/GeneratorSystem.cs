using Unity.Netcode;
using UnityEngine;

public class GeneratorSystem : NetworkBehaviour
{
    [Header("Generator Settings")]
    public int maxPowerLoad = 10;
    public NetworkVariable<int> currentLoad = new NetworkVariable<int>(0);
    public NetworkVariable<bool> isGeneratorOn = new NetworkVariable<bool>(true);
    
    [Tooltip("El porcentaje máximo de fallo cuando la carga está a punto de llegar al límite")]
    [SerializeField] float maxChanceOfFailure = 20f;

    [Header("PowerLoad System")]
    [SerializeField] PowerEmitter[] mainEmitters;

    [Header("Effects")]
    [SerializeField] ParticleSystem[] particleSystemsOnFailure;

    public override void OnNetworkSpawn()
    {
        isGeneratorOn.OnValueChanged += (oldVal, newVal) =>
        {
            EventBus.Publish(new GeneratorEvent { IsGeneratorOn = newVal });

            if (newVal == false)
            {
                foreach (var emitter in mainEmitters) emitter.SetEmitting(false);
                foreach (var pS in particleSystemsOnFailure) pS.Play();
            }
            else
            {
                foreach (var emitter in mainEmitters) emitter.SetEmitting(true);
                foreach (var pS in particleSystemsOnFailure) pS.Stop();
            }
        };

        currentLoad.OnValueChanged += (oldVal, newVal) =>
        {
            EventBus.Publish(new OnGeneratorChargeChanged { CurrentCharge = newVal, MaxCharge = maxPowerLoad });
        };
        
        if (IsServer)
        {
            EvaluateLoad();
        }
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