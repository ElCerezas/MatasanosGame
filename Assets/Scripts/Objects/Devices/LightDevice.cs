using UnityEngine;

[RequireComponent(typeof(Light))]
public class LightDevice : PoweredDevice
{
    [SerializeField] bool isEmergencyLight;
    [SerializeField] Light Light;
    private void Awake()
    {
        if (Light == null)
        {
            Light = GetComponent<Light>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn(); 
    }

    public override void Powered()
    {
        if (Light == null) return;

        if (isEmergencyLight)
        {
            Light.enabled = !hasPower.Value;
        }
        else
        {
            Light.enabled = hasPower.Value;
        }
    }
}