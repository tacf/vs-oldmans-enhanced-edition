using System.Collections.Generic;
using HarmonyLib;
using OldMansEnhancedEdition.Features;
using OldMansEnhancedEdition.Features.Mechanics;
using OldMansEnhancedEdition.Features.Recipies;
using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Common;


namespace OldMansEnhancedEdition;

#nullable disable
public class OldMansEnhancedEditionModSystem : ModSystem
{
    public static ModInfo ModInfo;
    
    private List<IFeature>  _features = new() { };
    
    public override void Start(ICoreAPI api)
    {
        ModInfo = Mod.Info;
        Logger.Init(this, api.Logger);
        Logger.Event(" has started initialization");
        
        _features.AddRange([
            new CraftOnlyQuenchedItems(),
            new ManualQuenchItems(api),
            new DeadCropsNoSeeds()
        ]);
        
        LoadFeatures(EnumAppSide.Universal, api);
        Logger.Log(" Finished universal features initialization");
    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        LoadFeatures(EnumAppSide.Server, api);
        Logger.Log(" Finished server features initialization");
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        LoadFeatures(EnumAppSide.Client, api);
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

    private void LoadFeatures(EnumAppSide side, ICoreAPI api)
    {
        foreach (IFeature feature in _features)
        {
            if (feature.Side != side) continue;
            if (!feature.Initialize(api))
                Logger.Error($"Failed to initialize feature {feature.GetType().Name}");
            else
                Logger.Log($"Loaded feature {feature.GetType().Name}");
        }
        Logger.Log(" Finished server initialization");
    }
}