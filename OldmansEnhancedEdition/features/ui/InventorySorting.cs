using System.Collections.Generic;
using System.Linq;
using OldMansEnhancedEdition.Features.UI.Utils;
using OldMansEnhancedEdition.Utils;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

#nullable disable

namespace OldMansEnhancedEdition.Features.UI;

public class InventorySorting : IFeature
{
    private static ICoreClientAPI _capi;
    private static InventorySortWidget _sortWidget;
    private GuiDialog _playerInventoryDialog;

    public EnumAppSide Side => EnumAppSide.Client;

    public InventorySorting(ICoreClientAPI capi)
    {
        _capi = capi;
    }

    public bool Initialize()
    {
        _sortWidget = new InventorySortWidget(_capi);
        
        foreach (GuiDialog dialog in _capi.Gui.LoadedGuis)
        {
            if (dialog is GuiDialogInventory invDialog)
            {
                _playerInventoryDialog = invDialog;
                break;
            }
        }

        if (_playerInventoryDialog != null)
        {
            _playerInventoryDialog.OnOpened += OnPlayerInventoryOpened;
            _playerInventoryDialog.OnClosed += OnPlayerInventoryClosed;
            Logger.Log("InventorySorting: Hooked into player inventory dialog events");
        }
        else
        {
            Logger.Error("InventorySorting: Could not find player inventory dialog");
        }


        _capi.Input.RegisterHotKey("omed_stash_hotbar", "Stash active hotbar item", GlKeys.V, HotkeyType.InventoryHotkeys, false, false, false);
        _capi.Input.SetHotKeyHandler("omed_stash_hotbar", (KeyCombination key) => InventoryUtil.ClearHandSlot(_capi));

        return true;
    }

    public void Teardown()
    {
        if (_playerInventoryDialog != null)
        {
            _playerInventoryDialog.OnOpened -= OnPlayerInventoryOpened;
            _playerInventoryDialog.OnClosed -= OnPlayerInventoryClosed;
        }
        _sortWidget?.TryClose();
        _sortWidget?.Dispose();
    }

    private void OnPlayerInventoryOpened()
    {
        if (_capi?.World?.Player?.WorldData?.CurrentGameMode == EnumGameMode.Creative) return;
        _sortWidget?.OnPlayerInventoryOpened(_playerInventoryDialog as GuiDialogInventory);
    }

    private void OnPlayerInventoryClosed()
    {
        _sortWidget?.OnPlayerInventoryClosed();
    }
}

public class InventorySortWidget : HudElement
{
    private GuiDialogInventory _playerInventoryDialog;
    private long _updateListenerId;

    public InventorySortWidget(ICoreClientAPI capi) : base(capi)
    {
        SetupDialog();
    }

    private void SetupDialog()
    {
        double buttonWidth = 30;
        double buttonHeight = 24;
        double spacing = 4;

        ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.None);

        ElementBounds sortBounds = ElementBounds.Fixed(0, 0, buttonWidth, buttonHeight);
        ElementBounds pushBounds = ElementBounds.Fixed(buttonWidth + spacing, 0, buttonWidth, buttonHeight);
        ElementBounds pullBounds = ElementBounds.Fixed((buttonWidth + spacing) * 2, 0, buttonWidth, buttonHeight);

        ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(4);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        bgBounds.WithChildren(sortBounds, pushBounds, pullBounds);

        SingleComposer = capi.Gui.CreateCompo("omed_sort_widget", dialogBounds)
            .AddDialogBG(bgBounds, false)
            .AddSmallButton("S", OnSort, sortBounds, EnumButtonStyle.Small)
            .AddSmallButton("↑", OnPush, pushBounds, EnumButtonStyle.Small)
            .AddSmallButton("↓", OnPull, pullBounds, EnumButtonStyle.Small)
            .Compose();
    }

    public void OnPlayerInventoryOpened(GuiDialogInventory dialog)
    {
        _playerInventoryDialog = dialog;

        _updateListenerId = capi.Event.RegisterGameTickListener(UpdatePosition, 20);

        UpdatePositionNow();
        TryOpen();
    }

    public void OnPlayerInventoryClosed()
    {
        _playerInventoryDialog = null;
        capi.Event.UnregisterGameTickListener(_updateListenerId);
        TryClose();
    }

    private void UpdatePosition(float dt)
    {
        UpdatePositionNow();
    }

    private void UpdatePositionNow()
    {
        if (_playerInventoryDialog == null) return;
        
        GuiComposer composer = _playerInventoryDialog.Composers?["maininventory"];
        if (composer == null) return;

        ElementBounds parentBounds = composer.Bounds;
        if (parentBounds == null) return;

        // Calculate centered position below the player inventory
        double widgetWidth = SingleComposer.Bounds.OuterWidth;
        double centerX = parentBounds.absX + (parentBounds.OuterWidth - widgetWidth) / 2;
        double belowY = parentBounds.absY + parentBounds.OuterHeight + 5;

        SingleComposer.Bounds.absFixedX = centerX;
        SingleComposer.Bounds.absFixedY = belowY;
    }

    private bool OnSort()
    {
        List<IInventory> inventories = capi.World.Player.InventoryManager.OpenedInventories;
        bool hasPlayerInventory = inventories.Any(inv => inv is InventoryBasePlayer);

        // Push hotbar items to backpack and sort it
        if (hasPlayerInventory)
        {
            PushHotbarToBackpack();
            InventoryUtil.SortBackpack(capi);
        }

        // Sort any open containers
        foreach (IInventory inv in inventories)
        {
            if (inv is not InventoryBasePlayer)
            {
                InventoryUtil.SortInventory(capi, inv);
            }
        }
        return true;
    }

    private void PushHotbarToBackpack()
    {
        IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
        IInventory backpack = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);

        if (hotbar == null || backpack == null) return;

        foreach (ItemSlot hotbarSlot in hotbar)
        {
            if (hotbarSlot.Empty) continue;

            // Only try to stack with existing items in backpack
            foreach (ItemSlot backpackSlot in backpack)
            {
                if (hotbarSlot.Empty) break;
                if (backpackSlot.Empty) continue;

                // Check if same item type and backpack slot has room
                if (backpackSlot.Itemstack.Collectible == hotbarSlot.Itemstack.Collectible &&
                    backpackSlot.StackSize < backpackSlot.MaxSlotStackSize)
                {
                    ItemStackMoveOperation op = new ItemStackMoveOperation(
                        capi.World, EnumMouseButton.Left, 0,
                        EnumMergePriority.DirectMerge, hotbarSlot.StackSize);
                    object packet = capi.World.Player.InventoryManager.TryTransferTo(hotbarSlot, backpackSlot, ref op);
                    if (packet != null) capi.Network.SendPacketClient(packet);
                }
            }
        }
    }

    private bool OnPush()
    {
        // Push backpack items to open containers
        InventoryUtil.SortIntoInventory(capi);

        // Then push hotbar items to backpack (stacking only)
        PushHotbarToBackpack();
        return true;
    }

    private bool OnPull()
    {
        InventoryUtil.PullInventories(capi);
        return true;
    }

    public override string ToggleKeyCombinationCode => null;
}
