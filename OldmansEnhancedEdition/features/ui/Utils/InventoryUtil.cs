using System.Linq;

namespace OldMansEnhancedEdition.Features.UI.Utils;

using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

public class InventoryUtil
{
    internal static List<string> sortOrder = new List<string>();
    internal static List<string> stackOrder = new List<string>();
    internal static List<EnumItemStorageFlags> storageFlagsOrder = new List<EnumItemStorageFlags>();

    internal class CollectibleComparer : IComparer<CollectibleObject>
    {
        public int Compare(CollectibleObject x, CollectibleObject y)
        {
            if (x == y) return 0;


            if (sortOrder.Count == 0)
            {
                return x.Id.CompareTo(y.Id);
            }

            int result = 0;
            foreach (string compareType in sortOrder)
            {
                result = Compare(x, y, compareType);
                if (result != 0) return result;
            }

            return result;
        }

        public int Compare(CollectibleObject x, CollectibleObject y, string compareType)
        {
            int result = 0;
            switch (compareType)
            {
                case "id":
                    result = x.Id.CompareTo(y.Id);
                    break;

                case "idinvert":
                    result = y.Id.CompareTo(x.Id);
                    break;

                case "name":
                    result = x.GetHeldItemName(new ItemStack(x)).CompareTo(y.GetHeldItemName(new ItemStack(y)));
                    break;

                case "nameinvert":
                    result = y.GetHeldItemName(new ItemStack(y)).CompareTo(x.GetHeldItemName(new ItemStack(x)));
                    break;

                case "block":
                    result = x.ItemClass - y.ItemClass;
                    break;

                case "item":
                    result = y.ItemClass - x.ItemClass;
                    break;

                case "durability":
                    result = x.Durability.CompareTo(y.Durability);
                    break;

                case "durabilityinvert":
                    result = y.Durability.CompareTo(x.Durability);
                    break;

                case "attackpower":
                    result = x.AttackPower.CompareTo(y.AttackPower);
                    break;

                case "attackpowerinvert":
                    result = y.AttackPower.CompareTo(x.AttackPower);
                    break;

                case "stacksize":
                    result = x.MaxStackSize.CompareTo(y.MaxStackSize);
                    break;

                case "stacksizeinvert":
                    result = y.MaxStackSize.CompareTo(x.MaxStackSize);
                    break;

                case "tool":
                    result = ((int?)x.Tool ?? 100) - ((int?)y.Tool ?? 100);
                    break;

                case "toolinvert":
                    result = ((int?)y.Tool ?? 100) - ((int?)x.Tool ?? 100);
                    break;

                case "tooltier":
                    result = x.ToolTier.CompareTo(y.ToolTier);
                    break;

                case "tooltierinvert":
                    result = y.ToolTier.CompareTo(x.ToolTier);
                    break;

                case "light":
                    result = x.LightHsv[2] - y.LightHsv[2];
                    break;

                case "lightinvert":
                    result = y.LightHsv[2] - x.LightHsv[2];
                    break;

                case "density":
                    result = x.MaterialDensity - y.MaterialDensity;
                    break;

                case "densityinvert":
                    result = y.MaterialDensity - x.MaterialDensity;
                    break;

                case "state":
                    result = x.MatterState - y.MatterState;
                    break;

                case "stateinvert":
                    result = y.MatterState - x.MatterState;
                    break;

                case "satiety":
                    result = (int)(x.NutritionProps?.Satiety ?? 0.0f - y.NutritionProps?.Satiety ?? 0.0f);
                    break;

                case "satietyinvert":
                    result = (int)(y.NutritionProps?.Satiety ?? 0.0f - x.NutritionProps?.Satiety ?? 0.0f);
                    break;

                case "intoxication":
                    result = (int)(x.NutritionProps?.Intoxication ?? 0.0f - y.NutritionProps?.Intoxication ?? 0.0f);
                    break;

                case "intoxicationinvert":
                    result = (int)(y.NutritionProps?.Intoxication ?? 0.0f - x.NutritionProps?.Intoxication ?? 0.0f);
                    break;

                case "health":
                    result = (int)(x.NutritionProps?.Health ?? 0.0f - y.NutritionProps?.Health ?? 0.0f);
                    break;

                case "healthinvert":
                    result = (int)(y.NutritionProps?.Health ?? 0.0f - x.NutritionProps?.Health ?? 0.0f);
                    break;

                case "storageflags":
                    foreach (EnumItemStorageFlags flags in storageFlagsOrder)
                    {
                        EnumItemStorageFlags flgasx = x.StorageFlags & flags;
                        EnumItemStorageFlags flgasy = y.StorageFlags & flags;
                        if (flgasx != flgasy)
                        {
                            result = flgasy - flgasx;
                            break;
                        }
                    }

                    break;


                default:
                    if (compareType.EndsWith("invert"))
                    {
                        string name = compareType.Remove(compareType.Length - 6);
                        result = (int)((y.Attributes?[name]?.AsDouble() ?? 0 - x.Attributes?[name]?.AsDouble() ?? 0) *
                                       1000);
                    }
                    else
                    {
                        result = (int)((x.Attributes?[compareType]?.AsDouble() ??
                                        0 - y.Attributes?[compareType]?.AsDouble() ?? 0) * 1000);
                    }

                    break;
            }

            return result;
        }
    }

