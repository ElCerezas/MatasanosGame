using Unity.Netcode;
using UnityEngine;

public class WoundBoneFollower : NetworkBehaviour
{
    private Transform targetBone;
    private Vector3 localOffset;
    private Quaternion localRotationOffset;
    public NetworkVariable<Unity.Collections.FixedString64Bytes> boneName =
        new NetworkVariable<Unity.Collections.FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        boneName.OnValueChanged += (_, newName) => TryAttachByName(newName.ToString());

        if (!string.IsNullOrEmpty(boneName.Value.ToString()))
            TryAttachByName(boneName.Value.ToString());
    }

    void TryAttachByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return;

        AlienWoundManager woundManager = GetComponentInParent<AlienWoundManager>();
        if (woundManager == null)
        {
            return;
        }

        SkinnedMeshRenderer smr = woundManager.GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr == null)
        {
            return;
        }

        foreach (Transform bone in smr.bones)
        {
            if (bone != null && bone.name == name)
            {
                AttachToBone(bone);
                return;
            }
        }
    }

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