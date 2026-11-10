using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 6: Bündnis-Händler in Silbermund – handelt nur mit Mitgliedern seines Bündnisses (Wahl in Kapitel 5).</summary>
    public class PactVendorGate : MonoBehaviour
    {
        public Pact Pact;
        VendorNpc _npc;
        void Awake() { _npc = GetComponent<VendorNpc>(); }
        void Update()
        {
            var gm = GameManager.Instance; if (_npc == null || gm == null || gm.Story == null) return;
            bool member = gm.Story.State.PactE == Pact;
            if (_npc.enabled != member) _npc.enabled = member;
        }
    }
}
