using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant.Landmarks
{
    /// <summary>Welt-Lauf W1: die zehn Bauwerks-Typen des Wahrzeichen-Baukastens (plus Landform „Massiv“ ohne Bauwerk).</summary>
    public enum LandmarkKind { Cathedral, Pyramid, Citadel, Pagoda, GiantTree, Colossus, Lighthouse, GlacierPalace, SunkenCity, SkyIsland, Massif }

    /// <summary>Die zehn Hürden-Oberflächen (Abschnitt 5).</summary>
    public enum HazardKind { Quicksand, ToxicWater, Lava, IceWater, Abyss, SteepWall, WindWall, SandPortal, ThornHedge, CorruptionField }

    /// <summary>Terrain-Operationen (Abschnitt 6), deterministisch im Höhen-Sampler angewandt.</summary>
    public enum TerrainOpKind { Massif, Peak, Mesa, Canyon, Crater, CliffCoast, Basin, Cave, FloatingIsland, Terraces, Delta }

    public enum RewardKind { None, Vista, Insight, Chest, Lore, HeroChallenge }

    /// <summary>Was ein Ort/eine Stufe zum Erreichen verlangt. <see cref="Traversal"/> trägt die Schlüssel (Reittiere/Beherrschungen).</summary>
    [Serializable]
    public class GateDef
    {
        public Traversal Keys;
        [Tooltip("Es gibt (zusätzlich) einen Fußweg")] public bool FootAlternative;
        [Tooltip("Schlüssel ist im Band der Zone noch nicht verfügbar – Rückkehr-Tor mit Hinweis")] public bool ReturnGate;
        public static GateDef Foot => new GateDef { Keys = Traversal.None, FootAlternative = true };
        public static GateDef Of(Traversal k, bool foot = false) => new GateDef { Keys = k, FootAlternative = foot };
    }

    [Serializable]
    public class TerrainOpDef
    {
        public TerrainOpKind Kind;
        public Vector2 Center;
        [Tooltip("Radius bzw. halbe Länge")] public float Radius;
        [Tooltip("Höhe über dem Grund (Massiv, Gipfel, Kraterrand, Terrassen), absolute Plateauhöhe (Tafelberg) bzw. Tiefe (Schlucht)")] public float Height;
        [Tooltip("Breite, Stufenhöhe, Kraterseetiefe …")] public float Param;
        public float Yaw;
    }

    /// <summary>Stufe eines Aufstiegs oder Wahrzeichens: Ort, Tor, Belohnung, Kontrollpunkte.</summary>
    [Serializable]
    public class StageDef
    {
        public int Index;
        public string Name;
        public Vector3 Position;
        public GateDef Gate = new GateDef();
        public RewardKind Reward;
        public List<Vector3> Checkpoints = new List<Vector3>();
    }

    [Serializable]
    public class LandmarkDef
    {
        public string Id, Name;
        [TextArea] public string Lore;
        public LandmarkKind Kind;
        public bool Main;
        public Vector3 Position;
        public float Height, Footprint, Yaw;
        [Tooltip("Höhe, mit der der Baukasten gebaut wird (Height = tatsächlich höchster Punkt)")] public float BuildHeight;
        public Color Primary = Color.gray, Accent = Color.white;
        public bool Interior;
        public string LightProfile, SoundProfile;
        public List<StageDef> Stages = new List<StageDef>();
    }

    [Serializable]
    public class HazardDef
    {
        public string Id;
        public HazardKind Kind;
        public Vector3 Center;
        public Vector2 Size;
        public float Yaw;
        public Traversal Keys;
        [Tooltip("Kreisförmig (Size.x = Durchmesser), sonst Rechteck")] public bool Circular;
    }

    [Serializable]
    public class ClimbDef
    {
        public string Id, Name;
        public Vector3 Base, Peak, LaunchTower, DescentTarget;
        public List<StageDef> Stages = new List<StageDef>();
        [Tooltip("Fußweg (Serpentine/Grat) als Wegpunkte mit Höhe – der Sampler fräst ihn ins Gelände")] public List<Vector3> FootPath = new List<Vector3>();
        [Tooltip("Springer-Vorsprünge (Abkürzung) und langer Kletterpfad zum Gipfel")] public List<Vector3> SpringerLedges = new List<Vector3>(), ClimbPath = new List<Vector3>();
        [Tooltip("Aufstieg an einem Bauwerk (Stufen sind Anker des Wahrzeichens)")] public string LandmarkId;
    }

    [Serializable]
    public class SecretDef
    {
        public string Id, Name, Hint;
        public Vector3 Position;
        public GateDef Gate = new GateDef();
        public bool IsLoreSpot;
    }

    [Serializable]
    public class LoreDef
    {
        public string Id, Title;
        [TextArea] public string Text;
        public string Figure;
        public Vector3 Position;
    }

    /// <summary>Entdeckungsschleife: Auslöser (sichtbar/hörbar) → Ziel (Belohnung) → nächstes Ziel (Sichtachse).</summary>
    [Serializable]
    public class LoopDef
    {
        public string Id, TriggerKind, Target;
        public Vector3 TriggerPos, TargetPos, NextGoal;
    }

    [Serializable]
    public class ShortcutDef
    {
        public string Id, Name;
        public Traversal Key;
        public Vector3 From, To;
        [Tooltip("Länge des Fußwegs außen herum (m)")] public float FootPath;
        [Tooltip("Länge der Reittier-Route (m)")] public float MountPath;
    }

    /// <summary>Fußweg zu einem Pflicht-Marker, der sonst ein Reittier verlangt (Damm zur Insel, Rampe auf den Tafelberg, Brücke über den Lavaring).</summary>
    [Serializable]
    public class FootLinkDef
    {
        public string Id, Name, Kind;
        public List<Vector3> Points = new List<Vector3>();
    }

    /// <summary>Umwelt-Erzählung ohne NPC-Dialog.</summary>
    [Serializable]
    public class SceneDef
    {
        public string Id, Kind, LoreId;
        public Vector3 Position;
    }

    /// <summary>Welt-Lauf W1: alles, was eine Zone an Wahrzeichen, Vertikalität und Hürden bekommt – Daten in der <see cref="ZoneDefinition"/>.</summary>
    [Serializable]
    public class ZoneWorldPlan
    {
        public int Version;
        public List<TerrainOpDef> Ops = new List<TerrainOpDef>();
        public List<LandmarkDef> Landmarks = new List<LandmarkDef>();
        public List<HazardDef> Hazards = new List<HazardDef>();
        public List<ClimbDef> Climbs = new List<ClimbDef>();
        public List<SecretDef> Secrets = new List<SecretDef>();
        public List<LoreDef> Lore = new List<LoreDef>();
        public List<LoopDef> Loops = new List<LoopDef>();
        public List<ShortcutDef> Shortcuts = new List<ShortcutDef>();
        public List<SceneDef> Scenes = new List<SceneDef>();
        public List<FootLinkDef> FootLinks = new List<FootLinkDef>();
        [Tooltip("Eingeebnete Plätze: x, z, Radius, Höhe (NaN = automatisch)")] public List<Vector4> Pads = new List<Vector4>();
        public float MainVisibility = -1f;
        [Tooltip("Stadt/Startzone: zu klein für Massiv und 90-s-Abkürzung (Aufstieg am Wahrzeichen, Abkürzung nur Hinweis)")] public bool CompactZone;
        public bool IsEmpty => Landmarks.Count == 0 && Ops.Count == 0;
        public LandmarkDef Main => Landmarks.Find(l => l.Main);
    }
}
