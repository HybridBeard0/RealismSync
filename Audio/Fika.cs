using System.Collections.Concurrent;
using Comfort.Common;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using LiteNetLib;
using RealismModSync.Audio.Components;
using RealismModSync.Audio.Packets;

namespace RealismModSync.Audio;

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
                server.RegisterPacket<RealismAudioPacket, NetPeer>(HandleRealismAudioPacketServer);
                break;
            case FikaClient client:
                client.RegisterPacket<RealismAudioPacket>(HandleRealismAudioPacket);
                break;
        }
        
    }

    private static void HandleRealismAudioPacketServer(RealismAudioPacket packet, NetPeer peer)
    {
        HandleRealismAudioPacket(packet);
        Plugin.REAL_Logger.LogInfo($"Rebroadcasting audio packet: {packet.Clip} from {packet.NetID} for device {packet.DeviceType}");
        Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
    }

    private static void HandleRealismAudioPacket(RealismAudioPacket packet)
    {
        Core.ObservedComponents.TryGetValue(packet.NetID, out var observedComponent);
        if (observedComponent == null)
        {
            Plugin.REAL_Logger.LogInfo($"Could not find observed audio component for {packet.NetID}");
            return;
        }
        
        Plugin.REAL_Logger.LogInfo($"Processing audio packet: {packet.Clip} from {packet.NetID} for device {packet.DeviceType}. Source Volume: {packet.Volume}.");
        
        switch (packet.DeviceType)
        {
            case RealismDeviceType.Geiger:
                observedComponent.PlayGeigerClips(packet.Clip, packet.Volume);
                break;
            case RealismDeviceType.GasAnalyzer:
                observedComponent.PlayGasAnalyserClips(packet.Clip, packet.Volume);
                break;
            default:
                Plugin.REAL_Logger.LogInfo($"Unknown device type {packet.DeviceType}");
                break;
        }
    }


    private static void OnGameWorldStarted(FikaGameCreatedEvent ev)
    {
        Plugin.REAL_Logger.LogInfo("Clearing audio components");
        if (Core.ObservedComponents != null)
        {
            Core.ObservedComponents.Clear();
        }
        else
        {
            Core.ObservedComponents = new ConcurrentDictionary<int, RSAObservedComponent>();
        }
        
        /* Not needed at this point
        // loop through the audio clips for Gas And Geiger and name them
        foreach(var (key, audioClip) in RealismMod.Plugin.DeviceAudioClips)
        {
            // set the name of the clip to the key
            var original = audioClip.name;
            RealismMod.Plugin.DeviceAudioClips[key].name = key;
            Plugin.REAL_Logger.LogInfo($"Set {key} audio clip name for device from {original}");
        } */
    }
}