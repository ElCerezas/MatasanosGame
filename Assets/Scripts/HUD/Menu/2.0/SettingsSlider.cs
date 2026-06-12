using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class SettingSlider : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private string settingKey;

    public void Initialize(GameSetting setting)
    {
        settingKey = setting.key;
        labelText.text = setting.key;
        slider.minValue = setting.minValue;
        slider.maxValue = setting.maxValue;
        slider.value = SettingsManager.Instance.GetSetting(setting.key);
        descriptionText.text = setting.description;

        slider.onValueChanged.AddListener(OnSliderChanged);
        UpdateValueText();
    }

    private void OnSliderChanged(float value)
    {
        SettingsManager.Instance.SetSetting(settingKey, value);
        UpdateValueText();
    }

    private void UpdateValueText()
    {
        valueText.text = slider.value.ToString("F2");
    }
}