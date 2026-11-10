using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectTyrant
{
    /// <summary>
    /// Offene Welt (Lauf 4): Core-Szene bleibt, genau eine Zonen-Szene ist additiv geladen. Zonenwechsel mit Ladebildschirm,
    /// eingefrorenem und unverwundbarem Spieler, Wechsel von NavMesh (über die Zonen-Szene), Ereignissen, Karte und Herabstufung.
    /// </summary>
    public partial class GameManager
    {
        [Header("Welt")]
        public WorldDefinition World;
        public bool Transitioning { get; private set; }
        public ZoneDefinition CurrentZone { get; private set; }
        public float LastLoadSeconds { get; private set; }
        string _loadedScene;

        class FreezeSource : IInvulnerabilitySource { public bool On; public bool IsInvulnerable => On; }
        readonly FreezeSource _freeze = new FreezeSource();

        void Start()
        {
            if (World == null) return;
            StartCoroutine(Boot());
        }

        IEnumerator Boot()
        {
            yield return null;   // PlayerProfile hat geladen (Ausführungsreihenfolge −140)
            var prof = PlayerProfile.Instance;
            string zone = prof != null ? prof.State.CurrentZone : World.StartZone;
            if (World.Zone(zone) == null) zone = World.StartZone;
            // Editor-Komfort: Zone bereits geladen (Szene manuell geöffnet) → übernehmen
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var s = SceneManager.GetSceneAt(i);
                var def = World.Zones.Find(z => z != null && z.SceneName == s.name);
                if (def != null && s.isLoaded) { _loadedScene = s.name; zone = def.Id; break; }
            }
            yield return LoadZone(zone, null, true);
        }

        /// <summary>Zonenwechsel über ein Portal (Zielportal-Id) oder Debug/Reise (Portal null → Position oder Spawn).</summary>
        public void ChangeZone(string zoneId, string targetPortal, Vector3? position = null)
        {
            Balance.BalanceWorld.PlayerTravelledAt = Time.time;   // Balance B1: Portal/Zonenwechsel beendet Verfolgungen
            if (Transitioning || World == null || World.Zone(zoneId) == null) return;
            if (!ZoneGate.Allowed(zoneId, Story != null ? Story.State : null, out var why)) { GameEventBus.Notify(why, new Color(1f, 0.6f, 0.5f)); return; }   // Lauf 6: Tyrannenfeste erst mit Kapitel 10
            StartCoroutine(LoadZone(zoneId, targetPortal, false, position));
        }

        IEnumerator LoadZone(string zoneId, string targetPortal, bool useSavedPosition, Vector3? position = null)
        {
            var def = World.Zone(zoneId);
            if (def == null) yield break;
            Transitioning = true;
            float t0 = Time.realtimeSinceStartup;
            var ui = UIController.Instance;
            var prof = PlayerProfile.Instance;
            var pc = Player != null ? Player.GetComponent<PlayerController>() : null;
            var cc = Player != null ? Player.GetComponent<CharacterController>() : null;
            if (Player != null && !Player.InvulnerabilitySources.Contains(_freeze)) Player.InvulnerabilitySources.Add(_freeze);
            _freeze.On = true;
            if (pc != null) pc.InputBlocked = true;
            if (cc != null) cc.enabled = false;
            if (prof != null && CurrentZone != null && CurrentZone.Id != zoneId) prof.CapturePosition();
            if (ui != null) ui.ShowLoading(def, PickTip(def), 0f);

            if (EventDirector.Instance != null) EventDirector.Instance.UnbindZone();
            if (!string.IsNullOrEmpty(_loadedScene) && _loadedScene != def.SceneName)
            {
                var un = SceneManager.UnloadSceneAsync(_loadedScene);
                while (un != null && !un.isDone) { if (ui != null) ui.ShowLoading(def, null, un.progress * 0.3f); yield return null; }
                _loadedScene = null;
            }
            if (_loadedScene != def.SceneName)
            {
                var op = SceneManager.LoadSceneAsync(def.SceneName, LoadSceneMode.Additive);
                while (op != null && !op.isDone) { if (ui != null) ui.ShowLoading(def, null, 0.3f + op.progress * 0.65f); yield return null; }
                _loadedScene = def.SceneName;
            }
            var scene = SceneManager.GetSceneByName(def.SceneName);
            if (scene.IsValid()) SceneManager.SetActiveScene(scene);
            yield return null;

            var root = ZoneRoot.Current;
            CurrentZone = def;
            MapProjection.SetZoneSize(def.Size);
            Waypoint = root != null && root.Waypoint != null ? root.Waypoint : null;
            if (prof != null)
            {
                prof.State.RegisterZoneSize(def.Id, def.Size);
                bool sameZone = prof.State.CurrentZone == def.Id;
                prof.State.CurrentZone = def.Id;
                if (!sameZone) prof.State.HasPosition = false;
                prof.SetZoneLevelCap(def.LevelMax);
            }
            if (ui != null) ui.BindZoneMap(root != null ? root.MapTexture : null);
            if (EventDirector.Instance != null && root != null) EventDirector.Instance.BindZone(root);

            // Spieler platzieren: Zielportal → gespeicherte Position → Spawn
            Vector3 target = def.DefaultSpawn; float yaw = 0f; string entrance = WaypointSafety.SpawnEntrance;
            var portal = !string.IsNullOrEmpty(targetPortal) ? def.Portal(targetPortal) : null;
            if (portal != null) { target = WaypointSafety.ArrivalPoint(portal); yaw = portal.Yaw; entrance = WaypointSafety.PortalEntrance(portal.Id); }
            else if (position.HasValue) { target = position.Value; entrance = null; }
            else if (useSavedPosition && prof != null && prof.State.HasPosition) { target = prof.State.Position; yaw = prof.State.RotationY; entrance = null; }
            target = GroundAt(target);
            if (Player != null)
            {
                if (pc != null) pc.Teleport(target, Quaternion.Euler(0, yaw, 0)); else Player.transform.position = target;
            }
            if (cc != null) cc.enabled = true;
            if (pc != null) pc.InputBlocked = false;
            if (ui != null) ui.ShowLoading(def, null, 1f);
            yield return new WaitForSecondsRealtime(0.15f);
            _freeze.On = false;
            if (ui != null) ui.HideLoading();
            LastLoadSeconds = Time.realtimeSinceStartup - t0;
            GameEventBus.RaiseAreaEntered(def.DisplayName, "Stufe " + def.LevelText);
            // Fix-Lauf F1: Ankunfts-Wegmarke des Eingangs freischalten (Spawn/Portal); bei gespeicherter Position die nächste Ankunft (Migration)
            if (prof != null) prof.UnlockArrivalWaypoint(def, entrance ?? NearestEntrance(def, Player != null ? Player.transform.position : target));
            if (prof != null) prof.RequestSave("Zonenwechsel");
            Transitioning = false;
        }

        static string NearestEntrance(ZoneDefinition def, Vector3 pos)
        {
            string best = WaypointSafety.SpawnEntrance; float bd = float.MaxValue;
            foreach (var e in WaypointSafety.Entrances(def)) { float d = WaypointSafety.FlatDistance(e.Value, pos); if (d < bd) { bd = d; best = e.Key; } }
            return best;
        }

        string PickTip(ZoneDefinition def)
        {
            var tips = def.Tips != null && def.Tips.Count > 0 ? def.Tips : World.Tips;
            return tips == null || tips.Count == 0 ? "" : tips[Random.Range(0, tips.Count)];
        }

        static Vector3 GroundAt(Vector3 p)
        {
            if (Physics.Raycast(new Vector3(p.x, p.y + 60f, p.z), Vector3.down, out var hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.2f;
            return p + Vector3.up * 0.5f;
        }
    }
}
