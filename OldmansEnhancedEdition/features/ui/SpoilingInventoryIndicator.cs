using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;

using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;
using Vintagestory.GameContent;

#nullable disable

namespace OldMansEnhancedEdition.Features.UI;

[HarmonyPatchCategory("omed_spoiling_indicator")]
public class SpoilingInventoryIndicator : IFeature
{
    private readonly string _patchCategoryName = "omed_spoiling_indicator";
    private static ICoreClientAPI _capi;
    private static IClientPlayer _player;
    private Harmony _harmony;
    private static long _playerAwaitListenerId;
    private const string RotSlotColor = "#aeb79a";

    public EnumAppSide Side => EnumAppSide.Universal;

    public SpoilingInventoryIndicator(ICoreClientAPI capi)
    {
        _capi = capi;
    }

    public bool Initialize()
    {
        _playerAwaitListenerId = _capi.Event.RegisterGameTickListener(CheckPlayerReady, 200);
        _harmony = OldMansEnhancedEditionModSystem.NewPatch("Spoiling Inventory Indicator", _patchCategoryName);
        Logger.Debug("Spoiling Indicator initialized");
        return true;
    }
    
    private void CheckPlayerReady(float dt)
    {
        if (_capi.PlayerReadyFired)
        {
            _player = _capi.World.Player;
            foreach ((string invKey, IInventory inv) in _player.InventoryManager.Inventories)
            {
                if (!IsValidInventoryType(inv)) continue;

                inv.SlotModified += slotId => SlotModified(invKey, slotId);
            }
            _capi.Event.UnregisterGameTickListener(_playerAwaitListenerId);
        }
    }

    private void SlotModified(string invKey, int slotId)
    {
        InventoryBase inv = (InventoryBase)_player.InventoryManager.Inventories[invKey];
        UpdateSlotColor(inv[slotId]);
    }

    private static bool IsValidInventoryType(IInventory inv)
    {
        var typeName = inv.GetType().Name;
        return typeName is "InventoryPlayerHotbar" or "InventoryPlayerBackPacks";
    }

    public void Teardown()
    {
        _harmony.UnpatchCategory(_patchCategoryName);
        _capi.Event.UnregisterGameTickListener(_playerAwaitListenerId);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialogInventory), nameof(GuiDialogInventory.OnGuiOpened))]
    public static void GuiDialogInventory_OnGuiOpened_PrefixPatch(IInventory ___backPackInv)
    {
        if (_capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative) return;
        foreach (int i in Enumerable.Range(0, ___backPackInv.Count))
        {
            UpdateSlotColor(___backPackInv[i]);
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialogBlockEntityInventory), nameof(GuiDialogBlockEntityInventory.OnGuiOpened))]
    public static void GuiDialogBlockEntityInventory_OnGuiOpened_PrefixPatch(GuiDialogBlockEntityInventory __instance)
    {
        foreach (int i in Enumerable.Range(0, __instance.Inventory.Count))
        {
            UpdateSlotColor(__instance.Inventory [i]);
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryBase), nameof(InventoryBase.OnItemSlotModified))]
    public static void InventoryBase_OnItemSlotModified_PostfixPatch(ItemSlot slot)
    {
        UpdateSlotColor(slot);
    }

    private static void UpdateSlotColor(ItemSlot itemSlot)
    {
        if (itemSlot == null) return;
        if (IsItemSpoiled(itemSlot) && itemSlot.HexBackgroundColor != RotSlotColor)
        {
            itemSlot.HexBackgroundColor = RotSlotColor;
            itemSlot.MarkDirty();
        } else if (!IsItemSpoiled(itemSlot) && itemSlot.HexBackgroundColor == RotSlotColor)
        {
            itemSlot.HexBackgroundColor = null;
            itemSlot.MarkDirty();
        }
    }

    private static bool IsItemSpoiled(ItemSlot itemSlot)
    { 
        
        TransitionState[] transitionStates =
            itemSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(_capi.World, itemSlot);
        if (transitionStates?.Any(state => state.Props.Type is EnumTransitionType.Perish && state.TransitionLevel > 0) ?? false)
            return true;
        
        // Holy Jesus ! Not sure how to fetch this any other way. Pertains to food containers like crocks
        float? fresh = ((itemSlot.Itemstack?.Attributes.GetTreeAttribute("contents")?.Values[0] as ItemstackAttribute)?.value.Attributes
            .GetTreeAttribute("transitionstate")?["freshHours"]?.GetValue() as float[])?[0];
        float? hoursPassed = ((itemSlot.Itemstack?.Attributes.GetTreeAttribute("contents")?.Values[0] as ItemstackAttribute)?.value.Attributes
            .GetTreeAttribute("transitionstate")?["transitionedHours"]?.GetValue() as float[])?[0];
        
        return (hoursPassed > fresh);

    }
}