using UnityEngine;

namespace ProjectTyrant
{
    /// <summary>Lauf 11: Sandportal (Glutwüste) – golden flimmernd, nur aufgesessen auf dem Dünenläufer nutzbar; teleportiert zum Partnerportal.</summary>
    public class SandPortal : MonoBehaviour, IMapMarkerProvider
    {
        public SandPortal Partner;
        public string Label = "Sandportal";
        float _cd;

        void OnEnable() => MapMarkerRegistry.Register(this);
        void OnDisable() => MapMarkerRegistry.Unregister(this);

        void Update()
        {
            if (_cd > 0f) { _cd -= Time.deltaTime; return; }
            var mc = MountController.Local; if (mc == null || Partner == null) return;
            float d = Vector3.Distance(mc.transform.position, transform.position);
            if (d > DuneRunnerModel.PortalMasterRadius + 0.5f) return;
            var mm = PlayerProfile.Instance != null ? PlayerProfile.Instance.Mastery : null;
            if (!DuneRunnerModel.CanUsePortal(mc.Motor.Mounted, d, MasteryEffects.DuneRunnerRank(mm)))
            {
                if (mc.Motor.Mounted != MountKind.DuneRunner && d < DuneRunnerModel.PortalRadius) { GameEventBus.Notify("Nur ein Dünenläufer findet durch das Sandportal", new Color(1f, 0.85f, 0.5f)); _cd = 3f; }
                return;
            }
            _cd = 2f; Partner._cd = 2f;
            var pc = mc.GetComponent<PlayerController>();
            if (pc != null) pc.Teleport(Partner.transform.position + Partner.transform.forward * 3f + Vector3.up * 0.3f, Partner.transform.rotation);
            SkillExecutor.VfxPulse(Partner.transform.position, 3f, new Color(1f, 0.8f, 0.4f));
        }

        public void CollectMarkers(System.Collections.Generic.List<MapMarkerInfo> into) { }
        public TooltipContent MarkerTooltip(MapMarkerInfo info) => null;
    }
}
