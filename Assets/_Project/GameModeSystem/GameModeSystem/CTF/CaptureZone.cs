// ============================================================
//  CaptureZone.cs
//  GameMode/CTF/CaptureZone.cs
//
//  RESPONSABILIDAD ÚNICA: Trigger de la zona de captura.
//  Cuando un portador de bandera enemiga entra aquí, notifica
//  a CaptureLogicSystem para que valide y ejecute la captura.
// ============================================================

namespace GameMode.CTF
{
    using Core.Events;
    using GameMode.Config;
    using UnityEngine;

    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class CaptureZone : MonoBehaviour
    {
        [SerializeField] private CTFConfig _config;
        [SerializeField] private int       _teamID = 0;   // A qué equipo pertenece esta zona
        [SerializeField] private Renderer  _zoneRenderer;

        public int TeamID => _teamID;

        private void Start()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;

            if (_config != null)
                transform.localScale = Vector3.one * _config.CaptureZoneRadius * 2f;

            // Colorear zona
            if (_zoneRenderer != null && _config != null)
            {
                var mat  = new Material(_zoneRenderer.material);
                var c    = _teamID == 0 ? _config.TeamAColor : _config.TeamBColor;
                c.a      = 0.25f;
                mat.color = c;
                _zoneRenderer.material = mat;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var carrier = other.GetComponentInParent<FlagCarrierComponent>();
            if (carrier == null) return;

            // Publicar para que CaptureLogicSystem lo valide
            EventBus<CaptureAttemptEvent>.Raise(new CaptureAttemptEvent
            {
                PlayerID      = carrier.PlayerID,
                PlayerTeamID  = carrier.TeamID,
                ZoneTeamID    = _teamID,
                CarriedFlag   = carrier.CarriedFlag
            });
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null) return;
            Gizmos.color = _teamID == 0 ?
                new Color(1f, 0.2f, 0.2f, 0.3f) :
                new Color(0.2f, 0.2f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, _config != null ? _config.CaptureZoneRadius : 2f);
            Gizmos.color = _teamID == 0 ? Color.red : Color.blue;
            Gizmos.DrawWireSphere(transform.position, _config != null ? _config.CaptureZoneRadius : 2f);
        }
    }

    // ── Evento interno de intento de captura ──────────────────

    public struct CaptureAttemptEvent
    {
        public int            PlayerID;
        public int            PlayerTeamID;
        public int            ZoneTeamID;
        public FlagController CarriedFlag;
    }
}
