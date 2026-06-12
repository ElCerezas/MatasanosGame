using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Transform settingsContainer;
    [SerializeField] private GameObject sliderPrefab;

    private void Start()
    {
        CreateSettings();
    }

    private void CreateSettings()
    {
        foreach (var setting in SettingsManager.Instance.GetAllSettings())
        {
            var sliderGO = Instantiate(sliderPrefab, settingsContainer);
            var group = sliderGO.GetComponent<SettingSlider>();
            group.Initialize(setting);
        }
    }
}