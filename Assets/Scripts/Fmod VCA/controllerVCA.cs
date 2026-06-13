using UnityEngine;
public class controllerVCA : MonoBehaviour
{
    private FMOD.Studio.VCA Vca;
    public string VCAName;

    void Start()
    {
        Vca = FMODUnity.RuntimeManager.GetVCA("vca:/" + VCAName);
        
        string key = VCAName == "Master" ? "Master Volume" : VCAName + " Volume";
        
        float currentVolume = SettingsManager.Instance.GetSetting(key);
        SetVolume(currentVolume);

        SettingsManager.Instance.OnSettingChanged += HandleSettingChanged;
    }

    private void HandleSettingChanged(string changedKey, float value)
    {
        string expectedKey = VCAName == "Master" ? "Master Volume" : VCAName + " Volume";
        
        if (changedKey == expectedKey)
        {
            SetVolume(value);
        }
    }

    public void SetVolume(float volume)
    {
        Vca.setVolume(volume);
    }

    private void OnDestroy()
    {
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.OnSettingChanged -= HandleSettingChanged;
        }
    }
}