// ============================================================
//  TimeManager.cs
//  Core/Time/TimeManager.cs
//
//  RESPONSABILIDAD ÚNICA: Control global del tiempo del juego.
//
//  CARACTERÍSTICAS:
//  • Pausa real (TimeScale = 0) sin bloquear UI
//  • Slow-motion con duración configurable y lerp suavizado
//  • TimeScale restaurable al valor anterior
//  • Comunicación via EventBus
//  • Sin Update innecesario: usa Coroutines controladas
//
//  USO:
//    timeManager.Pause();
//    timeManager.Resume();
//    timeManager.SetSlowMotion(0.25f, duration: 2f);
//    timeManager.SetTimeScale(1.5f);   // Fast-forward
// ============================================================

using System.Collections;
using Core.Config;
using Core.Debug;
using Core.Events;
using UnityEngine;

namespace Core.Time
{
    public class TimeManager
    {
        // ── Estado ────────────────────────────────────────────

        public float CurrentTimeScale    { get; private set; }
        public float PreviousTimeScale   { get; private set; }
        public bool  IsPaused            { get; private set; }
        public bool  IsSlowMotionActive  { get; private set; }

        // ── Config ────────────────────────────────────────────

        private readonly CoreConfig _config;

        // Referencia al MonoBehaviour para coroutines
        private readonly MonoBehaviour _coroutineRunner;

        private Coroutine _slowMotionCoroutine;
        private Coroutine _lerpCoroutine;

        // ── Constructor ───────────────────────────────────────

        public TimeManager(CoreConfig config, MonoBehaviour coroutineRunner)
        {
            _config          = config;
            _coroutineRunner = coroutineRunner;

            CurrentTimeScale  = _config.DefaultTimeScale;
            PreviousTimeScale = _config.DefaultTimeScale;
            UnityEngine.Time.timeScale = CurrentTimeScale;

            CoreLogger.LogSystem("TimeManager", $"Inicializado. TimeScale = {CurrentTimeScale}");
        }

        // ── Pausa ─────────────────────────────────────────────

        /// <summary>Pausa el juego (TimeScale = 0). No afecta UI (unscaledDeltaTime).</summary>
        public void Pause()
        {
            if (IsPaused)
            {
                CoreLogger.LogSystemDebug("TimeManager", "Ya está pausado.");
                return;
            }

            StopActiveCoroutines();

            PreviousTimeScale = CurrentTimeScale;
            ApplyTimeScale(0f, instant: true);
            IsPaused = true;

            EventBus<TimePausedEvent>.Raise(new TimePausedEvent { IsPaused = true });
            CoreLogger.LogSystem("TimeManager", "Juego pausado.");
        }

        /// <summary>Reanuda el juego restaurando el TimeScale previo.</summary>
        public void Resume()
        {
            if (!IsPaused)
            {
                CoreLogger.LogSystemDebug("TimeManager", "No estaba pausado.");
                return;
            }

            IsPaused = false;
            ApplyTimeScale(PreviousTimeScale, instant: true);

            EventBus<TimePausedEvent>.Raise(new TimePausedEvent { IsPaused = false });
            CoreLogger.LogSystem("TimeManager", $"Juego reanudado. TimeScale = {CurrentTimeScale}");
        }

        public void TogglePause()
        {
            if (IsPaused) Resume();
            else          Pause();
        }

        // ── TimeScale ─────────────────────────────────────────

        /// <summary>
        /// Cambia el TimeScale global. Opcionalmente con lerp suavizado.
        /// </summary>
        public void SetTimeScale(float scale, bool useLerp = false)
        {
            scale = Mathf.Clamp(scale, 0f, 10f);

            if (IsPaused)
            {
                // Guardar el valor para cuando se reanude
                PreviousTimeScale = scale;
                CoreLogger.LogSystemDebug("TimeManager", $"Juego pausado: TimeScale guardado = {scale}");
                return;
            }

            StopActiveCoroutines();

            if (useLerp && _config.TimeScaleLerpSpeed > 0f)
                _lerpCoroutine = _coroutineRunner.StartCoroutine(LerpTimeScale(scale));
            else
                ApplyTimeScale(scale, instant: true);
        }

