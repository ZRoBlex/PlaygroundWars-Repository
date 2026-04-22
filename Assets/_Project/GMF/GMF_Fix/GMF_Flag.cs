// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Flag.cs  (REEMPLAZA el anterior)           ║
// ║                                                          ║
// ║  CAMBIOS:                                                ║
// ║    + El jugador puede soltar la bandera con "G" (config) ║
// ║      FlagDropInput.cs se añade al prefab del jugador     ║
// ║    + Método público DropByPlayer() para input externo    ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections;
using Player.Authority;
using UnityEngine;

namespace GMF
{
    [RequireComponent(typeof(Collider))]
    public class Flag : ObjectiveBase
    {
        [Header("Flag Settings")]
        [SerializeField] private float _autoReturnTime = 15f;

        [Header("Visuals")]
        [SerializeField] private GameObject _flagMesh;
        [SerializeField] private GameObject _baseIndicator;

        // ── Estado ────────────────────────────────────────────

        private Vector3    _homePos;
        private Quaternion _homeRot;
        // private Transform  _carrierTransform;
        private Transform _followTarget;
        private int        _carrierID   = -1;
        private int        _carrierTeam = -1;
        private Coroutine  _returnTimer;

        public int  CarrierID      => _carrierID;
        public bool IsBeingCarried => _followTarget != null;

        // ── Lifecycle ─────────────────────────────────────────

        protected override void Start()
        {
            _homePos = transform.position;
            _homeRot = transform.rotation;
            GetComponent<Collider>().isTrigger = true;
            base.Start();
            UpdateVisuals();
        }

        private void Update()
        {
            // if (!IsBeingCarried || _followTarget == null) return;
            // transform.position = _followTarget.position + Vector3.up * 1.8f;
            if (!IsBeingCarried || _followTarget == null) return;

            transform.position = _followTarget.position;
            transform.rotation = _followTarget.rotation;
        }

        // ── Trigger ───────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive) return;
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null) return;
            HandleContact(auth);
        }

        // private void HandleContact(PlayerAuthority auth)
        // {
        //     int pid   = auth.PlayerID;
        //     int pTeam = GetPlayerTeam(pid);
        //     if (pTeam < 0) return;

        //     if (State == "Idle" && pTeam != _teamID)
        //         DoPickUp(pid, auth.transform, pTeam);
        //     else if (State == "Dropped" && pTeam == _teamID)
        //         DoReturn(pid);
        // }
        private void HandleContact(PlayerAuthority auth)
        {
            int pid   = auth.PlayerID;
            int pTeam = GetPlayerTeam(pid);
            if (pTeam < 0) return;

            if (State == "Idle" && pTeam != _teamID)
            {
                DoPickUp(pid, auth.transform, pTeam);
            }
            else if (State == "Dropped")
            {
                if (pTeam == _teamID)
                    DoReturn(pid);     // aliados devuelven
                else
                    DoPickUp(pid, auth.transform, pTeam); // enemigos recogen
            }
        }

        // ── Acciones públicas ─────────────────────────────────

        public void PickUp(int pid, Transform carrier, int team)
            => DoPickUp(pid, carrier, team);

        public void Drop(int pid) => DoDrop(pid);

        /// <summary>
        /// El jugador presionó la tecla de soltar bandera.
        /// Solo funciona si este jugador la está portando.
        /// </summary>
        public void DropByPlayer(int playerID)
        {
            if (State != "Carried" || _carrierID != playerID) return;
            DoDrop(playerID);
        }

        public void Capture(int playerID, int playerTeam)
        {
            if (State != "Carried") return;
            State = "Captured";
            EmitInteraction("Capture", playerID, playerTeam);
            DoReturn(-1);
        }

        public void ReturnToBase(int returnedByID = -1) => DoReturn(returnedByID);

        // ── Implementación ────────────────────────────────────

        private void DoPickUp(int pid, Transform carrier, int pTeam)
        {
            StopTimer();
            _followTarget = carrier;
            _carrierID        = pid;
            _carrierTeam      = pTeam;
            State             = "Carried";
            UpdateVisuals();

            // var bridge = carrier.GetComponent<FlagCarrierBridge>()
            //           ?? carrier.GetComponentInChildren<FlagCarrierBridge>();
            // bridge?.SetCarrying(this);
            var bridge = carrier.GetComponent<FlagCarrierBridge>()
            ?? carrier.GetComponentInChildren<FlagCarrierBridge>();

            _followTarget = bridge != null && bridge.CarryPoint != null
                ? bridge.CarryPoint
                : carrier; // fallback si no hay carry point

                bridge?.SetCarrying(this);

            EmitInteraction("Pickup", pid, pTeam);
        }

        private void DoDrop(int pid)
        {
            if (State != "Carried") return;
            ClearCarrierBridge();
            int dropTeam      = _carrierTeam;
            _followTarget = null;
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
            _followTarget = null;
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
            if (_followTarget == null) return;
            var b = _followTarget.GetComponent<FlagCarrierBridge>()
                 ?? _followTarget.GetComponentInChildren<FlagCarrierBridge>();
            b?.ClearCarrying();
        }

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

        public override void Reset()
        {
            StopTimer();
            ClearCarrierBridge();
            _followTarget = null;
            _carrierID = _carrierTeam = -1;
            transform.SetPositionAndRotation(_homePos, _homeRot);
            State = "Idle";
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_flagMesh      != null) _flagMesh.SetActive(State != "Carried");
            if (_baseIndicator != null) _baseIndicator.SetActive(State == "Idle");
        }

        private int GetPlayerTeam(int pid)
            => GameModeBase.Instance?.Context?.Teams?.GetTeam(pid) ?? -1;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = State == "Idle" ? Color.green : State == "Carried" ? Color.yellow : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}