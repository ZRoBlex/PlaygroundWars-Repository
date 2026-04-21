// ============================================================
//  FlagBase.cs
//  GameMode/CTF/FlagBase.cs
//
//  RESPONSABILIDAD ÚNICA: Representar la base de una bandera.
//  Contiene la posición de spawn y la FlagController asociada.
// ============================================================

using GameMode.Config;
using Core.Debug;
using UnityEngine;

namespace GameMode.CTF
{
    [DisallowMultipleComponent]
    public class FlagBase : MonoBehaviour
    {
        [Header("Configuración")]
        [SerializeField] private CTFConfig      _config;
        [SerializeField] private int            _teamID = 0;
        [SerializeField] private FlagController _flag;
        [SerializeField] private Renderer       _baseRenderer;

        public int            TeamID => _teamID;
        public FlagController Flag   => _flag;

        private void Start()
        {
            if (_flag == null)
                _flag = GetComponentInChildren<FlagController>();

            // Colorear la base con el color del equipo
            if (_baseRenderer != null && _config != null)
            {
                var col  = _teamID == 0 ? _config.TeamAColor : _config.TeamBColor;
                var mat  = new Material(_baseRenderer.material);
                col.a    = 0.5f;
                mat.color = col;
                _baseRenderer.material = mat;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (_config == null) return;
            Gizmos.color = _teamID == 0 ? new Color(1, 0.2f, 0.2f, 0.3f) : new Color(0.2f, 0.2f, 1f, 0.3f);
            Gizmos.DrawSphere(transform.position, 0.8f);
        }
    }
}

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

// ============================================================
//  CaptureLogicSystem.cs
//  GameMode/CTF/CaptureLogicSystem.cs
//
//  RESPONSABILIDAD ÚNICA: Validar y ejecutar capturas de bandera.
//
//  VALIDACIONES:
//  1. El jugador debe ser del equipo correcto (su propia zona)
//  2. La bandera que porta debe ser del equipo enemigo
//  3. La bandera debe estar en estado Carried
//  4. Solo procesa si tiene autoridad
// ============================================================

namespace GameMode.CTF
{
    using Core.Debug;
    using Core.Events;
    using GameMode.Config;
    using GameMode.Events;
    using GameMode.Score;
    using UnityEngine;

    [DisallowMultipleComponent]
    public class CaptureLogicSystem : MonoBehaviour
    {
        [SerializeField] private CTFConfig  _config;
        [SerializeField] private bool       _isAuthority = true;

        private ScoreSystem _score;

        // ── Inicialización ────────────────────────────────────

        public void Initialize(ScoreSystem score)
        {
            _score = score;
        }

        private void OnEnable()
        {
            EventBus<CaptureAttemptEvent>.Subscribe(OnCaptureAttempt);
        }

        private void OnDisable()
        {
            EventBus<CaptureAttemptEvent>.Unsubscribe(OnCaptureAttempt);
        }

        // ── Validación ────────────────────────────────────────

        private void OnCaptureAttempt(CaptureAttemptEvent e)
        {
            if (!_isAuthority) return;

            // VALIDACIÓN 1: El jugador debe estar en su propia zona
            if (e.PlayerTeamID != e.ZoneTeamID)
            {
                CoreLogger.LogSystemDebug("CaptureLogic",
                    $"P{e.PlayerID} no puede capturar en zona enemiga.");
                return;
            }

            // VALIDACIÓN 2: Debe estar portando una bandera
            if (e.CarriedFlag == null)
            {
                CoreLogger.LogSystemDebug("CaptureLogic",
                    $"P{e.PlayerID} no porta bandera.");
                return;
            }

            // VALIDACIÓN 3: La bandera debe ser del equipo contrario
            if (e.CarriedFlag.OwnerTeamID == e.PlayerTeamID)
            {
                CoreLogger.LogSystemDebug("CaptureLogic",
                    "No puedes capturar tu propia bandera.");
                return;
            }

            // VALIDACIÓN 4: La bandera debe estar en estado Carried
            if (e.CarriedFlag.State != FlagState.Carried)
            {
                CoreLogger.LogSystemDebug("CaptureLogic",
                    "La bandera no está en estado Carried.");
                return;
            }

            // ✅ Captura válida
            ExecuteCapture(e);
        }

        private void ExecuteCapture(CaptureAttemptEvent e)
        {
            CoreLogger.LogSystem("CaptureLogic",
                $"CAPTURA: P{e.PlayerID} (T{e.PlayerTeamID}) capturó bandera T{e.CarriedFlag.OwnerTeamID}");

            // Notificar a la bandera
            e.CarriedFlag.Capture(e.PlayerID, e.PlayerTeamID);

            // Sumar punto al equipo que capturó
            bool roundWon = _score?.AddScore(e.PlayerTeamID) ?? false;

            if (roundWon)
            {
                EventBus<OnTeamWonRoundEvent>.Raise(new OnTeamWonRoundEvent
                {
                    TeamID = e.PlayerTeamID,
                    Score  = _score?.GetScore(e.PlayerTeamID) ?? 0,
                    Round  = 0
                });
            }
        }
    }
}
