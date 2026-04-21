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
    //  CONTROL POINT (estructura base para KOTH)
    // ════════════════════════════════════════════════════════

    [RequireComponent(typeof(Collider))]
    public class ControlPoint : ObjectiveBase
    {
        [Header("Control Point")]
        [SerializeField] private float _captureTime   = 5f;   // Segundos para capturar
        [SerializeField] private float _scoreInterval = 1f;   // Puntos cada N segundos

        private int   _contestingTeam  = -1;
        private float _captureProgress = 0f;
        private float _scoreAccumulator = 0f;
        private int   _occupantCount   = 0;

        private readonly System.Collections.Generic.HashSet<int> _occupants = new();

        protected override void Start()
        {
            GetComponent<Collider>().isTrigger = true;
            base.Start();
        }

        private void OnTriggerEnter(Collider other)
        {
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null || !_occupants.Add(auth.PlayerID)) return;

            _occupantCount = _occupants.Count;
            // EmitInteraction("Enter", auth.PlayerID, GetPlayerTeam(auth.PlayerID));
        }

        private void OnTriggerExit(Collider other)
        {
            var auth = other.GetComponentInParent<PlayerAuthority>();
            if (auth == null || !_occupants.Remove(auth.PlayerID)) return;

            _occupantCount = _occupants.Count;
            // EmitInteraction("Exit", auth.PlayerID, GetPlayerTeam(auth.PlayerID));
        }

        private void Update()
        {
            if (_occupantCount == 0) return;

            // Determinar si hay un equipo controlando (y no está contested)
            // En producción: verificar que TODOS los ocupantes son del mismo equipo

            _scoreAccumulator += Time.deltaTime;
            if (_scoreAccumulator >= _scoreInterval)
            {
                _scoreAccumulator = 0f;
                if (_ownerTeamID >= 0)
                    EmitInteraction("Tick", -1, _ownerTeamID);
            }
        }

        public override void Reset()
        {
            _captureProgress  = 0f;
            _scoreAccumulator = 0f;
            _ownerTeamID      = -1;
            _occupants.Clear();
            CurrentState = "Idle";
        }

        // private int GetPlayerTeam(int pid)
        // {
        //     var tm = FindFirstObjectByType<CTF.Teams.TeamManager>();
        //     return tm?.GetTeamOf(pid) ?? -1;
        // }
    }
}
