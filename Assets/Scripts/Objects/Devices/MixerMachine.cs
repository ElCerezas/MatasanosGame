using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder.Shapes;

[RequireComponent(typeof(NetworkObject))]
public class MixerMachine : PoweredDevice
{
    [Header("Settings")]
    [SerializeField]int max_Component = 5;
    [SerializeField] MeshRenderer[] tubes;
    NetworkVariable<int> componentA = new NetworkVariable<int>(1);
    NetworkVariable<int> componentB = new NetworkVariable<int>(1);
    NetworkVariable<int> componentC = new NetworkVariable<int>(1);

    [Header("Liquids")]
    [SerializeField] Color tranquilizerColor;
    NetworkVariable<Vector3Int> tranquilizerFormula = new NetworkVariable<Vector3Int>();
    [SerializeField] Color stimulantColor;
    NetworkVariable<Vector3Int> stimulantFormula = new NetworkVariable<Vector3Int>();
    [SerializeField] Color sludgeColor;

    [Header("Liquid Flask")]
    [SerializeField] NetworkObject bottlePrefab;
    [SerializeField] private Transform bottleSpawnPoint;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer)
        {
            tranquilizerFormula.Value = new Vector3Int(Random.Range(1,max_Component), Random.Range(1, max_Component), Random.Range(1, max_Component));
            stimulantFormula.Value = new Vector3Int(Random.Range(1,max_Component), Random.Range(1, max_Component), Random.Range(1, max_Component));
        }
        componentA.OnValueChanged += (_, val) => OnComponentChanged(0, val);
        componentB.OnValueChanged += (_, val) => OnComponentChanged(1, val);
        componentC.OnValueChanged += (_, val) => OnComponentChanged(2, val);
        UpdateUI();
    }
    public override void OnNetworkDespawn()
    {
        componentA.OnValueChanged -= (_, val) => OnComponentChanged(0, val);
        componentB.OnValueChanged -= (_, val) => OnComponentChanged(1, val);
        componentC.OnValueChanged -= (_, val) => OnComponentChanged(2, val);
        base.OnNetworkDespawn();
    }
    public override void Powered()
    {
        UpdateUI();
    }

    public void IncrementComponent(int index)
    {
        if (!hasPower.Value) return;
        switch (index)
        {
            case 0: componentA.Value = componentA.Value >= max_Component ? 1 : componentA.Value + 1; break;
            case 1: componentB.Value = componentB.Value >= max_Component ? 1 : componentB.Value + 1; break;
            case 2: componentC.Value = componentC.Value >= max_Component ? 1 : componentC.Value + 1; break;
        }
    }
    public void ConfirmMix()
    {
        if (!hasPower.Value) return;

        Vector3Int current = new Vector3Int(componentA.Value, componentB.Value, componentC.Value);
        LiquidType liquidType;
        Color liquidColor;
        if (current == tranquilizerFormula.Value)
        {
            liquidType = LiquidType.Calmante;
            liquidColor = tranquilizerColor;
        }  
        else if (current == stimulantFormula.Value)
        {
            liquidType = LiquidType.Estimulante;
            liquidColor = stimulantColor;
        }
        else
        {
            liquidType = LiquidType.Sludge;
            liquidColor = sludgeColor;
        }

        componentA.Value = 1;
        componentB.Value = 1;
        componentC.Value = 1;
    }

    void OnComponentChanged(int index, int newValue)
    {
        tubes[index].sharedMaterial.SetFloat("_FillAmount", (float)newValue / max_Component);
        UpdateUI();
    }
    private void UpdateUI()
    {
        //TO DO
    }
}
