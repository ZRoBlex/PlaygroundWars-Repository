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
