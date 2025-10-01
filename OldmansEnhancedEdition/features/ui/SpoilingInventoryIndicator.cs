using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Vintagestory.API.Common;

using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Client;
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
    private static ICoreAPI _api;
    private static IClientPlayer _player;
    private Harmony _harmony;
    private static long _playerAwaitListenerId;
    private const string RotSlotColor = "#aeb79a";

    public EnumAppSide Side => EnumAppSide.Universal;

    public SpoilingInventoryIndicator(ICoreAPI api)
    {
        _api = api;
    }

    public bool Initialize()
    {
        // We seem to need both setups in order to have the proper access to update the player inventory when
        // player is interacting with it.
        switch (_api.Side)
        {
            case EnumAppSide.Client:
                _playerAwaitListenerId = ((ICoreClientAPI)_api).Event.RegisterGameTickListener(CheckPlayerReady, 200);
                break;
            case EnumAppSide.Server:
                _harmony = OldMansEnhancedEditionModSystem.NewPatch("Spoiling Inventory Indicator", _patchCategoryName);
                break;
        }
        return true;
    }
    
    private void CheckPlayerReady(float dt)
    {
        if (((ICoreClientAPI)_api).PlayerReadyFired)
        {
            _player = ((ICoreClientAPI)_api).World.Player;
            foreach ((string invKey, IInventory inv) in _player.InventoryManager.Inventories)
            {
                if (!IsValidInventoryType(inv)) continue;

                inv.SlotModified += slotId => SlotModified(invKey, slotId);
            }
            ((ICoreClientAPI)_api).Event.UnregisterGameTickListener(_playerAwaitListenerId);
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
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialogInventory), nameof(GuiDialogInventory.OnGuiOpened))]
    public static void BlockEntityDialogo_OnGuiOpened_PrefixPatch(ICoreClientAPI ___capi, IInventory ___backPackInv)
    {
        if (___capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Creative) return;
        foreach (int i in Enumerable.Range(0, ___backPackInv.Count))
        {
            UpdateSlotColor(___backPackInv[i]);
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(GuiDialogBlockEntityInventory), nameof(GuiDialogBlockEntityInventory.OnGuiOpened))]
    public static void BlockEntityDialog_OnGuiOpened_PrefixPatch(GuiDialogBlockEntityInventory __instance)
    {
        foreach (int i in Enumerable.Range(0, __instance.Inventory.Count))
        {
            UpdateSlotColor(__instance.Inventory[i]);
        }
    }
    
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InventoryBase), nameof(InventoryBase.OnItemSlotModified))]
    public static void BlockEntityDialog_OnGuiClosed_PostfixPatch(ItemSlot slot)
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
            itemSlot.Itemstack?.Collectible.UpdateAndGetTransitionStates(_api.World, itemSlot);
        return  transitionStates?.Any(state => state.Props.Type is EnumTransitionType.Perish && state.TransitionLevel > 0) ?? false;
    }
}