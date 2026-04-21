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
    public class Flag : ObjectiveBase
    {
        [Header("Flag Settings")]
        [SerializeField] private float _autoReturnTime = 15f;

        [Header("Visuals")]
        [SerializeField] private GameObject _mesh;
        [SerializeField] private GameObject _baseIndicator;

        // Estado
        private Vector3    _basePos;
        private Quaternion _baseRot;
        private Transform  _carrier;
        private int        _carrierID   = -1;
        private float      _dropTimer;
        private bool       _isDropped;

        public int  CarrierID => _carrierID;
        public bool IsCarried => _carrier != null;

        protected override void Start()
        {
            _basePos = transform.position;
            _baseRot = transform.rotation;
            base.Start();
        }

        private void Update()
        {
            if (IsCarried && _carrier != null)
                transform.position = _carrier.position + Vector3.up * 1.6f;

            if (_isDropped)
            {
                _dropTimer += Time.deltaTime;
                if (_dropTimer >= _autoReturnTime)
                    ReturnToBase(-1);
            }
        }

        // ── Trigger ───────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive) return;

            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null) return;

            // Bandera caída + es el equipo dueño → devolver
            // if (_isDropped && auth.GetComponent<CTF.Teams.TeamManager>() is var tm
            //                && tm?.GetTeamOf(auth.PlayerID) == _ownerTeamID)
            {
                EmitInteraction("Return", auth.PlayerID, _ownerTeamID);
                ReturnToBase(auth.PlayerID);
                return;
            }

            // Bandera en base + es equipo enemigo → recoger
            // if (!IsCarried && !_isDropped)
            // {
            //     int playerTeam = GetPlayerTeam(auth.PlayerID);
            //     if (playerTeam != _ownerTeamID && playerTeam >= 0)
            //         PickUp(auth.PlayerID, auth.transform, playerTeam);
            // }
        }

        // ── Acciones ──────────────────────────────────────────

        public void PickUp(int playerID, Transform carrier, int playerTeamID)
        {
            _carrier   = carrier;
            _carrierID = playerID;
            _isDropped = false;
            _dropTimer = 0f;
            CurrentState = "Carried";

            EmitInteraction("Pickup", playerID, playerTeamID);
        }

        public void Drop(int playerID)
        {
            _carrier   = null;
            _carrierID = -1;
            _isDropped = true;
            _dropTimer = 0f;
            CurrentState = "Dropped";

            // EmitInteraction("Drop", playerID, GetPlayerTeam(playerID));
        }

        public void Capture(int playerID, int playerTeamID)
        {
            _carrier   = null;
            _carrierID = -1;
            CurrentState = "Captured";

            EmitInteraction("Capture", playerID, playerTeamID);
            ReturnToBase(-1);
        }

        public void ReturnToBase(int returnedByID)
        {
            _carrier   = null;
            _carrierID = -1;
            _isDropped = false;
            _dropTimer = 0f;
            transform.SetPositionAndRotation(_basePos, _baseRot);
            CurrentState = "Idle";

            if (returnedByID >= 0)
                EmitInteraction("Return", returnedByID, _ownerTeamID);

            UpdateVisuals();
        }

        public override void Reset() => ReturnToBase(-1);

        private void UpdateVisuals()
        {
            if (_mesh != null)           _mesh.SetActive(!IsCarried);
            if (_baseIndicator != null)  _baseIndicator.SetActive(CurrentState == "Idle");
        }

        // private int GetPlayerTeam(int pid)
        // {
        //     var tm = FindFirstObjectByType<CTF.Teams.TeamManager>();
        //     return tm?.GetTeamOf(pid) ?? -1;
        // }
    }
}