        /// <summary>Restaura el TimeScale al valor por defecto de la configuración.</summary>
        public void ResetTimeScale()
        {
            SetTimeScale(_config.DefaultTimeScale);
        }

        // ── Slow Motion ───────────────────────────────────────

        /// <summary>
        /// Activa slow-motion.
        /// </summary>
        /// <param name="scale">TimeScale del slow-motion (ej. 0.25).</param>
        /// <param name="duration">Duración en segundos de tiempo real. 0 = indefinido.</param>
        public void SetSlowMotion(float scale = -1f, float duration = -1f)
        {
            float targetScale    = scale    < 0 ? _config.SlowMotionScale    : scale;
            float targetDuration = duration < 0 ? _config.SlowMotionDuration : duration;

            targetScale = Mathf.Clamp(targetScale, 0.01f, 1f);

            StopActiveCoroutines();

            IsSlowMotionActive = true;

            EventBus<SlowMotionEvent>.Raise(new SlowMotionEvent
            {
                IsActive = true,
                Scale    = targetScale,
                Duration = targetDuration
            });

            if (targetDuration > 0f)
                _slowMotionCoroutine = _coroutineRunner.StartCoroutine(SlowMotionRoutine(targetScale, targetDuration));
            else
                ApplyTimeScale(targetScale, instant: false);

            CoreLogger.LogSystem("TimeManager", $"SlowMotion activado: scale={targetScale}, duration={targetDuration}s");
        }

        /// <summary>Cancela el slow-motion y restaura TimeScale normal.</summary>
        public void StopSlowMotion()
        {
            if (!IsSlowMotionActive) return;

            StopActiveCoroutines();
            IsSlowMotionActive = false;
            ApplyTimeScale(_config.DefaultTimeScale, instant: false);

            EventBus<SlowMotionEvent>.Raise(new SlowMotionEvent { IsActive = false });
            CoreLogger.LogSystem("TimeManager", "SlowMotion desactivado.");
        }

        // ── Coroutines ────────────────────────────────────────

        private IEnumerator SlowMotionRoutine(float targetScale, float duration)
        {
            ApplyTimeScale(targetScale, instant: false);

            // Esperar en tiempo REAL (no afectado por TimeScale)
            yield return new WaitForSecondsRealtime(duration);

            IsSlowMotionActive = false;
            ApplyTimeScale(_config.DefaultTimeScale, instant: false);

            EventBus<SlowMotionEvent>.Raise(new SlowMotionEvent { IsActive = false });
            CoreLogger.LogSystem("TimeManager", "SlowMotion expirado.");
        }

        private IEnumerator LerpTimeScale(float target)
        {
            while (!Mathf.Approximately(UnityEngine.Time.timeScale, target))
            {
                float newScale = Mathf.Lerp(
                    UnityEngine.Time.timeScale,
                    target,
                    UnityEngine.Time.unscaledDeltaTime * _config.TimeScaleLerpSpeed
                );
                ApplyTimeScale(newScale, instant: true);
                yield return null;
            }
            ApplyTimeScale(target, instant: true);
        }

        // ── Implementación interna ────────────────────────────

        private void ApplyTimeScale(float newScale, bool instant)
        {
            float prev = CurrentTimeScale;
            CurrentTimeScale = newScale;
            UnityEngine.Time.timeScale = newScale;

            // Ajustar fixedDeltaTime para mantener la física consistente
            UnityEngine.Time.fixedDeltaTime = 0.02f * newScale;

            if (!instant || !Mathf.Approximately(prev, newScale))
            {
                EventBus<TimeScaleChangedEvent>.Raise(new TimeScaleChangedEvent
                {
                    PreviousScale = prev,
                    NewScale      = newScale
                });
            }
        }

        private void StopActiveCoroutines()
        {
            if (_slowMotionCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_slowMotionCoroutine);
                _slowMotionCoroutine = null;
            }
            if (_lerpCoroutine != null)
            {
                _coroutineRunner.StopCoroutine(_lerpCoroutine);
                _lerpCoroutine = null;
            }
        }
    }
}
