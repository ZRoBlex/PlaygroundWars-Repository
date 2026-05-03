using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AbilitySystem
{
    // ─────────────────────────────────────────────
    // BASE DE EFECTO DE ESTADO
    // ─────────────────────────────────────────────

    public abstract class StatusEffectBase
    {
        public string EffectID;
        public float Duration;
        public float TickRate = 0.2f;
        public int MaxStacks = 1;

        protected float _elapsed;
        protected int _stacks;

        public GameObject Target { get; private set; }

        public void Initialize(GameObject target)
        {
            Target = target;
            _elapsed = 0f;
            _stacks = 1;

            OnApply();
        }

        public void AddStack()
        {
            if (_stacks < MaxStacks)
                _stacks++;

            OnStackAdded();
        }

        public void Tick(float deltaTime)
        {
            _elapsed += deltaTime;

            OnTick(deltaTime);

            if (_elapsed >= Duration)
                OnExpire();
        }

        public bool IsExpired()
        {
            return _elapsed >= Duration;
        }

        // ── Métodos que cada efecto implementa ──

        protected virtual void OnApply() { }
        protected virtual void OnTick(float deltaTime) { }
        protected virtual void OnExpire() { }
        protected virtual void OnStackAdded() { }
    }

    // ─────────────────────────────────────────────
    // MANAGER DE EFECTOS DE ESTADO
    // ─────────────────────────────────────────────

    public class StatusEffectManager : MonoBehaviour
    {
        private List<StatusEffectBase> _activeEffects = new();
        private Coroutine _tickRoutine;

        private void OnEnable()
        {
            _tickRoutine = StartCoroutine(TickRoutine());
        }

        private void OnDisable()
        {
            if (_tickRoutine != null)
                StopCoroutine(_tickRoutine);
        }

        public void ApplyEffect(StatusEffectBase effect)
        {
            if (effect == null) return;

            // Buscar si ya existe
            var existing = _activeEffects.Find(e => e.EffectID == effect.EffectID);

            if (existing != null)
            {
                existing.AddStack();
                return;
            }

            effect.Initialize(gameObject);
            _activeEffects.Add(effect);
        }

        private IEnumerator TickRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(0.2f);

            while (true)
            {
                float dt = 0.2f;

                for (int i = _activeEffects.Count - 1; i >= 0; i--)
                {
                    var effect = _activeEffects[i];

                    effect.Tick(dt);

                    if (effect.IsExpired())
                    {
                        _activeEffects.RemoveAt(i);
                    }
                }

                yield return wait;
            }
        }
    }

    // ─────────────────────────────────────────────
    // EFECTO: SLOW
    // ─────────────────────────────────────────────

    public class SlowStatusEffect : StatusEffectBase
    {
        public float SlowPercent = 0.5f;

        protected override void OnApply()
        {
            var movement = Target.GetComponent<Player.Movement.PlayerMovement_Fixed>();
            if (movement != null)
            {
                movement.SetSpeedMultiplier(1f - SlowPercent);
            }
        }

        protected override void OnExpire()
        {
            var movement = Target.GetComponent<Player.Movement.PlayerMovement_Fixed>();
            if (movement != null)
            {
                movement.SetSpeedMultiplier(1f);
            }
        }
    }

    // ─────────────────────────────────────────────
    // EFECTO: FREEZE
    // ─────────────────────────────────────────────

    public class FreezeStatusEffect : StatusEffectBase
    {
        protected override void OnApply()
        {
            var movement = Target.GetComponent<Player.Movement.PlayerMovement_Fixed>();
            if (movement != null)
            {
                movement.SetSpeedMultiplier(0f);
            }
        }

        protected override void OnExpire()
        {
            var movement = Target.GetComponent<Player.Movement.PlayerMovement_Fixed>();
            if (movement != null)
            {
                movement.SetSpeedMultiplier(1f);
            }
        }
    }

    // ─────────────────────────────────────────────
    // EFECTO: DAMAGE OVER TIME
    // ─────────────────────────────────────────────

    public class DamageOverTimeEffect : StatusEffectBase
    {
        public float DamagePerSecond = 10f;

        protected override void OnTick(float deltaTime)
        {
            var health = Target.GetComponent<Player.Health.PlayerHealth>();
            if (health != null)
            {
                health.TakeDamage(
                    DamagePerSecond * deltaTime,
                    -1,
                    Target.transform.position,
                    Vector3.up
                );
            }
        }
    }
}