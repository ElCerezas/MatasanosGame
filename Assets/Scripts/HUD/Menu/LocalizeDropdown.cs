using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocalizeDropdown : MonoBehaviour
{
    [SerializeField] List<LocalizedString> dropdownOptions;
    private TMP_Dropdown tmpDropdown;
    
    private void Awake()
    {
        tmpDropdown = GetComponent<TMP_Dropdown>();
        RefreshOptions();
    }
    
    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += RefreshOptions;
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= RefreshOptions;
    }
    private void RefreshOptions(Locale newLocale = null)
    {
        int currentValue = tmpDropdown.value;
        bool wasExpanded = tmpDropdown.IsExpanded;

        if (wasExpanded)
            tmpDropdown.Hide();

        var options = new List<TMP_Dropdown.OptionData>(dropdownOptions.Count);
        for (int i = 0; i < dropdownOptions.Count; i++)
        {
            options.Add(new TMP_Dropdown.OptionData(
                dropdownOptions[i].GetLocalizedString()
            ));
        }

        tmpDropdown.options = options;
        tmpDropdown.value = Mathf.Clamp(currentValue, 0, options.Count - 1);
        tmpDropdown.RefreshShownValue();

        if (wasExpanded)
            tmpDropdown.Show();
    }
}
