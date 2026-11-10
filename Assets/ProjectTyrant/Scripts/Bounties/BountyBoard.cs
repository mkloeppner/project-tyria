using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 11: Kopfgeldtafel (Glutwüste). F: nächsten freien Auftrag annehmen → Spürsinn-Kreis (Notiz), Ziel erscheint im Kreis mit instabilen Eigenschaften
    /// (<see cref="BountyModel"/>), 10 Minuten Zeit; Sieg gibt Kopfgeld-Marken (Wallet-Karma-Ersatz: Flag-Zähler im Profil) und Erfolgsfortschritt.
    /// </summary>
    public class BountyBoard : Interactable, IMapMarkerProvider
    {
        [System.Serializable] public class Target { public string Id, Name, Archetype; public bool Champion; public Vector3 Area; }
        public List<Target> Targets = new List<Target>();
        readonly Dictionary<string, BountyModel> _models = new Dictionary<string, BountyModel>();
        Target _active; BountyModel _m; BountyTarget _spawned;

        protected override void OnEnable() { base.OnEnable(); MapMarkerRegistry.Register(this); }
        protected override void OnDisable() { base.OnDisable(); MapMarkerRegistry.Unregister(this); }

        static string Today => System.DateTime.Now.ToString("yyyy-MM-dd");
        public override string Prompt => _active != null ? "Kopfgeld läuft: " + _active.Name : "Kopfgeldtafel";

        public override void Interact(Combatant player)
        {
            if (_active != null) { GameEventBus.Notify("Laufender Auftrag: " + _active.Name + " · " + Mathf.CeilToInt(_m.TimeLeft) + " s", new Color(1f, 0.85f, 0.5f)); return; }
            foreach (var t in Targets)
            {
                if (!_models.TryGetValue(t.Id, out var m)) _models[t.Id] = m = new BountyModel(t.Id, t.Champion);
                m.NewDay(Today);
                if (!m.Accept(Today, (t.Id + Today).GetHashCode())) continue;
                _active = t; _m = m;
                var mods = new List<string>(); foreach (var x in m.Modifiers) mods.Add(BountyModel.ModifierName(x));
                GameEventBus.RaiseBanner((t.Champion ? "Legendärer Gesuchter: " : "Gesuchter: ") + t.Name, "Spürsinn: Gebiet markiert · " + string.Join(", ", mods), new Color(1f, 0.75f, 0.35f));
                return;
            }
            GameEventBus.Notify("Alle Ziele dieser Tafel sind heute erledigt", new Color(1f, 0.85f, 0.5f));
        }

        void Update()
        {
            if (_active == null || _m == null) return;
            _m.Tick(Time.deltaTime);
            var p = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;
            if (_m.State == BountyState.Searching && p != null && _m.Reach(Vector3.Distance(p.transform.position, _active.Area))) Spawn();
            if (_m.State == BountyState.Failed) { GameEventBus.Notify("Kopfgeld gescheitert: Zeit abgelaufen", new Color(1f, 0.5f, 0.4f)); Clear(); return; }
            if (_m.State == BountyState.Spawned && _spawned != null && _spawned.Self != null && _spawned.Self.IsDead)
            {
                int tokens = _m.Defeat(Today);
                var prof = PlayerProfile.Instance;
                if (prof != null) { prof.GrantReward(150, 5000, 0, "Kopfgeld"); RegionWallet.Add(prof.State, RegionCurrency.BountyTokens, tokens); prof.State.Achievements.Add("bounty_10"); if (_active.Champion) prof.State.Achievements.Add("bounty_legend"); }
                GameEventBus.RaiseBanner("Kopfgeld erfüllt", _active.Name + " · +" + tokens + " Kopfgeld-Marken", new Color(1f, 0.85f, 0.4f));
                Clear();
            }
        }

        void Spawn()
        {
            var db = GameManager.Instance != null && GameManager.Instance.Story != null ? GameManager.Instance.Story.Database : null;
            var a = db != null ? db.Archetype("Enemy_" + _active.Archetype) : null;
            if (a == null || a.Prefab == null) { GameEventBus.Notify("Ziel nicht gefunden", Color.red); Clear(); return; }
            var go = Instantiate(a.Prefab, ZoneAnchors.GroundAt(_active.Area + Vector3.up * 2f), Quaternion.identity);
            var b = go.GetComponent<NpcBrain>(); if (b != null) { b.Archetype = a; b.ApplyArchetype(); }
            _spawned = go.AddComponent<BountyTarget>(); _spawned.Modifiers.AddRange(_m.Modifiers);
            go.name = "Bounty_" + _active.Id;
            GameEventBus.RaiseBanner(_active.Name + " erscheint!", "10 Minuten", new Color(1f, 0.5f, 0.3f));
        }

        void Clear() { _active = null; _m = null; _spawned = null; }

        public void CollectMarkers(List<MapMarkerInfo> into)
        {
            into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Vendor, State = MarkerState.Open, Position = transform.position, Label = "Kopfgeldtafel", Owner = this });
            if (_active != null && _m != null && _m.State == BountyState.Searching)
                into.Add(new MapMarkerInfo { Kind = MapMarkerKind.Event, State = MarkerState.Open, Position = _active.Area, Label = "Spürsinn: " + _active.Name, Owner = this });
        }
        public TooltipContent MarkerTooltip(MapMarkerInfo info) => (Object)info.Owner == this ? TooltipBuilder.MapMarker(info, info.Label) : null;
    }

    /// <summary>Lauf 11: regionale Marken im Profil (Zähler in den Profil-Flags, kein neues Speicherfeld).</summary>
    public static class RegionWallet
    {
        public static int Get(ProfileState s, string currency) { foreach (var f in s.Flags) if (f.StartsWith(currency + "=")) { int.TryParse(f.Substring(currency.Length + 1), out int n); return n; } return 0; }
        public static void Set(ProfileState s, string currency, int n) { s.Flags.RemoveAll(f => f.StartsWith(currency + "=")); s.Flags.Add(currency + "=" + Mathf.Max(0, n)); }
        public static void Add(ProfileState s, string currency, int n) => Set(s, currency, Get(s, currency) + n);
    }
}
