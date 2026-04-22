using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class AlienHealthbarUI : NetworkBehaviour
{
    public Image fill;
    private float targetFill;
    public override void OnNetworkSpawn()
    {
        EventBus.Subscribe<OnAlienHealthChanged>(UpdateUI);
    }
    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<OnAlienHealthChanged>(UpdateUI);
    }
    private void UpdateUI(OnAlienHealthChanged e)
    {
        targetFill = e.CurrentHealth / e.MaxHealth;
    }
    void Update()
    {
        fill.fillAmount = Mathf.Lerp(fill.fillAmount, targetFill, Time.deltaTime * 10f);
    }
    public void SetFill(float Pct, float MaxPct)
    {
        targetFill = (MaxPct > 0f) ? Pct / MaxPct : 0f;
    }
    
}
