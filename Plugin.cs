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
    [BepInPlugin("RealismMod.Sync", "RealismModSync", "1.0.4")]
    [BepInDependency("com.fika.core", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("RealismMod", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.kobethuy.BringMeToLifeMod", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.DrakiaXYZ.QuestsExtended", BepInDependency.DependencyFlags.SoftDependency)]
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
            Health.Config.Bind(Config);
            QuestExtended.Config.Bind(Config);
            REAL_Logger.LogInfo($"{nameof(Plugin)} has bound settings");

            // Patch
            StanceReplication.Patch.Awake();
            HazardZones.Patch.Awake();
            Audio.Patch.Awake();
            Health.Patch.Awake();
            QuestExtended.Patch.Awake();
            REAL_Logger.LogInfo($"{nameof(Plugin)} has patched methods");

            // Core Initialize
            StanceReplication.Core.Initialize();
            HazardZones.Core.Initialize();
            Audio.Core.Initialize();
            Health.Core.Initialize();
            QuestExtended.Core.Initialize();
            REAL_Logger.LogInfo($"{nameof(Plugin)} has initialized core variables");

            // Fika 
            StanceReplication.Fika.Register();
            HazardZones.Fika.Register();
            Audio.Fika.Register();
            Health.Fika.Register();
            QuestExtended.Fika.Register();
            REAL_Logger.LogInfo($"{nameof(Plugin)} has registered Fika events");

            REAL_Logger.LogInfo($"{nameof(Plugin)} has been loaded.");
        }
    }
}
