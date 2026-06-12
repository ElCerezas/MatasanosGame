using UnityEngine;
using System.Collections.Generic;
using System;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [SerializeField] private GameSettingsConfig settingsConfig;
    private Dictionary<string, float> settingsData = new();

    public event Action<string, float> OnSettingChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    private void LoadSettings()
    {
        foreach (var setting in settingsConfig.settings)
        {
            float value = PlayerPrefs.GetFloat(setting.key, setting.defaultValue);
            settingsData[setting.key] = value;
        }
    }

    public float GetSetting(string key)
    {
        return settingsData.TryGetValue(key, out var value) ? value : 0f;
    }

    public void SetSetting(string key, float value)
    {
        if (!settingsData.ContainsKey(key))
            return;

        value = Mathf.Clamp(value, 
            settingsConfig.GetSetting(key).minValue,
            settingsConfig.GetSetting(key).maxValue);

        settingsData[key] = value;
        PlayerPrefs.SetFloat(key, value);
        PlayerPrefs.Save();

        OnSettingChanged?.Invoke(key, value);
    }

    public GameSetting GetSettingInfo(string key) => settingsConfig.GetSetting(key);

    public GameSetting[] GetAllSettings() => settingsConfig.settings;
}