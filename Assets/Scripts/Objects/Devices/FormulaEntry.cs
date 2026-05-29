using UnityEngine;
public class FormulaEntryUI : MonoBehaviour
{
    [SerializeField] private MixerIndicator componentA;
    [SerializeField] private MixerIndicator componentB;
    [SerializeField] private MixerIndicator componentC;

    public void Setup(Vector3Int formula)
    {
        componentA.UpdateIndicator(formula.x);
        componentB.UpdateIndicator(formula.y);
        componentC.UpdateIndicator(formula.z);
    }
}