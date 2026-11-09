using System.Collections.Generic;

namespace ProjectTyrant
{
    public partial class ProfileState
    {
        public SkillLoadoutModel Loadout = new SkillLoadoutModel();
        public TraitModel Traits = new TraitModel();

        partial void WriteSkills(SaveGameDto d)
        {
            d.UnlockedSkills.AddRange(Loadout.Unlocked);
            for (int i = 0; i < SkillLoadoutModel.SlotCount; i++) d.SlotSkills.Add(Loadout.Slots[i] != null ? Loadout.Slots[i].Id : "");
            foreach (var l in Traits.Lines)
            {
                var t = new TraitLineDto { Line = l.Id, Tiers = l.TiersUnlocked, Majors = (int[])l.Selected.Clone() };
                t.Unlocked.AddRange(l.UnlockedMajors);
                t.Unlocked.Sort();
                d.Traits.Add(t);
            }
        }

        partial void ReadSkills(SaveGameDto d, IList<SkillDefinition> skills)
        {
            Loadout.Clear();
            if (d.UnlockedSkills != null) foreach (var id in d.UnlockedSkills) if (!string.IsNullOrEmpty(id)) Loadout.Unlocked.Add(id);
            if (d.SlotSkills != null && skills != null)
                for (int i = 0; i < SkillLoadoutModel.SlotCount && i < d.SlotSkills.Count; i++)
                    foreach (var s in skills)
                        if (s != null && s.Id == d.SlotSkills[i] && Loadout.CanSlot(s, i, out _)) { Loadout.Slots[i] = s; break; }
            Loadout.FillDefaults(skills);
            Traits.Clear();
            if (d.Traits != null)
                foreach (var t in d.Traits)
                {
                    if (string.IsNullOrEmpty(t.Line)) continue;
                    var l = Traits.GetOrCreate(t.Line);
                    l.TiersUnlocked = System.Math.Max(0, System.Math.Min(TraitModel.Tiers, t.Tiers));
                    if (t.Unlocked != null) foreach (var u in t.Unlocked) if (u >= 0 && u < 9) l.UnlockedMajors.Add(u);
                    for (int k = 0; k < 3; k++)
                    {
                        int sel = t.Majors != null && t.Majors.Length > k ? t.Majors[k] : -1;
                        l.Selected[k] = sel >= 0 && sel < 3 && l.UnlockedMajors.Contains(k * 3 + sel) ? sel : -1;
                    }
                }
            Traits.NotifyChanged();
        }
    }
}
