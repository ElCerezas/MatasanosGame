using UnityEngine;

public class WoundBoneFollower : MonoBehaviour
{
    private Transform targetBone;
    private Vector3 localOffset;
    private Quaternion localRotationOffset;

    public void AttachToBone(Transform bone)
    {
        targetBone = bone;
        localOffset = bone.InverseTransformPoint(transform.position);
        localRotationOffset = Quaternion.Inverse(bone.rotation) * transform.rotation;
    }

    void LateUpdate()
    {
        if (targetBone == null) return;
        transform.position = targetBone.TransformPoint(localOffset);
        transform.rotation = targetBone.rotation * localRotationOffset;
    }
}
