using UnityEngine;
using System.Collections.Generic;

using LiteNetLib;
using LiteNetLib.Utils;

using Assets.Scripts;

#region Ex
#endregion Ex

public enum NetworkTags
{
    NT_S_Receiv_Player_Position,
    NT_S_Send_Players_Pos_Array
}

class Globals
{
    public static string Address = "localhost";
    public static int Serveur_Port = 15000;
    public static string Serv_Key = "Server_app_key";
}

public class Network : MonoBehaviour, INetEventListener {

    private NetManager _netClient;

    private NetDataWriter dataWriter;
    private Dictionary<long, NetPlayer> netPlayersDictionary;

    public Player player;
    public GameObject netPlayerPrefab;

    private Vector3 lastNetworkedPosition = Vector3.zero;

    private float lastDistance = 0.0f;
    const float MIN_DISTANCE_TO_SEND_POSITION = 0.01f;

    private NetPeer serverPeer;

    #region EventListener

    public void OnPeerConnected(NetPeer peer)
    {
        serverPeer = peer;
        Debug.Log($"OnPeerConnected: {peer.EndPoint.Host} : {peer.EndPoint.Port}");
    }

    public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
    {
        Debug.Log($"OnPeerConnected: {peer.EndPoint.Host} : {peer.EndPoint.Port} Reason: {disconnectInfo.Reason.ToString()}");
    }

    public void OnNetworkError(NetEndPoint endPoint, int socketErrorCode)
    {
        Debug.LogError($"OnNetworkError: {socketErrorCode}");
    }

    public void OnNetworkReceive(NetPeer peer, NetDataReader reader)
    {
        // Debug.Log("OnNetworkReceive");
        if (reader.Data == null)
            return;

        Debug.Log($"OnNetworkReceive: reader.Data.Length == {reader.Data.Length}");

        if (reader.Data.Length >= 4)
        {
            NetworkTags networkTag = (NetworkTags)reader.GetInt();
            if (networkTag == NetworkTags.NT_S_Send_Players_Pos_Array)
            {
                int lengthArr = (reader.Data.Length - 4) / (sizeof(long) + sizeof(float) * 3);

                Debug.Log("Got positions array data num : " + lengthArr);

                for (int i = 0; i < lengthArr; i++)
                {
                    long playerid = reader.GetLong();

                    if (!netPlayersDictionary.ContainsKey(playerid))
                        netPlayersDictionary.Add(playerid, new NetPlayer());

                    netPlayersDictionary[playerid].X = reader.GetFloat();
                    netPlayersDictionary[playerid].Y = reader.GetFloat();
                    netPlayersDictionary[playerid].Z = reader.GetFloat();

                    // Debug.Log("playerid: " + playerid + " Pose updated Ok");
                }
            }
        }
    }

    public void OnNetworkReceiveUnconnected(NetEndPoint remoteEndPoint, NetDataReader reader, UnconnectedMessageType messageType)
    {
        Debug.Log($"OnNetworkReceive: {reader.Data.Length}");
    }

    public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
    {
        Debug.Log("OnNetworkLatencyUpdate");
    }

    #endregion EventListener

    void Start()
    {
        _netClient = new NetManager(this, Globals.Serv_Key);
        _netClient.Start();
        _netClient.UpdateTime = 15;

        netPlayersDictionary = new Dictionary<long, NetPlayer>();
        dataWriter = new NetDataWriter();

        Debug.Log("_netClient.IsRunning OK !");

        _netClient.Connect(Globals.Address, Globals.Serveur_Port);

        Debug.Log("netClient Connect !");
    }

    private void FixedUpdate()
    {
        _netClient.PollEvents();

        var peer = _netClient.GetFirstPeer();

        if (peer != null && peer.ConnectionState == ConnectionState.Connected)
        {
            lastDistance = Vector3.Distance(lastNetworkedPosition, player.transform.position);
            if (lastDistance >= MIN_DISTANCE_TO_SEND_POSITION)
            {
                dataWriter.Reset();

                dataWriter.Put((int)NetworkTags.NT_S_Receiv_Player_Position);
                dataWriter.Put(player.transform.position.x);
                dataWriter.Put(player.transform.position.y);
                dataWriter.Put(player.transform.position.z);

                serverPeer.Send(dataWriter, SendOptions.Sequenced);

                lastNetworkedPosition = player.transform.position;
            }
        }
        else
        {
            // _netClient.SendDiscoveryRequest(new byte[] { 1 }, Globals.Serveur_Port);
            Debug.Log("peer = null");
        }

        foreach (var player in netPlayersDictionary)
        {
            if (!player.Value.GameObjectAdded)
            {
                player.Value.GameObjectAdded = true;
                player.Value.GameObject = Instantiate(netPlayerPrefab, player.Value.Position, Quaternion.identity);
            }
            else
                player.Value.GameObject.transform.position = player.Value.Position;
        }
    }
    
    private void OnApplicationQuit()
    {
        if (_netClient != null)
            if (_netClient.IsRunning)
                _netClient.Stop();
    }

}
