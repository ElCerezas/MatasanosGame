using Unity.Netcode;
using UnityEngine;

public class BloodMachine : PoweredDevice
{
    [Header("Machine Settings")]
    [SerializeField] private float fillRate = 1f;
    [SerializeField] private float fillQuantity = 1f;
    [SerializeField] private float maxBloodbagFillTime = 90f;

    [Header("BloodBags")]
    [SerializeField] private SnapZone[] snapZones;
    [SerializeField] private Transform[] injectionPins;

    private float[] fillTimers;

    public override void Powered()
    {
    }

    void Start()
    {
        fillTimers = new float[snapZones.Length];
    }

    void Update()
    {
        if (!IsServer) return;
        if (!hasPower.Value) return;

        for (int i = 0; i < snapZones.Length; i++)
        {
            SnapZone zone = snapZones[i];

            if (zone.currentItem == null) continue;

            BloodBag bag = zone.currentItem.GetComponentInParent<BloodBag>();
            if (bag == null) continue;

            if (bag.IsFull) continue;

            fillTimers[i] += Time.deltaTime;
            if (fillTimers[i] >= fillRate)
            {
                fillTimers[i] -= fillRate;
                bag.AddBlood(fillQuantity);
            }
        }
    }
}