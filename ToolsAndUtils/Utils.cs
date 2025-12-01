using System;
using System.Linq;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using UnityEngine;

namespace RealismModSync;

public class Utils
{
    public static async Task LoadLoot(Vector3 position, Quaternion rotation, string templateId, string mongoId)
    {
        Item item = Singleton<ItemFactoryClass>.Instance.CreateItem(mongoId, templateId, null);
        item.StackObjectsCount = 1;
        item.SpawnedInSession = true;
        ResourceKey[] resources = item.Template.AllResources.ToArray();
        
        await RealismMod.Utils.LoadBundle(resources);
        
        IPlayer player = Singleton<GameWorld>.Instance.RegisteredPlayers.FirstOrDefault(new Func<IPlayer, bool>(RealismMod.Utils.IPlayerMatches));
        Singleton<GameWorld>.Instance.ThrowItem(item, player, position, rotation, Vector3.zero, Vector3.zero, true, true, EFTHardSettings.Instance.ThrowLootMakeVisibleDelay);
    }

    public static Player GetYourPlayer()
    {
        if (!Singleton<GameWorld>.Instantiated)
            return null;

        var gameWorld = Singleton<GameWorld>.Instance;
        if (gameWorld == null)
            return null;

        return gameWorld.MainPlayer;
    }
}