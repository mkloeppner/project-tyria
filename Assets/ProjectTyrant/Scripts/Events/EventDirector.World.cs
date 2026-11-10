using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Welt-Aktoren der Ereignis-Engine: Spawns, Banner, Vorräte, Eskorte, Sammeln, Meta-Zustand.</summary>
    public partial class EventDirector
    {
        // ---------------- Hilfen ----------------
        NpcBrain Spawn(World w, EnemyArchetype a, Vector3 pos, Vector3? objective, Combatant preferred)
        {
            if (a == null) return null;
            // Balance B1: Event-Stufe 0–1 – keine Veteranen (Stufe 0), begrenzte Gegnerzahl, nie Spawns nahe Hub/Wegmarke
            int tier = w != null && !Balance.BalanceWorld.Legacy ? Balance.BalanceWorld.TierOf(w.R.Def) : 2;
            if (tier <= 1 && w != null)
            {
                var tv = Balance.EventTierRules.Values((Balance.EventTier)tier);
                if (!tv.Veterans && a.Tier == EnemyTier.Veteran && w.R.Def.Trash != null) a = w.R.Def.Trash;
                if (AliveOf(w) >= tv.MaxTrash && a.Tier == EnemyTier.Trash) return null;
                pos = Balance.BalanceWorld.ClearSpawn(pos);
            }
            var rnd = Random.insideUnitCircle * 1.5f;
            var p = pos + new Vector3(rnd.x, 0, rnd.y);
            p.y = ZoneRoot.GroundHeight(p.x, p.z) + 0.1f;
            var b = NpcBrain.Spawn(a, p, Quaternion.Euler(0, Random.Range(0f, 360f), 0), SpawnParent);
            if (b == null) return null;
            b.Objective = objective;
            b.PreferredTarget = preferred;
            b.UseLeash = objective == null;
            if (tier == 0 && Player != null) { b.LevelOverride = Mathf.Max(1, Player.Level - 1); b.ApplyArchetype(); }   // Einführung: Spieler-Level − 1
            w?.Spawned.Add(b);
            return b;
        }

        static Vector3 RingPoint(Vector3 c, float r)
        {
            var v = Random.insideUnitCircle.normalized * r * Random.Range(0.6f, 1f);
            return new Vector3(c.x + v.x, c.y, c.z + v.y);
        }

        static int AliveOf(Combatant[] arr) { int n = 0; if (arr != null) foreach (var c in arr) if (c != null && !c.IsDead) n++; return n; }
        static int AliveOf(World w) { int n = 0; foreach (var b in w.Spawned) if (b != null && !b.Self.IsDead) n++; return n; }

        static Combatant RandomAlive(Combatant[] arr)
        {
            if (arr == null) return null;
            var alive = new List<Combatant>();
            foreach (var c in arr) if (c != null && !c.IsDead) alive.Add(c);
            return alive.Count == 0 ? null : alive[Random.Range(0, alive.Count)];
        }

        void SpawnWaveAgainst(World w, Combatant[] objs, int count, bool withVeteran)
        {
            var d = w.R.Def;
            Vector3 from = d.WorldHook == "dornwacht_defend" && A != null ? A.OutpostAttackSpawns[Random.Range(0, A.OutpostAttackSpawns.Length)].position : RingPoint(d.Center, d.Radius * 0.9f);
            for (int i = 0; i < count; i++)
            {
                var target = RandomAlive(objs);
                if (target == null) return;
                Spawn(w, i == 0 && withVeteran ? d.Veteran : d.Trash, from, target.transform.position, target);
            }
            GameEventBus.Notify("Eine Angriffswelle nähert sich!", new Color(1f, 0.6f, 0.4f));
        }

        void SpawnCampDefenders(World w)
        {
            var d = w.R.Def;
            for (int i = 0; i < 5; i++) Spawn(w, d.Trash, A.CampSpawns[i % A.CampSpawns.Length].position, null, null);
            Spawn(w, d.Veteran, A.CampCenter.position + new Vector3(3, 0, -2), null, null);
        }

        // ---------------- Dornwacht-Welt (aus der alten Kette übernommen) ----------------
        static void SetBanners(WorldBanner[] banners, Faction f) { if (banners == null) return; foreach (var b in banners) if (b != null) b.SetOwner(f); }

        void ApplyNeutralWorld()
        {
            if (A != null) SetBanners(A.OutpostBanners, Faction.Ally);
            if (Crates != null) foreach (var c in Crates) if (c != null && c.IsDead) { c.Revive(1f); SetCrateVisual(c, true); }
            if (Guards != null) foreach (var g in Guards) if (g != null && g.Self.IsDead) g.Revive();
        }

        void ApplyOccupiedOutpost(World w, int occupiers)
        {
            if (A == null) return;
            SetBanners(A.OutpostBanners, Faction.Enemy);
            if (Guards != null) foreach (var g in Guards) if (g != null && !g.Self.IsDead) g.Self.Die(null);
            if (Crates != null) foreach (var c in Crates) if (c != null) { if (!c.IsDead) c.Die(null); SetCrateVisual(c, false); }
            var trash = w != null ? w.R.Def.Trash : Chain != null ? Chain.Trash : null;
            var vet = w != null ? w.R.Def.Veteran : Chain != null ? Chain.Veteran : null;
            for (int i = 0; i < occupiers; i++)
            {
                var spot = A.OutpostOccupierSpots[i % A.OutpostOccupierSpots.Length];
                Spawn(w, i == 0 ? vet : trash, spot.position, null, null);
            }
        }

        void SetCampFire(bool on) { if (A != null && A.CampFireFx != null) A.CampFireFx.SetActive(on); }

        void ResetDornwachtWorld()
        {
            _chainResetTimer = -1f;
            if (Scheduler != null && Scheduler.HasFlag(FlagDornwachtFallen)) return;
            ApplyNeutralWorld();
            if (A != null) SetBanners(A.CampBanners, Faction.Enemy);
            SetCampFire(true);
            GameEventBus.Notify("Die Aschkrallen sammeln sich erneut …", new Color(0.9f, 0.8f, 0.7f));
        }

        static void SetCrateVisual(Combatant c, bool on)
        {
            if (c == null) return;
            var v = c.transform.Find("Visual");
            if (v != null) v.gameObject.SetActive(on);
            var col = c.GetComponent<Collider>();
            if (col != null) col.enabled = on;
        }

        void OutcomeWorld(World w, bool ok)
        {
            if (w == null || A == null) return;
            switch (w.R.Def.WorldHook)
            {
                case "dornwacht_defend": if (ok) SetBanners(A.CampBanners, Faction.Enemy); break;
                case "camp_assault": if (ok) { SetBanners(A.CampBanners, Faction.Neutral); SetCampFire(false); } break;
                case "outpost_occupied": if (ok) ApplyNeutralWorld(); break;
                case "champion": if (ok) { SetBanners(A.CampBanners, Faction.Ally); SetCampFire(false); } break;
            }
        }

        // ---------------- Meta-Zustand & Welt-Folgen ----------------
        VendorNpc FindVendor(System.Predicate<VendorNpc> p)
        {
            foreach (var v in FindObjectsByType<VendorNpc>(FindObjectsInactive.Include)) if (p(v)) return v;
            return null;
        }

        void OnFlagChanged(string flag, bool set)
        {
            switch (flag)
            {
                case FlagDornwachtFallen:
                {
                    var kerla = FindVendor(v => v.RequiredCompletion == "heart_dornwacht");
                    if (kerla != null) kerla.Available = !set;
                    if (set)
                    {
                        ApplyOccupiedOutpost(null, 3);   // Besatzer bleiben (Spawn-Dichte), Wachen tot, Banner feindlich
                        GameEventBus.RaiseBanner("Die Dornwacht ist gefallen!", "Wegmarke umkämpft · Händlerin geflohen · Aschkrallen besetzen den Posten", new Color(1f, 0.45f, 0.35f));
                    }
                    else
                    {
                        ApplyNeutralWorld();
                        GameEventBus.RaiseBanner("Die Dornwacht ist gehalten", "Wachen kehren zurück", new Color(0.6f, 0.9f, 1f));
                    }
                    break;
                }
                case "karawane_angekommen":
                    if (CaravanVendor != null) CaravanVendor.Available = set;
                    GameEventBus.Notify(set ? "Die Karawanenhändlerin ist an der Dornwacht eingetroffen (5 min)." : "Die Karawane ist weitergezogen.", new Color(1f, 0.85f, 0.5f));
                    break;
                case "vorraete_knapp":
                {
                    var k = FindVendor(v => v.Vendor != null && v.Vendor.Id == "kraemer");
                    if (k != null) k.Model.PriceFactor = set ? 1.5f : 1f;
                    GameEventBus.Notify(set ? "Vorräte knapp: Krämer Ottfried verlangt 50 % mehr (10 min)." : "Sonnfurts Vorräte sind wieder aufgefüllt.", new Color(1f, 0.75f, 0.5f));
                    break;
                }
                case "elmhain_befreit":
                    if (set)
                    {
                        _ruinsLocals.Clear();
                        foreach (var c in Combatant.All)
                        {
                            if (c == null || c.Team != Team.Enemy || c.transform.IsChildOf(SpawnParent)) continue;
                            var d = c.transform.position - (A != null ? A.RuinsCenter.position : Vector3.zero); d.y = 0f;
                            if (d.magnitude < 20f) { var b = c.GetComponent<NpcBrain>(); if (b != null) _ruinsLocals.Add(b); }
                        }
                        foreach (var b in _ruinsLocals) if (b != null) ActorVisibility.SetHidden(b.gameObject, true);
                    }
                    else foreach (var b in _ruinsLocals) if (b != null) ActorVisibility.SetHidden(b.gameObject, false);
                    GameEventBus.Notify(set ? "Elmhain ist befreit – keine Aschkrallen für 10 min." : "Aschkrallen kehren nach Elmhain zurück.", new Color(0.8f, 0.9f, 1f));
                    break;
            }
        }

        // ---------------- Eskorte ----------------
        void StartEscort(World w)
        {
            var d = w.R.Def;
            if (d.Path == null || d.Path.Length < 2) { Scheduler.Complete(w.R, false); return; }
            var go = new GameObject("Karawane");
            go.transform.SetParent(SpawnParent, false);
            go.transform.position = d.Path[0];
            var cart = GameObject.CreatePrimitive(PrimitiveType.Cube); cart.name = "Visual";
            cart.transform.SetParent(go.transform, false); cart.transform.localPosition = new Vector3(0, 0.9f, 0); cart.transform.localScale = new Vector3(1.6f, 1.2f, 2.6f);
            Object.Destroy(cart.GetComponent<Collider>());
            var cover = GameObject.CreatePrimitive(PrimitiveType.Cylinder); cover.transform.SetParent(cart.transform, false);
            cover.transform.localPosition = new Vector3(0, 0.55f, 0); cover.transform.localEulerAngles = new Vector3(90, 0, 0); cover.transform.localScale = new Vector3(0.95f, 0.5f, 0.9f);
            Object.Destroy(cover.GetComponent<Collider>());
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.55f, 0.38f, 0.22f) };
            cart.GetComponent<MeshRenderer>().sharedMaterial = mat;
            cover.GetComponent<MeshRenderer>().sharedMaterial = new Material(mat) { color = new Color(0.9f, 0.85f, 0.7f) };
            var col = go.AddComponent<CapsuleCollider>(); col.center = new Vector3(0, 0.9f, 0); col.height = 1.8f; col.radius = 1f;
            var c = go.AddComponent<Combatant>();
            c.Team = Team.Ally; c.DisplayName = "Karawane"; c.Tier = EnemyTier.None;
            c.EnsureInitialized();
            // Wagen auf Gebietsstufe mit robuster Rüstung (sonst trifft jeder Plünderer gegen Stufe-1-Rüstung)
            c.Level = 10;
            var armor = new FlatAttributeSource(); armor.Values[(int)Attr.Toughness] = AttributeSet.PrimaryBase(10) * 2.2f; c.Stats.AddSource(armor);
            c.SetMaxHealth(12000f, true);
            w.Escort = c; w.Props.Add(go);
            w.PathTotal = 0f; for (int i = 1; i < d.Path.Length; i++) w.PathTotal += Vector3.Distance(d.Path[i - 1], d.Path[i]);
            GameEventBus.Notify("Eskortiere die Karawane zur Dornwacht – sie fährt nur, wenn du in der Nähe bist.", new Color(1f, 0.8f, 0.4f));
        }

        void TickEscort(World w, float dt)
        {
            var d = w.R.Def; var c = w.Escort;
            if (c == null || c.IsDead) { w.R.EscortAlive = false; return; }
            var p = Player;
            bool playerNear = p != null && !p.IsDead && Vector3.Distance(p.transform.position, c.transform.position) < 20f;
            bool blocked = false;
            foreach (var e in Combatant.All) if (e != null && !e.IsDead && e.Team == Team.Enemy && Vector3.Distance(e.transform.position, c.transform.position) < 6f) { blocked = true; break; }
            if (playerNear && !blocked && w.PathIndex < d.Path.Length)
            {
                var target = d.Path[w.PathIndex];
                var pos = c.transform.position;
                var to = target - pos; to.y = 0f;
                float step = 2.6f * dt;
                if (to.magnitude <= step) { pos = new Vector3(target.x, pos.y, target.z); w.PathDone += to.magnitude; w.PathIndex++; }
                else { pos += to.normalized * step; w.PathDone += step; c.transform.rotation = Quaternion.Slerp(c.transform.rotation, Quaternion.LookRotation(to), dt * 3f); }
                pos.y = ZoneRoot.GroundHeight(pos.x, pos.z);
                c.transform.position = pos;
            }
            w.R.EscortProgress = w.PathIndex >= d.Path.Length ? 1f : Mathf.Clamp01(w.PathDone / Mathf.Max(1f, w.PathTotal));
            if (!w.Raid1 && w.R.EscortProgress >= 0.3f) { w.Raid1 = true; Raid(w); }
            if (!w.Raid2 && w.R.EscortProgress >= 0.65f) { w.Raid2 = true; Raid(w); }
        }

        void Raid(World w)
        {
            var c = w.Escort; var d = w.R.Def;
            var ahead = c.transform.position + c.transform.forward * 14f;
            for (int i = 0; i < d.WaveSize; i++) Spawn(w, i == 0 && w.Raid2 ? d.Veteran : d.Trash, ahead, c.transform.position, c);
            GameEventBus.Notify("Überfall auf die Karawane!", new Color(1f, 0.55f, 0.35f));
        }

        // ---------------- Sammeln & Abliefern ----------------
        void StartCollect(World w)
        {
            var d = w.R.Def;
            int n = d.TargetCount + 3;
            for (int i = 0; i < n; i++)
            {
                var pos = RingPoint(d.Center, d.Radius * 0.7f);
                pos.y = ZoneRoot.GroundHeight(pos.x, pos.z);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = "Vorratskiste"; go.transform.SetParent(SpawnParent, false);
                go.transform.position = pos + Vector3.up * 0.35f; go.transform.localScale = Vector3.one * 0.7f; go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                go.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(0.65f, 0.45f, 0.22f) };
                var pick = go.AddComponent<EventPickup>(); pick.Runner = w.R; pick.PromptText = "Aufheben"; pick.Range = 2.5f;
                w.Props.Add(go);
            }
            var dp = new GameObject("Abgabestelle");
            dp.transform.SetParent(SpawnParent, false);
            dp.transform.position = d.DeliverPoint;
            var marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder); marker.transform.SetParent(dp.transform, false);
            marker.transform.localPosition = new Vector3(0, 0.05f, 0); marker.transform.localScale = new Vector3(3f, 0.05f, 3f);
            Object.Destroy(marker.GetComponent<Collider>());
            marker.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = new Color(1f, 0.7f, 0.2f) };
            var del = dp.AddComponent<EventDeliveryPoint>(); del.Runner = w.R; del.PromptText = "Abliefern"; del.Range = 3f;
            w.Props.Add(dp);
        }
    }

    /// <summary>Aufhebbarer Gegenstand eines Sammel-Ereignisses.</summary>
    public class EventPickup : Interactable
    {
        [System.NonSerialized] public EventRunner Runner;
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && Runner != null && Runner.Active;
        public override void Interact(Combatant player)
        {
            if (Runner == null || !Runner.Active) return;
            Runner.Carried++;
            EventDirector.Instance?.AddInteraction(Runner, 150f);
            GameEventBus.Notify("Vorräte aufgehoben (" + Runner.Carried + " getragen) – zur Abgabestelle bringen", new Color(1f, 0.85f, 0.5f));
            Destroy(gameObject);
        }
    }

    /// <summary>Abgabestelle eines Sammel-Ereignisses.</summary>
    public class EventDeliveryPoint : Interactable
    {
        [System.NonSerialized] public EventRunner Runner;
        public override bool CanInteract(Combatant player) => isActiveAndEnabled && Runner != null && Runner.Active && Runner.Carried > 0;
        public override void Interact(Combatant player)
        {
            if (Runner == null || Runner.Carried <= 0) return;
            int n = Runner.Carried;
            Runner.Delivered += n; Runner.Carried = 0;
            EventDirector.Instance?.AddInteraction(Runner, 150f * n);
            GameEventBus.Notify("Abgeliefert: " + n + " · " + Runner.Delivered + "/" + Runner.Def.TargetCount, new Color(1f, 0.85f, 0.5f));
        }
    }
}
