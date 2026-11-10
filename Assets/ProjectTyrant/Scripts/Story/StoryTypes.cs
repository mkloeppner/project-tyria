using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Emotionen der Gesprächsszenen (je eine Pose aus dem ClipBuilder).</summary>
    public enum Emotion { Neutral, Entschlossen, Besorgt, Amuesiert, Zornig, Trauernd }

    /// <summary>Darstellungsform einer Szene (GW2: Gesprächsszene, In-World-Cinematic, Sprechblase).</summary>
    public enum SceneForm { Conversation, Cinematic, Bubble }

    /// <summary>Typ eines Story-Schritts.</summary>
    public enum StepType { Instance, OpenWorld, Talk, Travel }

    /// <summary>Ziel-Arten; jede bildet eine Spielmechanik ab, die das Kapitel lehrt.</summary>
    public enum ObjectiveKind
    {
        Talk, Reach, Kill, KillTag, Dodge, Revive, Rally, Heal, WeaponSwap, Interact, Combo, Heart, Waypoint, Vista,
        Equip, Trait, Mount, Glide, Defiance, Event, Meta, Mastery, Scout, Bundle, Escort, Interrupt, Cleanse, Elite, Survive, Boss
    }

    /// <summary>Biografie (Charaktererstellung, GW2-Modell).</summary>
    public enum Origin { None, Hof, Posten, Ruinen }
    public enum Trait { None, Charme, Wuerde, Wildheit }
    public enum Lodestar { None, Verlorener, Raetsel, Schuld }
    public enum Pact { None, Wacht, Archiv, Schleier }

    /// <summary>Eine Zeile der Text-Tabelle (Id nach Schema K04_S02_L07).</summary>
    [Serializable]
    public class StoryLine
    {
        public string Id;
        public string Speaker;
        public Emotion Emotion;
        [TextArea] public string Text;
        public string Camera;            // Kamera-Hinweis (links, rechts, total, nah, Fahrt …)
        public AudioClip Voice;           // Sprachdatei (Pipeline), null = Stimm-Laute zur Laufzeit
        public AudioClip VoiceB;          // Held: zweite Stimm-Variante (Charaktererstellung)
        public float Duration;            // Sekunden (aus der Sprachdatei, sonst aus der Textlänge)
        public float[] Envelope = new float[0];   // Lippen-Hüllkurve: RMS je 50 ms (0..1)
        public string VoiceSource;        // "SAPI", "Laute", "generate_audio"
        public string Hash;               // Text-Hash der erzeugten Sprachdatei (neu nur bei Textänderung)
        public int Words => string.IsNullOrEmpty(Text) ? 0 : Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>Figuren-Stimmprofil und Aussehen.</summary>
    [Serializable]
    public class SpeakerProfile
    {
        public string Id;
        public string DisplayName;
        public Color Color = Color.white;
        [Tooltip("Tonhöhen-Faktor der Nachbearbeitung (1 = unverändert)")] public float Pitch = 1f;
        public float Rate = 1f;           // Sprechtempo (SAPI −10..10 wird daraus abgeleitet)
        public float Reverb = 0.1f;
        public float Formant = 0f;        // −1 dunkler … +1 heller
        public bool Female;
        public bool Doubled;              // zwei Stimmen übereinander (Tyrann)
        public string Role;               // Kurzbeschreibung (Figurengalerie)
        [TextArea] public string Bio;     // zwei Sätze (Figurengalerie)
        public int UnlockChapter;         // ab diesem Kapitel in der Galerie
        public Color Cloth = Color.gray, Trim = Color.white, Skin = new Color(0.9f, 0.72f, 0.58f), Hair = new Color(0.3f, 0.2f, 0.12f);
        public string Look = "villager";  // Rig-Variante (villager, soldier, scholar, agent, herald, tyrant, player)
        /// <summary>Tonhöhen-Bereich (Hz) für die Sprach-Analyse.</summary>
        public Vector2 PitchRange => Female ? new Vector2(150f, 320f) * Pitch : new Vector2(80f, 190f) * Pitch;
    }

    [Serializable]
    public class StoryObjective
    {
        public ObjectiveKind Kind;
        public string Target;             // Id/Tag (z. B. Gegner-Archetyp, Objekt-Id, Herz-Id)
        public int Count = 1;
        public string Text;               // „Weiche drei Sprüngen des Brüllers aus“
        public string Hint;               // Lern-Hinweis-Id (erscheint beim Start des Ziels)
        public bool Optional;
        public string Label;              // Anzeigename (Boss „Grolmok“, Objekt „Gefangenen befreien“)
        public Vector3 Offset;            // Ort relativ zum Schritt (Instanz-Spawns, Reach)
        public float Scale = 1f;          // Gegner-Stärke (HP-Faktor) in Instanzen
        public string Scene;              // Szene beim Start dieses Ziels (z. B. Mira geht zu Boden)
        public string Partner;            // Übungs-Gegner (Archetyp) für Lern-Ziele ohne eigenes Ziel-Objekt (Ausweichen, Kombo, Finisher)
    }

    [Serializable]
    public class StoryReward
    {
        public int Experience, Karma, Copper, HeroPoints, MasteryPoints;
        public string Item;
        public bool IsEmpty => Experience == 0 && Karma == 0 && Copper == 0 && HeroPoints == 0 && MasteryPoints == 0 && string.IsNullOrEmpty(Item);
    }

    /// <summary>Bedingung für Varianten (Schritt, Zeile, Szene).</summary>
    [Serializable]
    public class StoryCondition
    {
        public Origin Origin;
        public Pact Pact;
        public Trait Trait;
        public Lodestar Lodestar;
        public string Flag;               // gesetzt sein muss (leer = egal)
        public string NotFlag;            // darf nicht gesetzt sein
        public bool IsEmpty => Origin == Origin.None && Pact == Pact.None && Trait == Trait.None && Lodestar == Lodestar.None && string.IsNullOrEmpty(Flag) && string.IsNullOrEmpty(NotFlag);
    }

    [Serializable]
    public class StoryStep
    {
        public string Id;                 // K04_S02
        public string Title;
        [TextArea] public string Description;
        public StepType Type;
        public string Zone;
        public Vector3 Position;          // grüner Marker / Story-Portal
        public string Npc;                // Sprecher-Id am Marker (Talk)
        public StoryCondition Condition = new StoryCondition();   // Variante: nur wenn erfüllt
        public List<StoryObjective> Objectives = new List<StoryObjective>();
        public string SceneStart, SceneEnd;   // DialogueScript-Ids
        public List<string> Checkpoints = new List<string>();     // Instanz-Kontrollpunkte (Ziel-Index als Text)
        public StoryReward Reward = new StoryReward();
        public List<string> SetFlags = new List<string>();
        public string Letter;             // Brief nach Abschluss
        public float Radius = 60f;        // Instanz-Grenze
        public string Companion;          // Gefährte in der Instanz (Sprecher-Id; "pact" = Bündnis-Figur)
        public List<string> Props = new List<string>();   // Instanz-Requisiten „feuer@x,z“ (feuer, rauch, barrikade, zelt, kaefig, kisten)
    }

    [Serializable]
    public class LetterDef
    {
        public string Id;
        public string From;               // Sprecher-Id oder Herz-NPC-Name
        public string Subject;
        [TextArea] public string Body;
        public int Karma, Copper;
        public string Item;
    }

    [Serializable]
    public class HintDef
    {
        public string Id;
        public string Title;
        [TextArea] public string Text;
        public string Key;                // Tastenbild
        public string Clip;               // optionale Clip-Vorschau
    }

    /// <summary>Ein Eintrag einer Szene.</summary>
    [Serializable]
    public class DialogueEntry
    {
        public string Line;               // Zeilen-Id
        public string CameraCue;
        public string AnimCue;
        public string VfxCue;
        public string SfxCue;
        public float Wait;                // zusätzliche Wartezeit
        public StoryCondition Condition = new StoryCondition();   // Varianten-Zeile
        public int ChoiceIndex = -1;      // >= 0: nach dieser Zeile Wahl Nr.
    }

    [Serializable]
    public class DialogueOption
    {
        public string Line;               // Antwort des Helden (Zeilen-Id)
        public Trait Tendency;            // Persönlichkeit (Charme/Würde/Wildheit)
        public string Flag;               // gesetztes Flag (leer = nur Flavour)
        public string Reply;              // Gegenrede (Zeilen-Id)
        public Pact SetPact;              // Bündnis-Wahl
    }

    [Serializable]
    public class DialogueChoice
    {
        public string Prompt;
        public List<DialogueOption> Options = new List<DialogueOption>();
    }

    [Serializable]
    public class CutsceneActor
    {
        public string Speaker;
        public string Marker;             // Marker-Name in der Szene/Instanz
        public string Clip;               // Start-Clip
    }

    [Serializable]
    public class CameraKey
    {
        public float Time;
        public string Marker;             // Kamera-Marker
        public Vector3 Offset;
        public string LookAt;
    }

    /// <summary>Bündel-Gegenstand (GW2): Eimer, Heugabel, Fackel, Sandsack, Harpune, Laterne, Verbandskasten.</summary>
    [Serializable]
    public class BundleDef
    {
        public string Id;
        public string Name;
        public SkillDefinition[] Skills = new SkillDefinition[0];
        public SkillDefinition Drop;
        public Color Color = Color.white;
        public float LightRadius;         // Laterne: Lichtradius (Kristalltiefe)
    }
}
