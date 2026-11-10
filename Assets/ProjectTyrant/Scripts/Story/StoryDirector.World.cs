using System.Collections.Generic;
using UnityEngine;
using ProjectTyrant.Anim;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Bündel, Späher, Herz-Beiträge (Kampf, Kombo, Bündel, Emotes), Herz-Pop-up und Dankesbriefe.</summary>
    public partial class StoryDirector
    {
        public BundleObject BundleSource { get; private set; }
        GameObject _bundleProp;
        readonly HashSet<string> _heartPopups = new HashSet<string>();
        float _heartScan;

        void Start()
        {
            GameEventBus.SkillCast += OnSkillCast;
            EmoteRunner.Performed += OnEmote;
            SkillCaster.SlotOverride = BundleSlot;
            SkillCaster.SwapBlocked = () => Bundle.Active;
            EffectModel.Cleansed += OnCleansed;
            var slot = PlayerProfile.Instance != null ? PlayerProfile.Instance.ActiveCharacter : null; DialoguePlayer.HeroVoiceB = slot != null && slot.VoiceB;
        }

        void OnCleansed(EffectModel m, int n) { if (Player != null && m == Player.Effects) NotifyCleanse(n); }

        void OnDestroy()
        {
            GameEventBus.SkillCast -= OnSkillCast;
            EmoteRunner.Performed -= OnEmote;
            EffectModel.Cleansed -= OnCleansed;
            if (SkillCaster.SlotOverride == (System.Func<int, SkillDefinition>)BundleSlot) SkillCaster.SlotOverride = null;
            SkillCaster.SwapBlocked = null;
        }

        // ------------------------------------------------------------ Bündel
        SkillDefinition BundleSlot(int slot) { var s = Bundle.SlotSkill(slot, out bool over); return over ? (s ?? Empty) : null; }
        static SkillDefinition _empty; static SkillDefinition Empty { get { if (_empty == null) { _empty = ScriptableObject.CreateInstance<SkillDefinition>(); _empty.name = "Bundle_Empty"; _empty.Id = "Bundle_Empty"; _empty.DisplayName = "–"; _empty.Delivery = SkillDelivery.Self; _empty.CastTime = 0f; _empty.Cooldown = 9999f; } return _empty; } }

        public bool PickBundle(string id, BundleObject src)
        {
            var def = Database != null ? Database.Bundle(id) : null; if (def == null || Player == null) return false;
            if (!Bundle.Pick(id, def.Skills, def.Drop)) return false;
            BundleSource = src;
            _bundleProp = MakeProp(def);
            ShowHint("bundle");
            Player.GetComponent<SkillCaster>()?.NotifySlotsChanged();
            Report(ObjectiveKind.Bundle, id);
            return true;
        }

        public void DropBundle()
        {
            if (!Bundle.Active) return;
            Bundle.Release(); BundleSource = null;
            if (_bundleProp != null) Destroy(_bundleProp);
            Player?.GetComponent<SkillCaster>()?.NotifySlotsChanged();
        }

        GameObject MakeProp(BundleDef def)
        {
            var drv = Player.GetComponentInChildren<AnimDriver>(); var hand = drv != null && drv.HandSocket != null ? drv.HandSocket : Player.transform;
            var go = GameObject.CreatePrimitive(def.Id == "laterne" ? PrimitiveType.Sphere : def.Id == "sandsack" ? PrimitiveType.Cube : PrimitiveType.Cylinder);
            Destroy(go.GetComponent<Collider>()); go.name = "BundleProp_" + def.Id; go.transform.SetParent(hand, false);
            go.transform.localScale = def.Id == "heugabel" || def.Id == "harpune" ? new Vector3(0.06f, 0.7f, 0.06f) : new Vector3(0.28f, 0.22f, 0.28f);
            go.transform.localPosition = new Vector3(0, 0.15f, 0);
            var r = go.GetComponent<Renderer>(); var m = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")); m.color = def.Color; if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", def.Color); r.sharedMaterial = m;
            if (def.LightRadius > 0f) { var l = go.AddComponent<Light>(); l.type = LightType.Point; l.range = def.LightRadius; l.intensity = 2.5f; l.color = new Color(1f, 0.85f, 0.55f); }
            return go;
        }

        void OnSkillCast(Combatant c, SkillDefinition s)
        {
            if (c != Player || s == null || string.IsNullOrEmpty(s.Id) || !s.Id.StartsWith("Bundle_")) return;
            if (s == Bundle.Drop || s.Id == "Bundle_Drop") { DropBundle(); return; }
            Report(ObjectiveKind.Bundle, Bundle.Current);
            HeartContribution(HeartWay.Bundle, Player.transform.position, 0.06f, "Bündel");
        }

        // ------------------------------------------------------------ Herzen
        /// <summary>Beitrag zu einem Herz in der Nähe (Weg-Gewicht, höchstens 70 % je Weg).</summary>
        public bool HeartContribution(HeartWay way, Vector3 at, float amount, string reason)
        {
            var p = PlayerProfile.Instance; if (p == null) return false;
            foreach (var o in ZoneObject.Objects)
            {
                if (!(o is Heart h) || h.Done || h.Definition == null || !h.Contains(at)) continue;
                if (h.Ways != null && h.Ways.Length > 0 && System.Array.IndexOf(h.Ways, way) < 0) continue;
                p.AddHeartWay(h.Definition, way, amount * (way == HeartWay.Kill ? 1f : 1f), reason);
                return true;
            }
            return false;
        }

        void OnEmote(Combatant who, Emote e)
        {
            if (who != Player) return;
            foreach (var o in ZoneObject.Objects)
                if (o is Heart h && !h.Done && h.EmoteWanted == e && h.Contains(Player.transform.position) && Vector3.Distance(h.transform.position, Player.transform.position) < 8f)
                { PlayerProfile.Instance.AddHeartWay(h.Definition, HeartWay.Emote, 0.15f, "/" + e.ToString().ToLowerInvariant()); return; }
        }

        void ScanHeartPopups()
        {
            if ((_heartScan += Time.deltaTime) < 0.5f || Player == null) return; _heartScan = 0f;
            foreach (var o in ZoneObject.Objects)
            {
                if (!(o is Heart h) || h.Done || h.Definition == null || _heartPopups.Contains(h.Id) || !h.Contains(Player.transform.position)) continue;
                _heartPopups.Add(h.Id);
                UIController.Instance?.ShowHeartPopup(h);
                ShowHint("heart");
            }
        }

        /// <summary>Dankesbrief nach Herz-Abschluss (genau einmal, wenn in der Text-Tabelle vorhanden).</summary>
        public void OnHeartCompleted(string heartId) { if (Database != null && Database.Letter("Dank_" + heartId) != null) SendLetter("Dank_" + heartId); }

        // ------------------------------------------------------------ Späher
        public void SpeakToScout(Scout s)
        {
            if (s == null || State == null) return;
            bool first = ScoutModel.Speak(State, s.Id);
            PlayScene(s.Scene, () =>
            {
                if (!first) return;
                var prof = PlayerProfile.Instance;
                if (prof != null)
                {
                    prof.State.Fog.Reveal(s.SectorCenter, s.SectorRadius);
                    prof.GrantXp(HeartModel.Experience(GameManager.Instance.CurrentZone != null ? GameManager.Instance.CurrentZone.LevelMin : 1) / 2, "Späher");
                }
                var hearts = new List<KeyValuePair<string, Vector3>>();
                foreach (var o in ZoneObject.Objects) if (o is Heart h && h.Definition != null) hearts.Add(new KeyValuePair<string, Vector3>(h.Id, h.Center));
                foreach (var id in ScoutModel.HeartsInSector(s.SectorCenter, s.SectorRadius, hearts)) if (!State.Scouts.Contains("heart:" + id)) State.Scouts.Add("heart:" + id);
                GameEventBus.Notify("Späher: Gebiet und Herzen auf der Karte aufgedeckt", new Color(0.7f, 0.9f, 1f));
                GameEventBus.RaiseProfileChanged();
                Report(ObjectiveKind.Scout, s.Id);
            });
        }
    }
}
