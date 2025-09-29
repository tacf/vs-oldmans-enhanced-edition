using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;


namespace OldMansEnhancedEdition.Features.Mechanics;

public class ManualQuenchItems : IFeature
{
    private static ICoreServerAPI _sapi;
    private static AssetLocation _sizzleSound = new AssetLocation("sounds/sizzle");
    private static HashSet<IServerPlayer> _soundPlayedRegistry = new ();
    
    public EnumAppSide Side => EnumAppSide.Server;

    public ManualQuenchItems(ICoreAPI api)
    {
        _sapi = (api as ICoreServerAPI)!;
    }
    
    public bool Initialize(ICoreAPI api)
    {
        if (_sapi.Side != EnumAppSide.Server)  return false;
        long? listener = _sapi.Event.RegisterGameTickListener(OnGameTick, 200, 0);
        Logger.Debug("ManualQuenching ... initialized");
        return listener > 0;
    }

    private void OnGameTick(float obj)
    {
        foreach (IPlayer? player in _sapi.World.AllPlayers)
        {
            IServerPlayer? sPlayer = (IServerPlayer)player;

            if (!IsEntityExpressingIntentQuenching(sPlayer))
            {
                _soundPlayedRegistry.Remove(sPlayer);
                continue;
            }

            if (!IsTargetSuitable(sPlayer.CurrentBlockSelection)) continue;
            
            PlaySizzleSound(sPlayer);
            QuenchItem(_sapi.World, sPlayer, sPlayer.Entity.ActiveHandItemSlot);
        }
    }

    private static void PlaySizzleSound(IServerPlayer sPlayer)
    {
        if (_soundPlayedRegistry.Contains(sPlayer)) return;
        _sapi.World.PlaySoundAt(_sizzleSound, sPlayer, null, true, 32f, 1f);
        _soundPlayedRegistry.Add(sPlayer);
    }
    
    private static bool IsEntityExpressingIntentQuenching(IServerPlayer? sPlayer)
    {
        return sPlayer != null 
               && sPlayer.Entity.Controls.CtrlKey 
               && sPlayer.Entity.Controls.RightMouseDown
               && sPlayer.Entity.ActiveHandItemSlot?.Itemstack?.Collectible?.GetTemperature(_sapi.World, sPlayer.Entity.ActiveHandItemSlot.Itemstack) > 200;
    }
    
    private static bool IsTargetSuitable(BlockSelection blockSel)
    {
        return blockSel.Block?.GetBlockEntity<BlockEntityContainer>(blockSel) switch
        {
            BlockEntityBarrel barrel => barrel is { Sealed: false, CapacityLitres: > 0 },
            BlockEntityBucket bucket => !bucket.Inventory.Empty,
            _ => false
        };
    }
    
    private static void QuenchItem(IWorldAccessor world, IServerPlayer sPlayer, ItemSlot slot)
    {
        Vec3d particleSpawnPos = sPlayer.CurrentBlockSelection.Block.SelectionBoxes[0].Center;
        float temperature = slot.Itemstack.Collectible.GetTemperature(world, slot.Itemstack);
        if (temperature > 20f)
        {
            slot.Itemstack.Collectible.SetTemperature(world, slot.Itemstack, Math.Max(0f, temperature - 50f), true);
            if (temperature > 200f)
            {
                Entity.SplashParticleProps.BasePos.Set(particleSpawnPos);
                Entity.SplashParticleProps.AddVelocity.Set(0f, 0f, 0f);
                Entity.SplashParticleProps.QuantityMul = 0.1f;
                world.SpawnParticles(Entity.SplashParticleProps, null);
            }
            slot.MarkDirty();
        }
        else
        {
            _soundPlayedRegistry.Remove(sPlayer);
        }
    }


    public void Teardown()
    {
        _soundPlayedRegistry.Clear();
    }
}