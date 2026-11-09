namespace ProjectTyrant
{
    /// <summary>Lauf 4: Beherrschungen im Speicherstand (EP, verdiente Punkte, Rang/EP je Linie, gewählte Linie).</summary>
    public partial class ProfileState
    {
        public readonly MasteryModel Mastery = new MasteryModel();

        partial void WriteMastery(SaveGameDto d)
        {
            d.MasteryXp = Mastery.TotalXp;
            d.MasteryPointsEarned = Mastery.PointsEarned;
            d.SelectedMastery = Mastery.Selected.HasValue ? Mastery.Selected.Value.ToString() : null;
            foreach (var kv in Mastery.Tracks) d.Masteries.Add(new MasteryTrackDto { Line = kv.Key.ToString(), Rank = kv.Value.Rank, Xp = kv.Value.Xp });
        }

        partial void ReadMastery(SaveGameDto d)
        {
            Mastery.TotalXp = d.MasteryXp;
            Mastery.PointsEarned = d.MasteryPointsEarned;
            foreach (var t in Mastery.Tracks.Values) { t.Rank = 0; t.Xp = 0; }
            if (d.Masteries != null)
                foreach (var m in d.Masteries)
                    if (System.Enum.TryParse<MasteryLine>(m.Line, out var l)) { Mastery.Tracks[l].Rank = m.Rank; Mastery.Tracks[l].Xp = m.Xp; }
            Mastery.Selected = !string.IsNullOrEmpty(d.SelectedMastery) && System.Enum.TryParse<MasteryLine>(d.SelectedMastery, out var sel) ? sel : (MasteryLine?)null;
        }
    }
}
