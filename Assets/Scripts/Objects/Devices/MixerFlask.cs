using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class MixerFlask : NetworkBehaviour
{
    private Material m;
    private NetworkVariable<float> fillAmount = new NetworkVariable<float>(0f);
    public NetworkVariable<LiquidType> liquid = new NetworkVariable<LiquidType>(LiquidType.Empty);
    public NetworkVariable<Color> color = new NetworkVariable<Color>(Color.white);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        m = GetComponent<Renderer>().material;
        fillAmount.OnValueChanged += (_, val) => UpdateLiquid(val);
        color.OnValueChanged += (_, _) => UpdateLiquid(fillAmount.Value);

        UpdateLiquid(fillAmount.Value);
    }

    public void Fill(LiquidType t, Color c)
    {
        if (!IsServer) return;
        liquid.Value = t;
        color.Value = c;
        fillAmount.Value = 1f;
        if (t == LiquidType.Betadine)
        {
            ColliderDetector col = GetComponent<ColliderDetector>();
            col.detectorType = ColliderDetectorType.Betadine;
        }
    }

    public void Empty(float amount = 1f)
    {
        if (!IsServer) return;
        fillAmount.Value = Mathf.Clamp(fillAmount.Value - amount, 0f, 1f);
        if (fillAmount.Value <= 0f)
            liquid.Value = LiquidType.Empty;
    }

    private void UpdateLiquid(float newVal)
    {
        if (m == null) return;
        m.SetColor("_Color", color.Value);
        m.SetFloat("_FillAmount", Mathf.Min(newVal, 0.95f));
    }
    public void StartFilling(LiquidType t, Color c, float totalDuration, float initialFill)
    {
        if (IsServer) StartCoroutine(FillCoroutine(t, c, totalDuration, initialFill));
    }

    private IEnumerator FillCoroutine(LiquidType t, Color c, float totalDuration, float initialFill)
    {
        liquid.Value = t;
        color.Value = c;

        fillAmount.Value = initialFill;

        float elapsed = 0f;
        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;
            fillAmount.Value = Mathf.Lerp(initialFill, 1f, elapsed / totalDuration);
            yield return null;
        }

        fillAmount.Value = 1f;

        if (t == LiquidType.Betadine)
        {
            ColliderDetector col = GetComponent<ColliderDetector>();
            if (col != null) col.detectorType = ColliderDetectorType.Betadine;
        }
    }
}
