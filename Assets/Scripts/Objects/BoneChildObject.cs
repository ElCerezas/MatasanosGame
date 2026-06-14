using Unity.Netcode;
using UnityEngine;

public class BoneChildObject : NetworkBehaviour
{
    [SerializeField] Transform BoneParent;
    [SerializeField] Vector3 offset;
    [SerializeField] Vector3 rotationOffset;
    private void LateUpdate()
    {
        gameObject.transform.position = BoneParent.position + offset;
        gameObject.transform.rotation = BoneParent.rotation * Quaternion.Euler(rotationOffset);

    }
}
