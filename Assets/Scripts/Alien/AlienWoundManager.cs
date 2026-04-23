using Unity.Netcode;
using UnityEngine;

public class AlienWoundManager : NetworkBehaviour
{
    public GameObject woundPrefab;
    public int initialWounds = 3;

    [Header("Wound Placement")]
    [Range(0f, 1f)]
    public float minHeightBias = 0.2f;
    public int maxRaycastAttempts = 10;

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
        for (int attempt = 0; attempt < maxRaycastAttempts; attempt++)
        {
            Vector3 randomDir = Random.onUnitSphere;

            randomDir.y = Mathf.Abs(randomDir.y) * (1f - minHeightBias) + minHeightBias;
            randomDir.Normalize();

            Vector3 origin = transform.position + randomDir * 3f;
            Vector3 direction = (transform.position - origin).normalized;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, 6f))
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
                {
                    GenerateWound(hit.point, hit.normal);
                    return;
                }
            }
        }
    }

    public void GenerateWound(Vector3 position, Vector3 normal)
    {
        if (!IsServer) return;

        if (woundPrefab == null)
        {
            Debug.LogError("WoundPrefab no asignado", this);
            return;
        }

        Quaternion baseRotation = Quaternion.LookRotation(-normal);

        Quaternion correctedRotation = baseRotation * Quaternion.Euler(-90f, 0f, 0f);

        GameObject woundObj = Instantiate(woundPrefab, position, correctedRotation);

        NetworkObject netObj = woundObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("El woundPrefab no tiene NetworkObject", woundObj);
            Destroy(woundObj);
            return;
        }

        netObj.Spawn();
        netObj.TrySetParent(transform, worldPositionStays: true);
    }
}