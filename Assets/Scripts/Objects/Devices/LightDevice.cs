using UnityEngine;

[RequireComponent (typeof(Light))]
public class LightDevice : PoweredDevice
{
    [SerializeField] bool isEmergencyLight;
    [SerializeField] Light light;

    private void Start()
    {
        if (light == null)
            light = GetComponent<Light>();
    }
    public override void Powered()
    {
        light.enabled = isEmergencyLight ? !hasPower.Value : hasPower.Value;
    }

}
