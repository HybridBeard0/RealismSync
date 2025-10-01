using System.Reflection;
using Comfort.Common;
using EFT;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using RealismMod;
using RealismMod.Audio;
using RealismModSync.Audio.Packets;
using SPT.Reflection.Patching;
using UnityEngine;

namespace RealismModSync.Audio.Patches;

public class PlayGasAnalyserClipPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RealismAudioController).GetMethod(
            nameof(RealismAudioController.PlayGasAnalyserClips), BindingFlags.Instance | BindingFlags.Public);
    }

    [PatchPostfix]
    public static void Patch(RealismAudioController __instance, ref AudioSource ____gasAnalyserSource)
    {
        CoopHandler.TryGetCoopHandler(out var coopHandler);
        if (coopHandler == null)
        {
            Plugin.REAL_Logger.LogInfo($"CoopHandler is null in Geiger Clip Patch");
            return;
        }


        // recreate how realism handles this.
        // technically I dont need volumeModi.
        string clip = RealismMod.Plugin.RealismAudioController.GetGasAnalsyerClip(HazardTracker.BaseTotalToxicityRate, out float volumeModi);
        if (clip == null)
        {
            Plugin.REAL_Logger.LogInfo($"Could not detect a suitable gas analyzer clip");
            return;
        }
        
        // float volume = _muteGasAnalyser ? 0f : GetDeviceVolume(GEIGER_VOLUME, volumeModi);
        var packet = new RealismAudioPacket()
        {
            NetID = coopHandler.MyPlayer.NetId,
            Clip = clip,
            Volume = volumeModi, // magic number sourced from  RealismMod
            DeviceType = RealismDeviceType.GasAnalyzer
        };

        if (FikaBackendUtils.IsServer)
        {
            Plugin.REAL_Logger.LogInfo($"Sending {packet.Clip} as server with volume {packet.Volume}");
            Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }
        else
        {
            Plugin.REAL_Logger.LogInfo($"Sending {packet.Clip} as client with volume {packet.Volume}");
            Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        }
    }
}