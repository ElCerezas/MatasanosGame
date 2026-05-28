using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class MixerMachine : PoweredDevice
{
    [Header("Settings")]
    [SerializeField]int max_Component = 5;
    [SerializeField] MeshRenderer[] tubes;
    NetworkVariable<int> componentA = new NetworkVariable<int>(1);
    NetworkVariable<int> componentB = new NetworkVariable<int>(1);
    NetworkVariable<int> componentC = new NetworkVariable<int>(1);
    [SerializeField] private MixerIndicator componentAIndicator;
    [SerializeField] private MixerIndicator componentBIndicator;
    [SerializeField] private MixerIndicator componentCIndicator;
    

    [Header("Liquids")]
    [SerializeField] Color tranquilizerColor;
    [SerializeField] NetworkVariable<Vector3Int> tranquilizerFormula = new NetworkVariable<Vector3Int>();
    [SerializeField] Color stimulantColor;
    [SerializeField] NetworkVariable<Vector3Int> stimulantFormula = new NetworkVariable<Vector3Int>();
    [SerializeField] NetworkVariable<Vector3Int> betadineFormula = new NetworkVariable<Vector3Int>();
    [SerializeField] Color betadineColor;
    [SerializeField] Color sludgeColor;

    [Header("Liquid Flask")]
    [SerializeField] MixerFlask MixerFlask;

    public Vector3Int CurrentTranquilizerFormula => tranquilizerFormula.Value;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        tranquilizerFormula.OnValueChanged += (_, newVal) => EventBus.Publish(new OnFormulaGenerated { tranquilizerFormula = newVal });
        
        if (IsServer)
        {
            tranquilizerFormula.Value = new Vector3Int(Random.Range(1,max_Component), Random.Range(2, max_Component), Random.Range(1, max_Component));
            stimulantFormula.Value = new Vector3Int(Random.Range(1,max_Component), Random.Range(2, max_Component), Random.Range(1, max_Component));
            betadineFormula.Value = new Vector3Int(1, 1, 1);
        }
        componentA.OnValueChanged += (_, val) => OnComponentChanged(0, val);
        componentB.OnValueChanged += (_, val) => OnComponentChanged(1, val);
        componentC.OnValueChanged += (_, val) => OnComponentChanged(2, val);

        if (IsServer)
        {
            componentA.Value = 1;
            componentB.Value = 1;
            componentC.Value = 1;
        }
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
        } if (current == betadineFormula.Value)
        {
            liquidType = LiquidType.Betadine;
            liquidColor = betadineColor;
        }
        else
        {
            liquidType = LiquidType.Sludge;
            liquidColor = sludgeColor;
        }

        MixerFlask?.Fill(liquidType, liquidColor);
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
        componentAIndicator.UpdateIndicator(componentA.Value);
        componentBIndicator.UpdateIndicator(componentB.Value);
        componentCIndicator.UpdateIndicator(componentC.Value);
    }
}
