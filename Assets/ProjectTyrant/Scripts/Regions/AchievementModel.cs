using System.Collections.Generic;

namespace ProjectTyrant
{
    public enum AchievementCategory { Erkundung, Story, Kopfgeld, Rennen, Angeln, Weltbosse, Sammlungen }

    /// <summary>Lauf 11: ein Erfolg mit Zähler, Punkten, optionalem Titel und Belohnung.</summary>
    public class AchievementDef
    {
        public string Id, Name, Description, Title, Reward; public AchievementCategory Category; public int Goal = 1, Points = 5, MasteryPoints;
        public AchievementDef(string id, string name, AchievementCategory c, int goal, int points, string desc, string title = null, int mastery = 0, string reward = null) { Id = id; Name = name; Category = c; Goal = goal; Points = points; Description = desc; Title = title; MasteryPoints = mastery; Reward = reward; }
    }

    /// <summary>
    /// Lauf 11: Erfolgs-Tab (rein, getestet) – Kategorien, Fortschritt, Punkte, Titel, Belohnungen (Beherrschungspunkte, Marken, Skins);
    /// Regionen-Abschluss als Erfolg mit Truhe (alle Zonenabschlüsse einer Region).
    /// </summary>
    public class AchievementModel
    {
        public readonly Dictionary<string, int> Progress = new Dictionary<string, int>();
        public readonly HashSet<string> Done = new HashSet<string>();
        public readonly List<string> Titles = new List<string>();
        public int Points { get; private set; }
        public event System.Action<AchievementDef> Completed;

        public static readonly List<AchievementDef> All = Build();

        static List<AchievementDef> Build()
        {
            var l = new List<AchievementDef>
            {
                new AchievementDef("region_kernland", "Herr des Kernlands", AchievementCategory.Erkundung, 8, 25, "Schließe alle acht neuen Kernland-Zonen ab.", "Wanderer des Kernlands", 2, "box_region"),
                new AchievementDef("region_glutwueste", "Kind der Glut", AchievementCategory.Erkundung, 6, 25, "Schließe alle Zonen der Glutwüste ab.", "Dünenwanderer", 2, "box_region"),
                new AchievementDef("region_dracheninseln", "Jadeherz", AchievementCategory.Erkundung, 6, 25, "Schließe alle Zonen der Dracheninseln ab.", "Hüter der Jade", 2, "box_region"),
                new AchievementDef("story_zwischenspiel", "Asche des Siegels", AchievementCategory.Story, 2, 10, "Spiele das Zwischenspiel durch."),
                new AchievementDef("story_glut", "Thron der Glut", AchievementCategory.Story, 7, 20, "Beende die Geschichte der Glutwüste.", "Thronbrecher", 3),
                new AchievementDef("story_jade", "Herz der Flut", AchievementCategory.Story, 7, 20, "Beende die Geschichte der Dracheninseln.", "Drachenfreund", 3),
                new AchievementDef("bounty_10", "Kopfgeldjäger", AchievementCategory.Kopfgeld, 10, 10, "Besiege 10 Kopfgeld-Ziele.", "Kopfgeldjäger", 1),
                new AchievementDef("bounty_legend", "Legendäre Jagd", AchievementCategory.Kopfgeld, 5, 15, "Besiege 5 legendäre Gesuchte (Champions)."),
                new AchievementDef("race_both", "Dünenreiter", AchievementCategory.Rennen, 2, 10, "Beende beide Rennstrecken.", "Dünenreiter"),
                new AchievementDef("race_gold", "Goldene Hufe", AchievementCategory.Rennen, 2, 15, "Schlage auf beiden Strecken die Goldzeit."),
                new AchievementDef("fish_50", "Angler", AchievementCategory.Angeln, 50, 10, "Fange 50 Fische.", "Angler", 1),
                new AchievementDef("fish_legend", "Legendärer Fang", AchievementCategory.Angeln, 1, 15, "Fange einen legendären Fisch."),
                new AchievementDef("boss_new4", "Neue Riesen", AchievementCategory.Weltbosse, 4, 15, "Besiege die vier neuen Weltbosse.", null, 2),
                new AchievementDef("boss_metas", "Fall und Erwachen", AchievementCategory.Weltbosse, 2, 20, "Schließe die Metas Zarathis und Drachenende ab.", "Bezwinger der Flut", 2),
            };
            foreach (var z in FishCatalog.FishingZones) l.Add(new AchievementDef("fishcol_" + z, "Fischsammlung: " + z, AchievementCategory.Sammlungen, FishCatalog.For(z).Count, 5, "Fange alle Fische der Zone."));
            return l;
        }