    internal class SlotComparer : IComparer<ItemSlot>
    {
        private StackComparer StackComparer = new StackComparer();

        public int Compare(ItemSlot x, ItemSlot y)
        {
            return StackComparer.Compare(x.Itemstack, y.Itemstack);
        }
    }

    internal class StackComparer : IComparer<ItemStack>
    {
        public int Compare(ItemStack x, ItemStack y)
        {
            if (x == y) return 0;
            if (stackOrder.Count == 0) return 0;

            int result = 0;
            foreach (string compareType in stackOrder)
            {
                result = Compare(x, y, compareType);
                if (result != 0) return result;
            }

            return result;
        }

        private static int Compare(ItemStack x, ItemStack y, string compareType)
        {
            int result = 0;
            switch (compareType)
            {
                case "durability":
                    result =
                        x.Collectible.GetRemainingDurability(x).CompareTo(
                            y.Collectible.GetRemainingDurability(y));
                    break;

                case "durabilityinvert":
                    result =
                        y.Collectible.GetRemainingDurability(y).CompareTo(
                            x.Collectible.GetRemainingDurability(x));
                    break;

                case "stacksize":
                    result = y.StackSize.CompareTo(x.StackSize);
                    break;

                case "stacksizeinvert":
                    result = x.StackSize.CompareTo(y.StackSize);
                    break;

                case "transition":
                    result = (int)((TransitionState(x) - TransitionState(y)) * 1000.0f);
                    break;

                case "transitioninvert":
                    result = (int)((TransitionState(y) - TransitionState(x)) * 1000.0f);
                    break;

                default:
                    if (compareType.EndsWith("invert"))
                    {
                        string name = compareType.Remove(compareType.Length - 6);
                        result = (int)((y.Attributes?.GetDecimal(name) ?? 0 - x.Attributes?.GetDecimal(name) ?? 0) *
                                       1000);
                    }
                    else
                    {
                        result = (int)((x.Attributes?.GetDecimal(compareType) ??
                                        0 - y.Attributes?.GetDecimal(compareType) ?? 0) * 1000);
                    }

                    break;
            }

            return result;
        }
    }

