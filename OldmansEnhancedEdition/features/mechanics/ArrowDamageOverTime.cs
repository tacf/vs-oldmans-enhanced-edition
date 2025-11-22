using System;
using System.Collections.Generic;
using HarmonyLib;
using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

#nullable disable

namespace OldMansEnhancedEdition.Features.Mechanics;

[HarmonyPatchCategory("omed_arrow_dot")]
public class ArrowDamageOverTime : IFeature
{
    private const string PatchCategoryName = "omed_arrow_dot";

    // Hardcoded defaults
    private const float DamagePercent = 0.05f; // 5% of max health
    private const int TickIntervalMs = 1500; // Every 1.5 seconds
    private const int DurationMs = 5000; // 5 seconds total

    private Harmony _harmony;
    private static ICoreServerAPI _sapi;
    private static readonly Dictionary<long, DotState> AffectedEntities = new();
    private long _tickListenerId;

    public ArrowDamageOverTime(ICoreServerAPI api)
    {
        _sapi = api;
    }

    public EnumAppSide Side => EnumAppSide.Server;

    public bool Initialize()
    {
        _harmony = OldMansEnhancedEditionModSystem.NewPatch("Arrow Damage Over Time", PatchCategoryName);
        _tickListenerId = _sapi.World.RegisterGameTickListener(OnTick, TickIntervalMs);
        return true;
    }

    public void Teardown()
    {
        _harmony?.UnpatchCategory(PatchCategoryName);
        _sapi.World.UnregisterGameTickListener(_tickListenerId);
        AffectedEntities.Clear();
    }

    private void OnTick(float dt)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        List<long> toRemove = new List<long>();

        foreach (KeyValuePair<long, DotState> kvp in AffectedEntities)
        {
            long entityId = kvp.Key;
            DotState state = kvp.Value;

            // Check if DOT has expired
            if (now - state.StartTime > DurationMs)
            {
                toRemove.Add(entityId);
                continue;
            }

            // Check if it's time for next tick
            if (now - state.LastTickTime < TickIntervalMs)
                continue;

            Entity entity = _sapi.World.GetEntityById(entityId);
            if (entity == null || !entity.Alive)
            {
                toRemove.Add(entityId);
                continue;
            }

            // Apply damage: 5% of max health
            float maxHealth = entity.WatchedAttributes.GetTreeAttribute("health")?.GetFloat("maxhealth") ?? 0;
            if (maxHealth <= 0)
            {
                toRemove.Add(entityId);
                continue;
            }

            float damage = maxHealth * DamagePercent;
            entity.ReceiveDamage(new DamageSource
            {
                Source = EnumDamageSource.Internal,
                Type = EnumDamageType.PiercingAttack
            }, damage);

            state.LastTickTime = now;
        }

        foreach (long id in toRemove)
        {
            AffectedEntities.Remove(id);
        }
    }

    public static void ApplyDot(long entityId)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Refresh or start DOT
        AffectedEntities[entityId] = new DotState
        {
            StartTime = now,
            LastTickTime = now - TickIntervalMs // Trigger first tick immediately
        };
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(Entity), nameof(Entity.ReceiveDamage))]
    public static void OnEntityReceiveDamage(Entity __instance, DamageSource damageSource, float damage)
    {
        if (__instance.Api.Side != EnumAppSide.Server) return;
        if (damage <= 0) return;
        if (!__instance.Alive) return;
        if (__instance is EntityPlayer) return; // Don't apply to players

        // Check if damage came from an arrow projectile
        if (damageSource.SourceEntity is not EntityProjectile projectile) return;

        ItemStack projectileStack = projectile.ProjectileStack;
        if (projectileStack?.Collectible?.Code?.Path.Contains("arrow") != true) return;

        ApplyDot(__instance.EntityId);
    }

    private class DotState
    {
        public long StartTime { get; set; }
        public long LastTickTime { get; set; }
    }
}
