using System.Collections.Generic;
using UnityEngine;
using ProjectTyrant.Anim;

namespace ProjectTyrant
{
    /// <summary>
    /// Lauf 6: Gesprächsszene im GW2-Kern-Stil – zwei Figuren in voller Größe links und rechts vor einer gemalten Kulisse
    /// (Builder-Bild der Zone, weich gezeichnet), Sprecher hell, Nicht-Sprecher abgedunkelt. Bühne weit über der Welt, die weiterläuft.
    /// </summary>
    public class ConversationStage : MonoBehaviour
    {
        public static readonly Vector3 StageOrigin = new Vector3(0f, 2500f, 0f);
        public readonly Dictionary<string, StoryActor> Actors = new Dictionary<string, StoryActor>();
        public Transform CameraPose;
        readonly Dictionary<string, Light> _lights = new Dictionary<string, Light>();
        static Material _backMat, _floorMat;

        public static ConversationStage Open(StoryDatabase db, DialogueScript sc, Texture2D backdrop)
        {
            var root = new GameObject("PT_ConversationStage"); root.transform.position = StageOrigin;
            var st = root.AddComponent<ConversationStage>();
            // Kulisse
            var back = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(back.GetComponent<Collider>()); back.name = "Backdrop";
            back.transform.SetParent(root.transform, false); back.transform.localPosition = new Vector3(0, 2.2f, 6.5f); back.transform.localScale = new Vector3(17.8f, 10f, 1f);
            if (_backMat == null) _backMat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture"));
            var m = new Material(_backMat); if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", backdrop); else m.mainTexture = backdrop;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", new Color(0.85f, 0.85f, 0.85f));
            back.GetComponent<Renderer>().sharedMaterial = m;
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane); Destroy(floor.GetComponent<Collider>()); floor.name = "Floor";
            floor.transform.SetParent(root.transform, false); floor.transform.localPosition = new Vector3(0f, 0f, 0.5f); floor.transform.localScale = new Vector3(2.4f, 1f, 1.2f);
            if (_floorMat == null) _floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
            var fm = new Material(_floorMat); var fc = GroundTone(backdrop); var floorCol = fc * 0.62f; floorCol.a = 1f;   // Boden im Farbton des unteren Kulissenrands → keine harte Kante
            if (fm.HasProperty("_BaseColor")) fm.SetColor("_BaseColor", floorCol); else fm.color = floorCol;
            if (fm.HasProperty("_Smoothness")) fm.SetFloat("_Smoothness", 0f);
            floor.GetComponent<Renderer>().sharedMaterial = fm;
            // Übergang Boden → Kulisse: Verlaufsband in Bodenfarbe vor dem unteren Kulissenrand (keine harte Horizontkante)
            var fade = GameObject.CreatePrimitive(PrimitiveType.Quad); Destroy(fade.GetComponent<Collider>()); fade.name = "GroundFade";
            fade.transform.SetParent(root.transform, false); fade.transform.localPosition = new Vector3(0f, 0.9f, 6.3f); fade.transform.localScale = new Vector3(17.8f, 1.8f, 1f);
            fade.GetComponent<Renderer>().sharedMaterial = FadeMaterial(fc * 0.62f);
            // Kamera
            var cam = new GameObject("CameraPose"); cam.transform.SetParent(root.transform, false);
            cam.transform.localPosition = new Vector3(0f, 1.5f, -2.5f); cam.transform.localRotation = Quaternion.Euler(6f, 0f, 0f);   // Figuren ab Knie aufwärts, wie die Gesprächsszenen im Vorbild
            st.CameraPose = cam.transform;
            st.Place(db, sc.Left, new Vector3(-1.05f, 0f, 0.9f), 50f);
            st.Place(db, sc.Right, new Vector3(1.05f, 0f, 0.9f), -50f);
            return st;
        }

        void Place(StoryDatabase db, string speaker, Vector3 local, float yaw)
        {
            if (string.IsNullOrEmpty(speaker)) return;
            if (speaker == "pact" && GameManager.Instance != null && GameManager.Instance.Story != null) speaker = GameManager.Instance.Story.ResolveCompanion("pact");   // Lauf 6: Bündnis-Figur
            GameObject go = null;
            var player = PlayerProfile.Instance != null ? PlayerProfile.Instance.Player : null;
            if (speaker == "held" && player != null)
            {
                var vis = player.GetComponentInChildren<CharacterAnimator>();
                if (vis != null) { go = new GameObject("Held"); var clone = Instantiate(vis.gameObject, go.transform); clone.transform.localPosition = Vector3.zero; clone.transform.localRotation = Quaternion.identity; foreach (var c in clone.GetComponentsInChildren<Collider>()) Destroy(c); }
            }
            if (go == null) { var prefab = db.Figure(speaker); if (prefab == null) return; go = Instantiate(prefab); foreach (var c in go.GetComponentsInChildren<Collider>()) c.enabled = false; }
            go.transform.SetParent(transform, false); go.transform.localPosition = local; go.transform.localRotation = Quaternion.Euler(0f, 180f - yaw, 0f);   // zur Kamera, leicht zueinander gedreht
            var a = go.GetComponent<StoryActor>() ?? go.AddComponent<StoryActor>(); a.Speaker = speaker;
            Actors[speaker] = a;
            var lgo = new GameObject("Light_" + speaker); lgo.transform.SetParent(transform, false); lgo.transform.localPosition = local + new Vector3(0, 2.4f, -1.6f);
            var l = lgo.AddComponent<Light>(); l.type = LightType.Point; l.range = 6f; l.intensity = 1.2f; l.color = new Color(1f, 0.95f, 0.88f); l.shadows = LightShadows.None;
            _lights[speaker] = l;
        }

        /// <summary>Mittlere Farbe des unteren Kulissen-Fünftels (abgedunkelt) als Bodenfarbe; Fallback Erdton.</summary>
        static Color GroundTone(Texture2D t)
        {
            var fallback = new Color(0.32f, 0.36f, 0.24f);
            if (t == null || !t.isReadable) return fallback;
            try
            {
                Color sum = Color.black; int n = 0;
                for (int y = 0; y < t.height / 5; y += 4) for (int x = 0; x < t.width; x += 8) { sum += t.GetPixel(x, y); n++; }
                if (n == 0) return fallback; var c = sum / n; c.a = 1f; return c * 0.8f;
            }
            catch { return fallback; }
        }

        /// <summary>Unbeleuchtetes, transparentes Material mit senkrechtem Alpha-Verlauf (unten deckend, oben frei).</summary>
        static Material FadeMaterial(Color c)
        {
            c.a = 1f;
            var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 64; y++) { float a = 1f - Mathf.SmoothStep(0f, 1f, y / 63f); tex.SetPixel(0, y, new Color(c.r, c.g, c.b, a)); }
            tex.Apply();
            var m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Transparent"));
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex); else m.mainTexture = tex;
            if (m.HasProperty("_Surface")) { m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f); m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha); m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); m.SetFloat("_ZWrite", 0f); m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); }
            m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return m;
        }

        public void Highlight(string speaker)
        {
            foreach (var kv in _lights) kv.Value.intensity = kv.Key == speaker ? 1.3f : 0.3f;   // Sprecher hell, Nicht-Sprecher abgedunkelt
            var mpb = new MaterialPropertyBlock();
            foreach (var kv in Actors)
                foreach (var r in kv.Value.GetComponentsInChildren<Renderer>())
                {
                    r.GetPropertyBlock(mpb); mpb.SetColor("_BaseColor", kv.Key == speaker ? Color.white : new Color(0.5f, 0.5f, 0.56f)); r.SetPropertyBlock(mpb);
                }
        }

        public void Close() { Destroy(gameObject); }
    }
}
