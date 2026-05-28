using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCustomization : NetworkBehaviour
{
    //[SerializeField] Scrollbar scrollbar;
    //private int currentStep = 0;
    
    /*void Awake()
    {
        DontDestroyOnLoad(gameObject);
        scrollbar.onValueChanged.AddListener(OnScrollChange);
    }
    void OnScrollChange(float value)
    {
        int step = Mathf.RoundToInt(value * (scrollbar.numberOfSteps - 1));
        if (currentStep != step)
        {
            currentStep = step;
            Debug.Log(currentStep);
        }
    }

    public int GetStep()
    {
        return currentStep;
    }
    private void OnDestroy()
    {
        scrollbar.onValueChanged.RemoveListener(OnScrollChange);
    }*/
}
