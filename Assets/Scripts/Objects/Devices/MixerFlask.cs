using Unity.Netcode;
using UnityEngine;

public class MixerFlask : NetworkBehaviour
{
    private Material m;
    private NetworkVariable<float> fillAmount = new NetworkVariable<float>(0f);
    public NetworkVariable<LiquidType> liquid = new NetworkVariable<LiquidType>( LiquidType.Empty);
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
        m.SetFloat("_FillAmount", newVal);
    }
}
