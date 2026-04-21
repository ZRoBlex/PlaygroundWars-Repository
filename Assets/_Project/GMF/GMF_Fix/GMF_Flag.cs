// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Flag.cs  (REEMPLAZA el anterior)           ║
// ║                                                          ║
// ║  CAMBIOS:                                                ║
// ║    - GetPlayerTeam ahora usa GameModeBase.Instance       ║
// ║      (era FindFirstObjectByType → no encontraba inactivo)║
// ║    + Al recoger/soltar, actualiza FlagCarrierBridge      ║
// ║      del portador para que CaptureZone pueda detectarlo  ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections;
using Player.Authority;
using UnityEngine;

namespace GMF
{
    [RequireComponent(typeof(Collider))]
    public class Flag : ObjectiveBase
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Flag Settings")]
        [Tooltip("Segundos hasta retorno automático cuando está caída.")]
        [SerializeField] private float _autoReturnTime = 15f;

        [Header("Visuals")]
        [SerializeField] private GameObject _flagMesh;
        [SerializeField] private GameObject _baseIndicator;

        // ── Estado ────────────────────────────────────────────

        private Vector3    _homePos;
        private Quaternion _homeRot;
        private Transform  _carrierTransform;
        private int        _carrierID   = -1;
        private int        _carrierTeam = -1;
        private Coroutine  _returnTimer;

        public int  CarrierID      => _carrierID;
        public bool IsBeingCarried => _carrierTransform != null;

        // ── Lifecycle ─────────────────────────────────────────

        protected override void Start()
        {
            _homePos = transform.position;
            _homeRot = transform.rotation;
            GetComponent<Collider>().isTrigger = true;
            base.Start();
            UpdateVisuals();
        }

        // UPDATE JUSTIFICADO: seguir al portador en tiempo real.
        private void Update()
        {
            if (!IsBeingCarried || _carrierTransform == null) return;
            transform.position = _carrierTransform.position + Vector3.up * 1.8f;
        }

        // ── Trigger ───────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive) return;
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null) return;
            HandleContact(auth);
        }

        private void HandleContact(PlayerAuthority auth)
        {
            int pid   = auth.PlayerID;
            int pTeam = GetPlayerTeam(pid);
            if (pTeam < 0) return; // equipo no asignado aún

            if (State == "Idle" && pTeam != _teamID)
            {
                // Equipo enemigo → recoger
                DoPickUp(pid, auth.transform, pTeam, auth);
            }
            else if (State == "Dropped" && pTeam == _teamID)
            {
                // Equipo dueño → devolver
                DoReturn(pid);
            }
        }

        // ── Acciones públicas ─────────────────────────────────

        public void PickUp(int playerID, Transform carrier, int playerTeam, PlayerAuthority auth = null)
            => DoPickUp(playerID, carrier, playerTeam, auth);

        public void Drop(int playerID)
            => DoDrop(playerID);

        public void Capture(int playerID, int playerTeam)
        {
            if (State != "Carried") return;
            State = "Captured";
            EmitInteraction("Capture", playerID, playerTeam);
            DoReturn(-1);
        }

        public void ReturnToBase(int returnedByID = -1)
            => DoReturn(returnedByID);

        // ── Implementación ────────────────────────────────────

        private void DoPickUp(int pid, Transform carrier, int pTeam, PlayerAuthority auth = null)
        {
            StopTimer();
            _carrierTransform = carrier;
            _carrierID        = pid;
            _carrierTeam      = pTeam;
            State             = "Carried";
            UpdateVisuals();

            // Actualizar FlagCarrierBridge del portador
            var bridge = carrier.GetComponent<FlagCarrierBridge>()
                      ?? carrier.GetComponentInChildren<FlagCarrierBridge>();
            bridge?.SetCarrying(this);

            EmitInteraction("Pickup", pid, pTeam);
        }

        private void DoDrop(int pid)
        {
            if (State != "Carried") return;
            ClearCarrierBridge();
            int dropTeam      = _carrierTeam;
            _carrierTransform = null;
            _carrierID        = -1;
            _carrierTeam      = -1;
            State             = "Dropped";
            UpdateVisuals();
            EmitInteraction("Drop", pid, dropTeam);
            StartTimer();
        }

        private void DoReturn(int returnedByID)
        {
            StopTimer();
            ClearCarrierBridge();
            _carrierTransform = null;
            _carrierID        = -1;
            _carrierTeam      = -1;
            transform.SetPositionAndRotation(_homePos, _homeRot);
            State = "Idle";
            UpdateVisuals();
            if (returnedByID >= 0)
                EmitInteraction("Return", returnedByID, _teamID);
        }

        private void ClearCarrierBridge()
        {
            if (_carrierTransform == null) return;
            var bridge = _carrierTransform.GetComponent<FlagCarrierBridge>()
                      ?? _carrierTransform.GetComponentInChildren<FlagCarrierBridge>();
            bridge?.ClearCarrying();
        }

        // ── Timer ─────────────────────────────────────────────

        private void StartTimer()
        {
            StopTimer();
            _returnTimer = StartCoroutine(ReturnTimerRoutine());
        }

        private void StopTimer()
        {
            if (_returnTimer != null) { StopCoroutine(_returnTimer); _returnTimer = null; }
        }

        private IEnumerator ReturnTimerRoutine()
        {
            yield return new WaitForSeconds(_autoReturnTime);
            DoReturn(-1);
        }

        // ── IObjective ────────────────────────────────────────

        public override void Reset()
        {
            StopTimer();
            ClearCarrierBridge();
            _carrierTransform = null;
            _carrierID        = -1;
            _carrierTeam      = -1;
            transform.SetPositionAndRotation(_homePos, _homeRot);
            State = "Idle";
            UpdateVisuals();
        }

        // ── Helpers ───────────────────────────────────────────

        private void UpdateVisuals()
        {
            if (_flagMesh      != null) _flagMesh.SetActive(State != "Carried");
            if (_baseIndicator != null) _baseIndicator.SetActive(State == "Idle");
        }

        // ✅ FIX: usa Instance estático en lugar de FindFirstObjectByType
        private int GetPlayerTeam(int pid)
            => GameModeBase.Instance?.Context?.Teams?.GetTeam(pid) ?? -1;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = State == "Idle"    ? Color.green  :
                           State == "Carried" ? Color.yellow : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
