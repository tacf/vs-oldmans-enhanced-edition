using System;
using System.Collections.Generic;
using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

#nullable disable

namespace OldMansEnhancedEdition.Features.Mechanics;

public class HoneyParticles : IFeature
{
    // Hardcoded defaults
    private const int TickIntervalMs = 1000; // Check every 1 second
    private const int SearchRadius = 32;
    private const float SpawnChance = 0.5f; // 30% chance per tick per nest
    private const int MinParticles = 1;
    private const int MaxParticles = 3;
    private const float MinSize = 0.4f;
    private const float MaxSize = 0.6f;
    private const float ParticleLifespan = 4f;

    private static readonly int HoneyColor = ColorUtil.ToRgba(255, 255, 210, 80); // Bright golden amber

    private readonly ICoreClientAPI _capi;
    private static readonly Random Rand = new();
    private long _tickListenerId;

    public HoneyParticles(ICoreClientAPI api)
    {
        _capi = api;
    }

    public EnumAppSide Side => EnumAppSide.Client;

    public bool Initialize()
    {
        _tickListenerId = _capi.World.RegisterGameTickListener(OnTick, TickIntervalMs);
        return true;
    }

    public void Teardown()
    {
        _capi.World.UnregisterGameTickListener(_tickListenerId);
    }

    private void OnTick(float dt)
    {
        EntityPlayer player = _capi.World.Player?.Entity;
        if (player == null) return;

        BlockPos playerPos = player.Pos.AsBlockPos;
        BlockPos minPos = playerPos.AddCopy(-SearchRadius, -SearchRadius, -SearchRadius);
        BlockPos maxPos = playerPos.AddCopy(SearchRadius, SearchRadius, SearchRadius);

        _capi.World.BlockAccessor.WalkBlocks(minPos, maxPos, (block, x, y, z) =>
        {
            if (!IsWildBeeNest(block)) return;
            if (Rand.NextDouble() > SpawnChance) return;

            SpawnHoneyParticles(new BlockPos(x, y, z, 0));
        });
    }

    private bool IsWildBeeNest(Block block)
    {
        if (block?.Code == null) return false;
        string code = block.Code.Path;

        // Wild beehives in logs/trees
        return code.Contains("wildbeehive");
    }

    private void SpawnHoneyParticles(BlockPos pos)
    {
        Vec3d spawnPos = new Vec3d(
            pos.X + 0.5 + (Rand.NextDouble() * 0.6 - 0.3),
            pos.Y + 0.3 + (Rand.NextDouble() * 0.4),
            pos.Z + 0.5 + (Rand.NextDouble() * 0.6 - 0.3)
        );

        SimpleParticleProperties particles = new SimpleParticleProperties(
            MinParticles, MaxParticles,
            HoneyColor,
            spawnPos,
            spawnPos.AddCopy(0, 0.1, 0),
            new Vec3f(-0.05f, -0.02f, -0.05f),
            new Vec3f(0.05f, 0.02f, 0.05f),
            ParticleLifespan,
            0.15f, // Very slow gravity - honey drips slowly
            MinSize, MaxSize,
            EnumParticleModel.Cube
        );

        particles.ShouldDieInLiquid = true;
        particles.ShouldDieInAir = false;

        _capi.World.SpawnParticles(particles);
    }
}
