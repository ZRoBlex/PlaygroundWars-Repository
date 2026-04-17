// ============================================================
//  CoreLogger.cs
//  Core/Debug/CoreLogger.cs
//
//  Sistema centralizado de logging para el Core.
//  Controla niveles, tags, colores y el flag DebugMode global.
//
//  USO:
//    CoreLogger.Log("Mensaje normal");
//    CoreLogger.LogWarning("Advertencia");
//    CoreLogger.LogError("Error crítico");
//    CoreLogger.LogDebug("Solo en DebugMode");
//    CoreLogger.LogSystem("SceneLoader", "Escena cargada: Game");
// ============================================================

using UnityEngine;

namespace Core.Debug
{
    public enum LogLevel
    {
        Info    = 0,
        Warning = 1,
        Error   = 2,
        Debug   = 3,   // Solo se imprime si DebugMode = true
        Verbose = 4    // Extremadamente detallado, solo en desarrollo
    }

    public static class CoreLogger
    {
        // ── Configuración ─────────────────────────────────────

        /// <summary>Habilita logs de nivel Debug y Verbose.</summary>
        public static bool DebugMode = true;

        /// <summary>
        /// Nivel mínimo para imprimir. Info=todo, Warning=warning+, etc.
        /// En builds de producción usar Warning o Error.
        /// </summary>
        public static LogLevel MinLevel = LogLevel.Info;

        private const string PREFIX = "<b>[CORE]</b>";

        // ── Métodos públicos ──────────────────────────────────

        public static void Log(string message)
            => Print(message, LogLevel.Info, null);

        public static void LogWarning(string message)
            => Print(message, LogLevel.Warning, null);

        public static void LogError(string message)
            => Print(message, LogLevel.Error, null);

        public static void LogDebug(string message)
            => Print(message, LogLevel.Debug, null);

        public static void LogVerbose(string message)
            => Print(message, LogLevel.Verbose, null);

        /// <summary>Log con tag de sistema para filtrado fácil.</summary>
        public static void LogSystem(string system, string message)
            => Print(message, LogLevel.Info, system);

        public static void LogSystemDebug(string system, string message)
            => Print(message, LogLevel.Debug, system);

        public static void LogSystemError(string system, string message)
            => Print(message, LogLevel.Error, system);

        // ── Implementación interna ────────────────────────────

        private static void Print(string message, LogLevel level, string system)
        {
            // Filtrar por DebugMode
            if ((level == LogLevel.Debug || level == LogLevel.Verbose) && !DebugMode) return;

            // Filtrar por nivel mínimo
            if (level < MinLevel) return;

            string tag    = system != null ? $"[<color=yellow>{system}</color>]" : "";
            string prefix = $"{PREFIX}{tag}";

            switch (level)
            {
                case LogLevel.Info:
                    UnityEngine.Debug.Log($"{prefix} {message}");
                    break;

                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning($"{prefix} ⚠️ {message}");
                    break;

                case LogLevel.Error:
                    UnityEngine.Debug.LogError($"{prefix} ❌ {message}");
                    break;

                case LogLevel.Debug:
                    UnityEngine.Debug.Log($"<color=cyan>{prefix}[DBG]</color> {message}");
                    break;

                case LogLevel.Verbose:
                    UnityEngine.Debug.Log($"<color=#888888>{prefix}[VRB]</color> {message}");
                    break;
            }
        }
    }
}
