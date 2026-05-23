using UnityEngine;
using UnityEngine.UI;

public class BrightnessLogic : MonoBehaviour
{

    public Slider slider;
    public float sliderValue;
    public Image panelBrillo;

    private void Start()
    {
        slider.value = PlayerPrefs.GetFloat("brillo", 0.5f);
        panelBrillo.color = new Color(panelBrillo.color.r, panelBrillo.color.g, panelBrillo.color.b, slider.value);
    }

    public void ChangeSlider (float valor)
    {
        slider.value = valor;
        PlayerPrefs.SetFloat("brillo", sliderValue);
        panelBrillo.color = new Color (panelBrillo.color.r,panelBrillo.color.g,panelBrillo.color.b,slider.value);
    }
}
