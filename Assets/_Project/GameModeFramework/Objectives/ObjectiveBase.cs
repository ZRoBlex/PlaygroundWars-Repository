// ============================================================
//  Objectives.cs
//  GameModeFramework/Objectives/Objectives.cs
//
//  OBJETIVOS REUTILIZABLES DEL FRAMEWORK.
//  No conocen las reglas. Solo emiten eventos de interacción.
//
//  CONTENIDO:
//  • ObjectiveBase  — base MonoBehaviour para todos los objetivos
//  • Flag           — bandera que se puede recoger/soltar/capturar
//  • CaptureZone    — zona de captura (trigger)
//  • ControlPoint   — punto de control (KOTH) — estructura base
// ============================================================

using GameMode.Framework.Config;
using GameMode.Framework.Events;
using Core.Events;
using Player.Authority;
using UnityEngine;

namespace GameMode.Framework.Objectives
{
    // ════════════════════════════════════════════════════════
    //  OBJECTIVE BASE
    // ════════════════════════════════════════════════════════

    public abstract class ObjectiveBase : MonoBehaviour, IObjective
    {
        [Header("Configuración")]
        [SerializeField] protected string _objectiveID  = "objective";
        [SerializeField] protected int    _ownerTeamID  = -1;
        [SerializeField] protected bool   _startsActive = true;

        public string ObjectiveID  => _objectiveID;
        public int    OwnerTeamID  => _ownerTeamID;
        public bool   IsActive     { get; protected set; }
        public string CurrentState { get; protected set; } = "Idle";

        protected virtual void Start()
        {
            IsActive = _startsActive;

            // Auto-registrar en GameModeBase si existe en escena
            var gm = FindFirstObjectByType<GameModeBase>();
            gm?.RegisterObjective(this);
        }

        public virtual void Initialize(ObjectiveConfig cfg)
        {
            if (cfg == null) return;
            _objectiveID = cfg.ObjectiveID;
            _ownerTeamID = cfg.OwnerTeamID;
            IsActive     = cfg.StartsActive;
        }

        public abstract void Reset();

        public void SetActive(bool active) => IsActive = active;

        protected void EmitInteraction(string type, int playerID, int playerTeamID)
        {
            EventBus<ObjectiveInteractedEvent>.Raise(new ObjectiveInteractedEvent 
            {
                ObjectiveID         = _objectiveID,
                InteractionType     = type,
                PlayerID            = playerID,
                PlayerTeamID        = playerTeamID,
                ObjectiveOwnerTeamID = _ownerTeamID,
                Position             = transform.position
            });
        }
    }
}
