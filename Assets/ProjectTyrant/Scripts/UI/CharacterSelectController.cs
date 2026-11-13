using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 5 / M25: Charakterauswahl (PT_CharacterSelect) wie GW2 – bis zu 5 Charaktere als animierte Figuren vor einer Kulisse,
    /// Auswahl per Klick oder Pfeiltasten mit Kamerafahrt, Spielen/Erstellen/Löschen (Bestätigung mit Namenseingabe), Erstellung mit
    /// Klassenkarten, Name, Farbschema und Vorschau des Signatur-Skills. Musik-Zustand Menü.
    /// </summary>
    public partial class CharacterSelectController : MonoBehaviour
    {
        public const string SceneName = "PT_CharacterSelect", CoreScene = "PT_Core";
        public ClassLibrary Classes;
        public SoundBank Bank;
        public IconLibrary Icons;
        public Transform[] Figures = new Transform[5];
        public Transform CameraRig;
        public static readonly string[] Palette = { "#C8A040", "#3050A0", "#A03030", "#2E7D4F", "#6A3FA0", "#E0E0E0", "#303030", "#D07020" };

        AccountDto _acc; int _sel; Vector3 _camTarget; Quaternion _camRot;
        RectTransform _root, _list, _create, _delete; TextMeshProUGUI _info, _status; TMP_InputField _nameField, _deleteField;
        readonly List<Button> _slotButtons = new List<Button>();
        string _newClass = "waechter"; int _p, _s = 1; bool _creating;
        AudioSource _ui; readonly List<AudioSource> _music = new List<AudioSource>();
        readonly RoundRobin _rr = new RoundRobin(5);
        public int Selected => _sel;
        public AccountDto Account => _acc;

        void Start()
        {
            UiFactory.Icons = Icons;
            _acc = AccountStore.LoadOrMigrate();
            _sel = Mathf.Max(0, _acc.Selected);
            SetupAudio();
            BuildUi();
            Refresh();
            Focus(_sel, true);
        }

        void OnDestroy() { if (UiFactory.UiSound == (System.Action<string>)PlayUi) UiFactory.UiSound = null; }

        // ------------------------------------------------------------ Audio (Musik-Zustand Menü, UI-Klänge)
        void SetupAudio()
        {
            var o = GameOptions.Load();
            float music = o.Master * o.Master * o.Music * o.Music * Synth.DbToLin(-6f);
            var set = Bank != null ? Bank.GetMusic("menu/Menu") : null;
            if (set != null) for (int i = 0; i < 5; i++)
            {
                if (set.Stems[i] == null || (Stem)i == Stem.Percussion) continue;
                var a = gameObject.AddComponent<AudioSource>(); a.clip = set.Stems[i]; a.loop = true; a.volume = music; a.spatialBlend = 0f; a.Play(); _music.Add(a);
            }
            _ui = gameObject.AddComponent<AudioSource>(); _ui.spatialBlend = 0f;
            UiFactory.UiSound = PlayUi;
        }

        void PlayUi(string id)
        {
            var e = Bank != null ? Bank.Get("UI/" + id) : null; if (e == null || e.Clips.Length == 0) return;
            var o = GameOptions.Load();
            _ui.PlayOneShot(e.Clips[_rr.Next(e.Id, e.Clips.Length)], o.Master * o.Master * o.Ui * o.Ui * Synth.DbToLin(-10f));
        }

        // ------------------------------------------------------------ UI
        void BuildUi()
        {
            var cgo = new GameObject("PT_CharSelectUI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = cgo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var sc = cgo.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
            if (FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null) new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            _root = (RectTransform)cgo.transform;
            var title = UiFactory.Text("Title", _root, "Charakterauswahl", 34, TextAlignmentOptions.Center, UiFactory.AccentColor);
            title.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(800, 50));

            var panel = UiFactory.Image("List", _root, UiFactory.PanelColor, UiFactory.Sprite("ui_panel"), true);
            _list = panel.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(30, 40), new Vector2(380, 520));
            var v = UiFactory.VList(_list, 8, 12); ((RectTransform)v.transform).Stretch(0, 0, 0, 70);
            for (int i = 0; i < AccountModel.MaxCharacters; i++)
            {
                int idx = i; var b = UiFactory.Button("Slot" + i, v.transform, "", () => Focus(idx, false), 17); UiFactory.Layout(b, 78);
                b.GetComponentInChildren<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
                _slotButtons.Add(b);
            }
            var strip = UiFactory.Image("InfoBack", _root, new Color(0.08f, 0.07f, 0.06f, 0.72f), UiFactory.Sprite("ui_panel"), true);
            strip.raycastTarget = false; strip.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 76), new Vector2(760, 80));
            var titleBack = UiFactory.Image("TitleBack", _root, new Color(0.08f, 0.07f, 0.06f, 0.6f), UiFactory.Sprite("ui_panel"), true);
            titleBack.raycastTarget = false; titleBack.rectTransform.Place(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -20), new Vector2(420, 58));
            title.transform.SetAsLastSibling();
            _info = UiFactory.Text("Info", _root, "", 20, TextAlignmentOptions.Center, Color.white);
            _info.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 110), new Vector2(900, 40));
            _status = UiFactory.Text("Status", _root, "", 16, TextAlignmentOptions.Center, new Color(1f, 0.7f, 0.5f));
            _status.rectTransform.Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 80), new Vector2(900, 28));
            Btn("Play", "Spielen", new Vector2(0, 24), new Vector2(220, 48), Play);
            Btn("Create", "Erstellen", new Vector2(-250, 24), new Vector2(200, 44), OpenCreate);
            Btn("Delete", "Löschen", new Vector2(250, 24), new Vector2(200, 44), OpenDelete);
            BuildCreate(); BuildBio(); BuildDelete();
        }

        Button Btn(string n, string label, Vector2 pos, Vector2 size, System.Action a)
        {
            var b = UiFactory.Button(n, _root, label, a, 20);
            ((RectTransform)b.transform).Place(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos, size);
            return b;
        }

        void BuildCreate()
        {
            var bg = UiFactory.Image("Create", _root, UiFactory.PanelColor, UiFactory.Sprite("ui_panel"), true);
            _create = bg.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 40), new Vector2(560, 720));
            var head = UiFactory.Text("Head", _create, "Charakter erstellen", 24, TextAlignmentOptions.Center, UiFactory.AccentColor); head.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(0, 34));
            float y = -56;
            if (Classes != null) foreach (var c in Classes.Classes)
            {
                var cc = c;
                var card = UiFactory.Button("Class_" + c.Id, _create, "<b>" + c.DisplayName + "</b>\n<size=13>" + c.Summary + "</size>", () => PickClass(cc.Id), 17);
                ((RectTransform)card.transform).Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(-24, 88));
                card.GetComponent<Image>().color = c.Color * 0.45f + new Color(0, 0, 0, 1f);
                y -= 96;
            }
            _nameField = UiFactory.InputField("Name", _create, "Name (3–24 Zeichen)");
            ((RectTransform)_nameField.transform).Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, y - 6), new Vector2(-24, 38));
            y -= 54;
            for (int row = 0; row < 2; row++)
            {
                var lbl = UiFactory.Text("ColLbl" + row, _create, row == 0 ? "Primär" : "Sekundär", 15, TextAlignmentOptions.Left, Color.white);
                lbl.rectTransform.Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(14, y - 8), new Vector2(100, 30));
                for (int i = 0; i < Palette.Length; i++)
                {
                    int r = row, ci = i; ColorUtility.TryParseHtmlString(Palette[i], out var col);
                    var sw = UiFactory.Button("Col" + row + "_" + i, _create, "", () => { if (r == 0) _p = ci; else _s = ci; UpdatePreview(); }, 12);
                    ((RectTransform)sw.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(110 + i * 52, y - 6), new Vector2(44, 34));
                    sw.GetComponent<Image>().color = col;
                }
                y -= 44;
            }
            var ok = UiFactory.Button("Confirm", _create, "Weiter", OpenBio, 19); ((RectTransform)ok.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 12), new Vector2(200, 44));
            var back = UiFactory.Button("Back", _create, "Zurück", CloseCreate, 17); ((RectTransform)back.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 12), new Vector2(160, 44));
            _create.gameObject.SetActive(false);
        }

        void BuildDelete()
        {
            var bg = UiFactory.Image("Delete", _root, UiFactory.PanelColor, UiFactory.Sprite("ui_panel"), true);
            _delete = bg.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 220));
            var t = UiFactory.Text("Text", _delete, "Zum Löschen den Namen des Charakters eingeben:", 17, TextAlignmentOptions.Center, Color.white); t.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(-20, 60));
            _deleteField = UiFactory.InputField("Confirm", _delete, "Name"); ((RectTransform)_deleteField.transform).Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -6), new Vector2(380, 38));
            var ok = UiFactory.Button("Ok", _delete, "Endgültig löschen", ConfirmDelete, 16); ((RectTransform)ok.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 12), new Vector2(210, 40));
            ok.GetComponent<UiButtonFeedback>().ClickSound = "DestroyConfirm";
            var no = UiFactory.Button("No", _delete, "Abbrechen", () => _delete.gameObject.SetActive(false), 16); ((RectTransform)no.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 12), new Vector2(160, 40));
            _delete.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------ Ablauf
        void Refresh()
        {
            for (int i = 0; i < _slotButtons.Count; i++)
            {
                var c = AccountModel.Get(_acc, i);
                var cls = c != null && Classes != null ? Classes.Get(c.ClassId) : null;
                UiFactory.SetLabel(_slotButtons[i], c == null ? "  <i>Freier Platz</i>" : "  <b>" + c.Name + "</b>\n  <size=14>" + (cls != null ? cls.DisplayName : c.ClassId) + (string.IsNullOrEmpty(c.EliteSpec) ? "" : " · " + c.EliteSpec) + " · Stufe " + c.Level + " · " + ZoneName(c.Zone) + "</size>");
                _slotButtons[i].GetComponent<Image>().color = i == _sel ? new Color(0.45f, 0.35f, 0.2f) : UiFactory.ButtonColor;
                if (Figures.Length > i && Figures[i] != null)
                {
                    bool show = c != null || (_creating && i == _sel);
                    ShowFigure(Figures[i], show);
                    if (c != null && !(_creating && i == _sel)) Dress(Figures[i], cls, c.PrimaryHex, c.SecondaryHex, c.EliteSpec);
                }
            }
            var cur = AccountModel.Get(_acc, _sel);
            _info.text = cur != null ? cur.Name + " – zuletzt gespielt " + (string.IsNullOrEmpty(cur.LastPlayed) ? "–" : cur.LastPlayed) : "Freier Platz – „Erstellen“ für einen neuen Charakter";
        }

        /// <summary>
        /// Vorschaufigur ein-/ausblenden OHNE GameObject.SetActive: das Deaktivieren einer geriggten Figur baut im Animator das
        /// AnimationSet ab (Animator::Deactivate → mecanim DestroyAnimationSet) und ließ den Editor beim Schließen des
        /// Erstellungsdialogs abstürzen (Absturzbericht 12.11.2026). Die Figur bleibt aktiv, nur ihre Renderer werden umgeschaltet.
        /// </summary>
        static void ShowFigure(Transform fig, bool show)
        {
            if (fig == null) return;
            if (!fig.gameObject.activeSelf) fig.gameObject.SetActive(true);   // nur einmalig einschalten (gespeichert inaktiv), nie wieder aus
            ActorVisibility.SetRenderersVisible(fig, show);
        }

        static string ZoneName(string id) => id == "nebelmoor" ? "Nebelmoor" : id == "frostkamm" ? "Frostkamm" : id == "gluetoede" ? "Glutöde" : "Sonnfurt";

        void Dress(Transform fig, ClassDefinition c, string p, string s, string elite = null)
        {
            var vis = fig.GetComponentInChildren<PlayerVisuals>(true); if (vis == null) return;
            vis.ApplyLook(c, p, s);
            // Lauf 5 / M26: aktive Elite-Spezialisierung am Charakter – Farbakzent und Elite-Waffe
            EliteSpecDefinition e = null;
            if (c != null && !string.IsNullOrEmpty(elite)) foreach (var x in c.EliteSpecs) if (x != null && x.DisplayName == elite) e = x;
            vis.SetAccent(e != null ? e.Accent : (Color?)null);
            vis.ShowWeapon(e != null && e.Weapon != null ? e.Weapon.Visual : c != null && c.WeaponSets.Count > 0 && c.WeaponSets[0] != null ? c.WeaponSets[0].Visual : WeaponVisual.Greatsword);
        }

        void Focus(int slot, bool instant)
        {
            _sel = Mathf.Clamp(slot, 0, AccountModel.MaxCharacters - 1);
            if (Figures.Length > _sel && Figures[_sel] != null)
            {
                var f = Figures[_sel];
                _camTarget = f.position + f.forward * 4.2f + Vector3.up * 1.5f + f.right * 0.9f;
                _camRot = Quaternion.LookRotation(f.position + Vector3.up * 1.1f - _camTarget);
                if (instant && CameraRig != null) { CameraRig.position = _camTarget; CameraRig.rotation = _camRot; }
            }
            UiFactory.Sound("CharSelect");
            Refresh();
        }

        void Update()
        {
            if (CameraRig != null) { CameraRig.position = Vector3.Lerp(CameraRig.position, _camTarget, 1f - Mathf.Exp(-Time.deltaTime * 4f)); CameraRig.rotation = Quaternion.Slerp(CameraRig.rotation, _camRot, 1f - Mathf.Exp(-Time.deltaTime * 4f)); }
            if (InputManager.TextInputActive || _creating || _delete.gameObject.activeSelf) return;
            if (KeyDown(UnityEngine.InputSystem.Key.UpArrow, KeyCode.UpArrow) || KeyDown(UnityEngine.InputSystem.Key.LeftArrow, KeyCode.LeftArrow)) Focus((_sel + AccountModel.MaxCharacters - 1) % AccountModel.MaxCharacters, false);
            if (KeyDown(UnityEngine.InputSystem.Key.DownArrow, KeyCode.DownArrow) || KeyDown(UnityEngine.InputSystem.Key.RightArrow, KeyCode.RightArrow)) Focus((_sel + 1) % AccountModel.MaxCharacters, false);
            if (KeyDown(UnityEngine.InputSystem.Key.Enter, KeyCode.Return)) Play();
        }

        static bool KeyDown(UnityEngine.InputSystem.Key k, KeyCode legacy)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null) return kb[k].wasPressedThisFrame;
            return Input.GetKeyDown(legacy);
        }

        public void Play()
        {
            var c = AccountModel.Get(_acc, _sel);
            if (c == null) { OpenCreate(); return; }
            _acc.Selected = _sel; AccountStore.Save(_acc);
            UiFactory.UiSound = null;
            SceneManager.LoadScene(CoreScene);
        }

        void OpenCreate()
        {
            if (AccountModel.Get(_acc, _sel) != null) { int free = AccountModel.FreeSlot(_acc); if (free < 0) { _status.text = "Alle 5 Charakterplätze belegt"; UiFactory.Sound("Error"); return; } _sel = free; Focus(free, false); }
            _creating = true; _create.gameObject.SetActive(true); _nameField.text = "";
            PickClass(_newClass);
        }

        void CloseCreate() { _creating = false; _create.gameObject.SetActive(false); if (_bio != null) _bio.gameObject.SetActive(false); Refresh(); }

        public void PickClass(string id)
        {
            _newClass = id; var c = Classes != null ? Classes.Get(id) : null;
            if (c != null) { _p = System.Array.IndexOf(Palette, "#" + ColorUtility.ToHtmlStringRGB(c.Color)); if (_p < 0) _p = 0; }
            UpdatePreview();
            // Vorschau des Signatur-Skills (Animation + VFX)
            var fig = Figures.Length > _sel ? Figures[_sel] : null;
            var sig = c != null ? Find(c, c.SignatureSkill) : null;
            if (fig != null && sig != null && sig.Timeline != null && sig.Timeline.IsBuilt)
            {
                var anim = fig.GetComponentInChildren<Anim.CharacterAnimator>(true);
                if (anim != null) anim.PlayFull(sig.Timeline.Clip, 0.1f, 1f);
                SkillFx.Vfx(sig.Timeline.VfxRelease, fig.position + Vector3.up * 1.2f + fig.forward * 0.6f, fig.rotation, null, 0.9f);
                SkillFx.Vfx(sig.Timeline.VfxImpact, fig.position + fig.forward * 2.5f, Quaternion.identity, null, 0.9f);
            }
            _status.text = c != null ? c.DisplayName + ": " + c.MechanicSummary : "";
        }

        static SkillDefinition Find(ClassDefinition c, string idPart)
        {
            if (string.IsNullOrEmpty(idPart)) return null;
            foreach (var s in c.AllSkills()) if (s != null && (s.name.EndsWith(idPart) || s.Id.EndsWith(idPart))) return s;
            return null;
        }

        void UpdatePreview()
        {
            var fig = Figures.Length > _sel ? Figures[_sel] : null; if (fig == null) return;
            ShowFigure(fig, true);
            Dress(fig, Classes != null ? Classes.Get(_newClass) : null, Palette[_p], Palette[_s]);
        }

        void ConfirmCreate()
        {
            int slot = AccountModel.Create(_acc, _nameField.text, _newClass, Palette[_p], Palette[_s], out string why);
            if (slot < 0) { _status.text = why; UiFactory.Sound("Error"); return; }
            _acc.Selected = slot; AccountStore.Save(_acc);
            AccountStore.DeleteCharacterFile(slot);   // frischer Charakter (alte Datei eines gelöschten Platzes verwerfen)
            _sel = slot; CloseCreate();
            _status.text = "Charakter erstellt – „Spielen“ startet in Sonnfurt.";
        }

        /// <summary>Für QA/Tests: Charakter ohne UI anlegen.</summary>
        public int CreateDirect(string name, string classId) { int s = AccountModel.Create(_acc, name, classId, Palette[0], Palette[1], out _); if (s >= 0) { ApplyBio(AccountModel.Get(_acc, s)); AccountStore.DeleteCharacterFile(s); AccountStore.Save(_acc); _sel = s; Refresh(); } return s; }

        void OpenDelete()
        {
            if (AccountModel.Get(_acc, _sel) == null) { _status.text = "Kein Charakter auf diesem Platz"; return; }
            _deleteField.text = ""; _delete.gameObject.SetActive(true);
        }

        void ConfirmDelete()
        {
            if (!AccountModel.Delete(_acc, _sel, _deleteField.text, out string why)) { _status.text = why; UiFactory.Sound("Error"); return; }
            AccountStore.DeleteCharacterFile(_sel); AccountStore.Save(_acc);
            _delete.gameObject.SetActive(false); _status.text = "Charakter gelöscht"; Refresh();
        }
    }
}
