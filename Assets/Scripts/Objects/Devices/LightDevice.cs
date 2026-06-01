using UnityEngine;

[RequireComponent (typeof(Light))]
public class LightDevice : PoweredDevice
{
    [SerializeField] bool isEmergencyLight;
    [SerializeField] Light Light;

    private void Start()
    {
        if (Light == null)
            Light = GetComponent<Light>();
    }
    public override void Powered()
    {
        Light.enabled = isEmergencyLight ? !hasPower.Value : hasPower.Value;
    }

}
