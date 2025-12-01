using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Comfort.Common;
using EFT;
using LiteNetLib;

namespace RealismModSync.Health
{
    public static class Fika
    {
        private static bool _isNetworkReady = false;

        public static void Register()
        {
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
            FikaEventDispatcher.SubscribeEvent<FikaGameCreatedEvent>(OnGameCreated);
            
            Plugin.REAL_Logger.LogInfo("Health Fika events registered");
        }

        private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent ev)
        {
            switch (ev.Manager)
            {
                case FikaServer server:
                    server.RegisterPacket<Packets.RealismMedicalSyncPacket, NetPeer>(OnMedicalSyncPacketReceivedServer);
                    Plugin.REAL_Logger.LogInfo("Registered RealismMedicalSyncPacket with Fika server");
                    _isNetworkReady = true;
                    break;
                case FikaClient client:
                    client.RegisterPacket<Packets.RealismMedicalSyncPacket>(OnMedicalSyncPacketReceivedClient);
                    Plugin.REAL_Logger.LogInfo("Registered RealismMedicalSyncPacket with Fika client");
                    _isNetworkReady = true;
                    break;
            }
        }

        private static void OnGameCreated(FikaGameCreatedEvent ev)
        {
            // Game is now fully loaded and ready for network packets
            _isNetworkReady = true;
            Plugin.REAL_Logger.LogInfo("Health network sync ready for game");
        }

        private static void OnMedicalSyncPacketReceivedServer(Packets.RealismMedicalSyncPacket packet, NetPeer peer)
        {
            try
            {
                // Process the packet locally first
                NetworkSync.ProcessMedicalSyncPacket(packet);
                
                // Then broadcast to all other clients except the sender
                var server = Singleton<FikaServer>.Instance;
                if (server != null)
                {
                    server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error processing medical sync packet on server: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private static void OnMedicalSyncPacketReceivedClient(Packets.RealismMedicalSyncPacket packet)
        {
            try
            {
                NetworkSync.ProcessMedicalSyncPacket(packet);
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error processing medical sync packet on client: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public static void SendMedicalSyncPacket(Packets.RealismMedicalSyncPacket packet)
        {
            // Don't try to send packets if network is not ready
            if (!_isNetworkReady)
                return;

            // Don't send packets if game world is not active
            if (!Singleton<GameWorld>.Instantiated)
                return;

            try
            {
                // Try to send via FikaClient (for clients in multiplayer)
                if (Singleton<FikaClient>.Instantiated)
                {
                    var fikaClient = Singleton<FikaClient>.Instance;
                    if (fikaClient != null && fikaClient.NetClient != null && fikaClient.NetClient.IsRunning)
                    {
                        fikaClient.SendData(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        return;
                    }
                }

                // Try to send via FikaServer (for host)
                if (Singleton<FikaServer>.Instantiated)
                {
                    var fikaServer = Singleton<FikaServer>.Instance;
                    if (fikaServer != null && fikaServer.NetServer != null && fikaServer.NetServer.IsRunning)
                    {
                        fikaServer.SendDataToAll(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                        return;
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error sending medical sync packet: {ex.Message}");
            }
        }
    }
}
