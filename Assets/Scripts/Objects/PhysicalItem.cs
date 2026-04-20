using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkRigidbody))]
public class PhysicalItem : NetworkBehaviour, IGrabbable
{
    Rigidbody rb;
    public Transform holdPoint;
    [SerializeField] private bool isTool = false;
    float damping = 5f; //Amortiguació
    float springForce = 100f; //Força de braç
    float breakDistance = 5f;

    Dictionary<ulong, Transform> grabbers = new Dictionary<ulong, Transform>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (!IsServer || grabbers.Count == 0) return;

        Vector3 netForce = Vector3.zero;
        List<ulong> brokenGrabs = new List<ulong>();

        foreach (var kvp in grabbers)
        {
            if (kvp.Value == null) continue;
            Transform hPoint = kvp.Value;
            if (hPoint == null)
                brokenGrabs.Add(kvp.Key);

            Vector3 directionToTarget = hPoint.position - rb.position;
            float distance = directionToTarget.magnitude;
            if (distance > breakDistance)
                brokenGrabs.Add(kvp.Key);

            //Llei de hook tete => ForçaFinal = springForce * direction
            Vector3 fResult = directionToTarget * springForce;
            netForce += fResult;

            // Si es tool, alinear rotación al forward horizontal del holdPoint
            if (isTool)
            {
                rb.MovePosition(hPoint.position);

                Vector3 forward = hPoint.forward;
                forward.y = 0f;
                if (forward != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(forward);
                    rb.MoveRotation(targetRotation);
                }
                continue; // Saltar el cálculo de fuerzas
            }
        }
        foreach (ulong clientId in brokenGrabs)
        {
            RemoveGrabber(clientId);
        }

        if (grabbers.Count > 0)
        {
            netForce += -rb.linearVelocity * damping; //Amortiguacio per evitar pilota orbitant
            rb.AddForce(netForce, ForceMode.Force);
        }
    }
    public void AddGrabber(ulong clientId, Transform holdPoint)
    {
        if (!grabbers.ContainsKey(clientId))
        {
            grabbers.Add(clientId, holdPoint);
            rb.isKinematic = false;
        }
        if (isTool)
            NotifyHoldingToolClientRpc(true, clientId);
    }
    public void RemoveGrabber(ulong clientId)
    {
        if (grabbers.ContainsKey(clientId))
        {
            grabbers.Remove(clientId);
        }
        if (isTool)
            NotifyHoldingToolClientRpc(false, clientId);
    }

    // Notifica solo al cliente
    [ClientRpc]
    private void NotifyHoldingToolClientRpc(bool holding, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        PlayerCamera cam = FindFirstObjectByType<PlayerCamera>();
        if (cam != null && cam.IsOwner)
            cam.SetHoldingTool(holding);
    }
    public ulong GetNetworkObjectID()
    {
        return NetworkObjectId;
    }

}