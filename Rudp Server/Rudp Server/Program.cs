using System;
using System.Collections.Generic;
//using System.Net;//pour Com R-UDP LiteNetLib 8
//using System.Net.Sockets;//pour Com R-UDP LiteNetLib 8
using LiteNetLib;//pour Com R-UDP LiteNetLib 7
using LiteNetLib.Utils;//pour Com R-UDP LiteNetLib 7
using System.Threading;//pour Threading

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
    public static int Serv_Max_Connection = 10;
}

namespace Rudp_Server
{
    public class ServerPlayers_Pose
    {
        public NetPeer NetPeer { get; set; }

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public bool Moved { get; set; }

        public ServerPlayers_Pose(NetPeer peer_Id)
        {
            NetPeer = peer_Id;

            X = 0.0f;
            Y = 0.0f;
            Z = 0.0f;

            Moved = false;
        }
    }

    class Program : INetEventListener
    {
        private NetManager _netManager;

        private Dictionary<long, ServerPlayers_Pose> _dictionary_Server_Players_Pose;

        NetDataWriter _netDataWriter;

        #region EventListener

        public void OnPeerConnected(NetPeer _netPeer)
        {
            try
            {
                Console.WriteLine($"OnPeerConnected == Client connected _netPeer.ConnectId No[ {_netPeer.ConnectId} ]-->_netPeer.EndPoint {_netPeer.EndPoint}");
                Console.WriteLine($"_netPeer.EndPoint.Host: {_netPeer.EndPoint.Host} _netPeer.EndPoint.Port {_netPeer.EndPoint.Port}");

                NetDataWriter netDataWriter = new NetDataWriter();
                netDataWriter.Reset();
                netDataWriter.Put((int)NetworkTags.NT_S_Send_Players_Pos_Array);
                foreach (var p in _dictionary_Server_Players_Pose)
                {
                    netDataWriter.Put(p.Key);

                    netDataWriter.Put(p.Value.X);
                    netDataWriter.Put(p.Value.Y);
                    netDataWriter.Put(p.Value.Z);
                }

                _netPeer.Send(netDataWriter, SendOptions.ReliableOrdered);

                if (!_dictionary_Server_Players_Pose.ContainsKey(_netPeer.ConnectId))
                    _dictionary_Server_Players_Pose.Add(_netPeer.ConnectId, new ServerPlayers_Pose(_netPeer));

                _dictionary_Server_Players_Pose[_netPeer.ConnectId].Moved = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnPeerConnected Error: {ex.Message}");
            }
        }

