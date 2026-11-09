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
            GameEventBus.RaiseAreaEntered(name, "Stufe " + r.x + "–" + r.y);
        }
    }
}
