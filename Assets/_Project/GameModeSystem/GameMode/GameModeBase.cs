// ============================================================
//  GameModeBase_Fixed.cs
//  GameMode/GameMode/GameModeBase_Fixed.cs
//
//  ⚠️  REEMPLAZA: GameModeBase.cs
//
//  CORRECCIÓN: Añadido 'protected virtual void Awake()' para
//  que CaptureTheFlagMode pueda hacer override correctamente.
// ============================================================

using GameMode.Events;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace GameMode
{
    public abstract class GameModeBase : MonoBehaviour
    {
        [Header("Identificación")]
        [SerializeField] protected string _gameModeID  = "Base";
        [SerializeField] protected string _displayName = "Game Mode";

        public GameModePhase Phase       { get; protected set; } = GameModePhase.Idle;
        public bool          IsRunning   { get; protected set; }
        public float         ElapsedTime { get; protected set; }
        public string        GameModeID  => _gameModeID;

        // ✅ FIX: virtual Awake para que subclases puedan hacer override
        protected virtual void Awake() { }

        public virtual void StartGame()
        {
            IsRunning   = true;
            ElapsedTime = 0f;
            CoreLogger.LogSystem("GameMode", $"[{_gameModeID}] StartGame()");

            EventBus<OnGameStartedEvent>.Raise(new OnGameStartedEvent
            {
                GameModeID = _gameModeID,
                Timestamp  = Time.realtimeSinceStartup
            });
        }

        public virtual void EndGame(int winnerTeamID = -1)
        {
            IsRunning = false;
            CoreLogger.LogSystem("GameMode", $"[{_gameModeID}] EndGame. Winner={winnerTeamID}");

            EventBus<OnGameEndedEvent>.Raise(new OnGameEndedEvent
            {
                GameModeID   = _gameModeID,
                WinnerTeamID = winnerTeamID,
                Duration     = ElapsedTime
            });

            EventBus<Core.Events.GameStateChangeRequestedEvent>.Raise(
                new Core.Events.GameStateChangeRequestedEvent
                {
                    TargetState = Core.GameState.GameOver
                });
        }

        public virtual void UpdateGame()
        {
            if (!IsRunning) return;
            ElapsedTime += Time.deltaTime;
        }

        public virtual void ResetGame()
        {
            IsRunning   = false;
            ElapsedTime = 0f;
            Phase       = GameModePhase.Idle;
        }

        protected void SetPhase(GameModePhase newPhase)
        {
            GameModePhase prev = Phase;
            Phase = newPhase;
            CoreLogger.LogSystem("GameMode", $"[{_gameModeID}] Fase: {prev} → {newPhase}");

            EventBus<OnGameModePhaseChangedEvent>.Raise(new OnGameModePhaseChangedEvent
            {
                Previous = prev,
                Current  = newPhase
            });
        }

        protected virtual void Update()
        {
            if (IsRunning) UpdateGame();
        }
    }
}
