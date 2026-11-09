using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    [Serializable]
    public class VendorEntry
    {
        public ItemDefinition Item;
        public int PriceCopper;
        public int PriceKarma;
        public int Count = 1;
    }

    /// <summary>Händler-Sortiment (Kaufpreise fest je Item, Karma-Items kosten Karma).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Vendor", fileName = "Vendor_")]
    public class VendorDefinition : ScriptableObject
    {
        public string Id;
        public string DisplayName = "Händler";
        public VendorKind Kind;
        public List<VendorEntry> Stock = new List<VendorEntry>();
    }

}
