// ============================================================
//  SceneLoader.cs
//  Core/SceneManagement/SceneLoader.cs
//
//  RESPONSABILIDAD ÚNICA: Carga y descarga de escenas Unity.
//
//  CARACTERÍSTICAS:
//  • LoadAsync con progreso vía EventBus
//  • Modo Single (reemplaza) y Additive (apila)
//  • Loading screen mínima configurable (evita flash)
//  • Cola de cargas: solo procesa una a la vez
//  • Descarga de escenas Additive
//  • Sin referencias directas a otros sistemas
//
//  USO:
//    sceneLoader.LoadScene("Game");
//    sceneLoader.LoadSceneAdditive("HUD");
//    sceneLoader.UnloadScene("HUD");
//    EventBus<SceneLoadedEvent>.Subscribe(OnSceneLoaded);
// ============================================================

using System.Collections;
using System.Collections.Generic;
using Core.Config;
using Core.Debug;
using Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Core.SceneManagement
{
    public class SceneLoader
    {
        // ── Estado ────────────────────────────────────────────

        public bool  IsLoading       { get; private set; }
        public float LoadProgress    { get; private set; }
        public string CurrentScene   { get; private set; }

        // ── Config ────────────────────────────────────────────

        private readonly CoreConfig      _config;
        private readonly MonoBehaviour   _coroutineRunner;

        // Cola de escenas pendientes
        private readonly Queue<SceneLoadRequest> _loadQueue = new();
        private Coroutine _activeLoad;

        // ── Tipos internos ────────────────────────────────────

        private class SceneLoadRequest
        {
            public string    SceneName;
            public LoadSceneMode Mode;
            public System.Action OnComplete;
        }

        // ── Constructor ───────────────────────────────────────

        public SceneLoader(CoreConfig config, MonoBehaviour coroutineRunner)
        {
            _config          = config;
            _coroutineRunner = coroutineRunner;

            CurrentScene = SceneManager.GetActiveScene().name;
            CoreLogger.LogSystem("SceneLoader", $"Inicializado. Escena actual: {CurrentScene}");
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>Carga una escena en modo Single (reemplaza la actual).</summary>
        public void LoadScene(string sceneName, System.Action onComplete = null)
        {
            EnqueueLoad(sceneName, LoadSceneMode.Single, onComplete);
        }

        /// <summary>Carga una escena en modo Additive (se apila sobre la actual).</summary>
        public void LoadSceneAdditive(string sceneName, System.Action onComplete = null)
        {
            EnqueueLoad(sceneName, LoadSceneMode.Additive, onComplete);
        }

        /// <summary>Descarga una escena cargada en modo Additive.</summary>
        public void UnloadScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                CoreLogger.LogWarning($"[SceneLoader] Escena '{sceneName}' no existe en Build Settings.");
                return;
            }

            _coroutineRunner.StartCoroutine(UnloadRoutine(sceneName));
        }

        /// <summary>
        /// Recarga la escena activa actual.
        /// Útil para reiniciar una partida sin volver al menú.
        /// </summary>
        public void ReloadCurrentScene()
        {
            LoadScene(CurrentScene);
        }

        // ── Cola de carga ─────────────────────────────────────

        private void EnqueueLoad(string sceneName, LoadSceneMode mode, System.Action onComplete)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                CoreLogger.LogError("[SceneLoader] Nombre de escena vacío.");
                return;
            }

            _loadQueue.Enqueue(new SceneLoadRequest
            {
                SceneName  = sceneName,
                Mode       = mode,
                OnComplete = onComplete
            });

            CoreLogger.LogSystemDebug("SceneLoader", $"Encolada: {sceneName} ({mode}). Cola: {_loadQueue.Count}");

            // Iniciar proceso si no hay una carga activa
            if (!IsLoading)
                _activeLoad = _coroutineRunner.StartCoroutine(ProcessQueue());
        }

        // ── Coroutines ────────────────────────────────────────

        private IEnumerator ProcessQueue()
        {
            while (_loadQueue.Count > 0)
            {
                var request = _loadQueue.Dequeue();
                yield return _coroutineRunner.StartCoroutine(LoadRoutine(request));
            }
        }

        private IEnumerator LoadRoutine(SceneLoadRequest request)
        {
            IsLoading    = true;
            LoadProgress = 0f;

            CoreLogger.LogSystem("SceneLoader", $"Cargando: '{request.SceneName}' ({request.Mode})");

            EventBus<SceneLoadStartedEvent>.Raise(new SceneLoadStartedEvent
            {
                SceneName  = request.SceneName,
                IsAdditive = request.Mode == LoadSceneMode.Additive
            });

            // Tiempo mínimo de pantalla de carga para evitar flash
            float startTime = UnityEngine.Time.realtimeSinceStartup;

            // Iniciar carga async
            var asyncOp = SceneManager.LoadSceneAsync(request.SceneName, request.Mode);

            if (asyncOp == null)
            {
                CoreLogger.LogError(
                    $"[SceneLoader] No se pudo cargar '{request.SceneName}'. " +
                    "¿Está en File > Build Settings?"
                );
                IsLoading = false;
                yield break;
            }

            // No activar la escena hasta que esté lista
            asyncOp.allowSceneActivation = false;

            // Bucle de progreso (0 → 0.9 = carga, 1.0 = activación)
            while (asyncOp.progress < 0.9f)
            {
                LoadProgress = asyncOp.progress;

                EventBus<SceneLoadProgressEvent>.Raise(new SceneLoadProgressEvent
                {
                    SceneName = request.SceneName,
                    Progress  = LoadProgress
                });

                yield return null;
            }

            // Esperar duración mínima de loading screen
            float elapsed = UnityEngine.Time.realtimeSinceStartup - startTime;
            if (elapsed < _config.LoadingScreenMinDuration)
                yield return new WaitForSecondsRealtime(_config.LoadingScreenMinDuration - elapsed);

            // Activar la escena
            asyncOp.allowSceneActivation = true;

            // Esperar a que la activación complete
            while (!asyncOp.isDone)
                yield return null;

            LoadProgress = 1f;
            IsLoading    = false;

            if (request.Mode == LoadSceneMode.Single)
                CurrentScene = request.SceneName;

            EventBus<SceneLoadProgressEvent>.Raise(new SceneLoadProgressEvent
            {
                SceneName = request.SceneName,
                Progress  = 1f
            });

            EventBus<SceneLoadedEvent>.Raise(new SceneLoadedEvent
            {
                SceneName  = request.SceneName,
                IsAdditive = request.Mode == LoadSceneMode.Additive
            });

            request.OnComplete?.Invoke();

            CoreLogger.LogSystem("SceneLoader", $"Escena lista: '{request.SceneName}'");
        }

        private IEnumerator UnloadRoutine(string sceneName)
        {
            CoreLogger.LogSystem("SceneLoader", $"Descargando: '{sceneName}'");

            var asyncOp = SceneManager.UnloadSceneAsync(sceneName);
            if (asyncOp == null)
            {
                CoreLogger.LogWarning($"[SceneLoader] No se puede descargar '{sceneName}' (¿no está cargada?)");
                yield break;
            }

            while (!asyncOp.isDone)
                yield return null;

            EventBus<SceneUnloadedEvent>.Raise(new SceneUnloadedEvent { SceneName = sceneName });
            CoreLogger.LogSystem("SceneLoader", $"Escena descargada: '{sceneName}'");
        }
    }
}
