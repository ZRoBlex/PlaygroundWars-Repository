// ============================================================
//  CoreConfig.cs
//  Core/Config/CoreConfig.cs
//
//  ScriptableObject central de configuración del Core System.
//  Todos los valores ajustables viven aquí. Sin hardcoding.
//
//  CREAR:  Assets → Right Click → Create → Core → CoreConfig
//  USAR:   Asignar al campo CoreConfig del GameManager en el Inspector.
// ============================================================

using UnityEngine;

namespace Core.Config
{
    [CreateAssetMenu(fileName = "CoreConfig", menuName = "Core/CoreConfig", order = 0)]
    public class CoreConfig : ScriptableObject
    {
        // ── General ───────────────────────────────────────────

        [Header("General")]
        [Tooltip("Activar para usar Netcode/Mirror. Desactivado = singleplayer.")]
        public bool UseNetworking = false;

        [Tooltip("Habilita logs Debug y validaciones extra.")]
        public bool DebugMode = true;

        [Tooltip("Muestra HUD de debug en pantalla en runtime.")]
        public bool ShowDebugHUD = false;

        // ── Escenas ───────────────────────────────────────────

        [Header("Scene Names")]
        [Tooltip("Nombre exacto de la escena del menú principal.")]
        public string MainMenuScene = "MainMenu";

        [Tooltip("Nombre exacto de la escena del Lobby.")]
        public string LobbyScene = "Lobby";

        [Tooltip("Nombre exacto de la escena de juego principal.")]
        public string GameScene = "Game";

        [Tooltip("Nombre exacto de la pantalla de carga (Additive).")]
        public string LoadingScene = "LoadingScreen";

        [Tooltip("Tiempo mínimo que la pantalla de carga se muestra (evita flash).")]
        [Range(0f, 3f)]
        public float LoadingScreenMinDuration = 0.5f;

        // ── Tiempo ────────────────────────────────────────────

        [Header("Time Settings")]
        [Tooltip("TimeScale inicial al arrancar el juego.")]
        [Range(0f, 5f)]
        public float DefaultTimeScale = 1f;

        [Tooltip("TimeScale durante slow-motion.")]
        [Range(0.01f, 1f)]
        public float SlowMotionScale = 0.25f;

        [Tooltip("Duración predeterminada del slow-motion en segundos. 0 = indefinido.")]
        [Range(0f, 10f)]
        public float SlowMotionDuration = 2f;

        [Tooltip("Suavizado (lerp) al cambiar TimeScale. 0 = instantáneo.")]
        [Range(0f, 10f)]
        public float TimeScaleLerpSpeed = 5f;

        // ── Bootstrap ─────────────────────────────────────────

        [Header("Bootstrap")]
        [Tooltip("Escena inicial después del Bootstrap (normalmente MainMenu).")]
        public string InitialScene = "MainMenu";

        [Tooltip("Si es true, el Bootstrap persiste entre escenas (DontDestroyOnLoad).")]
        public bool PersistBootstrapper = true;
    }
}
