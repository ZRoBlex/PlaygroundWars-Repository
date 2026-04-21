// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_Flag.cs                                    ║
// ║  CARPETA: Assets/_Project/GameModeFramework/             ║
// ║                                                          ║
// ║  CLASES INCLUIDAS:                                       ║
// ║    • Flag (MonoBehaviour, hereda ObjectiveBase)          ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Bandera que puede ser recogida, soltada y capturada.  ║
// ║    Estados: Idle → Carried → Dropped → Idle.             ║
// ║    Emite ObjectiveInteractedEvt al cambiar estado.       ║
// ║    NO conoce reglas ni sabe qué significa cada estado.   ║
// ║                                                          ║
// ║  DEPENDENCIAS:                                           ║
// ║    • ObjectiveBase (base class)                          ║
// ║    • PlayerAuthority (para obtener PlayerID del portador)║
// ║    • Core.Events.EventBus (heredado de ObjectiveBase)    ║
// ║                                                          ║
// ║  SEPARACIÓN REQUERIDA: Ninguna.                          ║
// ║                                                          ║
// ║  CONFIGURACIÓN EN UNITY:                                 ║
// ║    1. Crear GameObject "Flag_TeamA"                      ║
// ║    2. Añadir Collider (isTrigger = true)                 ║
// ║    3. Añadir Flag.cs                                     ║
// ║    4. ObjectiveID = "flag_red"                           ║
// ║    5. TeamID = 0 (equipo dueño)                          ║
// ║    6. Asignar FlagMesh y BaseIndicator (visuals)         ║
// ║                                                          ║
// ║  ERRORES COMUNES:                                        ║
// ║    • Collider no es trigger → no detecta jugadores       ║
// ║    • TeamID no coincide con el equipo → reglas fallan    ║
// ║    • CarryPoint null → la bandera queda en (0,0,0)       ║
// ║                                                          ║
// ║  UPDATE JUSTIFICADO:                                     ║
// ║    Update() mueve la bandera con el portador.            ║
// ║    Alternativa (Coroutine) es más compleja sin ganancia. ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections;
using Core.Events;
using Player.Authority;
using UnityEngine;

namespace GMF
{
    [RequireComponent(typeof(Collider))]
    public class Flag : ObjectiveBase
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Flag Settings")]
        [Tooltip("Segundos hasta que la bandera vuelve sola si está caída.")]
        [SerializeField] private float _autoReturnTime = 15f;

        [Header("Visuals")]
        [SerializeField] private GameObject _flagMesh;
        [SerializeField] private GameObject _baseIndicator;
        [SerializeField] private Transform  _carryOffset;   // dónde aparece sobre el portador

        // ── Estado ────────────────────────────────────────────

        private Vector3    _homePos;
        private Quaternion _homeRot;
        private Transform  _carrierTransform;
        private int        _carrierID   = -1;
        private int        _carrierTeam = -1;
        private Coroutine  _returnTimer;

        public int  CarrierID    => _carrierID;
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

        // UPDATE JUSTIFICADO: necesario para seguir al portador en tiempo real.
        // Alternativa con Coroutine introduciría lag de un frame y más código.
        private void Update()
        {
            if (!IsBeingCarried || _carrierTransform == null) return;
            Vector3 offset = _carryOffset != null
                ? _carrierTransform.position + Vector3.up * 1.8f
                : _carrierTransform.position + Vector3.up * 1.8f;
            transform.position = offset;
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

            if (State == "Idle" && pTeam != _teamID && pTeam >= 0)
            {
                // Equipo enemigo tocó bandera en base → Pickup
                DoPickUp(pid, auth.transform, pTeam);
            }
            else if (State == "Dropped" && pTeam == _teamID)
            {
                // Equipo dueño tocó su bandera caída → Return
                DoReturn(pid);
            }
            // "Carried" → ya la lleva alguien, ignorar
        }

        // ── Acciones públicas (llamadas por reglas) ───────────

        /// <summary>Recoger la bandera. Llamado solo con autoridad de servidor.</summary>
        public void PickUp(int playerID, Transform carrier, int playerTeam)
        {
            DoPickUp(playerID, carrier, playerTeam);
        }

        /// <summary>Soltar la bandera (portador murió). Llamado solo con autoridad.</summary>
        public void Drop(int playerID)
        {
            DoDrop(playerID);
        }

        /// <summary>La bandera fue capturada. Llamado solo con autoridad.</summary>
        public void Capture(int playerID, int playerTeam)
        {
            if (State != "Carried") return;
            State = "Captured";
            EmitInteraction("Capture", playerID, playerTeam);
            DoReturn(-1); // Vuelve a base automáticamente
        }

        /// <summary>Devolver a base sin captura. Llamado por timer o por jugador aliado.</summary>
        public void ReturnToBase(int returnedByID = -1)
        {
            DoReturn(returnedByID);
        }

        // ── Implementación interna ────────────────────────────

        private void DoPickUp(int pid, Transform carrier, int pTeam)
        {
            StopTimer();
            _carrierTransform = carrier;
            _carrierID        = pid;
            _carrierTeam      = pTeam;
            State             = "Carried";
            UpdateVisuals();
            EmitInteraction("Pickup", pid, pTeam);
        }

        private void DoDrop(int pid)
        {
            if (State != "Carried") return;
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
            _carrierTransform = null;
            _carrierID        = -1;
            _carrierTeam      = -1;
            transform.SetPositionAndRotation(_homePos, _homeRot);
            State = "Idle";
            UpdateVisuals();
            if (returnedByID >= 0)
                EmitInteraction("Return", returnedByID, _teamID);
        }

        // ── Timer ─────────────────────────────────────────────

        private void StartTimer()
        {
            StopTimer();
            _returnTimer = StartCoroutine(ReturnTimerRoutine());
        }

        private void StopTimer()
        {
            if (_returnTimer != null)
            {
                StopCoroutine(_returnTimer);
                _returnTimer = null;
            }
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
            _carrierTransform = null;
            _carrierID        = -1;
            _carrierTeam      = -1;
            transform.SetPositionAndRotation(_homePos, _homeRot);
            State = "Idle";
            UpdateVisuals();
        }

        // ── Visuals ───────────────────────────────────────────

        private void UpdateVisuals()
        {
            if (_flagMesh       != null) _flagMesh.SetActive(State != "Carried");
            if (_baseIndicator  != null) _baseIndicator.SetActive(State == "Idle");
        }

        // ── Helper ────────────────────────────────────────────

        // FindFirstObjectByType usado UNA vez por interacción, no en Update.
        private int GetPlayerTeam(int pid)
        {
            var gm = FindFirstObjectByType<GameModeBase>();
            return gm?.Context?.Teams?.GetTeam(pid) ?? -1;
        }

        // ── Gizmos ────────────────────────────────────────────

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = State == "Idle"    ? Color.green  :
                           State == "Carried" ? Color.yellow : Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
