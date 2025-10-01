using System.Collections.Concurrent;
using Comfort.Common;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using LiteNetLib;
using RealismModSync.StanceReplication.Components;
using RealismModSync.StanceReplication.Packets;

namespace RealismModSync.StanceReplication;

public static class Fika
{

    public static void Register()
    {
        FikaEventDispatcher.SubscribeEvent<FikaNetworkManagerCreatedEvent>(OnNetworkManagerCreated);
        FikaEventDispatcher.SubscribeEvent<FikaGameCreatedEvent>(OnGameWorldStarted);
    }

    private static void OnNetworkManagerCreated(FikaNetworkManagerCreatedEvent ev)
    {
        switch (ev.Manager)
        {
            case FikaServer server:
                server.RegisterPacket<RealismStanceReplicationPacket, NetPeer>(HandleRealismPacketServer);
                break;
            case FikaClient client:
                client.RegisterPacket<RealismStanceReplicationPacket>(HandleRealismPacketClient);
                break;
        }
    }
    
    private static void OnGameWorldStarted(FikaGameCreatedEvent ev)
    {
        if (Core.ObservedComponents != null)
        {
            Core.ObservedComponents.Clear();
        }
        else
        {
            Core.ObservedComponents = new ConcurrentDictionary<int, RSR_Observed_Component>();
        }
    }
        
    private static void HandleRealismPacketClient(RealismStanceReplicationPacket packet)
    {
        if (Core.ObservedComponents.TryGetValue(packet.NetID, out var player))
        {
            player.SetAnimValues(packet.WeapPosition, packet.Rotation, packet.IsPatrol, packet.SprintAnimationVarient);
        }
    }

    private static void HandleRealismPacketServer(RealismStanceReplicationPacket packet, NetPeer peer)
    {
        HandleRealismPacketClient(packet);
        
        // Broadcast to all except the peer that sent it
        Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.Unreliable, peer);
    }
    
}