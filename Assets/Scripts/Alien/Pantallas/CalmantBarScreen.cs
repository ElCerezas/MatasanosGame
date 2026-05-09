using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CalmantBarScreen : NetworkBehaviour
{
    [SerializeField] private Image fill;
    [SerializeField] private GameObject CalmantBarUI;
    [SerializeField] private float lerpSpeed = 10f;
    
    private float targetFill;

    public override void OnNetworkSpawn()
    {
        EventBus.Subscribe<OnCalmantChanged>(UpdateUI);
        EventBus.Subscribe<OnAlienCalmantUsed>(Activate);
    }

    public override void OnNetworkDespawn()
    {
        EventBus.Unsubscribe<OnCalmantChanged>(UpdateUI);
        EventBus.Unsubscribe<OnAlienCalmantUsed>(Activate);
    }

    private void Activate(OnAlienCalmantUsed used)
    {
        CalmantBarUI.SetActive(true);
    }

    private void UpdateUI(OnCalmantChanged e)
    {
        targetFill = e.MaxCalmant > 0f ? e.CurrentCalmant / e.MaxCalmant : 0f;
        
        if (e.CurrentCalmant <= 0f)
        {
            CalmantBarUI.SetActive(false);
        }
    }

    void Update()
    {
        fill.fillAmount = Mathf.Lerp(fill.fillAmount, targetFill, Time.deltaTime * lerpSpeed);
    }
}