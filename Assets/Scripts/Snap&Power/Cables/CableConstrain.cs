using Unity.Netcode;
using UnityEngine;
public class CableConstraint : NetworkBehaviour
{
    [Header("Endpoints")]
    [SerializeField] Rigidbody plugRb;
    [SerializeField] Rigidbody socketHolderRb;

    [Header("Cable Settings")]
    [SerializeField] float maxLength = 3f;
    [SerializeField] float stiffness = 800f;
    [SerializeField] float damping = 40f;

    public NetworkVariable<Vector3> NetPlugPos = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector3> NetSocketPos = new NetworkVariable<Vector3>(default, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void FixedUpdate()
    {
        if (plugRb == null || socketHolderRb == null) return;
        if (!IsServer) return;

        ApplyConstraint();
        NetPlugPos.Value = plugRb.position;
        NetSocketPos.Value = socketHolderRb.position;
    }

    void ApplyConstraint()
    {
        Vector3 delta = socketHolderRb.position - plugRb.position;
        float distance = delta.magnitude;

        if (distance <= maxLength) return;

        float excess = distance - maxLength;
        Vector3 direction = delta / distance;

        Vector3 relativeVel = socketHolderRb.linearVelocity - plugRb.linearVelocity;
        float velAlongCable = Vector3.Dot(relativeVel, direction);

        float forceMag = (excess * stiffness) + (velAlongCable * damping);
        forceMag = Mathf.Max(0f, forceMag);
        Vector3 force = direction * forceMag;
        if (!plugRb.isKinematic)
            plugRb.AddForce(force, ForceMode.Force);

        if (!socketHolderRb.isKinematic)
            socketHolderRb.AddForce(-force, ForceMode.Force);
    }
}