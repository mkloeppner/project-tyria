using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 6 / M28: Biografie bei der Charaktererstellung (wie die drei Fragen in GW2, mit eigenen Inhalten): Herkunft (Hof/Posten/Ruinen),
    /// Wesenszug (Charme/Würde/Wildheit), Leitstern (Verlorener/Rätsel/Schuld) und Stimm-Variante A/B. Setzt die Felder des Charakterplatzes.
    /// </summary>
    public partial class CharacterSelectController
    {
        RectTransform _bio; TextMeshProUGUI _bioInfo;
        public int BioOrigin = 1, BioTrait = 2, BioLodestar = 2; public bool BioVoiceB;
        readonly Button[][] _bioButtons = new Button[4][];

        static readonly string[][] BioLabels =
        {
            new[] { "Hof am Fluss", "Grenzposten", "Ruinensiedlung" },
            new[] { "Charme", "Würde", "Wildheit" },
            new[] { "Ein Verlorener", "Ein Rätsel", "Eine alte Schuld" },
            new[] { "Stimme A", "Stimme B" },
        };
        static readonly string[] BioHeads = { "Woher stammst du?", "Wie begegnest du anderen?", "Was treibt dich an?", "Deine Stimme" };
        static readonly string[][] BioHelp =
        {
            new[] { "Aufgewachsen auf einem Hof bei Sonnfurt – du kennst Felder, Vieh und Nachbarn.", "Aufgewachsen an der Wacht – Drill, Wachfeuer, Pflicht.", "Aufgewachsen zwischen alten Ruinen – Gelehrte, Scherben, Geheimnisse." },
            new[] { "Gewinnende Worte öffnen Türen.", "Haltung und Anstand zählen.", "Du handelst, bevor andere fertig reden." },
            new[] { "Jemand ist verschwunden, den du finden willst.", "Ein Traum, ein Zeichen – du willst verstehen.", "Etwas aus der Vergangenheit lastet auf dir." },
            new[] { "Hellere Stimme.", "Tiefere Stimme." },
        };

        void BuildBio()
        {
            var bg = UiFactory.Image("Bio", _root, UiFactory.PanelColor, UiFactory.Sprite("ui_panel"), true);
            _bio = bg.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-30, 40), new Vector2(560, 720));
            var head = UiFactory.Text("Head", _bio, "Biografie", 24, TextAlignmentOptions.Center, UiFactory.AccentColor); head.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(0, 34));
            float y = -54;
            for (int q = 0; q < 4; q++)
            {
                var h = UiFactory.Text("Q" + q, _bio, BioHeads[q], 17, TextAlignmentOptions.Left, Color.white); h.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(-28, 28));
                y -= 32;
                int n = BioLabels[q].Length; _bioButtons[q] = new Button[n];
                float w = (536f - (n - 1) * 8f) / n;
                for (int i = 0; i < n; i++)
                {
                    int qq = q, ii = i;
                    var b = UiFactory.Button("Q" + q + "_" + i, _bio, BioLabels[q][i], () => PickBio(qq, ii), 15);
                    ((RectTransform)b.transform).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(12 + i * (w + 8f), y), new Vector2(w, 44));
                    _bioButtons[q][i] = b;
                }
                y -= 58;
            }
            _bioInfo = UiFactory.Text("Info", _bio, "", 15, TextAlignmentOptions.TopLeft, new Color(0.85f, 0.85f, 0.8f));
            _bioInfo.rectTransform.Place(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, y - 6), new Vector2(-28, 200));
            var ok = UiFactory.Button("Confirm", _bio, "Erstellen", ConfirmBio, 19); ((RectTransform)ok.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-12, 12), new Vector2(200, 44));
            var back = UiFactory.Button("Back", _bio, "Zurück", () => { _bio.gameObject.SetActive(false); _create.gameObject.SetActive(true); }, 17); ((RectTransform)back.transform).Place(new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 12), new Vector2(160, 44));
            _bio.gameObject.SetActive(false);
            RefreshBio();
        }

        public void PickBio(int q, int i)
        {
            if (q == 0) BioOrigin = i + 1; else if (q == 1) BioTrait = i + 1; else if (q == 2) BioLodestar = i + 1; else BioVoiceB = i == 1;
            UiFactory.Sound("Click"); RefreshBio();
        }

        int BioSel(int q) => q == 0 ? BioOrigin - 1 : q == 1 ? BioTrait - 1 : q == 2 ? BioLodestar - 1 : BioVoiceB ? 1 : 0;

        void RefreshBio()
        {
            if (_bio == null) return;
            var sb = new System.Text.StringBuilder();
            for (int q = 0; q < 4; q++)
            {
                int s = BioSel(q);
                for (int i = 0; i < _bioButtons[q].Length; i++) _bioButtons[q][i].GetComponent<Image>().color = i == s ? UiFactory.AccentColor * 0.8f + new Color(0, 0, 0, 1) : new Color(0.16f, 0.17f, 0.2f, 1f);
                sb.Append("• ").Append(BioHelp[q][s]).Append('\n');
            }
            _bioInfo.text = sb.ToString();
        }

        void OpenBio()
        {
            if (string.IsNullOrEmpty(_newClass)) { _status.text = "Bitte eine Klasse wählen"; UiFactory.Sound("Error"); return; }
            if (!AccountModel.ValidName(_acc, _nameField.text, out string why)) { _status.text = why; UiFactory.Sound("Error"); return; }
            _create.gameObject.SetActive(false); _bio.gameObject.SetActive(true); RefreshBio();
        }

        void ConfirmBio()
        {
            int slot = AccountModel.Create(_acc, _nameField.text, _newClass, Palette[_p], Palette[_s], out string why);
            if (slot < 0) { _status.text = why; UiFactory.Sound("Error"); return; }
            ApplyBio(AccountModel.Get(_acc, slot));
            _acc.Selected = slot; AccountStore.Save(_acc);
            AccountStore.DeleteCharacterFile(slot);
            _sel = slot; _bio.gameObject.SetActive(false); CloseCreate();
            _status.text = "Charakter erstellt – „Spielen“ startet deine Geschichte.";
        }

        void ApplyBio(CharacterSlotDto c) { if (c == null) return; c.Origin = BioOrigin; c.Trait = BioTrait; c.Lodestar = BioLodestar; c.VoiceB = BioVoiceB; }
    }
}
