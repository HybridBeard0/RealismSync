using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using EFT;
using Fika.Core.Coop.Utils;
using Newtonsoft.Json;
using RealismMod;
using RealismModSync.Audio.Components;
using RealismModSync.StanceReplication.Components;
using UnityEngine;

namespace RealismModSync.Audio;

public static class Core
{

    public static ConcurrentDictionary<int, RSAObservedComponent> ObservedComponents;
    
    public static void Initialize()
    {
        ObservedComponents = new ConcurrentDictionary<int, RSAObservedComponent>();
        
        Plugin.REAL_Logger.LogInfo($"Patching RealismMod Device Audio clips to have names match key");
               
    }
}