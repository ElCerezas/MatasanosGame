using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterCustomitationMachine : NetworkBehaviour
{
    [Serializable]
    public struct Hat
    {
        public Mesh mesh;
        public Material mat;
    }

    [Header("Custom Settings")]
    [SerializeField] public List<Color> colores;
    [SerializeField] public List<Hat> hats;
    [SerializeField] public int maxFaces;

    private int colorID = 0;
    private int eyeID = 0;
    private int mouthID = 0;
    private int hatID = 0;

    public void ChangeColor(ulong player, int v)
    {
        ChangeColorServerRpc(player, v);
    }
    public void ChangEyes(ulong player, int v)
    {
        ChangEyesServerRpc(player, v);
    }
    public void ChangMouth(ulong player, int v)
    {
        ChangMouthServerRpc(player, v);
    }
    public void ChangeHat(ulong player, int v)
    {
        ChangeHatServerRpc(player, v);
    }

    [ServerRpc(RequireOwnership = false)]
    void ChangeColorServerRpc(ulong player, int v)
    {
        colorID += v;
        if (colorID >= colores.Count) colorID = 0;
        else if (colorID < 0) colorID = colores.Count - 1;

        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(player); // OK aquí, es server
        if (playerObject == null) return;

        ApplyColorClientRpc(playerObject.NetworkObjectId, colores[colorID]);
    }

    [ServerRpc(RequireOwnership = false)]
    void ChangEyesServerRpc(ulong player, int v)
    {
        eyeID += v;
        if (eyeID >= maxFaces) eyeID = 0;
        else if (eyeID < 0) eyeID = maxFaces - 1;

        ApplyEyesClientRpc(player, eyeID);
    }

    [ServerRpc(RequireOwnership = false)]
    void ChangMouthServerRpc(ulong player, int v)
    {
        mouthID += v;
        if (mouthID >= maxFaces) mouthID = 0;
        else if (mouthID < 0) mouthID = maxFaces - 1;

        ApplyMouthClientRpc(player, mouthID);
    }

    [ServerRpc(RequireOwnership = false)]
    void ChangeHatServerRpc(ulong player, int v)
    {
        hatID += v;
        if (hatID >= hats.Count) hatID = 0;
        else if (hatID < 0) hatID = hats.Count - 1;
        ApplyHatClientRpc(player, hatID);
    }

    [ClientRpc]
    void ApplyColorClientRpc(ulong playerNetworkObjectId, Color color)
    {
        var visual = GetVisual(playerNetworkObjectId);
        if (visual == null) return;
        visual.ChangeColor(color);
    }

    [ClientRpc]
    void ApplyEyesClientRpc(ulong player, int eyesIndex)
    {
        var visual = GetVisual(player);
        if (visual == null) return;
        if (visual.IsOwner)
            visual.SetEyeFaceIndex(eyesIndex);
    }

    [ClientRpc]
    void ApplyMouthClientRpc(ulong player, int mouthIndex)
    {
        var visual = GetVisual(player);
        if (visual == null) return;

        if (visual.IsOwner)
            visual.SetMouthFaceIndex(mouthIndex);
    }

    [ClientRpc]
    void ApplyHatClientRpc(ulong player, int hatIndex)
    {
        var visual = GetVisual(player);
        if (visual == null) return;

        if (hatIndex >= 0 && hatIndex < hats.Count)
        {
            Hat selectedHat = hats[hatIndex];
            visual.ChangeHat(selectedHat.mesh, selectedHat.mat);
        }
    }

    PlayerVisual GetVisual(ulong networkObjectId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj))
        {
            return netObj.GetComponent<PlayerVisual>();
        }
        return null;
    }
}