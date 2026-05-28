using UnityEngine;
using Unity.Netcode;
using System;
public class FormulaHandlerUI : NetworkBehaviour
{
    [SerializeField] private MixerIndicator componentAIndicator;
    [SerializeField] private MixerIndicator componentBIndicator;
    [SerializeField] private MixerIndicator componentCIndicator;
    [SerializeField] private MixerMachine mixerMachine;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        EventBus.Subscribe<OnFormulaGenerated>(UpdateFormula);

        if (mixerMachine != null)
        {
            ForceFormulaVisuals(mixerMachine.CurrentTranquilizerFormula);
        }
    }

    private void UpdateFormula(OnFormulaGenerated onFormulaGenerated)
    {
        ForceFormulaVisuals(onFormulaGenerated.tranquilizerFormula);
    }

    private void ForceFormulaVisuals(Vector3Int formula)
    {
        componentAIndicator.UpdateIndicator(formula.x);
        componentBIndicator.UpdateIndicator(formula.y);
        componentCIndicator.UpdateIndicator(formula.z);
    }
}
