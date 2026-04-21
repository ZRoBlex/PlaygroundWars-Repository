// ============================================================
//  FlagController.cs
//  GameMode/CTF/FlagController.cs
//
//  RESPONSABILIDAD ÚNICA: Comportamiento de la bandera.
//
//  ESTADOS:
//    Idle    → En su base. Puede ser recogida por enemigos.
//    Carried → La porta un jugador (sigue su transform).
//    Dropped → En el suelo. Timer de retorno automático activo.
//
//  AUTORIDAD:
//    Solo la autoridad (servidor/host/offline) modifica el estado.
//    En modo cliente, el estado se recibe via red y solo
//    se actualizan los visuals (mesh position, indicator).
// ============================================================

using System.Collections;
using GameMode.Config;
using GameMode.Events;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace GameMode.CTF
{
    public enum FlagState { Idle, Carried, Dropped }

    [DisallowMultipleComponent]
    public class FlagController : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Configuración")]
        [SerializeField] private CTFConfig  _config;
        [SerializeField] private int        _ownerTeamID = 0;

        [Header("Visuals")]
        [SerializeField] private GameObject _flagMesh;
        [SerializeField] private GameObject _baseIndicator;
        [SerializeField] private Renderer   _flagRenderer;

        // ── Estado ────────────────────────────────────────────

        public FlagState State       { get; private set; } = FlagState.Idle;
        public int       OwnerTeamID => _ownerTeamID;
        public int       CarrierID   { get; private set; } = -1;

        private Vector3    _basePos;
        private Quaternion _baseRot;
        private Transform  _carrierTransform;
        private Coroutine  _returnTimer;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _basePos = transform.position;
            _baseRot = transform.rotation;
        }

        private void Start()
        {
            UpdateVisuals();
        }

        private void Update()
        {
            // Seguir al portador
            if (State == FlagState.Carried && _carrierTransform != null)
            {
                transform.position = _carrierTransform.position + Vector3.up * 1.6f;
                transform.rotation = _carrierTransform.rotation;
            }
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>Un jugador recoge la bandera. Solo llamar con autoridad.</summary>
        public bool PickUp(int playerID, Transform carrier)
        {
            if (State == FlagState.Carried) return false;

            StopReturnTimer();
            _carrierTransform = carrier;
            CarrierID         = playerID;
            SetState(FlagState.Carried);

            CoreLogger.LogSystem("FlagController",
                $"[T{_ownerTeamID}] Bandera recogida por P{playerID}");

            EventBus<OnFlagPickedEvent>.Raise(new OnFlagPickedEvent
            {
                CarrierID       = playerID,
                FlagTeamID      = _ownerTeamID,
                PickupPosition  = transform.position
            });

            return true;
        }

        /// <summary>El portador suelta la bandera (murió). Solo llamar con autoridad.</summary>
        public void Drop(int playerID)
        {
            if (State != FlagState.Carried) return;

            Vector3 pos       = transform.position;
            _carrierTransform = null;
            CarrierID         = -1;
            SetState(FlagState.Dropped);
            StartReturnTimer();

            CoreLogger.LogSystem("FlagController",
                $"[T{_ownerTeamID}] Bandera soltada por P{playerID} en {pos}");

            EventBus<OnFlagDroppedEvent>.Raise(new OnFlagDroppedEvent
            {
                CarrierID    = playerID,
                FlagTeamID   = _ownerTeamID,
                DropPosition = pos
            });
        }

        /// <summary>La bandera vuelve a su base sin captura. Solo llamar con autoridad.</summary>
        public void Return(int returnedByID = -1)
        {
            if (State == FlagState.Idle) return;

            StopReturnTimer();
            _carrierTransform = null;
            CarrierID         = -1;
            transform.SetPositionAndRotation(_basePos, _baseRot);
            SetState(FlagState.Idle);

            CoreLogger.LogSystem("FlagController",
                $"[T{_ownerTeamID}] Bandera devuelta a base (P{returnedByID})");

            EventBus<OnFlagReturnedEvent>.Raise(new OnFlagReturnedEvent
            {
                FlagTeamID   = _ownerTeamID,
                ReturnedByID = returnedByID,
                BasePosition = _basePos
            });
        }

        /// <summary>La bandera fue capturada. Solo llamar con autoridad.</summary>
        public void Capture(int capturingPlayerID, int capturingTeamID)
        {
            if (State != FlagState.Carried) return;

            StopReturnTimer();
            Vector3 capPos    = transform.position;
            _carrierTransform = null;
            CarrierID         = -1;

            CoreLogger.LogSystem("FlagController",
                $"[T{_ownerTeamID}] ¡CAPTURADA! por P{capturingPlayerID} (T{capturingTeamID})");

            EventBus<OnFlagCapturedEvent>.Raise(new OnFlagCapturedEvent
            {
                CapturingPlayerID = capturingPlayerID,
                CapturingTeamID   = capturingTeamID,
                FlagTeamID        = _ownerTeamID,
                CapturePosition   = capPos
            });

            Return(-1);
        }

        /// <summary>Reset forzado sin eventos (entre rondas).</summary>
        public void ForceReset()
        {
            StopReturnTimer();
            _carrierTransform = null;
            CarrierID         = -1;
            transform.SetPositionAndRotation(_basePos, _baseRot);
            SetState(FlagState.Idle);
        }

        // ── Timer de retorno automático ───────────────────────

        private void StartReturnTimer()
        {
            StopReturnTimer();
            _returnTimer = StartCoroutine(ReturnTimerRoutine());
        }

        private void StopReturnTimer()
        {
            if (_returnTimer != null) { StopCoroutine(_returnTimer); _returnTimer = null; }
        }

        private IEnumerator ReturnTimerRoutine()
        {
            float t = _config != null ? _config.FlagAutoReturnTime : 15f;
            yield return new WaitForSeconds(t);
            Return(-1);
        }

        // ── Visuals ───────────────────────────────────────────

        private void SetState(FlagState s)
        {
            State = s;
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_flagMesh != null)
                _flagMesh.SetActive(State != FlagState.Carried);
            if (_baseIndicator != null)
                _baseIndicator.SetActive(State == FlagState.Idle);
        }

        // ── Gizmos ────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = State == FlagState.Idle    ? Color.green :
                           State == FlagState.Carried ? Color.yellow : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);

            if (_config != null)
            {
                Gizmos.color = Color.white * 0.4f;
                Gizmos.DrawWireSphere(transform.position, _config.FlagPickupRadius);
            }
        }
    }
}
