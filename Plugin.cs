using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using Comfort.Common;
using Fika.Core.Modding;
using Fika.Core.Modding.Events;
using Fika.Core.Networking;
using LiteNetLib;
using LiteNetLib.Utils;
using System.Collections.Generic;
using ChartAndGraph;

namespace RealismModSync
{
    [BepInPlugin("RealismMod.Sync", "RealismModSync", "1.0.1")]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("RealismMod", BepInDependency.DependencyFlags.HardDependency)]
    [BepInIncompatibility("com.lacyway.rsr")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource REAL_Logger;

        protected void Awake()
        {
            REAL_Logger = Logger;

            // Bind Config
            StanceReplication.Config.Bind(Config);
            HazardZones.Config.Bind(Config);
            Audio.Config.Bind(Config);
            REAL_Logger.LogInfo($"{nameof(Plugin)} has binded settings");

            // Patch
            StanceReplication.Patch.Awake();
            HazardZones.Patch.Awake();
            Audio.Patch.Awake();
            REAL_Logger.LogInfo($"{nameof(Plugin)} has patched methods");

            // Core Initialize
            StanceReplication.Core.Initialize();
            HazardZones.Core.Initialize();
            Audio.Core.Initialize();
            REAL_Logger.LogInfo($"{nameof(Plugin)} has initialized core variables");

            // Fika 
            StanceReplication.Fika.Register();
            HazardZones.Fika.Register();
            Audio.Fika.Register();
            REAL_Logger.LogInfo($"{nameof(Plugin)} has registered Fika events");

            // anything else? idk I just vibe here

            REAL_Logger.LogInfo($"{nameof(Plugin)} has been loaded.");

        }
    }
}
