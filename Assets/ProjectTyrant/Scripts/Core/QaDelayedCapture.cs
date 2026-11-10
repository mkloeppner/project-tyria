using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>QA (Fix-Lauf F1): Screenshot-Serie nach Spielzeit-Verzögerung, unabhängig vom Takt der Editor-Befehle. Schreibt nur nach Temp/PT_QA und meldet aktive Flächen-/Effekt-Ebenen im Log.</summary>
    public class QaDelayedCapture : MonoBehaviour
    {
        readonly List<KeyValuePair<float, string>> _shots = new List<KeyValuePair<float, string>>();
        float _t;
        public static string LastReport = "";
        public static string Folder = "lauf6";   // Unterordner in Temp/PT_QA (F1: fixlauf_f1)

        public static void Series(string prefix, params float[] delays)
        {
            var go = new GameObject("QaDelayedCapture"); var c = go.AddComponent<QaDelayedCapture>();
            for (int i = 0; i < delays.Length; i++) c._shots.Add(new KeyValuePair<float, string>(delays[i], prefix + (delays.Length > 1 ? "_" + (i + 1) : "")));
            LastReport = "";
        }

        void Update()
        {
            _t += Time.deltaTime;
            while (_shots.Count > 0 && _t >= _shots[0].Key)
            {
                string file = _shots[0].Value; _shots.RemoveAt(0);
                System.IO.Directory.CreateDirectory("Temp/PT_QA/" + Folder);
                ScreenCapture.CaptureScreenshot("Temp/PT_QA/" + Folder + "/" + file + ".png");
                int o = 0, oOn = 0, e = 0, eOn = 0;
                foreach (var l in FindObjectsByType<VfxAreaLayer>())
                {
                    bool on = l.gameObject.activeInHierarchy;
                    if (l.Layer == AreaLayer.AreaOutline) { o++; if (on) oOn++; } else { e++; if (on) eOn++; }
                }
                LastReport += file + ": Fläche " + oOn + "/" + o + ", Effekt " + eOn + "/" + e + " · ";
                Debug.Log("[ProjectTyrant] QA " + file + ": Fläche " + oOn + "/" + o + ", Effekt " + eOn + "/" + e);
            }
            if (_shots.Count == 0) Destroy(gameObject);
        }
    }
}
