using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Alle Zonen + Ladebildschirm-Tipps.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/World")]
    public class WorldDefinition : ScriptableObject
    {
        public string StartZone = "sonnfurt";
        public List<ZoneDefinition> Zones = new List<ZoneDefinition>();
        [TextArea] public List<string> Tips = new List<string>();
        public ZoneDefinition Zone(string id) => Zones.Find(z => z != null && z.Id == id);
    }
}
