using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;

public class FullScreenLogic : MonoBehaviour
{

    public Toggle toggle;
    public TMP_Dropdown resolucionesDropDown;
    Resolution [] resoluciones; 

    private void Start()
    {
        if (Screen.fullScreen)
        {
            toggle.isOn = true;
        }
        else
        {
            toggle.isOn = false;
        }

        Resolucion();
    }

    private void Update()
    {
        
    }

    public void ActivarPantallaCompleta (bool isPantallaCompleta)
    {
        Screen.fullScreen = isPantallaCompleta;
    }

    public void Resolucion ()
    {
        resoluciones = Screen.resolutions;
        resolucionesDropDown.ClearOptions ();
        List <string> opciones = new List<string> ();
        int resolucionActual = 0;

        for (int i = 0; i < resoluciones.Length; i++)
        {
            string opcion = resoluciones[i].width + "x" + resoluciones[i].height;
            opciones.Add (opcion);


            if (Screen.fullScreen && resoluciones[i].width == Screen.currentResolution.width &&
                resoluciones[i].height == Screen.currentResolution.height)
            {
                resolucionActual = i;
            }

        }

        resolucionesDropDown.AddOptions (opciones);
        resolucionesDropDown.value = resolucionActual;
        resolucionesDropDown.RefreshShownValue ();

        resolucionesDropDown.value = PlayerPrefs.GetInt("numeroDeResoluciones", 0);
    }

    public void CambiarResolucion (int indiceResolucion)
    {
        PlayerPrefs.SetInt("numeroDeResoluciones",resolucionesDropDown.value);

        Resolution resolucion = resoluciones[indiceResolucion];
        Screen.SetResolution(resolucion.width,resolucion.height, Screen.fullScreen);
    }

    public void SalirJuego ()
    {
        Application.Quit();
    }

}
