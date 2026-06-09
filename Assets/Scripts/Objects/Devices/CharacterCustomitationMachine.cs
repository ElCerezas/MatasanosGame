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

    [Header("Color Settings")]
    [SerializeField] private List<Color> colores = new();

    private Dictionary<ulong, int> playerColorIndex = new Dictionary<ulong, int>();

    public void NextColor(ulong player)
    {
        if (!playerColorIndex.ContainsKey(player))
            playerColorIndex.TryAdd(player, 0);

        if (playerColorIndex[player] == colores.Count - 1)
            playerColorIndex[player] = 0;
        else
            playerColorIndex[player]++;

        Color colorActual = colores[playerColorIndex[player]];
        ApplyColorServerRpc(player, colorActual);

    }

    public void PrevColor(ulong player)
    {
        if (!playerColorIndex.ContainsKey(player))
            playerColorIndex.TryAdd(player, 0);

        if (playerColorIndex[player] == 0)
            playerColorIndex[player] = colores.Count-1;
        else
            playerColorIndex[player]--;

        Color colorActual = colores[playerColorIndex[player]];
        ApplyColorServerRpc(player, colorActual);
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
}


   
