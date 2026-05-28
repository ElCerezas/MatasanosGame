using Unity.Netcode;
using UnityEngine;

public class AlienJawGrabbable : NetworkBehaviour, IGrabbable
{
    [Header("Force Settings")]
    [SerializeField] float springForce = 100f;
    [SerializeField] float forceToFullyOpen = 150f;
    [SerializeField] float breakDistance = 5f;

    [Header("Feel")]
    [SerializeField] float smoothSpeed = 8f;
    [SerializeField] float resistanceSpeed = 3f;

    [Header("Animation")]
    [SerializeField] Animator jawAnimator;

    [SerializeField] NetworkVariable<float> jawOpenAmount = new NetworkVariable<float>(0f);

    float currentVisualAmount;
    Transform grabberHoldPoint;
    ulong grabberClientId = ulong.MaxValue;
    float initialGrabDistance;

    public override void OnNetworkSpawn()
    {
        currentVisualAmount = jawOpenAmount.Value;
    }

    void Update()
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
        }
        if (jawAnimator != null)
        {
            float lerpSpeed = grabberHoldPoint != null ? smoothSpeed : resistanceSpeed;
            currentVisualAmount = Mathf.Lerp(currentVisualAmount, jawOpenAmount.Value, Time.deltaTime * lerpSpeed);
            jawAnimator.SetFloat("MouthOpenPercent", currentVisualAmount);
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
}