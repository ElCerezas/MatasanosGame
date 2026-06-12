using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
public class CharacterCustomitationMachine : NetworkBehaviour
{
    [Serializable] public struct Hat : INetworkSerializeByMemcpy
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
        if (IsServer) return;

        colorID += v;
        if (colorID >= colores.Count)
            colorID = 0;
        else if (colorID < 0)
            colorID = colores.Count - 1;

        ApplyColorClientRpc(player, colores[colorID]);
    }
    public void ChangEyes(ulong player, int v)
    {
        if (IsServer) return;
        eyeID += v;
        if (eyeID >= maxFaces)
            eyeID = 0;
        else if (eyeID < 0)
            eyeID = maxFaces - 1;
        ApplyEyesClientRpc(player, eyeID);
    }
    public void ChangMouth(ulong player, int v)
    {
        if (IsServer) return;
        mouthID += v;
        if (mouthID >= maxFaces)
            mouthID = 0;
        else if (mouthID < 0)
            mouthID = maxFaces - 1;
        ApplyMouthClientRpc(player, mouthID);
    }
    public void ChangeHat(ulong player, int v)
    {
        if (IsServer) return;
        hatID += v;
        if (hatID >= hats.Count)
            hatID = 0;
        else if (hatID < 0)
            hatID = hats.Count - 1;
        ApplyHatClientRpc(player, hats[hatID]);
    }

    [ClientRpc]
    void ApplyColorClientRpc(ulong player, Color color)
    {
        var visual = GetVisual(player);
        if (visual == null || !visual.IsOwner) return;

        visual.ChangeColor(color);
    }
    [ClientRpc]
    void ApplyEyesClientRpc(ulong player, int eyesIndex)
    {
        var visual = GetVisual(player);
        if (visual == null || !visual.IsOwner) return;
            visual.SetEyeFaceIndex(eyesIndex);
    }
    [ClientRpc]
    void ApplyMouthClientRpc(ulong player, int mouthIndex)
    {
        var visual = GetVisual(player);
        if (visual == null || !visual.IsOwner) return;
        visual.SetMouthFaceIndex(mouthIndex);

    }
    [ClientRpc]
    void ApplyHatClientRpc(ulong player, Hat hat)
    {
        var visual = GetVisual(player);
        if (visual == null || !visual.IsOwner) return;

        visual.ChangeHat(hat.mesh, hat.mat);
    }

    PlayerVisual GetVisual(ulong player)
    {
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(player))
            return null;

        var playerObject = NetworkManager.Singleton.ConnectedClients[player].PlayerObject;
        if (playerObject == null) return null;

        return playerObject.GetComponent<PlayerVisual>();
    }
}