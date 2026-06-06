using Unity.Netcode;
using UnityEngine;

public class GeneratorSystem : NetworkBehaviour
{
    [Header("Generator Settings")]
    public int maxPowerLoad = 10;
    public NetworkVariable<int> currentLoad = new NetworkVariable<int>(0);
    public NetworkVariable<bool> isGeneratorOn = new NetworkVariable<bool>(true);

    [Header("PowerLoad System")]
    [SerializeField] PowerEmitter[] mainEmitters;

    [Header("Effects")]
    [SerializeField]ParticleSystem[] particleSystemsOnFailure;

    public override void OnNetworkSpawn()
    {
        isGeneratorOn.OnValueChanged += (oldVal, newVal) =>
           {
               EventBus.Publish(new GeneratorEvent { IsGeneratorOn = newVal });
           };
        currentLoad.OnValueChanged += (oldVal, newVal) =>
        {
            EventBus.Publish(new OnGeneratorChargeChanged { CurrentCharge = newVal, MaxCharge = maxPowerLoad });

        };
        
        if (IsServer)
        {
            foreach (var emitter in mainEmitters)
                emitter.SetEmitting(isGeneratorOn.Value);

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
            isGeneratorOn.Value = false;
            foreach (var emitter in mainEmitters)
            {
                emitter.SetEmitting(false);
            }
            foreach (var pS in particleSystemsOnFailure)
            {
                pS.Play();
            }

            currentLoad.Value = 0;
        }
    }
    public void TurnOnGenerator()
    {
        if (!IsServer) return;
        isGeneratorOn.Value = true;
        foreach (var emitter in mainEmitters)
        {
            emitter.SetEmitting(true);
        }
        foreach (var pS in particleSystemsOnFailure)
        {
            pS.Stop();
        }
        EvaluateLoad();
    }
}