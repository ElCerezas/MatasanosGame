using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingSlider : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI labelText;
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI valueText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [Header("Configuración para Sliders ya en Escena")]
    [SerializeField] private bool isOnScene = false;
    [SerializeField] private string claveDelSetting;

    private string settingKey;

    private void Start()
    {
        if (isOnScene)
        {
            GameSetting setting = SettingsManager.Instance.GetSettingInfo(claveDelSetting);
            if (setting != null)
            {
                Initialize(setting);
            }
            else
            {
                Debug.LogWarning($"No se encontró la configuración para la clave: {claveDelSetting}");
            }
        }
    }

    public void Initialize(GameSetting setting)
    {
        settingKey = setting.key;
        labelText.text = setting.key;
        
        slider.onValueChanged.RemoveAllListeners();

        slider.minValue = setting.minValue;
        slider.maxValue = setting.maxValue;
        
        float savedValue = SettingsManager.Instance.GetSetting(setting.key);
        slider.SetValueWithoutNotify(savedValue); 

        if (setting.description != null && setting.description != "")
        {
            descriptionText.text = setting.description;
        }

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