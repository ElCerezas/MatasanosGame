using UnityEngine;
using TMPro;
using UnityEngine.Localization.Settings;

public class LanguageLogic : MonoBehaviour
{
    public TMP_Dropdown dropdown;
    public int language;
    
    void Start()
    {
        StartCoroutine(LoadLocales());
    }

    System.Collections.IEnumerator LoadLocales()
    {
        yield return LocalizationSettings.InitializationOperation;
        if (LocalizationSettings.InitializationOperation.Status == UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            language = PlayerPrefs.GetInt("language", 0);
            dropdown.value = language;
            setLanguage();
        }
    }

    void Update()
    {
        
    }

    public void setLanguage()
    {
        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[language];
        PlayerPrefs.SetInt ("language",dropdown.value);
        language = dropdown.value;
        
        //Debug.Log("dropdown value: " + dropdown.value);
        //Debug.Log("language value: " + language);
    }
}
