using Unity.Netcode;
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(NetworkObject))]
public class Estornudo : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] Transform attackOrigin;
    [SerializeField] float coneRange = 5f;
    [SerializeField] float coneAngle = 45f;
    [SerializeField] float attackForce = 2000f;
    [SerializeField] float ragdollDuration = 3f;
    [SerializeField] float rotationForce = 500f;
    [SerializeField] LayerMask targetLayer;
    [SerializeField] bool showDebugCone = true;
    [SerializeField] private ParticleSystem estornudoParticles;

    private void Awake()
    {
        if (attackOrigin == null)
            attackOrigin = transform;
    }

    public void ExecuteEstornudo()
    {
        if (!IsServer)
        {
            ExecuteEstornudoServerRpc();
            return;
        }

        PerformEstornudo();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ExecuteEstornudoServerRpc()
    {
        PerformEstornudo();
    }

    private void PerformEstornudo()
    {
        Vector3 origin = attackOrigin.position;
        Vector3 direction = attackOrigin.forward;

        Collider[] hitColliders = Physics.OverlapSphere(origin, coneRange, targetLayer);

        foreach (Collider hitCollider in hitColliders)
        {
            if (hitCollider.gameObject == gameObject)
                continue;

            Vector3 directionToTarget = (hitCollider.transform.position - origin).normalized;
            float angleToTarget = Vector3.Angle(direction, directionToTarget);

            if (angleToTarget <= coneAngle / 2f)
            {
                NetworkObject targetNetObj = hitCollider.GetComponent<NetworkObject>();
                if (targetNetObj != null)
                {
                    ApplyEstornudoEffectClientRpc(targetNetObj.NetworkObjectId, directionToTarget);
                }
            }
        }

        EstornudoVFXClientRpc();
    }

    [ClientRpc]
    private void ApplyEstornudoEffectClientRpc(ulong targetNetObjectId, Vector3 pushDirection)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetObjectId, out NetworkObject targetNetObj))
            return;

        Rigidbody targetRb = targetNetObj.GetComponent<Rigidbody>();
        PlayerStateManager stateManager = targetNetObj.GetComponent<PlayerStateManager>();

        if (stateManager != null)
        {
            stateManager.EnterRagdollPublic(ragdollDuration);
        }

        if (targetRb != null)
        {
            targetRb.isKinematic = false;
            targetRb.constraints = RigidbodyConstraints.None;
            targetRb.freezeRotation = false;
            targetRb.linearVelocity = Vector3.zero;
            targetRb.angularVelocity = Vector3.zero;

            targetNetObj.StartCoroutine(DelayedForce(targetRb, pushDirection));
        }
    }

    private IEnumerator DelayedForce(Rigidbody rb, Vector3 direction)
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        Vector3 backwardDirection = direction; 
        backwardDirection.y = 0.5f;

        rb.AddForce(backwardDirection.normalized * attackForce, ForceMode.Impulse);
        rb.AddTorque(rb.transform.forward * rotationForce, ForceMode.Impulse);
        EstornudoEndVFXClientRpc();
    }

    [ClientRpc]
    private void EstornudoVFXClientRpc()
    {
        // SFX y VFX aquí
        estornudoParticles.gameObject.SetActive(true);
    }
    [ClientRpc] 
    private void EstornudoEndVFXClientRpc()
    {
        estornudoParticles.gameObject.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugCone) return;
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        Vector3 direction = attackOrigin != null ? attackOrigin.forward : transform.forward;
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(origin, coneRange);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + direction * coneRange);
        Vector3 coneEdge1 = Quaternion.AngleAxis(coneAngle / 2f, (attackOrigin != null ? attackOrigin.up : transform.up)) * direction * coneRange;
        Vector3 coneEdge2 = Quaternion.AngleAxis(-coneAngle / 2f, (attackOrigin != null ? attackOrigin.up : transform.up)) * direction * coneRange;
        Gizmos.DrawLine(origin, origin + coneEdge1);
        Gizmos.DrawLine(origin, origin + coneEdge2);
        int arcSegments = 10;
        for (int i = 0; i < arcSegments; i++)
        {
            float angle1 = (-coneAngle / 2f) + (coneAngle / arcSegments) * i;
            float angle2 = (-coneAngle / 2f) + (coneAngle / arcSegments) * (i + 1);
            Vector3 point1 = Quaternion.AngleAxis(angle1, (attackOrigin != null ? attackOrigin.up : transform.up)) * direction * coneRange;
            Vector3 point2 = Quaternion.AngleAxis(angle2, (attackOrigin != null ? attackOrigin.up : transform.up)) * direction * coneRange;
            Gizmos.DrawLine(origin + point1, origin + point2);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugCone) return;
        Vector3 origin = attackOrigin != null ? attackOrigin.position : transform.position;
        Vector3 direction = attackOrigin != null ? attackOrigin.forward : transform.forward;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, origin + direction * coneRange);
    }
}