using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.VisualScripting;
using UnityEngine;

public class LanGame : MonoBehaviour
{
    const int DiscoveryPort = 47777;
    const ushort GamePort = 7777;
    const string Magic = "MatasanosGame";

    public string roomName = "SAGA_BCN";

    UdpClient broadcaster, listener;
    float nextBroadcast;
    bool hosting;
    readonly Dictionary<string, (string name, float lastSeen)> found = new();

    // NOVA VARIABLE: Controla si la interfície d'usuari es mostra o no
    private bool showUI = false;

    public void CreateGame()
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetConnectionData("0.0.0.0", GamePort, "0.0.0.0");

        if (NetworkManager.Singleton.StartHost())
        {
            hosting = true;
            broadcaster = new UdpClient { EnableBroadcast = true };
        }
    }

    public void JoinGame(string ip)
    {
        var utp = NetworkManager.Singleton.GetComponent<UnityTransport>();
        utp.SetConnectionData(ip, GamePort);
        NetworkManager.Singleton.StartClient();
    }

    public void StartSearching()
    {
        found.Clear();
        listener?.Close();
        listener = new UdpClient(DiscoveryPort) { EnableBroadcast = true };
    }

    // NOU MÈTODE: Per desconnectar-se de la partida i reiniciar la xarxa
    public void Disconnect()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        hosting = false;

        broadcaster?.Close();
        broadcaster = null;

        listener?.Close();
        listener = null;

        found.Clear();
    }

    void Update()
    {
        // NOVA LÒGICA: Activar/desactivar la UI en prémer la tecla 'Y'
        if (Input.GetKeyDown(KeyCode.Y))
        {
            showUI = !showUI;
        }

        if (hosting && broadcaster != null && Time.time >= nextBroadcast)
        {
            nextBroadcast = Time.time + 1f;
            var data = Encoding.UTF8.GetBytes($"{Magic}|{roomName}");
            broadcaster.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, DiscoveryPort));
        }

        if (listener != null)
        {
            while (listener.Available > 0)
            {
                IPEndPoint ep = null;
                var msg = Encoding.UTF8.GetString(listener.Receive(ref ep)).Split('|');
                if (msg.Length == 2 && msg[0] == Magic)
                    found[ep.Address.ToString()] = (msg[1], Time.time);
            }
        }
    }

    string manualIp = "192.168.0.10";
    void OnGUI()
    {
        // NOVA LÒGICA: Si showUI és fals, marxem del mètode sense dibuixar el layout
        if (!showUI) return;

        GUILayout.BeginArea(new Rect(20, 20, 320, 500));

        // NOVA LÒGICA: Si ja estem connectats, mostrem el botó de desconnectar
        if (NetworkManager.Singleton != null) return;
        if (NetworkManager.Singleton.IsListening)
        {
            GUILayout.Label("Estàs connectat a una sala.");
            if (GUILayout.Button("Desconnectar"))
            {
                Disconnect();
            }
        }
        else // Si no estem connectats, mostrem la interfície habitual per buscar/crear partida
        {
            roomName = GUILayout.TextField(roomName);
            if (GUILayout.Button("Crear partida")) CreateGame();
            if (GUILayout.Button("Buscar partidas")) StartSearching();

            var toRemove = new List<string>();
            foreach (var kv in found)
            {
                if (Time.time - kv.Value.lastSeen > 4f) { toRemove.Add(kv.Key); continue; }
                if (GUILayout.Button($"Unirse: {kv.Value.name} ({kv.Key})")) JoinGame(kv.Key);
            }
            foreach (var k in toRemove) found.Remove(k);

            GUILayout.Space(10);
            manualIp = GUILayout.TextField(manualIp);
            if (GUILayout.Button("Unirse por IP")) JoinGame(manualIp);
        }

        GUILayout.EndArea();
    }

    void OnDestroy()
    {
        broadcaster?.Close();
        listener?.Close();

        // Assegurar el tancament de la xarxa si es destrueix l'objecte de sobte
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
    }
}