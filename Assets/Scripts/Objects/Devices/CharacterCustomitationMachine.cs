using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCustomitationMachine : NetworkBehaviour
{
    [Serializable]
    public struct Hat : INetworkSerializeByMemcpy
    {
        public Mesh mesh;
        public Material mat;
    }

    [Header("Custom Settings")]

    [SerializeField] public List<Color> colores;
    [SerializeField] public List<Hat> hats;
    [SerializeField] public int maxFaces;
    [SerializeField] public int maxMouth;

    private int colorID = 0;
    private int hatID = 0;
    private int faceID = 0;

    public void ChangeColor(ulong player, int v)
    {
        if(IsServer) return;

        colorID += v;
        if (colorID > colores.Count)
        {
            colorID = 0;
        }
        Color newColor = colores[colorID];

        ApplyColorServerRpc(player, newColor);
    }

    public void ChangeHat(ulong player, int v)
    {
        if (IsServer) return;

        hatID += v;
        if (hatID > hats.Count)
        {
            hatID = 0;
        }
        Hat newHat = hats[hatID];

        ApplyHatServerRpc(player, newHat);
    }
    public void ChangeFace(ulong player, int v)
    {
        if (IsServer) return;

        faceID += v;
        if (faceID > maxFaces)
        {
            faceID = 0;
        }
        int newFace = faceID;

        ApplyFaceServerRpc(player, newFace);
    }


    [ServerRpc]
    private void ApplyColorServerRpc(ulong player, Color color)
    {
        ApplyColorClientRpc(player, color);
    }

    [ClientRpc]
    private void ApplyColorClientRpc(ulong player, Color color)
    {
        NetworkManager.Singleton.ConnectedClients[player].PlayerObject.GetComponent<PlayerVisual>().ChangeColor(color);
    }

    [ServerRpc]
    private void ApplyHatServerRpc(ulong player, Hat hat)
    {
        ApplyHatClientRpc(player, hat);
    }

    [ClientRpc]
    private void ApplyHatClientRpc(ulong player, Hat hat)
    {
        //NetworkManager.Singleton.ConnectedClients[player].PlayerObject.GetComponent<PlayerVisual>().ChangeColor(hat);
    }

    [ServerRpc]
    private void ApplyFaceServerRpc(ulong player, int face)
    {
        ApplyFaceClientRpc(player, face);
    }

    [ClientRpc]
    private void ApplyFaceClientRpc(ulong player, int face)
    {
        //NetworkManager.Singleton.ConnectedClients[player].PlayerObject.GetComponent<PlayerVisual>().ChangeColor(face);
    }
}



