using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;

using OldMansEnhancedEdition.Utils;
using Vintagestory.Common;

#nullable disable

namespace OldMansEnhancedEdition.Features.Recipies;

[HarmonyPatchCategory("omed_craft_only_quenched_items")]
public class CraftOnlyQuenchedItems : IFeature
{
    private readonly string _patchCategoryName = "omed_craft_only_quenched_items";
    private static ICoreAPI _api;
    private static Notifier _notifier;
    private Harmony _harmony;


    public EnumAppSide Side => EnumAppSide.Client;

    public CraftOnlyQuenchedItems(ICoreAPI api)
    {
        _api = api;
    }

    public bool Initialize()
    {
        _harmony = OldMansEnhancedEditionModSystem.NewPatch("Craft Quenched Items Only", _patchCategoryName);
        _notifier = new Notifier(_api);
        Logger.Debug("Disallow Craft hot Items ... initialized");
        return true;
    }

    public void Teardown()
    {
        _harmony.UnpatchCategory(_patchCategoryName);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryCraftingGrid), "FindMatchingRecipe")]
    public static bool FindMatchingRecipe_PrefixPatch(ItemSlot[] ___slots)
    {
        bool allCold = ___slots.All(x =>
        {
            float? temp = x.Itemstack?.Collectible?.GetTemperature(_api.World, x.Itemstack);
            return  temp is null or <= 200;
        });
        if (_api.Side == EnumAppSide.Client && !allCold)
        {
            _notifier.SendPlayerWarning("nocrafthotitems", "Ingredients to hot to craft");
        }
        return allCold;
    }
    
}