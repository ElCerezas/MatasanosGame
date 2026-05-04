
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEditor;
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



    // Añade estas variables para guardar el estado de debug
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
            Vector3 randomDir = UnityEngine.Random.onUnitSphere*10;


            bool didHit = false;

            //randomDir.y = Mathf.Abs(randomDir.y) * (1f - minHeightBias) + minHeightBias;
            //randomDir.Normalize();
            Vector3 origin = transform.position + randomDir;
            Vector3 direction = (transform.position - origin).normalized;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, 10f, layerMask))
            {
                Debug.DrawRay(origin, direction);
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
                {
                    Debug.Log(hit.collider.transform == transform);
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

        if (woundFocoPrefab == null || woundFocoPrefab == null)
        {
            Debug.LogError("WoundPrefab no asignado", this);
            return;
        }

        Quaternion baseRotation = Quaternion.LookRotation(-normal);

        Quaternion correctedRotation = baseRotation * Quaternion.Euler(-90f, 0f, 0f);
        GameObject woundObj;
        int random = UnityEngine.Random.Range(0, 2);
        Debug.Log(random);
        if (random == 0)
        {
            woundObj = Instantiate(woundFocoPrefab, position, correctedRotation);
        }
        else woundObj = Instantiate(woundBandagePrefab, position, correctedRotation);


        NetworkObject netObj = woundObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            //Debug.LogError("El woundPrefab no tiene NetworkObject", woundObj);
            Destroy(woundObj);
            return;
        }

        netObj.Spawn();
        netObj.TrySetParent(transform, worldPositionStays: true);
    }

    void OnDrawGizmos()
    {
        // Dibuja la esfera de muestreo
        Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
        Gizmos.DrawSphere(_debugSphereCenter, _debugSphereRadius);

        Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
        Gizmos.DrawWireSphere(_debugSphereCenter, _debugSphereRadius);

        // Dibuja cada raycast
        foreach (var (origin, direction, wasHit) in _debugRays)
        {
            // Punto de origen del rayo
            Gizmos.color = wasHit ? Color.green : Color.red;
            Gizmos.DrawSphere(origin, 0.05f);

            // Línea del rayo
            Gizmos.color = wasHit
                ? new Color(0f, 1f, 0f, 0.8f)
                : new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawRay(origin, direction * 6f);
        }
    }
}