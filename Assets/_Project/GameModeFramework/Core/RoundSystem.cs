// ============================================================
//  RoundSystem.cs  (standalone C#, no MonoBehaviour)
// ============================================================

namespace GameMode.Framework
{
    using System;
    using System.Collections;
    using GameMode.Framework.Config;
    using GameMode.Framework.Events;
    using Core.Events;
    using UnityEngine;

    public class RoundSystem
    {
        public float  Timer    { get; private set; }
        public bool   Active   { get; private set; }

        public Action OnTimeExpired;

        private readonly RoundConfig  _config;
        private readonly MonoBehaviour _runner;
        private Coroutine              _timerCoroutine;

        public RoundSystem(RoundConfig config, MonoBehaviour runner)
        {
            _config = config;
            _runner = runner;
        }

        public void StartRound(int roundNum)
        {
            Active = true;
            Timer  = _config.RoundDuration;

            EventBus<RoundStartedEvent>.Raise(new RoundStartedEvent
            {
                Round    = roundNum,
                Duration = _config.RoundDuration
            });

            if (_config.RoundDuration > 0f)
            {
                if (_timerCoroutine != null) _runner.StopCoroutine(_timerCoroutine);
                _timerCoroutine = _runner.StartCoroutine(TimerRoutine());
            }
        }

        public void Stop()
        {
            Active = false;
            if (_timerCoroutine != null)
            {
                _runner.StopCoroutine(_timerCoroutine);
                _timerCoroutine = null;
            }
        }

        private IEnumerator TimerRoutine()
        {
            while (Timer > 0f)
            {
                yield return null;
                Timer -= Time.deltaTime;
                EventBus<RoundTimerTickEvent>.Raise(new RoundTimerTickEvent
                {
                    Remaining = Mathf.Max(0f, Timer),
                    Total     = _config.RoundDuration
                });
            }
            Active = false;
            OnTimeExpired?.Invoke();
        }
    }
}
