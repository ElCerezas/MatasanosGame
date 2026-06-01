using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AlienWoundManager : NetworkBehaviour
{
    public GameObject woundFocoPrefab;
    public GameObject woundBandagePrefab;
    public int initialWounds = 0;
    public LayerMask layerMask;

    [Header("Wound Placement")]
    [Range(0f, 1f)]
    public float minHeightBias = 0.2f;
    public int maxRaycastAttempts = 10;

    [Header("Debug")]
    [SerializeField] bool drawDebugRays = false;
    private List<(Vector3 origin, Vector3 direction, bool hit)> _debugRays = new();
    private Vector3 _debugSphereCenter;
    private float _debugSphereRadius = 10f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
            StartCoroutine(SpawnWoundsNextFrame());
    }

    System.Collections.IEnumerator SpawnWoundsNextFrame()
    {
        yield return null;
        SpawnInitialWounds(initialWounds);
    }

    void SpawnInitialWounds(int count)
    {
        for (int i = 0; i < count; i++)
        {
            TryGenerateRandomWound();
        }
    }

    void TryGenerateRandomWound()
    {
        _debugSphereCenter = transform.position;
        for (int attempt = 0; attempt < maxRaycastAttempts; attempt++)
        {
            Vector3 randomDir = UnityEngine.Random.onUnitSphere * 10;
            bool didHit = false;

            Vector3 origin = transform.position + randomDir;
            Vector3 direction = (transform.position - origin).normalized;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, 10f, layerMask))
            {
                Debug.DrawRay(origin, direction);
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
                {
                    if (Vector3.Dot(hit.normal, Vector3.down) < minHeightBias)
                    {
                        didHit = true;
                        _debugRays.Add((origin, direction, true));
                        GenerateWound(hit.point, hit.normal);
                        return;
                    }
                }
            }

            if (!didHit)
                _debugRays.Add((origin, direction, false));
        }
    }

    public void GenerateWound(Vector3 position, Vector3 normal)
    {
        if (!IsServer) return;

        if (woundFocoPrefab == null || woundBandagePrefab == null)
        {
            Debug.LogError("WoundPrefab no asignado", this);
            return;
        }

        Quaternion baseRotation = Quaternion.LookRotation(-normal);
        Quaternion correctedRotation = baseRotation * Quaternion.Euler(-90f, 0f, 0f);

        GameObject woundObj;
        int random = UnityEngine.Random.Range(0, 2);
        woundObj = random == 0
            ? Instantiate(woundFocoPrefab, position, correctedRotation)
            : Instantiate(woundBandagePrefab, position, correctedRotation);

        NetworkObject netObj = woundObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Destroy(woundObj);
            return;
        }

        netObj.Spawn();
        netObj.TrySetParent(transform, worldPositionStays: true);

        Transform closestBone = GetClosestBone(position);
        if (closestBone != null)
        {
            WoundBoneFollower follower = woundObj.GetComponent<WoundBoneFollower>();
            if (follower != null)
            {
                follower.AttachToBone(closestBone);
                follower.boneName.Value = closestBone.name;
            }
        }
    }

    Transform GetClosestBone(Vector3 worldPos)
    {
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null) return null;

        Transform closest = null;
        float minDist = float.MaxValue;

        foreach (Transform bone in smr.bones)
        {
            if (bone == null) continue;
            float dist = Vector3.Distance(worldPos, bone.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = bone;
            }
        }
        return closest;
    }

    void OnDrawGizmos()
    {
        if (!drawDebugRays) return;

        Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
        Gizmos.DrawSphere(_debugSphereCenter, _debugSphereRadius);

        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(_debugSphereCenter, _debugSphereRadius);

        foreach (var (origin, direction, wasHit) in _debugRays)
        {
            Gizmos.color = wasHit ? Color.green : Color.red;
            Gizmos.DrawSphere(origin, 0.05f);

            Gizmos.color = wasHit
                ? new Color(0f, 1f, 0f, 0.8f)
                : new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawRay(origin, direction * 6f);
        }
    }
}