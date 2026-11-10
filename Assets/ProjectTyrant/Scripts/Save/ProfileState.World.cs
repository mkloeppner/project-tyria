using System.Collections.Generic;

namespace ProjectTyrant
{
    /// <summary>Lauf 4: Reittiere (Besitz, Auswahl) im reinen Spielerfortschritt. Beherrschungen: ProfileState.Mastery.cs.</summary>
    public partial class ProfileState
    {
        public readonly HashSet<string> Mounts = new HashSet<string>();
        public MountKind SelectedMount = MountKind.None;
        public MountPrefsDto MountPrefs = new MountPrefsDto();          // Lauf 10
        public readonly List<string> AdminMarks = new List<string>();   // Lauf 10: „mount:Skyscale“, „mastery:Greif“, „story:…“

        /// <summary>Lauf 10: reines Auswahlmodell aus Besitz + Einstellungen.</summary>
        public MountSelectionModel MountSelection()
        {
            var m = new MountSelectionModel();
            foreach (MountKind k in System.Enum.GetValues(typeof(MountKind))) if (k != MountKind.None && Mounts.Contains(k.ToString())) m.Owned.Add(k);
            foreach (var a in AdminMarks) if (a.StartsWith("mount:") && System.Enum.TryParse<MountKind>(a.Substring(6), out var ak)) m.AdminUnlocked.Add(ak);
            m.Default = SelectedMount;
            m.LastUsed = !string.IsNullOrEmpty(MountPrefs.LastUsed) && System.Enum.TryParse<MountKind>(MountPrefs.LastUsed, out var lu) ? lu : MountKind.None;
            m.UseLastInsteadOfDefault = MountPrefs.UseLast; m.RadialSetsDefault = MountPrefs.RadialSetsDefault;
            if (MountPrefs.Keys != null) m.KeyBindings.AddRange(MountPrefs.Keys);
            return m;
        }

        public void StoreMountSelection(MountSelectionModel m)
        {
            SelectedMount = m.Owns(m.Default) ? m.Default : SelectedMount;
            MountPrefs.LastUsed = m.LastUsed == MountKind.None ? null : m.LastUsed.ToString();
            MountPrefs.UseLast = m.UseLastInsteadOfDefault; MountPrefs.RadialSetsDefault = m.RadialSetsDefault;
            MountPrefs.Keys = new List<string>(m.KeyBindings);
        }

        public void AdminMark(string mark) { if (!AdminMarks.Contains(mark)) AdminMarks.Add(mark); }
        public bool IsAdminMarked(string mark) => AdminMarks.Contains(mark);

        partial void WriteWorld(SaveGameDto d)
        {
            d.Mounts.AddRange(Mounts);
            d.SelectedMount = SelectedMount == MountKind.None ? null : SelectedMount.ToString();
            d.MountPrefs = MountPrefs.Clone(); d.AdminMarks = new List<string>(AdminMarks);
            WriteMastery(d);
        }

        partial void ReadWorld(SaveGameDto d)
        {
            Mounts.Clear();
            if (d.Mounts != null) foreach (var m in d.Mounts) if (System.Enum.TryParse<MountKind>(m, out var k) && k != MountKind.None) Mounts.Add(k.ToString());
            SelectedMount = !string.IsNullOrEmpty(d.SelectedMount) && System.Enum.TryParse<MountKind>(d.SelectedMount, out var sel) && Mounts.Contains(sel.ToString()) ? sel : MountKind.None;
            MountPrefs = d.MountPrefs != null ? d.MountPrefs.Clone() : new MountPrefsDto();
            AdminMarks.Clear(); if (d.AdminMarks != null) AdminMarks.AddRange(d.AdminMarks);
            ReadMastery(d);
        }

        partial void WriteMastery(SaveGameDto d);
        partial void ReadMastery(SaveGameDto d);
    }
}
