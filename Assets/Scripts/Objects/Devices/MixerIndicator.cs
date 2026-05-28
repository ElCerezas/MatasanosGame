using UnityEngine;

public class MixerIndicator : MonoBehaviour
{
    [SerializeField] private GameObject Empty;
    [SerializeField] private GameObject Level1;
    [SerializeField] private GameObject Level2;
    [SerializeField] private GameObject Level3;
    [SerializeField] private GameObject Level4;

    private int currentLevel = 0; 

    public void UpdateIndicator(int level)
    {
        currentLevel = level;

        if (gameObject.activeInHierarchy)
        {
            ApplyVisuals();
        }
    }

    private void OnEnable()
    {
        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        Empty.SetActive(currentLevel == 0);
        Level1.SetActive(currentLevel == 1);
        Level2.SetActive(currentLevel == 2);
        Level3.SetActive(currentLevel == 3);
        Level4.SetActive(currentLevel == 4);
    }
}