using System.Collections.Generic;
using Cairo;
using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Color = System.Drawing.Color;

#nullable disable

namespace OldMansEnhancedEdition.Features.UI;

public class FloatingDamageText : IFeature
{
    // Hardcoded defaults
    private const float FloatSpeed = 1f; // Units per second upward
    private const float LifetimeSeconds = 2f;
    private const float FontSize = 28f;

    private static ICoreClientAPI _capi;
    private static readonly List<DamageTextEntry> DamageTexts = new();
    private static readonly object Lock = new();
    private static readonly Dictionary<long, float> EntityHealthCache = new();
    private long _healthCheckTickId;

    private const int HealthCheckIntervalMs = 100;
    private static readonly int DefaultDamageColor = Color.Red.ToArgb();

    public FloatingDamageText(ICoreClientAPI api)
    {
        _capi = api;
    }

    public EnumAppSide Side => EnumAppSide.Client;

    public bool Initialize()
    {
        if (ModConfig.Instance.FloatingCombatText == false)
            return false;
        _capi.Event.RegisterRenderer(new DamageTextRenderer(_capi, DamageTexts, Lock), EnumRenderStage.Ortho);
        _healthCheckTickId = _capi.Event.RegisterGameTickListener(OnHealthCheckTick, HealthCheckIntervalMs);
        return true;
    }

    public void Teardown()
    {
        _capi.Event.UnregisterGameTickListener(_healthCheckTickId);
        lock (Lock)
        {
            DamageTexts.Clear();
            EntityHealthCache.Clear();
        }
    }

    private void OnHealthCheckTick(float dt)
    {
        EntityPlayer player = _capi.World.Player?.Entity;
        if (player == null) return;

        // Check entities near the player
        Entity[] entities = _capi.World.GetEntitiesAround(
            player.Pos.XYZ,
            50f, 50f,
            e => e is EntityAgent && e.Alive
        );

        List<long> toRemove = new List<long>();

        lock (Lock)
        {
            // Check for health changes
            foreach (Entity entity in entities)
            {
                ITreeAttribute healthTree = entity.WatchedAttributes.GetTreeAttribute("health");
                if (healthTree == null) continue;

                float currentHealth = healthTree.GetFloat("currenthealth", 0);
                long entityId = entity.EntityId;

                if (EntityHealthCache.TryGetValue(entityId, out float previousHealth))
                {
                    float damage = previousHealth - currentHealth;
                    if (damage > 0.01f) // Health decreased = damage taken
                    {
                        float entityHeight = entity.SelectionBox?.Y2 ?? entity.CollisionBox?.Y2 ?? 1.0f;
                        Vec3d pos = entity.Pos.XYZ.Add(0, entityHeight + 0.5, 0);

                        DamageTexts.Add(new DamageTextEntry
                        {
                            Position = pos.Clone(),
                            Damage = damage,
                            SpawnTime = _capi.ElapsedMilliseconds,
                            Color = DefaultDamageColor
                        });
                    }
                }

                EntityHealthCache[entityId] = currentHealth;
            }

            // Clean up cache for entities no longer nearby
            foreach (long cachedId in EntityHealthCache.Keys)
            {
                bool found = false;
                foreach (Entity entity in entities)
                {
                    if (entity.EntityId == cachedId) { found = true; break; }
                }
                if (!found) toRemove.Add(cachedId);
            }

            foreach (long id in toRemove)
            {
                EntityHealthCache.Remove(id);
            }
        }
    }

    public class DamageTextEntry
    {
        public Vec3d Position { get; set; }
        public float Damage { get; set; }
        public long SpawnTime { get; set; }
        public int Color { get; set; }
    }

    private class DamageTextRenderer : IRenderer
    {
        private readonly ICoreClientAPI _api;
        private readonly List<DamageTextEntry> _texts;
        private readonly object _lock;

        public DamageTextRenderer(ICoreClientAPI api, List<DamageTextEntry> texts, object lockObj)
        {
            _api = api;
            _texts = texts;
            _lock = lockObj;
        }

        public double RenderOrder => 1.0;
        public int RenderRange => 100;

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            long now = _api.ElapsedMilliseconds;
            List<DamageTextEntry> toRemove = new List<DamageTextEntry>();

            lock (_lock)
            {
                foreach (DamageTextEntry entry in _texts)
                {
                    float age = (now - entry.SpawnTime) / 1000f;

                    if (age > LifetimeSeconds)
                    {
                        toRemove.Add(entry);
                        continue;
                    }

                    // Calculate position (float upward)
                    Vec3d renderPos = entry.Position.Clone();
                    renderPos.Y += age * FloatSpeed;

                    // Calculate alpha (fade out)
                    float alpha = 1f - (age / LifetimeSeconds);

                    // Project 3D world position to screen (ViewMat handles camera transform)
                    Vec3d screenPos = MatrixToolsd.Project(
                        renderPos,
                        _api.Render.PerspectiveProjectionMat,
                        _api.Render.PerspectiveViewMat,
                        _api.Render.FrameWidth,
                        _api.Render.FrameHeight
                    );

                    // Only render if in front of camera
                    if (screenPos == null || screenPos.Z < 0) continue;

                    string text = entry.Damage.ToString("0.#");

                    // Extract color components and apply alpha for fade-out
                    Color color = Color.FromArgb(entry.Color);
                    double[] textColor = { color.R / 255.0, color.G / 255.0, color.B / 255.0, alpha };
                    double[] strokeColor = { 1, 1, 1, alpha }; // Black stroke

                    float screenX = (float)screenPos.X;
                    float screenY = _api.Render.FrameHeight - (float)screenPos.Y;

                    // Create font with stroke (border) built-in
                    CairoFont font = CairoFont.WhiteMediumText()
                        .WithFontSize(FontSize)
                        .WithColor(textColor)
                        .WithWeight(FontWeight.Bold)
                        .WithStroke(strokeColor, 2.0);

                    // Render single texture with stroke
                    LoadedTexture texture = _api.Gui.TextTexture.GenTextTexture(text, font);
                    _api.Render.Render2DTexture(
                        texture.TextureId,
                        screenX - texture.Width / 2,
                        screenY - texture.Height / 2,
                        texture.Width,
                        texture.Height,
                        50
                    );
                    texture.Dispose();
                }

                foreach (DamageTextEntry entry in toRemove)
                {
                    _texts.Remove(entry);
                }
            }
        }

        public void Dispose() { }
    }
}
