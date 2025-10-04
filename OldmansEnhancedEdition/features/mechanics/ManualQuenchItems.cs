using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
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
    
    public bool Initialize()
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
                GenerateSteamParticles(sPlayer.CurrentBlockSelection.Position, world);
            }
            slot.MarkDirty();
        }
        else
        {
            _soundPlayedRegistry.Remove(sPlayer);
        }
    }
    
    // https://github.com/anegostudios/vssurvivalmod/blob/90b87707038ed3803e7b76170e9f75d64196ea83/BlockBehavior/BehaviorFiniteSpreadingLiquid.cs#L364
    private static void GenerateSteamParticles(BlockPos pos, IWorldAccessor world)
    {
        float minQuantity = 50;
        float maxQuantity = 100;
        int color = ColorUtil.ToRgba(100, 225, 225, 225);
        Vec3d minPos = new Vec3d();
        Vec3d addPos = new Vec3d();
        Vec3f minVelocity = new Vec3f(-0.25f, 0.1f, -0.25f);
        Vec3f maxVelocity = new Vec3f(0.25f, 0.1f, 0.25f);
        float lifeLength = 2.0f;
        float gravityEffect = -0.015f;
        float minSize = 0.1f;
        float maxSize = 0.1f;

        SimpleParticleProperties steamParticles = new SimpleParticleProperties(
            minQuantity, maxQuantity,
            color,
            minPos, addPos,
            minVelocity, maxVelocity,
            lifeLength,
            gravityEffect,
            minSize, maxSize,
            EnumParticleModel.Quad
        );
        steamParticles.Async = true;
        steamParticles.MinPos.Set(pos.ToVec3d().AddCopy(0.5, 1.1, 0.5));
        steamParticles.AddPos.Set(new Vec3d(0.5, 1.0, 0.5));
        steamParticles.SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEARINCREASE, 1.0f);
        world.SpawnParticles(steamParticles);
    }


    public void Teardown()
    {
        _soundPlayedRegistry.Clear();
    }
}