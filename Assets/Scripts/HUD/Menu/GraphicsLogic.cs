using UnityEngine;
using TMPro;

public class GraphicsLogic : MonoBehaviour
{

    public TMP_Dropdown dropdown;

    public int calidad;

    private void Start()
    {
        calidad = PlayerPrefs.GetInt("numeroDeCalidad", 3);
        dropdown.value = calidad;
        AjustarCalidad();
    }

    private void Update()
    {
        
    }

    public void AjustarCalidad ()
    {
        QualitySettings.SetQualityLevel(dropdown.value);
        PlayerPrefs.SetInt ("numeroDeCalidad",dropdown.value);
        calidad = dropdown.value;
    }

}
