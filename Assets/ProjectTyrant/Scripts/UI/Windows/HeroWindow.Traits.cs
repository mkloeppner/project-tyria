using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Tab „Eigenschaften“: 3 Spezialisierungen × 3 Ränge (je 1 kleine + 3 große Eigenschaften).</summary>
    public partial class HeroWindow
    {
        class TraitButton { public string Spec; public int Tier, Choice = -1; public TraitDefinition Trait; public Image Bg, Frame; public TextMeshProUGUI Glyph, Cost; }
        class LineUi { public SpecializationDefinition Spec; public RectTransform Panel; public Button Activate; public TextMeshProUGUI Status; public Button Unlock; public readonly List<TraitButton> Buttons = new List<TraitButton>(); public Image[] TierBg = new Image[3]; }
        readonly List<LineUi> _lineUis = new List<LineUi>();
        TextMeshProUGUI _traitsInfo;
        // Lauf 5 / M26: Platz 3 – dritte Kernlinie oder eine Elite-Spezialisierung (Reiter oben in der Spalte)
        readonly List<LineUi> _slot3 = new List<LineUi>(); readonly List<Button> _slot3Tabs = new List<Button>(); int _slot3View = -1;

        partial void BuildTraitsPage()
        {
            var page = TraitsPage;
            var prof = PlayerProfile.Instance;
            var specs = prof != null && prof.Specializations != null ? prof.Specializations : new SpecializationDefinition[0];
            _traitsInfo = UiFactory.Text("Info", page, "", 15, TextAlignmentOptions.BottomLeft, new Color(0.85f, 0.85f, 0.9f));
            _traitsInfo.rectTransform.Place(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 0), new Vector2(10, 2), new Vector2(-20, 40));
            var cols = new List<SpecializationDefinition>(); int core = 0;
            foreach (var sp in specs) { if (sp == null) continue; if (sp is EliteSpecDefinition || core < 3) cols.Add(sp); if (!(sp is EliteSpecDefinition)) core++; }
            for (int col = 0; col < cols.Count; col++)
            {
                var spec = cols[col];
                bool slot3 = col >= 2; bool elite = spec is EliteSpecDefinition;
                float x = 10 + Mathf.Min(col, 2) * 345;
                var ui = new LineUi { Spec = spec };
                var panel = UiFactory.Image("Line_" + spec.Id, page, new Color(spec.Color.r, spec.Color.g, spec.Color.b, elite ? 0.14f : 0.08f), UiFactory.Sprite("ui_round"));
                panel.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -2), new Vector2(335, 568));
                ui.Panel = panel.rectTransform;
                if (elite)
                {   // eigener Rahmen für Elite-Linien
                    var fr = UiFactory.Image("EliteFrame", panel.transform, new Color(spec.Color.r, spec.Color.g, spec.Color.b, 0.9f), UiFactory.Sprite("ui_frame")); fr.rectTransform.Stretch(-2, -2, -2, -2); fr.raycastTarget = false;
                    string eid = spec.Id;
                    ui.Activate = UiFactory.Button("Activate", panel.transform, "Aktivieren", () => OnActivateElite(eid), 13);
                    ((RectTransform)ui.Activate.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -6), new Vector2(110, 26));
                }
                var name = UiFactory.Text("Name", panel.transform, spec.DisplayName, 21, TextAlignmentOptions.Left, spec.Color);
                name.fontStyle = FontStyles.Bold;
                name.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -6), new Vector2(-20, 28));
                if (!slot3)
                {
                    var desc = UiFactory.Text("Desc", panel.transform, spec.Description, 13, TextAlignmentOptions.Left, new Color(0.75f, 0.75f, 0.8f));
                    desc.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -34), new Vector2(-20, 20));
                }
                ui.Status = UiFactory.Text("Status", panel.transform, "", 14, TextAlignmentOptions.Left, new Color(0.6f, 0.9f, 1f));
                ui.Status.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(10, -58), new Vector2(-190, 30));
                string sid = spec.Id;
                ui.Unlock = UiFactory.Button("Unlock", panel.transform, elite ? "Schritt freischalten (10)" : "Rang freischalten (5)", () => OnUnlockTier(sid), 13);
                ((RectTransform)ui.Unlock.transform).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-8, -56), new Vector2(170, 30));
                for (int t = 1; t <= 3; t++)
                {
                    float y = -96 - (t - 1) * 156;
                    var tierBg = UiFactory.Image("Tier" + t, panel.transform, new Color(0, 0, 0, 0.25f), UiFactory.Sprite("ui_round"));
                    tierBg.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(6, y), new Vector2(-12, 148));
                    ui.TierBg[t - 1] = tierBg;
                    var lbl = UiFactory.Text("Lbl", tierBg.transform, "Rang " + t, 14, TextAlignmentOptions.Left, UiFactory.AccentColor);
                    lbl.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, 1), new Vector2(8, -4), new Vector2(-16, 20));
                    var minor = MakeTraitButton(tierBg.transform, new Vector2(12, -50), 50, true);
                    minor.Spec = sid; minor.Tier = t; minor.Trait = spec.Minors[t - 1];
                    ui.Buttons.Add(minor);
                    for (int c = 0; c < 3; c++)
                    {
                        var mb = MakeTraitButton(tierBg.transform, new Vector2(90 + c * 78, -36), 66, false);
                        mb.Spec = sid; mb.Tier = t; mb.Choice = c; mb.Trait = spec.Major(t, c);
                        int tt = t, cc = c;
                        mb.Bg.gameObject.AddComponent<UiClickRelay>().Left = () => OnMajorClick(sid, tt, cc);
                        ui.Buttons.Add(mb);
                    }
                }
                _lineUis.Add(ui);
                if (slot3) _slot3.Add(ui);
            }
            // Reiter für Platz 3 (nur wenn die Klasse Elite-Spezialisierungen hat)
            if (_slot3.Count > 1)
            {
                var row = UiFactory.Rect("Slot3Tabs", page).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10 + 2 * 345 + 8, -32), new Vector2(320, 22));
                float w = 316f / _slot3.Count;
                for (int i = 0; i < _slot3.Count; i++)
                {
                    int idx = i; var sp = _slot3[i].Spec;
                    var b = UiFactory.Button("Tab" + i, row, sp is EliteSpecDefinition ? sp.DisplayName : "Kern", () => { _slot3View = idx; UiFactory.Sound("Tab"); MarkDirty(); }, 11);
                    ((RectTransform)b.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(i * w, 0), new Vector2(w - 3, 22));
                    b.gameObject.AddComponent<UiTooltipTrigger>().Provider = () => SlotTabTooltip(sp);
                    _slot3Tabs.Add(b);
                }
                row.SetAsLastSibling();
            }
        }

        TooltipContent SlotTabTooltip(SpecializationDefinition sp)
        {
            var c = new TooltipContent { Title = sp.DisplayName, Subtitle = sp is EliteSpecDefinition ? "Elite-Spezialisierung · Platz 3" : "Kernlinie · Platz 3" };
            var e = sp as EliteSpecDefinition;
            if (e == null) { c.Add(sp.Description, new Color(0.85f, 0.85f, 0.85f)); return c; }
            c.Add(e.Description, new Color(0.85f, 0.85f, 0.85f));
            c.Add("Mechanik (" + e.MechanicLabel + "): " + e.MechanicSummary, e.Accent);
            if (e.Weapon != null) c.Add("Waffe: " + e.Weapon.DisplayName + " (Schritt 2)", new Color(0.8f, 0.9f, 1f));
            c.Add("Skilltyp: " + TooltipBuilder.CategoryName(e.SkillType) + " – 1 Heilung, 3 Hilfe, 1 Elite (Schritt 3)", new Color(0.8f, 0.9f, 1f));
            c.Add("Ab Stufe " + EliteSpecDefinition.RequiredLevel + " · 3 Schritte à " + EliteSpecDefinition.StepCost + " Heldenpunkte · nur außerhalb des Kampfes wechselbar", new Color(0.7f, 0.7f, 0.75f));
            return c;
        }

        void OnActivateElite(string id)
        {
            var prof = PlayerProfile.Instance; if (prof == null) return;
            bool active = prof.Traits.ActiveElite == id;
            if (prof.ActivateElite(active ? null : id)) { UiFactory.Sound("TraitSelect"); MarkDirty(); }
            else UiFactory.Sound("Error");
        }

        TraitButton MakeTraitButton(Transform parent, Vector2 pos, float size, bool minor)
        {
            var b = new TraitButton();
            b.Bg = UiFactory.Image("Trait", parent, UiFactory.SlotColor, UiFactory.Sprite(minor ? "ui_circle" : "ui_round"), true);
            b.Bg.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(size, size));
            b.Frame = UiFactory.Image("Frame", b.Bg.transform, Color.clear, UiFactory.Sprite(minor ? "ui_ring" : "ui_frame"));
            b.Frame.rectTransform.Stretch();
            b.Glyph = UiFactory.Text("Glyph", b.Bg.transform, "", minor ? 18 : 22, TextAlignmentOptions.Center);
            b.Glyph.fontStyle = FontStyles.Bold;
            b.Glyph.rectTransform.Stretch();
            b.Cost = UiFactory.Text("Cost", b.Bg.transform, "", 12, TextAlignmentOptions.BottomRight, new Color(0.6f, 0.9f, 1f));
            b.Cost.rectTransform.Stretch(2, 4, 2, 2);
            b.Bg.gameObject.AddComponent<UiTooltipTrigger>().Provider = () => TraitTooltip(b);
            return b;
        }

        TooltipContent TraitTooltip(TraitButton b)
        {
            var prof = PlayerProfile.Instance;
            var line = prof != null ? prof.Traits.Get(b.Spec) : null;
            string state;
            if (line == null) state = "";
            else if (b.Choice < 0) state = line.TiersUnlocked >= b.Tier ? "Aktiv (kleine Eigenschaft)" : "Wird mit Rang " + b.Tier + " aktiv";
            else
            {
                bool unlocked = line.UnlockedMajors.Contains((b.Tier - 1) * 3 + b.Choice);
                bool sel = line.Selected[b.Tier - 1] == b.Choice && line.TiersUnlocked >= b.Tier;
                bool el = TraitModel.IsElite(line);
                state = sel ? "Aktiv" : unlocked ? "Freigeschaltet – Klicken zum Auswählen" : line.TiersUnlocked < b.Tier ? (el ? "Erst Schritt " + b.Tier + " freischalten" : "Erst Rang " + b.Tier + " freischalten") : el ? "Klicken: auswählen (im Schritt enthalten)" : "Klicken: für " + TraitModel.MajorCost + " Heldenpunkte freischalten";
                if (el && !sel && line.Id != PlayerProfile.Instance.Traits.ActiveElite) state += " · wirkt nur, wenn die Spezialisierung aktiv ist";
            }
            return TooltipBuilder.Trait(b.Trait, line != null ? line.Spec : null, state);
        }

        void OnUnlockTier(string specId)
        {
            var prof = PlayerProfile.Instance; if (prof == null) return;
            var line = prof.Traits.Get(specId);
            if (prof.BuildLocked) { GameEventBus.Notify("Im Kampf nicht änderbar", new Color(1f, 0.6f, 0.4f)); return; }
            if (!prof.Traits.CanUnlockTier(line, prof.Progression, out string why)) { GameEventBus.Notify(why, new Color(1f, 0.6f, 0.4f)); return; }
            if (TraitModel.IsElite(line))
            {
                string[] step = { "Linie", "Waffe", "Skilltyp" };
                _ui.Dialog.Confirm("Elite-Spezialisierung", line.Spec.DisplayName + ": Schritt " + (line.TiersUnlocked + 1) + " (" + step[Mathf.Clamp(line.TiersUnlocked, 0, 2)] + ") für " + EliteSpecDefinition.StepCost + " Heldenpunkte freischalten?", () => { prof.UnlockEliteStep(specId); MarkDirty(); }, "Freischalten");
                return;
            }
            _ui.Dialog.Confirm("Rang freischalten", line.Spec.DisplayName + ": Rang " + (line.TiersUnlocked + 1) + " für " + TraitModel.TierCost + " Heldenpunkte freischalten?", () => { prof.UnlockTraitTier(specId); MarkDirty(); }, "Freischalten");
        }

        void OnMajorClick(string specId, int tier, int choice)
        {
            var prof = PlayerProfile.Instance; if (prof == null) return;
            var line = prof.Traits.Get(specId);
            if (line == null) return;
            if (line.UnlockedMajors.Contains((tier - 1) * 3 + choice)) { prof.SelectMajor(specId, tier, choice); UiFactory.Sound("TraitSelect"); MarkDirty(); return; }
            if (prof.BuildLocked) { GameEventBus.Notify("Im Kampf nicht änderbar", new Color(1f, 0.6f, 0.4f)); return; }
            if (!prof.Traits.CanUnlockMajor(line, tier, choice, prof.Progression, out string why)) { GameEventBus.Notify(why, new Color(1f, 0.6f, 0.4f)); return; }
            var t = line.Spec.Major(tier, choice);
            if (TraitModel.IsElite(line)) { prof.UnlockMajor(specId, tier, choice); MarkDirty(); return; }   // im Elite-Schritt enthalten
            _ui.Dialog.Confirm("Eigenschaft freischalten", t.DisplayName + " für " + TraitModel.MajorCost + " Heldenpunkte freischalten?", () => { prof.UnlockMajor(specId, tier, choice); MarkDirty(); }, "Freischalten");
        }

        partial void RefreshTraits()
        {
            var prof = PlayerProfile.Instance;
            if (prof == null) return;
            int spent = 0;
            // Platz 3: sichtbare Spalte (Standard: aktive Elite oder Kernlinie)
            if (_slot3.Count > 0)
            {
                if (_slot3View < 0 || _slot3View >= _slot3.Count) { _slot3View = 0; for (int i = 0; i < _slot3.Count; i++) if (_slot3[i].Spec.Id == prof.Traits.ActiveElite) _slot3View = i; }
                for (int i = 0; i < _slot3.Count; i++)
                {
                    _slot3[i].Panel.gameObject.SetActive(i == _slot3View);
                    if (i < _slot3Tabs.Count)
                    {
                        bool act = _slot3[i].Spec is EliteSpecDefinition ? prof.Traits.ActiveElite == _slot3[i].Spec.Id : prof.Traits.ActiveElite == null;
                        var img = _slot3Tabs[i].GetComponent<Image>(); if (img != null) img.color = i == _slot3View ? _slot3[i].Spec.Color * 0.8f + new Color(0, 0, 0, 0.2f) : act ? new Color(0.35f, 0.3f, 0.15f) : new Color(0.16f, 0.15f, 0.18f);
                        UiFactory.SetLabel(_slot3Tabs[i], (act ? "● " : "") + (_slot3[i].Spec is EliteSpecDefinition ? _slot3[i].Spec.DisplayName : "Kern"));
                    }
                }
            }
            foreach (var ui in _lineUis)
            {
                var line = prof.Traits.Get(ui.Spec.Id);
                if (line == null) continue;
                bool elite = TraitModel.IsElite(line);
                spent += elite ? line.TiersUnlocked * EliteSpecDefinition.StepCost : line.TiersUnlocked * TraitModel.TierCost + line.UnlockedMajors.Count * TraitModel.MajorCost;
                if (elite)
                {
                    bool act = prof.Traits.ActiveElite == line.Id; string[] step = { "gesperrt", "Linie", "Linie + Waffe", "Linie + Waffe + Skilltyp" };
                    ui.Status.text = "Schritt " + line.TiersUnlocked + " / 3 · " + step[Mathf.Clamp(line.TiersUnlocked, 0, 3)] + (act ? " · <color=#FD6>aktiv</color>" : "");
                    ui.Unlock.gameObject.SetActive(line.TiersUnlocked < 3);
                    UiFactory.SetLabel(ui.Unlock, prof.Progression.Level < EliteSpecDefinition.RequiredLevel ? "ab Stufe " + EliteSpecDefinition.RequiredLevel : "Schritt " + (line.TiersUnlocked + 1) + " freischalten (" + EliteSpecDefinition.StepCost + ")");
                    if (ui.Activate != null) { ui.Activate.gameObject.SetActive(line.TiersUnlocked >= 1); UiFactory.SetLabel(ui.Activate, act ? "Ablegen" : "Aktivieren"); }
                }
                else
                {
                    bool off = !prof.Traits.IsLineActive(line);
                    ui.Status.text = "Rang " + line.TiersUnlocked + " / 3 · " + line.UnlockedMajors.Count + " / 9 groß" + (off ? " · <color=#F96>Platz 3 belegt durch Elite</color>" : "");
                    ui.Unlock.gameObject.SetActive(line.TiersUnlocked < 3);
                    UiFactory.SetLabel(ui.Unlock, "Rang " + (line.TiersUnlocked + 1) + " freischalten (" + TraitModel.TierCost + ")");
                }
                for (int t = 0; t < 3; t++) ui.TierBg[t].color = line.TiersUnlocked > t ? new Color(ui.Spec.Color.r, ui.Spec.Color.g, ui.Spec.Color.b, 0.12f) : new Color(0, 0, 0, 0.35f);
                foreach (var b in ui.Buttons)
                {
                    bool tierOpen = line.TiersUnlocked >= b.Tier;
                    bool active, unlocked;
                    if (b.Choice < 0) { active = tierOpen; unlocked = tierOpen; }
                    else { unlocked = line.UnlockedMajors.Contains((b.Tier - 1) * 3 + b.Choice); active = tierOpen && line.Selected[b.Tier - 1] == b.Choice; }
                    b.Glyph.text = b.Trait != null ? b.Trait.Glyph : "?";
                    b.Glyph.color = active ? Color.white : unlocked ? new Color(0.85f, 0.85f, 0.9f) : new Color(0.45f, 0.45f, 0.5f);
                    b.Bg.color = active ? ui.Spec.Color * 0.7f + new Color(0, 0, 0, 0.3f) : unlocked ? new Color(0.25f, 0.23f, 0.28f) : new Color(0.12f, 0.11f, 0.14f);
                    b.Frame.color = active ? UiFactory.AccentColor : unlocked ? new Color(1, 1, 1, 0.35f) : new Color(0, 0, 0, 0.6f);
                    b.Cost.text = b.Choice >= 0 && !unlocked && tierOpen && !elite ? TraitModel.MajorCost.ToString() : "";
                }
            }
            _traitsInfo.text = "Eigenschaften: " + spent + " Heldenpunkte investiert   ·   verfügbar: <color=#9CF>" + prof.Progression.AvailableHeroPoints + "</color>"
                + (prof.BuildLocked ? "   ·   <color=#F96>Im Kampf gesperrt</color>" : "   ·   Auswahl jederzeit außerhalb des Kampfes änderbar");
        }
    }
}
