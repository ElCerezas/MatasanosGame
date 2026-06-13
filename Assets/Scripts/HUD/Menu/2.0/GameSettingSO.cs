using UnityEngine;

[System.Serializable]
public class GameSetting
{
    public bool isOnScene;
    public string key;
    public float defaultValue;
    public float minValue = 0f;
    public float maxValue = 10f;
    public string panelName;
    [TextArea] public string description;
}

[CreateAssetMenu(fileName = "GameSettingsConfig", menuName = "Game/Settings Config")]
public class GameSettingsConfig : ScriptableObject
{
    [SerializeField] public GameSetting[] settings;

    public GameSetting GetSetting(string key)
    {
        foreach (var setting in settings)
            if (setting.key == key) return setting;
        return null;
    }
}