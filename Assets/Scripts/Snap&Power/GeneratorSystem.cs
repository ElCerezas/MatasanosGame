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

    public override void OnNetworkSpawn()
    {
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
        //Debug.Log("2");
        if (!isGeneratorOn.Value) return;
       // Debug.Log("1");
        int calculatedLoad = 0;
        foreach (var emitter in mainEmitters)
        {
            //Debug.Log("AAAAAAAAAAAAAAAAAAAAAAAAAAAA");
            calculatedLoad += emitter.GetLoad();
        }
        currentLoad.Value = calculatedLoad;
        //Debug.Log($"PowerLoad: {currentLoad.Value} / {maxPowerLoad}");

        if (currentLoad.Value > maxPowerLoad)
        {
            //Debug.LogWarning("Sobrecarga: BoOoOm");
            isGeneratorOn.Value = false;
            foreach (var emitter in mainEmitters)
            {
                emitter.SetEmitting(false);
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
        EvaluateLoad();
    }
}