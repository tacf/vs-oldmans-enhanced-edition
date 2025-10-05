using System.Collections.Generic;
using HarmonyLib;
using OldMansEnhancedEdition.Features;
using OldMansEnhancedEdition.Features.Mechanics;
using OldMansEnhancedEdition.Features.Recipies;
using OldMansEnhancedEdition.Features.UI;
using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;


namespace OldMansEnhancedEdition;

#nullable disable
public class OldMansEnhancedEditionModSystem : ModSystem
{
    private static string _configFile;
    
    public override void Start(ICoreAPI api)
    {
        Logger.Init(this, api.Logger);
        Logger.Event($" loading features (Version: {Mod.Info.Version})");
        Logger.Log(" Finished universal features initialization");
        _configFile = $"{Mod.Info.Name}.json".Replace(" ", "");

        ModConfig.Instance = api.LoadModConfig<ModConfig>(_configFile) ?? new ModConfig();
        api.StoreModConfig(ModConfig.Instance, _configFile);
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        List<IFeature> features =
        [
            new ManualQuenchItems(api),
            new DeadCropsNoSeeds(),
        ];
        LoadFeatures(features);
        Logger.Log(" Finished server features initialization");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        List<IFeature> features =
        [
            new CraftOnlyQuenchedItems(api),
            new CTRLStopOnLadders(api),
            new SpoilingInventoryIndicator(api),
            new HungerCooldownBuff(api),
            new InteractionProgress(api),
        ];
        LoadFeatures(features);
        Logger.Log(" Finished client features initialization");
    }
    

    public static Harmony NewPatch(string description, string category)
    {
        Harmony patcher = null;
        if (!Harmony.HasAnyPatches(category))
        {
            patcher = new Harmony(category);
            patcher.PatchCategory(category);
            Logger.Log($"Patched {description}");
        }
        else Logger.Error($"Patch '{category}' ('{description}') failed. Check if other patches with same id have been loaded");

        return patcher;
    }

    private void LoadFeatures(List<IFeature> features)
    {
        foreach (IFeature feature in features)
        {
            if (!feature.Initialize())
                Logger.Log($"Feature {feature.GetType().Name} not loaded");
            else
                Logger.Log($"Loaded feature {feature.GetType().Name}");
        }
        Logger.Log(" Finished server initialization");
    }
}