        public void OnNetworkReceive(NetPeer _netPeer, NetDataReader _netDataReader)
        {
            try
            {
                if (_netDataReader.Data == null)
                    return;

                NetworkTags networkTag = (NetworkTags)_netDataReader.GetInt();
                if (networkTag == NetworkTags.NT_S_Receiv_Player_Position)
                {

                    float x = _netDataReader.GetFloat();
                    float y = _netDataReader.GetFloat();
                    float z = _netDataReader.GetFloat();

                    Console.WriteLine($"netPeer.ConnectId --> {_netPeer.ConnectId} Got pos packet : {x} | {y} | {z} ");

                    _dictionary_Server_Players_Pose[_netPeer.ConnectId].X = x;
                    _dictionary_Server_Players_Pose[_netPeer.ConnectId].Y = y;
                    _dictionary_Server_Players_Pose[_netPeer.ConnectId].Z = z;

                    _dictionary_Server_Players_Pose[_netPeer.ConnectId].Moved = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnNetworkReceive Error: {ex.Message}");
            }
        }

        public void OnPeerDisconnected(NetPeer _netPeer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine($"[Server] Peer disconnected: " + _netPeer.ConnectId + ", reason: " + disconnectInfo.Reason);
            try
            {
                Console.WriteLine($"OnPeerDisconnected: {_netPeer.EndPoint.Host} : {_netPeer.EndPoint.Port} Reason: {disconnectInfo.Reason.ToString()}");

                if (_dictionary_Server_Players_Pose.ContainsKey(_netPeer.ConnectId))
                    _dictionary_Server_Players_Pose.Remove(_netPeer.ConnectId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnPeerDisconnected Error: {ex.Message}");
            }
        }

        public void OnNetworkError(NetEndPoint _netendPoint, int socketErrorCode)
        {
            // Console.WriteLine("[OnNetworkError] error: " + socketErrorCode);
            try
            {
                Console.WriteLine($"OnNetworkError: {socketErrorCode}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnNetworkError Error: {ex.Message}");
            }
        }

        public void OnNetworkReceiveUnconnected(NetEndPoint _netendPoint, NetDataReader _netDataReader, UnconnectedMessageType messageType)
        {
            try
            {
                Console.WriteLine($"OnNetworkReceiveUnconnected");

                if (messageType == UnconnectedMessageType.DiscoveryRequest)
                {
                    _netManager.SendDiscoveryResponse(new byte[] { 1 }, _netendPoint);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnNetworkReceiveUnconnected Error: {ex.Message}");
            }
        }

        public void OnNetworkLatencyUpdate(NetPeer _netPeer, int latency)
        {
            try
            {

            }
            catch (Exception ex)
            {
                Console.WriteLine($"OnNetworkLatencyUpdate Error: {ex.Message}");
            }
        }

        #endregion EventListener

        public void SendPlayerPositions()
        {
            try
            {
                Dictionary<long, ServerPlayers_Pose> _dictionary_send_to_Players = new Dictionary<long, ServerPlayers_Pose>(_dictionary_Server_Players_Pose);

                int To_Player = 0;
                foreach (var send_To_Player in _dictionary_send_to_Players)
                {
                    if (send_To_Player.Value == null)
                    {
                        Console.WriteLine($"Player Absent No: {To_Player}");
                        continue;
                    }
                    To_Player += 1;

                    _netDataWriter.Reset();
                    _netDataWriter.Put((int)NetworkTags.NT_S_Send_Players_Pos_Array);

                    int amountPlayersMoved = 0;

                    foreach (var Players_Pos in _dictionary_send_to_Players)
                    {
                        if (send_To_Player.Key == Players_Pos.Key)
                            continue;

                        if (!Players_Pos.Value.Moved)
                            continue;

                        _netDataWriter.Put(Players_Pos.Key);

                        _netDataWriter.Put(Players_Pos.Value.X);
                        _netDataWriter.Put(Players_Pos.Value.Y);
                        _netDataWriter.Put(Players_Pos.Value.Z);

                        amountPlayersMoved++;
                    }

                    if (amountPlayersMoved > 0)
                        send_To_Player.Value.NetPeer.Send(_netDataWriter, SendOptions.Sequenced);
                }

                foreach (var player in _dictionary_Server_Players_Pose)
                    player.Value.Moved = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendPlayerPositions Error: {ex.Message}");
            }
        }

        public void Run()
        {
            try
            {
                _dictionary_Server_Players_Pose = new Dictionary<long, ServerPlayers_Pose>();

                _netManager = new NetManager(this, Globals.Serv_Max_Connection, Globals.Serv_Key);

                _netDataWriter = new NetDataWriter();

                if (_netManager.Start(Globals.Serveur_Port))
                    Console.WriteLine($"{Globals.Serv_Max_Connection} player NetManager started listening on port {Globals.Serveur_Port}");
                else
                {
                    Console.WriteLine("Server cold not start!");
                    return;
                }

                while (_netManager.IsRunning)
                {
                    _netManager.PollEvents();

                    SendPlayerPositions();

                    /*System.Threading.*/
                    Thread.Sleep(15);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Run Error: {ex.Message}");
            }
        }

        static void Main(string[] args)
        {
            Program program = new Program();
            program.Run();
            Console.ReadKey();
        }
    }
}