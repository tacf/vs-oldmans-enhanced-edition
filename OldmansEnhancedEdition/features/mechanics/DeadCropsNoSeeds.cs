using HarmonyLib;
using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Common;

using Vintagestory.GameContent;

#nullable disable

namespace OldMansEnhancedEdition.Features.Mechanics;

[HarmonyPatchCategory("omed_deadcrops_noseeds")]
public class DeadCropsNoSeeds : IFeature
{
    private readonly string _patchCategoryName = "omed_deadcrops_noseeds";
    private Harmony _harmony;


    public EnumAppSide Side => EnumAppSide.Server;

    public bool Initialize()
    {
        if (ModConfig.Instance.DeadCropsDontDropSeeds == false)
            return false;
        _harmony = OldMansEnhancedEditionModSystem.NewPatch("Dead Crops drop no seeds", _patchCategoryName);
        return true;
    }

    public void Teardown()
    {
        _harmony.UnpatchCategory(_patchCategoryName);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockDeadCrop), nameof(BlockDeadCrop.GetDrops))]
    public static bool GetDrops_PrefixPatch(ref ItemStack[] __result)
    {
        __result = [];
        return false;
    }
    
}