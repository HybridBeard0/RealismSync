using System.Collections.Concurrent;
using System.Reflection;
using Comfort.Common;
using EFT;
using Fika.Core.Coop.GameMode;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using LiteNetLib;
using RealismMod;
using RealismMod.Audio;
using RealismModSync.HazardZones.Packets;
using UnityEngine;

namespace RealismModSync.HazardZones;

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
                server.RegisterPacket<RealismInteractablePacket, NetPeer>(HandleInteractionChangeServer);
                server.RegisterPacket<RealismCanTurnValvePacket, NetPeer>(HandleCanTurnValvePacketServer);
                break;
            case FikaClient client:
                client.RegisterPacket<RealismHazardPacket>(HandleHazardPacket);
                client.RegisterPacket<RealismLootPacket>(HandleLootPacket);
                client.RegisterPacket<RealismAssetPacket>(HandleAssetPacket);
                client.RegisterPacket<RealismGasEventPacket>(HandleGasEvent);
                client.RegisterPacket<RealismMapRadPacket>(HandleMapRadPacket);
                client.RegisterPacket<RealismInteractablePacket>(HandleInteractionChangeClient);
                client.RegisterPacket<RealismCanTurnValvePacket>(HandleCanTurnValvePacketClient);
                break;
        }
        
    }

    private static void HandleCanTurnValvePacketServer(RealismCanTurnValvePacket packet, NetPeer peer)
    {
        HandleCanTurnValvePacket(packet);
        
        // rebroadcast to other clients except the sender
        Plugin.REAL_Logger.LogInfo($"Rebroadcasting can turn valve   packet to clients: {packet.Path} with value {packet.NextState}");
        Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
        
    }
    
    private static void HandleCanTurnValvePacketClient(RealismCanTurnValvePacket packet)
    {
        HandleCanTurnValvePacket(packet);
    }

    private static void HandleCanTurnValvePacket(RealismCanTurnValvePacket packet)
    {
        Plugin.REAL_Logger.LogInfo($"Processing rotate stuck packet: {packet.Path}, adjusting to {packet.NextState}");

        var zoneObject = GameObject.Find(packet.Path);
        if (zoneObject == null)
        {
            Plugin.REAL_Logger.LogError($"Could not find zone object: {packet.Path}");
            return;
        }
        
        var zone = zoneObject.GetComponent<InteractionZone>();
        if (zone == null)
        {
            Plugin.REAL_Logger.LogInfo($"Zone {packet.Path} could not be fetched");
            return;
        }
        
        
        // can turn valve call. Use reflection to get the method
        Plugin.REAL_Logger.LogInfo($"Attempting to call CanTurnValve on zone: {packet.Path}");
        var targetMethod = typeof(InteractionZone).GetMethod("CanTurnValve", BindingFlags.Instance | BindingFlags.NonPublic);
        if (targetMethod == null)
        {
            Plugin.REAL_Logger.LogError($"Could not find CanTurnValve on {packet.Path}");
        }
        targetMethod?.Invoke(zone, [packet.NextState]);
    }

    private static void HandleInteractionChangeServer(RealismInteractablePacket packet, NetPeer peer)
    {
        HandleInteractionChangePacket(packet);
        
        // rebroadcast to other clients except the sender
        Plugin.REAL_Logger.LogInfo($"Rebroadcasting interaction change packet to clients: {packet.Path} with value {packet.InteractableState}");
        Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered, peer);
    }

    private static void HandleInteractionChangeClient(RealismInteractablePacket packet)
    {
        HandleInteractionChangePacket(packet);
    }

    private static void HandleInteractionChangePacket(RealismInteractablePacket packet)
    {
        Plugin.REAL_Logger.LogInfo($"Processing interaction change packet: {packet.Path}, adjusting to {packet.InteractableState}");
        
        var zoneObject = GameObject.Find(packet.Path);
        if (zoneObject == null)
        {
            Plugin.REAL_Logger.LogError($"Could not find zone object: {packet.Path}");
            return;
        }

        var zone = zoneObject.GetComponent<InteractionZone>();
        if (zone == null)
        {
            Plugin.REAL_Logger.LogInfo($"Zone {packet.Path} could not be fetched");
            return;
        }
        
        
        if(packet.InteractableState == EInteractableState.None)
        {
            Plugin.REAL_Logger.LogInfo($"Interactable {packet.InteractableType} state is none? No clue how to sync this yet.");
            return;
        }
        

        
        switch (packet.InteractableType)
        {
            case EIneractableType.Valve:
                
                if (packet.InteractableState == EInteractableState.On)
                {
                    Plugin.REAL_Logger.LogInfo($"Turning valve on at: {packet.Path}");
                    zone.TurnONValve();
                }
                else if(packet.InteractableState == EInteractableState.Off)
                {
                    Plugin.REAL_Logger.LogInfo($"Turning valve off at: {packet.Path}");
                    zone.TurnOffValve();
                }
                
                break;
            case EIneractableType.Button:
                if (packet.InteractableState == EInteractableState.On)
                {
                    Plugin.REAL_Logger.LogInfo($"Turning button on at: {packet.Path}");
                    zone.TurnOnButton();
                }
                else if (packet.InteractableState == EInteractableState.Off)
                {
                    Plugin.REAL_Logger.LogInfo($"Turning button off at: {packet.Path}");
                    zone.TurnOffButton();
                }
                
                break;
            default:
                Plugin.REAL_Logger.LogError($"Unknown interactable type: {packet.Path} | {packet.InteractableType}");
                break;
        }
    }

    private static void HandleGasEvent(RealismGasEventPacket packet)
    {
        // mirror what Realism Mod is doing
        Plugin.REAL_Logger.LogInfo("Starting Gas Event");
        Core.DoMapGasEvent = true;
        Player player = RealismMod.Utils.GetYourPlayer();
        AmbientAudioInitializer.CreateAmbientAudioPlayer(player, player.gameObject.transform, RealismMod.Plugin.RealismAudioController.GasEventAudioClips, volume: 1.2f, minDelayBeforePlayback: 60f); //spooky short playback
        AmbientAudioInitializer.CreateAmbientAudioPlayer(player, player.gameObject.transform, RealismMod.Plugin.RealismAudioController.GasEventLongAudioClips, true, 5f, 30f, 0.2f, 55f, 65f, minDelayBeforePlayback: 0f); //long ambient
    }

    private static void HandleMapRadPacket(RealismMapRadPacket packet)
    {
        // mirror what Realism Mod is doing 
        Plugin.REAL_Logger.LogInfo($"Starting Map Radiation");
        Core.DoMapRad = true;
        Player player = RealismMod.Utils.GetYourPlayer();
        AmbientAudioInitializer.CreateAmbientAudioPlayer(player, player.gameObject.transform, RealismMod.Plugin.RealismAudioController.RadEventAudioClips, volume: 1f, minDelayBeforePlayback: 60f); //thunder
    }

    private static void HandleAssetPacket(RealismAssetPacket packet)
    {
        Plugin.REAL_Logger.LogInfo($"Received asset packet: {packet.AssetName} at position {packet.Position} rotation {packet.Rotation}");
        Core.LoadAsset(packet.AssetName, packet.Position, packet.Rotation);
    }

    private static void HandleLootPacket(RealismLootPacket packet)
    {
        Plugin.REAL_Logger.LogInfo($"Received loot packet: {packet.MongoID} at {packet.Position}|{packet.Rotation} of template {packet.TemplateId}");
        Core.LoadLooseLoot(packet.Position, packet.Rotation, packet.TemplateId, packet.MongoID);
    }

    private static void HandleHazardPacket(RealismHazardPacket packet)
    {
        
        Plugin.REAL_Logger.LogInfo($"Hazard Packet received. Zone: {packet.ZoneKey} Type: {(int)packet.ZoneType}");

        Core.HazardGroups.TryGetValue(packet.ZoneKey, out var hazardGroup);
        if (hazardGroup == null)
        {
            Plugin.REAL_Logger.LogError($"Hazard group {packet.ZoneKey} not found. Can't spawn.");
            return;
        }

        Plugin.REAL_Logger.LogInfo($"Updating cache zone results for: {packet.ZoneKey}");
        Core.ZoneResults.AddOrUpdate(packet.ZoneKey, true, (key, oldValue) => true);
        
        // immediately spawn in the zones when the packet is received
        switch (packet.ZoneType)
        {
            case EZoneType.Gas:
            case EZoneType.GasAssets:
                Plugin.REAL_Logger.LogInfo($"Spawning Gas Zone/Gas Assets: {packet.ZoneKey}");
                ZoneSpawner.CreateZone<GasZone>(hazardGroup, packet.ZoneType);
                break;
            case EZoneType.Radiation:
            case EZoneType.RadAssets:
                Plugin.REAL_Logger.LogInfo($"Spawning Radiation Zone/Radiation Assets: {packet.ZoneKey}");
                ZoneSpawner.CreateZone<RadiationZone>(hazardGroup, packet.ZoneType);
                break;
            case EZoneType.Interactable:
                Plugin.REAL_Logger.LogInfo($"Spawning Interactable Zones at : {packet.ZoneKey}");
                ZoneSpawner.CreateZone<InteractionZone>(hazardGroup, EZoneType.Interactable);
                break;
            case EZoneType.SafeZone:
                Plugin.REAL_Logger.LogInfo($"Spawning Safe Zones at: {packet.ZoneKey}");
                ZoneSpawner.CreateZone<LabsSafeZone>(hazardGroup, EZoneType.SafeZone);
                break;
            case EZoneType.Quest:
                Plugin.REAL_Logger.LogInfo($"Spawning Quest Zones at: {packet.ZoneKey}");
                ZoneSpawner.CreateZone<QuestZone>(hazardGroup, EZoneType.Quest);
                break;
            default:
                Plugin.REAL_Logger.LogWarning($"Unknown Zone Type: {(int)packet.ZoneType} for key {packet.ZoneKey}");
                break;
            
        }
    }

    private static void LoadZones(EZoneType zoneType, string map)
    {
        Plugin.REAL_Logger.LogInfo($"Loading Realism zones for {map} and type {zoneType}");
        var zones = ZoneData.GetZones(zoneType, map);
        if (zones != null)
        {
            foreach (var zone in zones)
            {
                var zoneKey = Core.GenerateZoneKey(zone, zoneType);
                Plugin.REAL_Logger.LogInfo($"Zone: {zoneKey} is cached");
                Core.HazardGroups.AddOrUpdate(zoneKey, zone, (s, group) => zone);
            }
        }
        else
        {
            Plugin.REAL_Logger.LogInfo($"Realism returned null for Zone data for zones of type {zoneType} for  {map} could be loaded");
        }
    }
    
    private static void OnGameWorldStarted(FikaGameCreatedEvent ev)
    {
        Plugin.REAL_Logger.LogInfo("Initializing Zone Results and loading Zone data");

        // handle any initiation when the game world starts here
        if (Core.ZoneResults == null)
        {
            Core.ZoneResults = new ConcurrentDictionary<string, bool>();
        }
        
        Core.ZoneResults.Clear();

        
        if (Core.ZoneWillSpawn == null)
        {
            Core.ZoneWillSpawn = new ConcurrentDictionary<string, bool>();
        }
        
        Core.ZoneWillSpawn.Clear();

        // make sure this is set to false. This only impacts Fika Clients realistically
        Core.DoMapGasEvent = false;
        Core.DoMapRad = false;

        var map = ((CoopGame)ev.Game).Location_0.Id.ToLower();
        
        LoadZones(EZoneType.Gas, map);
        LoadZones(EZoneType.GasAssets, map);
        LoadZones(EZoneType.Radiation, map);
        LoadZones(EZoneType.RadAssets, map);
        LoadZones(EZoneType.Interactable, map);
        LoadZones(EZoneType.SafeZone, map);
        LoadZones(EZoneType.Quest, map);

    }

    
}