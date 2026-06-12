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

    [Header("Pins")]
    [SerializeField] AnimationCurve pinMoveCurve;
    [SerializeField] float pinRaiseHeight;
    [SerializeField] private float pinMoveSpeed = 2f;

    private float[] fillTimers;
    private float[] pinProgress;
    private Vector3[] pinBaseLocalPositions;

    public override void Powered()
    {
    }
    void Start()
    {
        fillTimers = new float[snapZones.Length];
        pinProgress = new float[snapZones.Length];

        pinBaseLocalPositions = new Vector3[injectionPins.Length];
        for (int i = 0; i < injectionPins.Length; i++)
        {
            pinBaseLocalPositions[i] = injectionPins[i].localPosition;
        }
    }

    void Update()
    {
        HandlePinAnimations();

        if (!IsServer) return;
        if (!hasPower.Value) return;

        HandleFilling();
    }
    private void HandleFilling()
    {
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

    private void HandlePinAnimations()
    {
        for (int i = 0; i < snapZones.Length; i++)
        {
            bool shouldBeUp = false;
            SnapZone zone = snapZones[i];

            if (zone.currentItem != null)
            {
                BloodBag bag = zone.currentItem.GetComponentInParent<BloodBag>();
                if (bag != null && !bag.IsFull && hasPower.Value)
                    shouldBeUp = true;
            }

            if (shouldBeUp)
                pinProgress[i] += Time.deltaTime * pinMoveSpeed;
            else
                pinProgress[i] -= Time.deltaTime * pinMoveSpeed;

            pinProgress[i] = Mathf.Clamp01(pinProgress[i]);
            float curveValue = pinMoveCurve.Evaluate(pinProgress[i]);

            Vector3 targetPos = pinBaseLocalPositions[i];
            targetPos.y += curveValue * pinRaiseHeight;

            injectionPins[i].localPosition = targetPos;
        }
    }
}