    private static float TransitionState(ItemStack itemStack)
    {
        ITreeAttribute attr = (ITreeAttribute)itemStack?.Attributes["transitionstate"];
        if (attr == null) return -99999.0f;
        float trans = -99999.0f;

        float[] freshHours = (attr["freshHours"] as FloatArrayAttribute)?.value;
        float[] transitionHours = (attr["transitionHours"] as FloatArrayAttribute)?.value;
        float[] transitionedHours = (attr["transitionedHours"] as FloatArrayAttribute)?.value;
        int length = Math.Max(Math.Max(freshHours.Length, transitionHours.Length), transitionedHours.Length);

        for (int ii = 0; ii < length; ++ii)
        {
            float freshHour = freshHours.Length > ii ? freshHours[ii] : 0.0f;
            float transitionHour = transitionHours.Length > ii ? transitionHours[ii] : 0.0f;
            float transitionedHour = transitionedHours.Length > ii ? transitionedHours[ii] : 0.0f;
            if (transitionHour <= 0.0f) continue;

            float freshHoursLeft = Math.Max(0.0f, freshHour - transitionedHour);
            float transitionLevel = Math.Max(0.0f, transitionedHour - freshHour) / transitionHour;
            if (freshHoursLeft > 0.0f) trans = Math.Max(trans, -freshHoursLeft);
            if (transitionLevel > 0.0f) trans = Math.Max(trans, transitionLevel);
        }

        return trans;
    }
    

    public static bool SortBackpack(ICoreClientAPI capi)
    {
        IInventory backpack = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
        SortInventory(capi, backpack, 4, true);
        return true;
    }

    public static void SortIntoInventory(ICoreClientAPI capi)
    {
        IInventory backpack = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
        IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);

        List<IInventory> inventories = capi.World.Player.InventoryManager.OpenedInventories;