        public static AchievementDef Get(string id) { foreach (var a in All) if (a.Id == id) return a; return null; }

        /// <summary>Fortschritt +n; bei Erreichen: Punkte, Titel, Ereignis. Rückgabe true = gerade abgeschlossen.</summary>
        public bool Add(string id, int n = 1)
        {
            var a = Get(id); if (a == null || Done.Contains(id) || n <= 0) return false;
            Progress.TryGetValue(id, out int p); p = System.Math.Min(a.Goal, p + n); Progress[id] = p;
            if (p < a.Goal) return false;
            Done.Add(id); Points += a.Points; if (!string.IsNullOrEmpty(a.Title) && !Titles.Contains(a.Title)) Titles.Add(a.Title);
            Completed?.Invoke(a); return true;
        }

        /// <summary>Regionen-Abschluss aus den Zonenabschlüssen (Ids „zonechest:&lt;zone&gt;“).</summary>
        public int RegionProgress(WorldRegion r, ICollection<string> completed)
        {
            int n = 0; foreach (var s in RegionZones.InRegion(r)) if (completed.Contains("zonechest:" + s.Id)) n++; return n;
        }

        public void SyncRegions(ICollection<string> completed)
        {
            foreach (WorldRegion r in System.Enum.GetValues(typeof(WorldRegion)))
            {
                string id = "region_" + (r == WorldRegion.Kernland ? "kernland" : r == WorldRegion.Glutwueste ? "glutwueste" : "dracheninseln");
                Progress.TryGetValue(id, out int have); int now = RegionProgress(r, completed);
                if (now > have) Add(id, now - have);
            }
        }

        public IEnumerable<AchievementDef> InCategory(AchievementCategory c) { foreach (var a in All) if (a.Category == c) yield return a; }
    }

    /// <summary>Lauf 11: regionale Marken und Quartiermeister (Sonnentor, Jadehafen).</summary>
    public static class RegionCurrency
    {
        public const string BountyTokens = "marken_kopfgeld", DragonTokens = "marken_drachen";
        public struct Offer { public string Item; public string Currency; public int Price; public Offer(string i, string c, int p) { Item = i; Currency = c; Price = p; } }

        public static readonly Offer[] Sonnentor = { new Offer("ar_l80_chest", BountyTokens, 30), new Offer("tr_amulet_l80", BountyTokens, 25), new Offer("gift_wueste", BountyTokens, 250), new Offer("skin_duene", BountyTokens, 60) };
        public static readonly Offer[] Jadehafen = { new Offer("ar_l80_legs", DragonTokens, 30), new Offer("tr_ring_l80", DragonTokens, 25), new Offer("gift_jade", DragonTokens, 250), new Offer("jade_core", DragonTokens, 20) };

        public static bool Buy(Dictionary<string, int> wallet, Offer o)
        {
            wallet.TryGetValue(o.Currency, out int have);
            if (have < o.Price) return false;
            wallet[o.Currency] = have - o.Price; return true;
        }

        /// <summary>Legendär-Pipeline: Gaben der Regionen als Alternativen (eine genügt).</summary>
        public static bool GiftForLegendary(ICollection<string> owned) => owned.Contains("gift_wueste") || owned.Contains("gift_jade") || owned.Contains("gift_erkundung");
    }
}
