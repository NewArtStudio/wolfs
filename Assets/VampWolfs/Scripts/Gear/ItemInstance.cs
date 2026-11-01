using System;

namespace VampWolfs
{
    /// <summary>
    /// One concrete item owned by someone: an equipment piece (unique, with its own refine, points and
    /// sockets — like Space's ItemStack) or a stack of materials.
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public string Uid;

        /// <summary>Id of the EquipDef or MaterialDef in the catalog.</summary>
        public string DefId;

        /// <summary>Stack size (materials). Equipment is always 1.</summary>
        public int Qty = 1;

        /// <summary>Refine level +0..+10.</summary>
        public int Refine;

        /// <summary>Attribute points the player added with tonics (on top of the model's base).</summary>
        public Attributes Alloc;

        /// <summary>How many card sockets are open (keys open them in order).</summary>
        public int CardsOpen;

        public bool GemOpen;

        /// <summary>Material id of the gem in the socket (empty = none).</summary>
        public string Gem;

        public static ItemInstance Create(string defId, int qty)
        {
            return new ItemInstance { Uid = Guid.NewGuid().ToString("N"), DefId = defId, Qty = Math.Max(1, qty), Gem = "" };
        }

        /// <summary>Copy with a new uid (used when splitting stacks for drops).</summary>
        public ItemInstance CloneWithQty(int qty)
        {
            ItemInstance copy = (ItemInstance)MemberwiseClone();
            copy.Uid = Guid.NewGuid().ToString("N");
            copy.Qty = qty;
            return copy;
        }
    }
}
