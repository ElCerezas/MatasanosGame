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
    public float verticalTolerance = 0.2f;
    public float raycastSphereRadius = 10f;
    public int maxRaycastAttempts = 20;
    public Vector3 sphereCenterOffset = Vector3.zero;

    [Header("Wound Collision Prevention")]
    public float minWoundDistance = 0.65f;

    [Header("Debug")]
    [SerializeField] bool drawDebugRays = false;
    [Range(0.5f, 10f)] public float gizmoSize = 2f;

    private struct DebugRay
    {
        public Vector3 origin;
        public Vector3 hitPoint;
        public bool hit;
        public bool validPlacement;
        public Vector3 normal;
    }

    private List<DebugRay> _debugRays = new();
    private List<Vector3> _spawnedWoundPositions = new();

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _spawnedWoundPositions.Clear();
            _debugRays.Clear();
            StartCoroutine(SpawnWoundsNextFrame());
        }
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

    Vector3 GetSphereCenter()
    {
        SkinnedMeshRenderer smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null)
            return smr.bounds.center + sphereCenterOffset;
        return transform.position + sphereCenterOffset;
    }

    void TryGenerateRandomWound()
    {
        Vector3 sphereCenter = GetSphereCenter();
        for (int attempt = 0; attempt < maxRaycastAttempts; attempt++)
        {
            Vector3 randomDir = Random.onUnitSphere;
            Vector3 origin = sphereCenter + randomDir * raycastSphereRadius;
            Vector3 direction = (sphereCenter - origin).normalized;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, raycastSphereRadius, layerMask))
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
                {
                    float verticalAlignment = Mathf.Abs(Vector3.Dot(hit.normal, Vector3.up));
                    bool isValid = verticalAlignment < verticalTolerance;

                    if (isValid && !IsOverlappingWithExistingWounds(hit.point))
                    {
                        _debugRays.Add(new DebugRay { origin = origin, hitPoint = hit.point, hit = true, validPlacement = true, normal = hit.normal });
                        GenerateWound(hit.point, hit.normal);
                        return;
                    }
                    else
                    {
                        _debugRays.Add(new DebugRay { origin = origin, hitPoint = hit.point, hit = true, validPlacement = isValid, normal = hit.normal });
                    }
                }
            }
            else
            {
                Vector3 endPoint = origin + direction * raycastSphereRadius;
                _debugRays.Add(new DebugRay { origin = origin, hitPoint = endPoint, hit = false, validPlacement = false, normal = Vector3.zero });
            }
        }
    }

    bool IsOverlappingWithExistingWounds(Vector3 position)
    {
        foreach (Vector3 woundPos in _spawnedWoundPositions)
        {
            if (Vector3.Distance(position, woundPos) < minWoundDistance)
                return true;
        }
        return false;
    }

    public void GenerateWound(Vector3 position, Vector3 normal)
    {
        if (!IsServer) return;
        if (woundFocoPrefab == null || woundBandagePrefab == null) return;

        Quaternion baseRotation = Quaternion.LookRotation(-normal);
        Quaternion correctedRotation = baseRotation * Quaternion.Euler(-90f, 0f, 0f);

        GameObject woundObj = Random.Range(0, 2) == 0
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
        _spawnedWoundPositions.Add(position);

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

        Vector3 center = GetSphereCenter();

        Gizmos.color = new Color(0.5f, 0.7f, 1f, 0.15f);
        Gizmos.DrawWireSphere(center, raycastSphereRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(center, 0.2f);

        float angleLimit = Mathf.Asin(verticalTolerance) * Mathf.Rad2Deg;
        DrawToleranceRing(center, raycastSphereRadius, angleLimit, 40, new Color(0f, 1f, 0f, 0.8f));
        DrawToleranceRing(center, raycastSphereRadius, -angleLimit, 40, new Color(0f, 1f, 0f, 0.8f));

        foreach (DebugRay ray in _debugRays)
        {
            if (ray.hit)
            {
                if (ray.validPlacement)
                    Gizmos.color = new Color(0f, 1f, 0f, 0.7f);
                else
                    Gizmos.color = new Color(1f, 0f, 0f, 0.7f);

                Gizmos.DrawLine(ray.origin, ray.hitPoint);
                Gizmos.DrawSphere(ray.hitPoint, gizmoSize * 0.08f);
            }
            else
            {
                Gizmos.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
                Gizmos.DrawLine(ray.origin, ray.hitPoint);
                Gizmos.DrawSphere(ray.origin, gizmoSize * 0.05f);
            }
        }

        if (_spawnedWoundPositions.Count > 0)
        {
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.4f);
            foreach (Vector3 woundPos in _spawnedWoundPositions)
                Gizmos.DrawSphere(woundPos, minWoundDistance);
        }
    }

    void DrawToleranceRing(Vector3 center, float radius, float angleDegrees, int segments, Color color)
    {
        Gizmos.color = color;
        float heightOffset = radius * Mathf.Sin(angleDegrees * Mathf.Deg2Rad);
        float adjustedRadius = radius * Mathf.Cos(angleDegrees * Mathf.Deg2Rad);
        Vector3 lastPoint = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float theta = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Sin(theta) * adjustedRadius;
            float z = Mathf.Cos(theta) * adjustedRadius;
            Vector3 currentPoint = center + new Vector3(x, heightOffset, z);

            if (i > 0)
                Gizmos.DrawLine(lastPoint, currentPoint);
            lastPoint = currentPoint;
        }
    }
}