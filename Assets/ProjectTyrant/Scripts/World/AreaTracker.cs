using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Erkennt den Wechsel des Gebiets und blendet den Gebietsnamen ein (GW2).</summary>
    public class AreaTracker : MonoBehaviour
    {
        public string CurrentArea { get; private set; }
        float _timer;

        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = 0.5f;
            string name = ZoneRoot.RegionNameAt(transform.position);
            if (name == CurrentArea) return;
            CurrentArea = name;
            var r = ZoneRoot.LevelRangeAt(transform.position);
            // Balance B1: Gebietsbanner mit Level-Bereich und Gefahrenstufe; Gefahrenhinweis einmalig je Gebiet
            int danger = Balance.BalanceWorld.DangerAt(transform.position);
            bool city = Balance.BalanceWorld.Map != null && Balance.BalanceWorld.Map.IsCity(new Vector2(transform.position.x, transform.position.z));
            string sub = city ? "Stadt · sicher" : "Stufe " + r.x + "–" + r.y + " · " + Balance.DangerTable.Names[Mathf.Clamp(danger, 0, 5)];
            GameEventBus.RaiseAreaEntered(name, sub);
            var c = GetComponent<Combatant>();
            int lvl = c != null ? c.Level : 1;
            if (!city && r.x >= lvl + 3 && Balance.BalanceWorld.FirstTime("gefahr_" + ZoneRoot.ZoneName + "_" + name))
                GameEventBus.RaiseBanner("Dieses Gebiet ist gefährlich für deine Stufe", name + " · Gegner Stufe " + r.x + "–" + r.y, new Color(1f, 0.45f, 0.35f));
        }
    }
}
