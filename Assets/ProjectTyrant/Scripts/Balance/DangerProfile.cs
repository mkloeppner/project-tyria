using UnityEngine;

namespace ProjectTyrant.Balance
{
    /// <summary>Balance B1: Profil einer Gefahrenstufe als Asset (Data/Balance/DangerProfile_0..5).</summary>
    [CreateAssetMenu(menuName = "Project Tyrant/Balance/Danger Profile")]
    public class DangerProfile : ScriptableObject
    {
        public DangerSettings Settings = new DangerSettings();
    }
}
