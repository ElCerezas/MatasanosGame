using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using TMPro;

public class CharacterCustomitationMachine : NetworkBehaviour
{
    [Serializable]
    public struct Hat
    {
        public Mesh mesh;
        public Material mat;
    }
    public static CharacterCustomitationMachine Instance;

    [Header("Custom Settings")]
    [SerializeField] public List<Color> colores;
    [SerializeField] public List<Hat> hats;
    [SerializeField] public int maxFaces;

    [Header("UI")]
    [SerializeField] TMP_Text colorIndexText;
    [SerializeField] TMP_Text eyeIndexText;
    [SerializeField] TMP_Text mouthIndexText;
    [SerializeField] TMP_Text hatIndexText;

    void Awake()
    {
        Instance = this;
    }

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
        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(player);
        if (playerObject == null) return;
        var visual = playerObject.GetComponent<PlayerVisual>();

        int newId = visual.colorIndex.Value + v;
        if (newId >= colores.Count) newId = 0;
        else if (newId < 0) newId = colores.Count - 1;
        visual.colorIndex.Value = newId;

        UpdateUIClientRpc(player, newId, visual.eyeFaceIndex.Value, visual.mouthFaceIndex.Value, visual.hatIndex.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    void ChangEyesServerRpc(ulong player, int v)
    {
        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(player);
        if (playerObject == null) return;
        var visual = playerObject.GetComponent<PlayerVisual>();

        int newId = visual.eyeFaceIndex.Value + v;
        if (newId >= maxFaces) newId = 0;
        else if (newId < 0) newId = maxFaces - 1;
        visual.eyeFaceIndex.Value = newId;

        UpdateUIClientRpc(player, visual.colorIndex.Value, newId, visual.mouthFaceIndex.Value, visual.hatIndex.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    void ChangMouthServerRpc(ulong player, int v)
    {
        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(player);
        if (playerObject == null) return;
        var visual = playerObject.GetComponent<PlayerVisual>();

        int newId = visual.mouthFaceIndex.Value + v;
        if (newId >= maxFaces) newId = 0;
        else if (newId < 0) newId = maxFaces - 1;
        visual.mouthFaceIndex.Value = newId;

        UpdateUIClientRpc(player, visual.colorIndex.Value, visual.eyeFaceIndex.Value, newId, visual.hatIndex.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    void ChangeHatServerRpc(ulong player, int v)
    {
        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(player);
        if (playerObject == null) return;
        var visual = playerObject.GetComponent<PlayerVisual>();

        int newId = visual.hatIndex.Value + v;
        if (newId >= hats.Count) newId = 0;
        else if (newId < 0) newId = hats.Count - 1;
        visual.hatIndex.Value = newId;

        UpdateUIClientRpc(player, visual.colorIndex.Value, visual.eyeFaceIndex.Value, visual.mouthFaceIndex.Value, newId);
    }

    [ClientRpc]
    void UpdateUIClientRpc(ulong player, int colorIdx, int eyeIdx, int mouthIdx, int hatIdx)
    {
        if (NetworkManager.Singleton.LocalClientId != player) return;

        UpdateText(colorIndexText, colorIdx);
        UpdateText(eyeIndexText, eyeIdx);
        UpdateText(mouthIndexText, mouthIdx);
        UpdateText(hatIndexText, hatIdx);
    }

    void UpdateText(TMP_Text text, int value)
    {
        if (text == null) return;
        text.text = (value + 1).ToString("D2");
    }
}