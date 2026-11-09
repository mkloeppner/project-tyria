using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>Lauf 4: Reittiere (Besitz, Auswahl) im reinen Spielerfortschritt. Beherrschungen: ProfileState.Mastery.cs.</summary>
    public partial class ProfileState
    {
        public readonly HashSet<string> Mounts = new HashSet<string>();
        public MountKind SelectedMount = MountKind.None;

        partial void WriteWorld(SaveGameDto d)
        {
            d.Mounts.AddRange(Mounts);
            d.SelectedMount = SelectedMount == MountKind.None ? null : SelectedMount.ToString();
            WriteMastery(d);
        }

        partial void ReadWorld(SaveGameDto d)
        {
            Mounts.Clear();
            if (d.Mounts != null) foreach (var m in d.Mounts) if (System.Enum.TryParse<MountKind>(m, out var k) && k != MountKind.None) Mounts.Add(k.ToString());
            SelectedMount = !string.IsNullOrEmpty(d.SelectedMount) && System.Enum.TryParse<MountKind>(d.SelectedMount, out var sel) && Mounts.Contains(sel.ToString()) ? sel : MountKind.None;
            ReadMastery(d);
        }

        partial void WriteMastery(SaveGameDto d);
        partial void ReadMastery(SaveGameDto d);
    }
}
