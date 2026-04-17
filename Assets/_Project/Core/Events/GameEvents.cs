// ============================================================
//  GameEvents.cs
//  Core/Events/GameEvents.cs
//
//  Structs de eventos del Core System.
//  Todos los sistemas se comunican mediante estos structs
//  a través del EventBus genérico.
//
//  REGLA: Un struct por evento. Sin referencias a MonoBehaviours.
//         Usar solo tipos de valor (int, float, bool, string, enum).
// ============================================================

namespace Core.Events
{
    // ── GameManager ──────────────────────────────────────────

    /// <summary>Disparado una sola vez cuando todos los sistemas del Core están listos.</summary>
    public struct GameInitializedEvent
    {
        public float Timestamp; // Time.realtimeSinceStartup en el momento de init
    }

    /// <summary>Solicita reinicio completo del Core (útil para botón "Reiniciar" en editor).</summary>
    public struct GameRestartRequestedEvent { }

    // ── GameStateManager ─────────────────────────────────────

    /// <summary>Disparado cada vez que el estado del juego cambia.</summary>
    public struct GameStateChangedEvent
    {
        public GameState Previous;
        public GameState Current;
    }

    /// <summary>Solicitud de cambio de estado (cualquier sistema puede pedirlo).</summary>
    public struct GameStateChangeRequestedEvent
    {
        public GameState TargetState;
    }

    // ── SceneLoader ───────────────────────────────────────────

    /// <summary>Antes de iniciar la carga de una escena.</summary>
    public struct SceneLoadStartedEvent
    {
        public string SceneName;
        public bool   IsAdditive;
    }

    /// <summary>Progreso de carga (0.0 - 1.0).</summary>
    public struct SceneLoadProgressEvent
    {
        public string SceneName;
        public float  Progress;
    }

    /// <summary>La escena terminó de cargarse y está activa.</summary>
    public struct SceneLoadedEvent
    {
        public string SceneName;
        public bool   IsAdditive;
    }

    /// <summary>Una escena fue descargada (solo en modo Additive).</summary>
    public struct SceneUnloadedEvent
    {
        public string SceneName;
    }

    // ── TimeManager ───────────────────────────────────────────

    /// <summary>Disparado cuando cambia el TimeScale global.</summary>
    public struct TimeScaleChangedEvent
    {
        public float PreviousScale;
        public float NewScale;
    }

    /// <summary>Disparado cuando el juego se pausa o reanuda a nivel de tiempo.</summary>
    public struct TimePausedEvent
    {
        public bool IsPaused;
    }

    /// <summary>SlowMotion activado/desactivado.</summary>
    public struct SlowMotionEvent
    {
        public bool  IsActive;
        public float Scale;
        public float Duration; // 0 = indefinido
    }
}
