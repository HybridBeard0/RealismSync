using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using Comfort.Common;
using LiteNetLib;

namespace RealismModSync.Health
{
    public static class Fika
    {
        public static void Register()
        {
            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
            
            Plugin.REAL_Logger.LogInfo("Health Fika events registered");
        }

        private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent ev)
        {
            switch (ev.Manager)
            {
                case FikaServer server:
                    server.RegisterPacket<Packets.RealismMedicalSyncPacket, NetPeer>(OnMedicalSyncPacketReceivedServer);
                    Plugin.REAL_Logger.LogInfo("Registered RealismMedicalSyncPacket with Fika server");
                    break;
                case FikaClient client:
                    client.RegisterPacket<Packets.RealismMedicalSyncPacket>(OnMedicalSyncPacketReceivedClient);
                    Plugin.REAL_Logger.LogInfo("Registered RealismMedicalSyncPacket with Fika client");
                    break;
            }
        }

        private static void OnMedicalSyncPacketReceivedServer(Packets.RealismMedicalSyncPacket packet, NetPeer peer)
        {
            try
            {
                // Process the packet locally first
                NetworkSync.ProcessMedicalSyncPacket(packet);
                
                // Then broadcast to all other clients except the sender
                Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
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
            try
            {
                var fikaClient = Singleton<FikaClient>.Instance;
                if (fikaClient != null)
                {
                    fikaClient.SendData(ref packet, LiteNetLib.DeliveryMethod.ReliableOrdered);
                }
                else
                {
                    Plugin.REAL_Logger.LogWarning("FikaClient instance is null, cannot send medical sync packet");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error sending medical sync packet: {ex.Message}");
            }
        }
    }
}
