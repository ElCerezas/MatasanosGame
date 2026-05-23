using System.Runtime.CompilerServices;
using UnityEngine;

public class controllerVCA : MonoBehaviour
{
    private FMOD.Studio.VCA Vca;
    public string VCAName;  


    void Start()
    {
        Vca = FMODUnity.RuntimeManager.GetVCA("vca:/" + VCAName);
    }

    void Update()
    {
        
    }
}
