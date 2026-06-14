using Unity.Netcode;
using UnityEngine;

public class AlienJawGrabbable : NetworkBehaviour, IGrabbable
{
    [Header("Force Settings")]
    [SerializeField] private float springForce = 100f;
    [SerializeField] private float forceToFullyOpen = 150f;
    [SerializeField] private float breakDistance = 5f;

    [Header("Feel")]
    [SerializeField] private float smoothSpeed = 8f;
    [SerializeField] private float resistanceSpeed = 3f;

    [Header("Animation")]
    [SerializeField] private Animator jawAnimator;

    [SerializeField] private NetworkVariable<float> jawOpenAmount = new NetworkVariable<float>(0f);
    [SerializeField] private NetworkVariable<bool> isMouthOpen = new NetworkVariable<bool>(false);
    [SerializeField] SnapZone[] snapZones;

    private float currentVisualAmount;
    private Transform grabberHoldPoint;
    private ulong grabberClientId = ulong.MaxValue;
    private float initialGrabDistance;

    public override void OnNetworkSpawn()
    {
        currentVisualAmount = jawOpenAmount.Value;
        isMouthOpen.OnValueChanged += (oldVal, newVal) => UpdateMouthColliders();
        UpdateMouthColliders();
    }

    private void Update()
    {
        if (IsServer)
        {
            float targetOpenAmount = 0f;

            if (grabberHoldPoint != null)
            {
                float currentDistance = Vector3.Distance(transform.position, grabberHoldPoint.position);
                if (currentDistance > breakDistance)
                {
                    RemoveGrabber(grabberClientId);
                }
                else
                {
                    float pullDistance = Mathf.Max(0, currentDistance - initialGrabDistance);
                    float appliedForce = pullDistance * springForce;
                    targetOpenAmount = Mathf.Clamp01(appliedForce / forceToFullyOpen);
                }
            }

            float speed = grabberHoldPoint != null ? smoothSpeed : resistanceSpeed;
            jawOpenAmount.Value = Mathf.MoveTowards(jawOpenAmount.Value, targetOpenAmount, speed * Time.deltaTime);
            isMouthOpen.Value = jawOpenAmount.Value >= 0.8f;
        }

        if (jawAnimator != null)
        {
            float lerpSpeed = grabberHoldPoint != null ? smoothSpeed : resistanceSpeed;
            currentVisualAmount = Mathf.Lerp(currentVisualAmount, jawOpenAmount.Value, Time.deltaTime * lerpSpeed);
            jawAnimator.SetFloat("MouthOpenPercent", currentVisualAmount);
            int layerIndex = jawAnimator.GetLayerIndex("MouthOpener");
            if (layerIndex != -1)
            {
                jawAnimator.SetLayerWeight(layerIndex, currentVisualAmount);
            }
        }
    }

    public ulong GetNetworkObjectID() => NetworkObjectId;

    public void AddGrabber(ulong clientId, NetworkObject playerNetObj)
    {
        if (grabberHoldPoint != null) return;
        if (playerNetObj.TryGetComponent(out PlayerInteractor interactor))
        {
            grabberClientId = clientId;
            grabberHoldPoint = interactor.holdPoint;
            initialGrabDistance = Vector3.Distance(transform.position, grabberHoldPoint.position);
        }
    }

    public void RemoveGrabber(ulong clientId)
    {
        if (clientId != grabberClientId) return;

        grabberHoldPoint = null;
        grabberClientId = ulong.MaxValue;
    }
    void UpdateMouthColliders()
    {
        foreach (SnapZone s in snapZones)
        {
            if (s.TryGetComponent(out Collider snapCollider))
                snapCollider.enabled = isMouthOpen.Value;
            SnappableItem i = s.currentItem;
            if (i == null) continue;
            if (i.TryGetComponent(out Collider itemCollider))
            {
                itemCollider.enabled = isMouthOpen.Value;
            }
        }
    }
}