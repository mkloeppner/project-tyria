using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Briefe (GW2-Post) – Liste links, Brief rechts, Beilage per Klick ins Inventar.</summary>
    public class MailWindow
    {
        public readonly UiWindow Window;
        readonly RectTransform _list;
        readonly TextMeshProUGUI _body;
        readonly Button _take;
        string _selected; int _version = -1;

        public MailWindow(WindowManager mgr, UIController ui)
        {
            Window = new UiWindow(mgr, "mail", "Briefe", new Vector2(820, 520));
            _list = UiFactory.Rect("List", Window.Content).Place(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(280, -16));
            var v = _list.gameObject.AddComponent<VerticalLayoutGroup>(); v.spacing = 4; v.childControlHeight = true; v.childControlWidth = true; v.childForceExpandHeight = false;
            _body = UiFactory.Text("Body", Window.Content, "Kein Brief ausgewählt.", 17, TextAlignmentOptions.TopLeft, new Color(0.92f, 0.9f, 0.85f));
            _body.rectTransform.Stretch(300, 14, 12, 64);
            _take = UiFactory.Button("Take", Window.Content, "Beilage nehmen", Take, 16);
            ((RectTransform)_take.transform).Place(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 12), new Vector2(200, 40));
            Window.Opened += () => _version = -1;
        }

        StoryDirector Dir => GameManager.Instance != null ? GameManager.Instance.Story : null;

        public void Refresh()
        {
            var d = Dir; if (d == null || d.State == null || d.Database == null) return;
            int ver = d.State.Mail.Count * 1000 + MailModel.Unread(d.State) + (_selected != null ? _selected.GetHashCode() % 997 : 0);
            if (ver == _version) return; _version = ver;
            UiFactory.Clear(_list);
            for (int i = d.State.Mail.Count - 1; i >= 0; i--)
            {
                var m = d.State.Mail[i]; var def = d.Database.Letter(m.Id); if (def == null) continue;
                string id = m.Id;
                var b = UiFactory.Button("M_" + id, _list, (m.Read ? "" : "<b>• </b>") + def.Subject + "\n<size=12><color=#AAAAAA>" + Sender(d, def) + " · " + m.Day + "</color></size>", () => Select(id), 15);
                UiFactory.Layout(b, 48);
                var lbl = b.GetComponentInChildren<TextMeshProUGUI>(); if (lbl != null) lbl.alignment = TextAlignmentOptions.Left;
                if (id == _selected) b.GetComponent<Image>().color = new Color(0.4f, 0.32f, 0.2f);
            }
            ShowSelected(d);
        }

        static string Sender(StoryDirector d, LetterDef def) => d.Database.Cast != null && d.Database.Cast.Get(def.From) != null ? d.Database.Cast.Name(def.From) : def.From;

        void Select(string id) { _selected = id; var d = Dir; if (d != null) MailModel.Read(d.State, id); _version = -1; UiFactory.Sound("Paper"); }

        void ShowSelected(StoryDirector d)
        {
            var def = _selected != null ? d.Database.Letter(_selected) : null;
            if (def == null) { _body.text = d.State.Mail.Count == 0 ? "Noch keine Briefe." : "Brief links auswählen."; _take.gameObject.SetActive(false); return; }
            var m = d.State.Mail.Find(x => x.Id == _selected);
            string att = (def.Karma > 0 ? def.Karma + " Karma " : "") + (def.Copper > 0 ? WalletModel.Format(def.Copper) + " " : "") + (!string.IsNullOrEmpty(def.Item) ? def.Item : "");
            _body.text = "<b>" + def.Subject + "</b>\n<color=#AAAAAA>Von " + Sender(d, def) + "</color>\n\n" + def.Body + (att.Length > 0 ? "\n\n<color=#E8C870>Beilage: " + att + (m != null && m.Taken ? " (genommen)" : "") + "</color>" : "");
            _take.gameObject.SetActive(att.Length > 0 && m != null && !m.Taken);
        }

        void Take()
        {
            var d = Dir; if (d == null || _selected == null) return;
            var def = d.Database.Letter(_selected); if (def == null || !MailModel.Take(d.State, _selected)) return;
            var p = PlayerProfile.Instance;
            if (p != null) { p.GrantReward(def.Karma, def.Copper, 0, "Brief"); if (!string.IsNullOrEmpty(def.Item) && p.Items != null) { var it = p.Items.Get(def.Item); if (it != null) p.GiveItem(it); } }
            _version = -1; UiFactory.Sound("Loot");
        }
    }
}