        foreach (IInventory inventory in inventories)
        {
            if (inventory is InventoryBasePlayer) continue;
            Dictionary<CollectibleObject, int> collectibles = new Dictionary<CollectibleObject, int>();

            if (inventory is InventoryTrader trader)
            {
                foreach (ItemSlotTrade slot in trader.BuyingSlots)
                {
                    if (slot.Itemstack == null || collectibles.ContainsKey(slot.Itemstack.Collectible)) continue;
                    if (slot?.TradeItem?.Stock == 0) continue;
                    if (slot?.Itemstack.Collectible != null)
                        collectibles.Add(slot.Itemstack.Collectible,
                            slot?.StackSize * (slot as ItemSlotTrade)?.TradeItem?.Stock ?? 0);
                }
            }
            else
            {
                foreach (ItemSlot slot in inventory)
                {
                    if (slot.Itemstack != null && !collectibles.ContainsKey(slot.Itemstack.Collectible))
                    {
                        collectibles.Add(slot.Itemstack.Collectible, 0);
                    }
                }
            }

            SortIntoInventory(capi, backpack, inventory, collectibles, 4);
            SortIntoInventory(capi, hotbar, inventory, collectibles, 0);
        }
    }

    public static void SortIntoInventory(ICoreClientAPI capi, IInventory sourceInv, IInventory destInv,
        Dictionary<CollectibleObject, int> collectibles, int first)
    {
        int slotId = -1;
        foreach (ItemSlot slot in sourceInv)
        {
            slotId++;
            if (slotId < first) continue;
            if (slot.Itemstack == null) continue;
            CollectibleObject key = slot.Itemstack.Collectible;
            if (!collectibles.ContainsKey(key)) continue;
            while (slot.StackSize > 0)
            {
                int demand = collectibles[key];
                WeightedSlot dest = destInv.GetBestSuitedSlot(slot);
                if (dest?.slot == null) break;
                if (dest.slot.Itemstack != null)
                    if (dest.slot.Itemstack.StackSize >= dest.slot.Itemstack.Collectible.MaxStackSize) break;

                if (slot is ItemSlotOffhand) break;
                int transfer = demand > 0 ? Math.Min(demand, slot.StackSize) : slot.StackSize;

                ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, 0,
                    EnumMergePriority.DirectMerge, transfer);
                object obj = capi.World.Player.InventoryManager.TryTransferTo(slot, dest.slot, ref op);
                if (obj != null) capi.Network.SendPacketClient(obj);

                //rare case for specific items where GetBestSuitedSlot does not return a valid slot
                if (op.MovedQuantity == 0)
                {
                    foreach (ItemSlot destSlot in destInv)
                    {
                        if (!destSlot.Empty) continue;
                        op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, 0,
                            EnumMergePriority.DirectMerge, transfer);
                        obj = capi.World.Player.InventoryManager.TryTransferTo(slot, destSlot, ref op);
                        if (obj != null) capi.Network.SendPacketClient(obj);
                        break;
                    }

                    if (op.MovedQuantity == 0) break;
                }

                if (demand <= 0) continue;

                demand = demand - op.MovedQuantity;
                if (demand <= 0) collectibles.Remove(key);
                else collectibles[key] = demand;
                break;
            }
        }
    }

    private static int GetFlagCount(long value)
    {
        int count = 0;
        while (value != 0)
        {
            value = value & (value - 1);
            count++;
        }

        return count;
    }

    public static void SortInventory(ICoreClientAPI capi, IInventory inventory, int first = 0, bool lockedSlots = false)
    {
        switch (inventory)
        {
            case InventorySmelting:
            case InventoryTrader:
                return;
        }

        if (inventory.PutLocked || inventory.TakeLocked) return;

        //create dictionary
        int counter = -1;
        SortedDictionary<CollectibleObject, List<ItemSlot>> slots = new(new CollectibleComparer());
        Dictionary<EnumItemStorageFlags, List<ItemSlot>> destDic = new();

        foreach (ItemSlot slot in inventory)
        {
            counter++;
            if (counter < first) continue;
            if (slot is ItemSlotLiquidOnly) continue;

            destDic.TryGetValue(slot.StorageType, out List<ItemSlot> destList);
            if (destList == null)
            {
                destList = new List<ItemSlot>();
                destDic[slot.StorageType] = destList;
            }

            destList.Add(slot);

            if (slot.Itemstack == null)
            {
                continue;
            }

            List<ItemSlot> slotList;
            if (!slots.TryGetValue(slot.Itemstack.Collectible, out slotList))
            {
                slotList = new List<ItemSlot>();
                slots.Add(slot.Itemstack.Collectible, slotList);
            }

            slotList.Add(slot);
        }

        //merge stacks
        bool merged = false;
        foreach (List<ItemSlot> slotList in slots.Values)
        {
            ItemSlot notFull = null;
            List<ItemSlot> removeList = new List<ItemSlot>();
            foreach (ItemSlot slot in slotList)
            {
                if (slot.StackSize < slot.Itemstack.Collectible.MaxStackSize)
                {
                    if (notFull == null)
                    {
                        notFull = slot;
                        continue;
                    }

                    ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, 0,
                        EnumMergePriority.DirectMerge, slot.StackSize);
                    object obj = capi.World.Player.InventoryManager.TryTransferTo(slot, notFull, ref op);
                    if (obj != null && op.MovedQuantity > 0)
                    {
                        capi.Network.SendPacketClient(obj);
                        merged = true;
                    }

                    if (notFull.StackSize >= notFull.Itemstack.Collectible.MaxStackSize) notFull = null;
                    if (slot.Itemstack == null) removeList.Add(slot);
                    else notFull ??= slot;
                }
            }

            //remove empty stacks
            foreach (ItemSlot slot in removeList)
            {
                slotList.Remove(slot);
            }
        }

        if (merged) return;
        
        foreach (List<ItemSlot> slotList in slots.Values)
        {
            slotList.Sort(new SlotComparer());
            ItemSlot source;

            while (slotList.Count > 0)
            {
                //find the best inventory for the stack
                //must be done for each stack of the same item
                //because an inventory can become full
                source = slotList.PopOne();
                ItemStack stack = source.Itemstack;
                EnumItemStorageFlags flags = stack.Collectible.StorageFlags;
                EnumItemStorageFlags containerFlags = 0;
                int flagCount = 0xffffff;

                //find the most specific container that fits the item
                foreach (EnumItemStorageFlags slotFlags in destDic.Keys)
                {
                    if ((flags & slotFlags) == 0) continue;
                    int count = GetFlagCount((long)slotFlags);
                    if (count >= flagCount) continue;
                    containerFlags = slotFlags;
                    flagCount = count;
                }

                if (containerFlags == 0) break;
                List<ItemSlot> destList = destDic[containerFlags];
                if (destList.Count == 0)
                {
                    destDic.Remove(containerFlags);
                    continue;
                }

                foreach (ItemSlot dest in destList)
                {
                    if (dest == source)
                    {
                        destList.Remove(dest);
                        if (destList.Count == 0)
                        {
                            destDic.Remove(containerFlags);
                        }

                        break;
                    }

                    object obj = dest.Inventory.TryFlipItems(dest.Inventory.GetSlotId(dest), source);
                    if (obj != null)
                    {
                        if (source.Itemstack != null)
                        {
                            //updates the slot list so that it stays correct after switching the items in the slots
                            slots.TryGetValue(source.Itemstack.Collectible, out List<ItemSlot> sourceList);

                            int index = 0;
                            if (sourceList.Count == 0) return;
                            while (sourceList[index] != dest)
                            {
                                index++;
                                if (index >= sourceList.Count) return;
                            }

                            sourceList[index] = source;
                        }

                        capi.Network.SendPacketClient(obj);
                        destList.Remove(dest);
                        if (destList.Count == 0)
                        {
                            destDic.Remove(containerFlags);
                        }

                        break;
                    }
                }
            }
        }
    }

    private static void SortOpenInventories(ICoreClientAPI capi)
    {
        List<IInventory> inventories = capi.World.Player.InventoryManager.OpenedInventories;

        foreach (IInventory inventory in inventories.Where(inventory => inventory is not InventoryBasePlayer))
        {
            SortInventory(capi, inventory);
        }
    }

    public static void PullInventories(ICoreClientAPI capi)
    {
        IInventory backpack = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.backpackInvClassName);
        IInventory hotbar = capi.World.Player.InventoryManager.GetOwnInventory(GlobalConstants.hotBarInvClassName);
        List<IInventory> inventories = capi.World.Player.InventoryManager.OpenedInventories;

        // Collect all collectibles currently in player inventory
        HashSet<CollectibleObject> playerCollectibles = new HashSet<CollectibleObject>();
        if (backpack != null)
        {
            foreach (ItemSlot slot in backpack)
            {
                if (slot.Itemstack?.Collectible != null)
                    playerCollectibles.Add(slot.Itemstack.Collectible);
            }
        }
        if (hotbar != null)
        {
            foreach (ItemSlot slot in hotbar)
            {
                if (slot.Itemstack?.Collectible != null)
                    playerCollectibles.Add(slot.Itemstack.Collectible);
            }
        }

        // Only pull items that match what player already has
        foreach (IInventory inventory in inventories)
        {
            if (inventory is InventoryBasePlayer) continue;

            PullMatchingItems(capi, inventory, backpack, playerCollectibles);
            PullMatchingItems(capi, inventory, hotbar, playerCollectibles);
        }
    }

    private static void PullMatchingItems(ICoreClientAPI capi, IInventory sourceInv, IInventory destInv, HashSet<CollectibleObject> allowedCollectibles)
    {
        if (destInv == null || allowedCollectibles.Count == 0) return;

        int first = 0;
        int last = sourceInv.Count;
        InventoryTrader trader = sourceInv as InventoryTrader;
        if (trader != null)
        {
            first = 35;
            last = trader.Count - 1;
        }

        for (int i = first; i < last; ++i)
        {
            if (sourceInv[i] is ItemSlotLiquidOnly) continue;
            if (sourceInv[i].Itemstack == null) continue;

            // Only pull if player already has this item type
            if (!allowedCollectibles.Contains(sourceInv[i].Itemstack.Collectible)) continue;

            while (sourceInv[i].StackSize > 0)
            {
                WeightedSlot dest = destInv.GetBestSuitedSlot(sourceInv[i]);
                if (dest?.slot == null) break;

                if (dest.slot.Itemstack != null)
                    if (dest.slot.Itemstack.StackSize >= dest.slot.Itemstack.Collectible.MaxStackSize) break;

                if (destInv[i] is ItemSlotOffhand) break;

                ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, 0,
                    EnumMergePriority.DirectMerge, sourceInv[i].StackSize);
                object obj = capi.World.Player.InventoryManager.TryTransferTo(sourceInv[i], dest.slot, ref op);
                if (obj != null) capi.Network.SendPacketClient(obj);
            }
        }
    }

    private static void FillInventory(ICoreClientAPI capi, IInventory sourceInv, IInventory destInv)
    {
        Dictionary<CollectibleObject, List<ItemSlot>> toFill = new Dictionary<CollectibleObject, List<ItemSlot>>();
        foreach (ItemSlot slot in destInv)
        {
            if (slot.Itemstack == null) continue;
            if (slot.StackSize < slot.MaxSlotStackSize)
            {
                toFill.TryGetValue(slot.Itemstack.Collectible, out List<ItemSlot> list);
                if (list == null)
                {
                    list = new List<ItemSlot>();
                    toFill.Add(slot.Itemstack.Collectible, list);
                }

                list.Add(slot);
            }
        }

        foreach (ItemSlot sourceSlot in sourceInv)
        {
            if (sourceSlot.Itemstack == null) continue;
            toFill.TryGetValue(sourceSlot.Itemstack.Collectible, out List<ItemSlot> list);
            if (list == null) continue;
            foreach (ItemSlot destSlot in list)
            {
                if (destSlot.StackSize >= destSlot.MaxSlotStackSize) continue;
                ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.Left, 0,
                    EnumMergePriority.DirectMerge, sourceSlot.StackSize);
                object obj = capi.World.Player.InventoryManager.TryTransferTo(sourceSlot, destSlot, ref op);
                if (obj != null) capi.Network.SendPacketClient(obj);
                if (sourceSlot.StackSize == 0) break;
            }
        }
    }

    public static bool ClearHandSlot(ICoreClientAPI capi)
    {
        ItemSlot slot = capi.World.Player.InventoryManager.ActiveHotbarSlot;
        if (slot == null) return false;
        if (slot.Empty) return false;
        ItemStackMoveOperation op = new ItemStackMoveOperation(capi.World, EnumMouseButton.None, EnumModifierKey.SHIFT,
            EnumMergePriority.AutoMerge);
        object[] objs = capi.World.Player.InventoryManager.TryTransferAway(slot, ref op, true, true);
        if (objs == null) return false;
        foreach (object obj in objs)
            capi.Network.SendPacketClient(obj);

        return true;
    }

    public static void FindBestSlot(CollectibleObject collectible, IPlayer player, ref ItemSlot bestSlot)
    {
        if (collectible == null) return;
        if (player == null) return;
        ItemSlot tempSlot = null;

        player.InventoryManager.Find((ItemSlot slot) =>
        {
            if (slot.Itemstack?.Collectible == collectible)
            {
                if (tempSlot != null)
                {
                    if (tempSlot.Inventory == slot.Inventory)
                    {
                        if (slot.StackSize < tempSlot.StackSize)
                        {
                            tempSlot = slot;
                        }
                    }
                    else if (slot.Inventory.ClassName == GlobalConstants.hotBarInvClassName)
                    {
                        tempSlot = slot;
                    }
                }
                else tempSlot = slot;
            }

            return false;
        });
        if (tempSlot != null) bestSlot = tempSlot;
    }
    
}