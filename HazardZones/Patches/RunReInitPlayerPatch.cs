using System.Reflection;
using Comfort.Common;
using Fika.Core.Coop.Utils;
using Fika.Core.Networking;
using LiteNetLib;
using RealismMod;
using RealismMod.Audio;
using RealismModSync.HazardZones.Packets;
using SPT.Reflection.Patching;

namespace RealismModSync.HazardZones.Patches;

public class RunReInitPlayerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(RealismAudioController).GetMethod(
            nameof(RealismAudioController.RunReInitPlayer), BindingFlags.Instance | BindingFlags.Public);
    }

    [PatchPostfix]
    public static void Patch()
    {
        if (FikaBackendUtils.IsClient)
        {
            // this patch does not need to execute if it's on a client
            return;
        }
        
        if (GameWorldController.DoMapGasEvent)
        {
            var packet = new RealismGasEventPacket();
            Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }

        if (GameWorldController.DoMapRads)
        {
            var packet = new RealismMapRadPacket();
            Singleton<FikaServer>.Instance.SendDataToAll(ref packet, DeliveryMethod.ReliableOrdered);
        }
    }
}