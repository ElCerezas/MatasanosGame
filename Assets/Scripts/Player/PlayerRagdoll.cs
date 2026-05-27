using UnityEngine;
public class PlayerRagdoll : MonoBehaviour
{
    Rigidbody[] ragdollBodies;
    Collider[] colliders;
    Animator animator;

    void Awake()
    {
        ragdollBodies = GetComponentsInChildren<Rigidbody>();
        colliders = GetComponentsInChildren<Collider>();
        animator = GetComponent<Animator>();
        SetRagdoll(false);
    }

    public void SetRagdoll(bool active)
    {
        animator.enabled = !active;
        foreach (var rb in ragdollBodies)
            rb.isKinematic = !active;
        foreach (var col in colliders)
            col.enabled = active;
    }

    public void ApplyForce(Vector3 force)
    {
        ragdollBodies[0].AddForce(force, ForceMode.Impulse);
    }
}