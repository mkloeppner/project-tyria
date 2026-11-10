using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectTyrant
{
    /// <summary>Ladebildschirm (Zonenname, Level-Bereich, Beschreibung, Fortschritt, Tipp) und Bindung der Zonen-Karte.</summary>
    public partial class UIController
    {
        RectTransform _loading;
        TextMeshProUGUI _loadTitle, _loadSub, _loadDesc, _loadTip;
        Image _loadFill, _loadBg;
        RawImage _loadArt;

        void BuildLoading()
        {
            _loadBg = UiFactory.Image("Loading", OverlayLayer, new Color(0.05f, 0.05f, 0.07f, 1f), null, true);
            _loading = _loadBg.rectTransform.Stretch();
            // M27: Zonen-Illustration bildschirmfüllend, darüber ein Schleier für lesbaren Text
            var artGo = new GameObject("Art", typeof(RectTransform), typeof(RawImage)); artGo.transform.SetParent(_loading, false);
            _loadArt = artGo.GetComponent<RawImage>(); _loadArt.raycastTarget = false; ((RectTransform)artGo.transform).Stretch();
            var veil = UiFactory.Image("Veil", _loading, new Color(0f, 0f, 0f, 0.45f)); veil.raycastTarget = false; veil.rectTransform.Stretch();
            _loadTitle = UiFactory.Text("Title", _loading, "", 64, TextAlignmentOptions.Center, UiFactory.AccentColor);
            _loadTitle.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(0, 90));
            _loadTitle.fontStyle = FontStyles.Bold;
            _loadSub = UiFactory.Text("Sub", _loading, "", 28, TextAlignmentOptions.Center, new Color(0.85f, 0.85f, 0.9f));
            _loadSub.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 85), new Vector2(0, 40));
            _loadDesc = UiFactory.Text("Desc", _loading, "", 22, TextAlignmentOptions.Top, new Color(0.8f, 0.8f, 0.82f));
            _loadDesc.rectTransform.Place(new Vector2(0.2f, 0.5f), new Vector2(0.8f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0, 40), new Vector2(0, 120));
            var barBg = UiFactory.Image("Bar", _loading, new Color(0, 0, 0, 0.7f), UiFactory.Sprite("ui_round"));
            barBg.rectTransform.Place(new Vector2(0.25f, 0), new Vector2(0.75f, 0), new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(0, 18));
            _loadFill = UiFactory.Image("Fill", barBg.transform, UiFactory.AccentColor, UiFactory.Sprite("ui_round"));
            _loadFill.type = Image.Type.Filled; _loadFill.fillMethod = Image.FillMethod.Horizontal; _loadFill.rectTransform.Stretch(2, 2, 2, 2);
            _loadTip = UiFactory.Text("Tip", _loading, "", 20, TextAlignmentOptions.Center, new Color(0.7f, 0.85f, 1f));
            _loadTip.rectTransform.Place(new Vector2(0.15f, 0), new Vector2(0.85f, 0), new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(0, 50));
            _loading.gameObject.SetActive(false);
        }

        public bool LoadingVisible => _loading != null && _loading.gameObject.activeSelf;

        /// <summary>Zeigt/aktualisiert den Ladebildschirm (tip null = unverändert).</summary>
        public void ShowLoading(ZoneDefinition z, string tip, float progress)
        {
            if (_loading == null) BuildLoading();
            if (!_loading.gameObject.activeSelf)
            {
                _loading.gameObject.SetActive(true); _loading.SetAsLastSibling();
                Windows.CloseAll(); Tooltip.Hide();
            }
            if (z != null)
            {
                _loadTitle.text = z.DisplayName;
                _loadSub.text = "Stufe " + z.LevelText;
                _loadDesc.text = z.Description;
                if (z.Biome != null) _loadBg.color = Color.Lerp(new Color(0.04f, 0.04f, 0.06f), z.Biome.FogColor, 0.18f);
                _loadArt.texture = z.LoadingImage; _loadArt.enabled = z.LoadingImage != null;
                if (z.LoadingImage != null) FitCover(_loadArt, z.LoadingImage);
            }
            if (tip != null) _loadTip.text = string.IsNullOrEmpty(tip) ? "" : "Tipp: " + tip;
            _loadFill.fillAmount = Mathf.Clamp01(progress);
        }

        /// <summary>Bild füllt den Bildschirm ohne Verzerrung (Ränder werden beschnitten).</summary>
        static void FitCover(RawImage img, Texture tex)
        {
            var size = ((RectTransform)img.transform.parent).rect.size; if (size.x <= 0f || size.y <= 0f) size = new Vector2(Screen.width, Screen.height);
            float screenA = size.x / size.y, texA = (float)tex.width / tex.height;
            img.uvRect = screenA > texA ? new Rect(0f, (1f - texA / screenA) * 0.5f, 1f, texA / screenA) : new Rect((1f - screenA / texA) * 0.5f, 0f, screenA / texA, 1f);
        }

        public void HideLoading() { if (_loading != null) _loading.gameObject.SetActive(false); }

        /// <summary>Minimap und Weltkarte auf die Karten-Textur der aktuellen Zone umstellen.</summary>
        public void BindZoneMap(Texture2D tex)
        {
            if (tex != null) MapTexture = tex;
            Minimap?.SetTexture(MapTexture);
            WorldMap?.SetTexture(MapTexture);
            if (WorldMap != null) WorldMap.Window.Title.text = "Weltkarte – " + (GameManager.Instance != null && GameManager.Instance.CurrentZone != null ? GameManager.Instance.CurrentZone.DisplayName : Procedural.ZoneLayout.WildernessName);
        }
    }
}
