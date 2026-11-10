using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Kontinentansicht: alle Zonen als Flächen (Name, Level, Zonenabschluss %, Wegmarken, Timer), unbetretene abgedunkelt.</summary>
    public partial class WorldMapWindow
    {
        public bool ContinentMode { get; private set; }
        RectTransform _continent;
        class ZoneTile { public ZoneDefinition Def; public Image Bg, Frame; public TextMeshProUGUI Name, Info; public readonly List<(Image icon, ZoneWaypointInfo wp)> Wps = new List<(Image, ZoneWaypointInfo)>(); }
        readonly List<ZoneTile> _tiles = new List<ZoneTile>();
        float _continentTimer;

        /// <summary>Zusatztext je Zone (z. B. Weltboss-/Meta-Timer, ab M19).</summary>
        public static System.Func<ZoneDefinition, string> ZoneTimerText;

        void BuildContinent()
        {
            var bg = UiFactory.Image("Continent", _view, new Color(0.12f, 0.18f, 0.24f, 1f), null, true);
            _continent = bg.rectTransform.Stretch();
            var title = UiFactory.Text("Title", _continent, "Kontinent Ashveil  <size=60%><color=#AAB>(Mausrad hoch: zurück zur Zone)</color></size>", 26, TextAlignmentOptions.TopLeft, UiFactory.AccentColor);
            title.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(16, -10), new Vector2(-32, 40));
            var world = GameManager.Instance != null ? GameManager.Instance.World : null;
            if (world != null)
                foreach (var z in world.Zones)
                {
                    if (z == null) continue;
                    var t = new ZoneTile { Def = z };
                    t.Bg = UiFactory.Image("Zone_" + z.Id, _continent, z.ContinentColor, UiFactory.Sprite("ui_round"), true);
                    var r = t.Bg.rectTransform;
                    r.anchorMin = new Vector2(0.04f + z.ContinentRect.xMin * 0.92f, 0.04f + z.ContinentRect.yMin * 0.84f);
                    r.anchorMax = new Vector2(0.04f + z.ContinentRect.xMax * 0.92f, 0.04f + z.ContinentRect.yMax * 0.84f);
                    r.offsetMin = r.offsetMax = Vector2.zero;
                    t.Frame = UiFactory.Image("Frame", r, Color.clear, UiFactory.Sprite("ui_frame"));
                    t.Frame.rectTransform.Stretch();
                    t.Name = UiFactory.Text("Name", r, z.DisplayName, 22, TextAlignmentOptions.Top, Color.white);
                    t.Name.rectTransform.Stretch(6, 6, 8, 0); t.Name.fontStyle = FontStyles.Bold;
                    t.Info = UiFactory.Text("Info", r, "", 15, TextAlignmentOptions.Top, new Color(0.95f, 0.92f, 0.85f));
                    t.Info.rectTransform.Stretch(6, 6, 36, 0);
                    foreach (var wp in z.Waypoints)
                    {
                        var icon = UiFactory.Image("WP", r, Color.white, UiFactory.Sprite("map_waypoint"), true);
                        var ir = icon.rectTransform;
                        float u = Mathf.Clamp01(wp.Position.x / z.Size + 0.5f), v = Mathf.Clamp01(wp.Position.z / z.Size + 0.5f);
                        ir.anchorMin = ir.anchorMax = new Vector2(0.08f + u * 0.84f, 0.06f + v * 0.6f);
                        ir.sizeDelta = new Vector2(20, 20);
                        var zz = z; var w = wp;
                        icon.gameObject.AddComponent<UiClickRelay>().Left = () => _ui.RequestZoneTravel(zz, w);
                        icon.gameObject.AddComponent<UiTooltipTrigger>().Provider = () => WaypointTip(zz, w);
                        t.Wps.Add((icon, wp));
                    }
                    var zone = z;
                    t.Bg.gameObject.AddComponent<UiClickRelay>().Left = () => { if (GameManager.Instance != null && GameManager.Instance.CurrentZone == zone) ShowContinent(false); };
                    _tiles.Add(t);
                }
            // Lauf 11: Regionen-Reiter (Kernland, Glutwüste, Dracheninseln) mit Regionen-Abschluss
            var tabs = UiFactory.Rect("RegionTabs", _continent);
            tabs.Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-16, -8), new Vector2(620, 36));
            var hl = tabs.gameObject.AddComponent<HorizontalLayoutGroup>(); hl.spacing = 8; hl.childControlWidth = true; hl.childForceExpandWidth = true; hl.childControlHeight = true;
            _regionButtons.Clear();
            foreach (WorldRegion reg in System.Enum.GetValues(typeof(WorldRegion)))
            {
                var rr = reg;
                var b = UiFactory.Button("Tab_" + reg, tabs, RegionZones.RegionName(reg), () => { _regionTab = rr; ApplyRegionTab(); }, 16);
                _regionButtons.Add((reg, b.GetComponentInChildren<TextMeshProUGUI>()));
            }
            _continent.gameObject.SetActive(false);
        }

        WorldRegion _regionTab;
        readonly List<(WorldRegion region, TextMeshProUGUI label)> _regionButtons = new List<(WorldRegion, TextMeshProUGUI)>();

        /// <summary>Lauf 11: nur die Zonen der gewählten Region zeigen; Reiter mit Regionen-Abschluss (abgeschlossene Zonen / alle).</summary>
        void ApplyRegionTab()
        {
            foreach (var t in _tiles) if (t.Bg != null) t.Bg.gameObject.SetActive(t.Def.Region == _regionTab);
            var prof = PlayerProfile.Instance;
            foreach (var (reg, label) in _regionButtons)
            {
                if (label == null) continue;
                int all = 0, done = 0;
                foreach (var t in _tiles) if (t.Def.Region == reg) { all++; if (prof != null && prof.State.Completed.Contains("zonechest:" + t.Def.Id)) done++; }
                label.text = (reg == _regionTab ? "<b>" : "") + RegionZones.RegionName(reg) + " " + done + "/" + all + (reg == _regionTab ? "</b>" : "");
            }
        }

        TooltipContent WaypointTip(ZoneDefinition z, ZoneWaypointInfo w)
        {
            var prof = PlayerProfile.Instance;
            bool known = prof != null && prof.State.Completed.Contains(w.Id);
            var gm = GameManager.Instance;
            bool other = gm != null && gm.CurrentZone != z;
            var c = new TooltipContent { Title = w.Name, TitleColor = new Color(0.6f, 0.85f, 1f), Subtitle = z.DisplayName + " · Wegmarke" };
            if (!known) { c.Footer = "Noch nicht entdeckt"; c.FooterColor = new Color(0.7f, 0.7f, 0.75f); }
            else if (prof != null && prof.Player != null)
            {
                c.Add("Reisekosten: " + WalletModel.Format(prof.ZoneTravelCost(prof.Player.transform.position, w.Position, other)), new Color(0.9f, 0.85f, 0.6f));
                c.Footer = other ? "Klicken: Reise in eine andere Zone (Ladebildschirm)" : "Klicken: Schnellreise"; c.FooterColor = new Color(0.6f, 0.9f, 1f);
            }
            return c;
        }

        public void ShowContinent(bool on)
        {
            if (on && _continent == null) BuildContinent();
            if (_continent == null) { ContinentMode = false; return; }
            ContinentMode = on;
            _continent.gameObject.SetActive(on);
            _continentTimer = 0f;
            if (on) { var gmz = GameManager.Instance != null ? GameManager.Instance.CurrentZone : null; if (gmz != null) _regionTab = gmz.Region; ApplyRegionTab(); }   // Lauf 11
            _ui.Tooltip.Hide();
        }

        void RefreshContinent()
        {
            if ((_continentTimer -= Time.unscaledDeltaTime) > 0f) return;
            _continentTimer = 0.5f;
            var prof = PlayerProfile.Instance; var gm = GameManager.Instance;
            foreach (var t in _tiles)
            {
                bool current = gm != null && gm.CurrentZone == t.Def;
                bool visited = current || (prof != null && System.Linq.Enumerable.Contains(prof.State.FogZones, t.Def.Id));
                float pct = ZoneCompletionPercent(t.Def, prof);
                t.Bg.color = visited ? t.Def.ContinentColor : Color.Lerp(t.Def.ContinentColor, Color.black, 0.65f);
                t.Frame.color = current ? UiFactory.AccentColor : new Color(1, 1, 1, 0.25f);
                string timer = ZoneTimerText != null ? ZoneTimerText(t.Def) : null;
                t.Info.text = "Stufe " + t.Def.LevelText + "\n" + (visited ? "Zonenabschluss " + Mathf.FloorToInt(pct * 100f) + " %" : "<color=#BBB>Unerforscht</color>")
                    + (current ? "\n<color=#FFD27A>Du bist hier</color>" : "") + (string.IsNullOrEmpty(timer) ? "" : "\n" + timer);
                foreach (var (icon, wp) in t.Wps)
                {
                    bool known = prof != null && prof.State.Completed.Contains(wp.Id);
                    icon.color = known ? Color.white : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                }
            }
        }

        public static float ZoneCompletionPercent(ZoneDefinition z, PlayerProfile prof)
        {
            if (z == null || prof == null || z.CompletionIds == null || z.CompletionIds.Count == 0) return 0f;
            int done = 0; foreach (var id in z.CompletionIds) if (prof.State.Completed.Contains(id)) done++;
            return done / (float)z.CompletionIds.Count;
        }
    }
}
