// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_ObjectiveBase.cs                           ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • ObjectiveBase (abstract MonoBehaviour)              ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Base para todas las entidades de escena del framework.║
// ║    Gestiona: registro en ObjectiveRegistry, ID, TeamID,  ║
// ║    estado activo, y el método helper EmitInteraction().  ║
// ║                                                          ║
// ║  SEPARACIÓN REQUERIDA:                                   ║
// ║    Flag        → GMF_Flag.cs                             ║
// ║    CaptureZone → GMF_CaptureZone.cs                      ║
// ║    ControlPoint → GMF_ControlPoint.cs                    ║
// ║                                                          ║
// ║  CONFIGURACIÓN EN UNITY:                                 ║
// ║    1. Crear un GameObject vacío en escena                ║
// ║    2. Añadir el componente concreto (Flag, etc.)         ║
// ║    3. El Start() se auto-registra en GameModeBase        ║
// ║    4. Asignar ObjectiveID único en Inspector             ║
// ║                                                          ║
// ║  ERRORES COMUNES:                                        ║
// ║    • ObjectiveID duplicado → registro sobreescribe       ║
// ║    • GameModeBase no en escena → Start() falla silencioso║
// ║    • Reset() no restaura estado visual → override        ║
// ╚══════════════════════════════════════════════════════════╝

using Core.Events;
using GMF.Config;
using UnityEngine;

namespace GMF
{
    public abstract class ObjectiveBase : MonoBehaviour, IObjective
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Objetivo")]
        [Tooltip("ID único en la escena. Usado por reglas para identificar este objetivo.")]
        [SerializeField] protected string _objectiveID  = "objective_default";

        [Tooltip("Equipo dueño. -1 = neutral (puede ser tomado por cualquiera).")]
        [SerializeField] protected int    _teamID       = -1;

        [SerializeField] protected bool   _startsActive = true;

        // ── IObjective ────────────────────────────────────────

        public string ObjectiveID  => _objectiveID;
        public int    TeamID       => _teamID;
        public bool   IsActive     { get; protected set; }
        public string State        { get; protected set; } = "Idle";

        // ── Lifecycle ─────────────────────────────────────────

        protected virtual void Start()
        {
            IsActive = _startsActive;

            // Auto-registro en GameModeBase.
            // FindFirstObjectByType es aceptable aquí porque:
            // • ocurre una sola vez en Start()
            // • no está en Update()
            // • es el patrón de inicialización documentado
            var gm = FindFirstObjectByType<GameModeBase>();
            if (gm != null)
                gm.RegisterObjective(this);
            else
                Core.Debug.CoreLogger.LogWarning(
                    $"[{_objectiveID}] No hay GameModeBase en escena. " +
                    "Añadir manualmente via GameModeBase.RegisterObjective().");
        }

        protected virtual void OnDestroy()
        {
            var gm = FindFirstObjectByType<GameModeBase>();
            gm?.UnregisterObjective(_objectiveID);
        }

        // ── Contrato ──────────────────────────────────────────

        public virtual void Initialize(ObjectiveConfig cfg)
        {
            if (cfg == null) return;
            _objectiveID = cfg.ObjectiveID;
            _teamID      = cfg.TeamID;
            IsActive     = cfg.StartsActive;
        }

        public abstract void Reset();

        public void SetActive(bool active)
        {
            IsActive = active;
            gameObject.SetActive(active);
        }

        // ── Helper ────────────────────────────────────────────

        /// <summary>
        /// Publica ObjectiveInteractedEvt al EventBus.
        /// Todas las subclases deben usar este método para notificar.
        /// </summary>
        protected void EmitInteraction(string interaction, int playerID, int playerTeamID)
        {
            if (!IsActive) return;

            EventBus<ObjectiveInteractedEvt>.Raise(new ObjectiveInteractedEvt
            {
                ObjectiveID       = _objectiveID,
                Interaction       = interaction,
                PlayerID          = playerID,
                PlayerTeamID      = playerTeamID,
                ObjectiveTeamID   = _teamID,
                Position          = transform.position
            });
        }
    }
}
