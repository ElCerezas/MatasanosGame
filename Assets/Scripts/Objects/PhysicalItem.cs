using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

[RequireComponent(typeof(NetworkRigidbody))]
public class PhysicalItem : NetworkBehaviour, IGrabbable
{
    Rigidbody rb;
    public Transform holdPoint;

    [SerializeField] float dampening = 5f;

    public float springForce = 5f; //NO se declara aqui
    public float breakDistance = 6f; //NO se declara aqui
    Dictionary<ulong, Transform> grabbers = new Dictionary<ulong, Transform>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }
    private void Start()
    {
        springForce = PlayerInteractor.testVar;
    }
    private void FixedUpdate()
    {
        if (!IsServer || grabbers.Count == 0) return;

        Vector3 netForce = Vector3.zero; 
        List<ulong> brokenGrabs = new List<ulong>();

        foreach (var kvp in grabbers) //Calculo de todos los "muelles" que se suman a netForce
        {
            Transform holdPoint = kvp.Value;
            if (holdPoint == null)
            {
                brokenGrabs.Add(kvp.Key);
                continue;
            }

            Vector3 directionToTarget = holdPoint.position - rb.position;
            float distance = directionToTarget.magnitude;
            if (distance > breakDistance)
            {
                brokenGrabs.Add(kvp.Key);
                continue;
            }

            //Llei de hook tete => ForçaFinal = springForce * direction
            Vector3 fResult = directionToTarget * this.springForce;

            //fTotal = Suma(forcesResultat)
            netForce += fResult;
        }
        foreach (ulong clientId in brokenGrabs)
        {
            RemoveGrabber(clientId);
        }

        if (grabbers.Count > 0)
        {
            Vector3 dampingForce = -rb.linearVelocity * dampening;
            netForce += dampingForce;

            rb.AddForce(netForce, ForceMode.Acceleration);
        }
    }
    public void AddGrabber(ulong clientId, Transform holdPoint)
    {
        if (!grabbers.ContainsKey(clientId))
        {
            grabbers.Add(clientId, holdPoint);
            rb.isKinematic = false;
        }
    }
    public void RemoveGrabber(ulong clientId)
    {
        if (grabbers.ContainsKey(clientId))
        {
            grabbers.Remove(clientId);
        }
    }

}
