using Unity.Netcode;
using UnityEngine;

public class AlienJawGrabbable : NetworkBehaviour, IGrabbable
{
    [Header("Jaw Bone")]
    [SerializeField] Transform jawBone;
    [SerializeField] Vector3 jawClosedLocalRotation;
    [SerializeField] Vector3 jawOpenLocalRotation;

    [Header("Drag Settings")]
    [SerializeField] float dragSensitivity = 0.8f;
    [SerializeField] float maxDragDistance = 1.5f;

    [Header("Feel")]
    [SerializeField] float smoothSpeed = 8f;
    [SerializeField] float resistanceSpeed = 3f;

    NetworkVariable<float> jawOpenAmount = new NetworkVariable<float>(0f);

    float localTargetAmount;
    NetworkObject grabberNetObj;
    ulong grabberClientId = ulong.MaxValue;
    Vector3 grabStartPosition;
    float grabStartJawAmount;

    public override void OnNetworkSpawn()
    {
        jawOpenAmount.OnValueChanged += (_, next) => localTargetAmount = next;
        localTargetAmount = jawOpenAmount.Value;
    }
    void Update()
    {
        if (IsServer)
        {
            float target;

            if (grabberNetObj != null)
            {
                float delta = grabberNetObj.transform.position.y - grabStartPosition.y;
                target = grabStartJawAmount + (delta * dragSensitivity / maxDragDistance);
            }
            else
            {
                target = 0f;
            }
            float maxD = (grabberNetObj != null ? smoothSpeed : resistanceSpeed) * Time.deltaTime;
            jawOpenAmount.Value = Mathf.MoveTowards(jawOpenAmount.Value, Mathf.Clamp01(target), maxD);
        }
        Quaternion closed = Quaternion.Euler(jawClosedLocalRotation);
        Quaternion open = Quaternion.Euler(jawOpenLocalRotation);

        jawBone.localRotation = Quaternion.Lerp( jawBone.localRotation, Quaternion.Lerp(closed, open, localTargetAmount), Time.deltaTime * smoothSpeed);
    }
    public ulong GetNetworkObjectID() => NetworkObjectId;

    public void AddGrabber(ulong clientId, NetworkObject playerNetObj)
    {
        if (grabberNetObj != null) return;

        grabberClientId = clientId;
        grabberNetObj = playerNetObj;
        grabStartPosition = playerNetObj.transform.position;
        grabStartJawAmount = jawOpenAmount.Value;
    }

    public void RemoveGrabber(ulong clientId)
    {
        if (clientId != grabberClientId) return;

        grabberNetObj = null;
        grabberClientId = ulong.MaxValue;
    }
}