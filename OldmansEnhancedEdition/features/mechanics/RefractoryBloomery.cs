using HarmonyLib;
using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

#nullable disable

namespace OldMansEnhancedEdition.Features.Mechanics;

[HarmonyPatchCategory("omed_bloomery_refractory")]
public class RefractoryBloomery : IFeature
{
    private readonly string _patchCategoryName = "omed_bloomery_refractory";
    private Harmony _harmony;


    public EnumAppSide Side => EnumAppSide.Server;

    public bool Initialize()
    {
        _harmony = OldMansEnhancedEditionModSystem.NewPatch("Refractory Bloomery", _patchCategoryName);
        Logger.Debug("Refractory Bloomery ... initialized");
        return true;
    }

    public void Teardown()
    {
        _harmony.UnpatchCategory(_patchCategoryName);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(BlockBloomery), nameof(BlockBloomery.OnBlockBroken))]
    public static void OnBlockBroken_PostfixPatch(IWorldAccessor world,
        BlockPos pos,
        IPlayer byPlayer,
        float dropQuantityMultiplier = 1f)
    {
        Block aboveBlock = world.BlockAccessor.GetBlock(pos.UpCopy());
        if (aboveBlock.Code.Path.StartsWith("bloomerychimneyrefractory"))
        {
            aboveBlock.OnBlockBroken(world, pos.UpCopy(), byPlayer, dropQuantityMultiplier);
        }
    }
    
}