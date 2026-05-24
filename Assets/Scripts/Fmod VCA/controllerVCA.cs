using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UI;

public class controllerVCA : MonoBehaviour
{
    private FMOD.Studio.VCA Vca;
    public string VCAName;

    private Slider slider;

    void Start()
    {
        Vca = FMODUnity.RuntimeManager.GetVCA("vca:/" + VCAName);
        slider = GetComponent<Slider>();
    }

    void Update()
    {
        
    }


    public void SetVolume (float volume)
    {

        Vca.setVolume(volume);

    }



}
