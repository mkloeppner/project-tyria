using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Alle Items (Lookup nach Id und Name).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Item Database", fileName = "ItemDatabase")]
    public class ItemDatabase : ScriptableObject
    {
        public List<ItemDefinition> Items = new List<ItemDefinition>();
        Dictionary<string, ItemDefinition> _byId;

        public ItemDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (_byId == null || _byId.Count != Items.Count)
            {
                _byId = new Dictionary<string, ItemDefinition>();
                foreach (var i in Items) if (i != null && !string.IsNullOrEmpty(i.Id)) _byId[i.Id] = i;
            }
            return _byId.TryGetValue(id, out var d) ? d : null;
        }

        public ItemDefinition FindByName(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var t = text.Trim().ToLowerInvariant();
            foreach (var i in Items) if (i != null && (i.Id.ToLowerInvariant() == t || i.DisplayName.ToLowerInvariant() == t)) return i;
            foreach (var i in Items) if (i != null && i.DisplayName.ToLowerInvariant().Contains(t)) return i;
            return null;
        }
    }
}
