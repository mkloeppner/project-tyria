using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 5 / M25: alle spielbaren Klassen. Eigene Datei, damit Unity das ScriptableObject dem Skript zuordnen kann.</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Class Library", fileName = "ClassLibrary")]
    public class ClassLibrary : ScriptableObject
    {
        public List<ClassDefinition> Classes = new List<ClassDefinition>();
        public ClassDefinition Get(string id) { foreach (var c in Classes) if (c != null && c.Id == id) return c; return Classes.Count > 0 ? Classes[0] : null; }
    }
}
