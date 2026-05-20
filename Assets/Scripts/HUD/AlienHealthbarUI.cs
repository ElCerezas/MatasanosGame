using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class AlienHealthbarUI : NetworkBehaviour
{
    public TextMeshProUGUI fill;
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
        targetFill = e.CurrentHealth / e.MaxHealth * 100f;
    }
    void Update()
    {
        fill.text = targetFill.ToString() + "%";
    }
    
}
