// ============================================================
//  FlagCarrierComponent.cs
//  GameMode/CTF/FlagCarrierComponent.cs
//
//  RESPONSABILIDAD ÚNICA: Capacidad del jugador de cargar banderas.
//
//  • Detecta banderas cercanas (trigger) y permite tomarlas
//  • Suelta la bandera automáticamente al morir
//  • Expone CarriedFlag para que CaptureZone pueda consultarlo
//  • Comunica penalización de velocidad al PlayerMovement
// ============================================================

using GameMode.Config;
using GameMode.Events;
using Core.Debug;
using Core.Events;
using Player.Authority;
using Player.Events;
using UnityEngine;

namespace GameMode.CTF
{
    [RequireComponent(typeof(PlayerAuthority))]
    [DisallowMultipleComponent]
    public class FlagCarrierComponent : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [SerializeField] private CTFConfig _config;

        [Header("Carry Point (Transform en la espalda/hombro)")]
        [SerializeField] private Transform _carryPoint;

        // ── Referencias ───────────────────────────────────────

        private PlayerAuthority _authority;

        // ── Estado ────────────────────────────────────────────

        public FlagController CarriedFlag  { get; private set; }
        public bool           IsCarrying   => CarriedFlag != null;
        public int            PlayerID     => _authority.PlayerID;
        public int            TeamID       { get; set; } = -1;  // Seteado por TeamManager

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _authority  = GetComponent<PlayerAuthority>();
            if (_carryPoint == null) _carryPoint = transform;
        }

        private void OnEnable()
        {
            EventBus<PlayerDiedEvent>.Subscribe(OnPlayerDied);
            EventBus<PlayerRespawnedEvent>.Subscribe(OnPlayerRespawned);
            EventBus<PlayerAssignedToTeamEvent>.Subscribe(OnTeamAssigned);
        }

        private void OnDisable()
        {
            EventBus<PlayerDiedEvent>.Unsubscribe(OnPlayerDied);
            EventBus<PlayerRespawnedEvent>.Unsubscribe(OnPlayerRespawned);
            EventBus<PlayerAssignedToTeamEvent>.Unsubscribe(OnTeamAssigned);
        }

        // ── Trigger de bandera ────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!_authority.HasAuthority) return;
            if (IsCarrying) return;

            var flag = other.GetComponent<FlagController>();
            if (flag == null) return;

            TryPickupFlag(flag);
        }

        private void TryPickupFlag(FlagController flag)
        {
            // Bandera enemiga → recoger
            if (flag.OwnerTeamID != TeamID)
            {
                if (flag.PickUp(_authority.PlayerID, _carryPoint))
                    CarriedFlag = flag;
            }
            // Bandera propia caída → devolver a base
            else if (flag.State == FlagState.Dropped)
            {
                flag.Return(_authority.PlayerID);
                CoreLogger.LogSystem("FlagCarrier",
                    $"P{_authority.PlayerID} devolvió su propia bandera.");
            }
        }

        // ── Soltar bandera ────────────────────────────────────

        public void DropFlag()
        {
            if (!IsCarrying) return;
            CarriedFlag.Drop(_authority.PlayerID);
            CarriedFlag = null;
        }

        // ── Penalización de velocidad ─────────────────────────

        public float GetSpeedMultiplier()
            => (IsCarrying && _config != null)
               ? 1f - _config.CarrierSpeedPenalty
               : 1f;

        // ── Callbacks ─────────────────────────────────────────

        private void OnPlayerDied(PlayerDiedEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            if (IsCarrying) DropFlag();
        }

        private void OnPlayerRespawned(PlayerRespawnedEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            CarriedFlag = null;  // Seguridad: limpiar estado
        }

        private void OnTeamAssigned(PlayerAssignedToTeamEvent e)
        {
            if (e.PlayerID != _authority.PlayerID) return;
            TeamID = e.TeamID;
        }
    }

    // ── Evento de asignación de equipo (si no existe en Player system) ──

    public struct PlayerAssignedToTeamEvent
    {
        public int    PlayerID;
        public int    TeamID;
        public string TeamName;
    }
}
