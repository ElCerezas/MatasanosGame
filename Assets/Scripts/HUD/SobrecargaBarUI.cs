using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SobrecargaBarUI : NetworkBehaviour
{
    public Image fill;
    private float targetFill;
    public override void OnNetworkSpawn()
    {
        EventBus.Subscribe<OnGeneratorChargeChanged>(UpdateUI);
        var generator = FindFirstObjectByType<GeneratorSystem>();
        if (generator != null)
            targetFill = (float)generator.currentLoad.Value / generator.maxPowerLoad;
    }
    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<OnGeneratorChargeChanged>(UpdateUI);
    }
    private void UpdateUI(OnGeneratorChargeChanged e)
    {
        targetFill = e.CurrentCharge / e.MaxCharge;
    }
    void Update()
    {
        fill.fillAmount = Mathf.Lerp(fill.fillAmount, targetFill, Time.deltaTime * 5f);
    }

}
