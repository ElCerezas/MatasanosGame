using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Transform settingsContainer;
    [SerializeField] private GameObject sliderPrefab;
    [SerializeField] private string PanelName;

    private void Start()
    {
        CreateSettings();
    }

    private void CreateSettings()
    {
        foreach (var setting in SettingsManager.Instance.GetAllSettings())
        {
            if (setting.isOnScene == false && PanelName == setting.panelName)
            {
                var sliderGO = Instantiate(sliderPrefab, settingsContainer);
                var group = sliderGO.GetComponent<SettingSlider>();
                group.Initialize(setting);
            }
        }
    }
}