using System.Reflection;
using Comfort.Common;
using EFT;
using Fika.Core.Coop.ClientClasses;
using Fika.Core.Coop.Components;
using Fika.Core.Coop.GameMode;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using RealismMod;
using RealismMod.Audio;
using RealismModSync.Audio.Packets;
using SPT.Reflection.Patching;
using UnityEngine;

namespace RealismModSync.Audio.Patches;

public class PlayGeigerClipPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RealismAudioController).GetMethod(
            nameof(RealismAudioController.PlayGeigerClips), BindingFlags.Instance | BindingFlags.Public);
    }

    [PatchPostfix]
    public static void Patch(RealismAudioController __instance, ref AudioSource ____geigerAudioSource)
    {
        CoopHandler.TryGetCoopHandler(out var coopHandler);
        if (coopHandler == null)
        {
            Plugin.REAL_Logger.LogInfo($"CoopHandler is null in Geiger Clip Patch");
            return;
        }
        
        string[] clips = RealismMod.Plugin.RealismAudioController.GetGeigerClip(HazardTracker.BaseTotalRadiationRate);
        if (clips == null) return;
        int rndNumber = Random.Range(0, clips.Length);
        string clip = clips[rndNumber];
        
        var packet = new RealismAudioPacket()
        {
            NetID = coopHandler.MyPlayer.NetId,
            Clip = clip,
            Volume = .32f, // magic number sourced from Realism. Can use reflection to make this more reliable
            DeviceType = RealismDeviceType.Geiger
        };
    
        if (FikaBackendUtils.IsServer)
        {
            Plugin.REAL_Logger.LogInfo($"Sending {clip} as server");
            Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }
        else
        {
            Plugin.REAL_Logger.LogInfo($"Sending {clip} as client");
            Singleton<FikaClient>.Instance.SendData(ref packet, DeliveryMethod.ReliableOrdered);
        }
    }
}