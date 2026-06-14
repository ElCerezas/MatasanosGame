using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerDecalIsolator : NetworkBehaviour
{
    public Renderer playerHeadMesh;
    public DecalProjector eyeDecalProjector;
    public DecalProjector mouthDecalProjector;

    public override void OnNetworkSpawn()
    {
        int playerId = (int)OwnerClientId;

        uint uniquePlayerLayer = (uint)(1 << (8 + playerId));

        playerHeadMesh.renderingLayerMask |= uniquePlayerLayer;
        eyeDecalProjector.renderingLayerMask = uniquePlayerLayer;
        mouthDecalProjector.renderingLayerMask = uniquePlayerLayer;
    }
}