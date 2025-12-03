using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using LiteNetLib;
using Comfort.Common;

namespace RealismModSync.QuestExtended
{
    public static class Fika
    {
        public static void Register()
        {
            if (!Config.EnableQuestSync.Value)
            {
                Plugin.REAL_Logger.LogInfo("Quest Extended sync is disabled, not registering Fika events");
                return;
            }

            if (!Core.IsQuestExtendedAvailable())
            {
                Plugin.REAL_Logger.LogInfo("Quest Extended not available, not registering Fika events");
                return;
            }

            FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
            FikaEventDispatcher.SubscribeEvent<FikaClientCreatedEvent>(OnClientCreated);
            FikaEventDispatcher.SubscribeEvent<FikaServerCreatedEvent>(OnServerCreated);

            Plugin.REAL_Logger.LogInfo("Quest Extended Fika events registered");
        }

        private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent @event)
        {
            @event.NetworkManager.RegisterPacket<Packets.QuestExtendedSyncPacket>();
        }

        private static void OnClientCreated(FikaClientCreatedEvent @event)
        {
            @event.Client.NetClient.SubscribeNetSerializable<Packets.QuestExtendedSyncPacket, NetPeer>(OnQuestSyncPacketReceived, () => new Packets.QuestExtendedSyncPacket());
        }

        private static void OnServerCreated(FikaServerCreatedEvent @event)
        {
            @event.Server.NetServer.SubscribeNetSerializable<Packets.QuestExtendedSyncPacket, NetPeer>(OnQuestSyncPacketReceivedServer, () => new Packets.QuestExtendedSyncPacket());
        }

        private static void OnQuestSyncPacketReceived(Packets.QuestExtendedSyncPacket packet, NetPeer peer)
        {
            if (Config.EnableQuestSync.Value)
            {
                Plugin.REAL_Logger.LogInfo($"Client received quest sync packet: {packet.QuestId}/{packet.ConditionId}");
            }

            NetworkSync.ProcessQuestSyncPacket(packet);
        }

        private static void OnQuestSyncPacketReceivedServer(Packets.QuestExtendedSyncPacket packet, NetPeer peer)
        {
            if (Config.EnableQuestSync.Value)
            {
                Plugin.REAL_Logger.LogInfo($"Server received quest sync packet from peer, broadcasting: {packet.QuestId}/{packet.ConditionId}");
            }

            // Server receives from one client and broadcasts to all others
            if (Singleton<FikaServer>.Instantiated)
            {
                var server = Singleton<FikaServer>.Instance;
                server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
            }

            // Also process locally on server
            NetworkSync.ProcessQuestSyncPacket(packet);
        }

        public static void SendQuestSyncPacket(Packets.QuestExtendedSyncPacket packet)
        {
            if (!Config.EnableQuestSync.Value)
                return;

            if (!Core.CanSendQuestSync())
                return;

            try
            {
                if (FikaBackendUtils.IsServer)
                {
                    if (Singleton<FikaServer>.Instantiated)
                    {
                        var server = Singleton<FikaServer>.Instance;
                        server.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
                    }
                }
                else if (FikaBackendUtils.IsClient)
                {
                    if (Singleton<FikaClient>.Instantiated)
                    {
                        var client = Singleton<FikaClient>.Instance;
                        client.SendData(ref packet, DeliveryMethod.ReliableOrdered);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.REAL_Logger.LogError($"Error sending quest sync packet: {ex.Message}");
            }
        }
    }
}
