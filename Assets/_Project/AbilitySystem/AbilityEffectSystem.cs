using System.Collections.Generic;
using UnityEngine;
using Core.Events;

namespace AbilitySystem
{
    // ─────────────────────────────────────────────
    // INTERFAZ DE EFECTO
    // ─────────────────────────────────────────────

    public interface IAbilityEffect
    {
        void Apply(AbilityInstance instance, AbilityTargetData targetData);
    }

    // ─────────────────────────────────────────────
    // EVENTOS (NUEVO FLUJO DESACOPLADO)
    // ─────────────────────────────────────────────

    public struct ApplyDamageEffectEvent
    {
        public GameObject Target;
        public float Damage;
        public int SourcePlayerID;
    }

    public struct ApplyHealEffectEvent
    {
        public GameObject Target;
        public float Heal;
        public int SourcePlayerID;
    }

    public struct SpawnZoneEffectEvent
    {
        public GameObject Prefab;
        public Vector3 Position;
        public float Duration;
        public int SourcePlayerID;
    }

    public struct ApplyImpulseEffectEvent
    {
        public GameObject Target;
        public Vector3 Direction;
        public float Force;
        public int SourcePlayerID;
    }

    // ─────────────────────────────────────────────
    // SISTEMA DE EFECTOS
    // ─────────────────────────────────────────────

    public static class AbilityEffectSystem
    {
        // 🔥 SOLO versión correcta (eliminamos la incorrecta)
        public static void ApplyEffect(IAbilityEffect effect, AbilityInstance instance, AbilityTargetData targetData)
        {
            if (effect == null)
            {
                Debug.LogWarning("[AbilityEffectSystem] Effect null");
                return;
            }

            effect.Apply(instance, targetData);
        }
    }

    // ─────────────────────────────────────────────
    // EFECTOS (AHORA SOLO EMITEN INTENCIÓN)
    // ─────────────────────────────────────────────

    // 🔴 DAÑO
    [System.Serializable]
    public class DamageEffect : IAbilityEffect
    {
        public float DamageAmount = 25f;

        public void Apply(AbilityInstance instance, AbilityTargetData data)
        {
            if (instance.Owner == null) return;

            var authority = instance.Owner.GetComponent<Player.Authority.PlayerAuthority>();
            if (authority == null) return;

            int sourceID = authority.PlayerID;

            foreach (var target in data.Targets)
            {
                if (target == null) continue;

                EventBus<ApplyDamageEffectEvent>.Raise(
                    new ApplyDamageEffectEvent
                    {
                        Target = target,
                        Damage = DamageAmount,
                        SourcePlayerID = sourceID
                    });
            }
        }
    }

    // 🟢 CURACIÓN
    [System.Serializable]
    public class HealEffect : IAbilityEffect
    {
        public float HealAmount = 20f;

        public void Apply(AbilityInstance instance, AbilityTargetData data)
        {
            if (instance.Owner == null) return;

            var authority = instance.Owner.GetComponent<Player.Authority.PlayerAuthority>();
            if (authority == null) return;

            int sourceID = authority.PlayerID;

            foreach (var target in data.Targets)
            {
                if (target == null) continue;

                EventBus<ApplyHealEffectEvent>.Raise(
                    new ApplyHealEffectEvent
                    {
                        Target = target,
                        Heal = HealAmount,
                        SourcePlayerID = sourceID
                    });
            }
        }
    }

    // 🟣 SPAWN DE ZONA
    [System.Serializable]
    public class SpawnZoneEffect : IAbilityEffect
    {
        public GameObject ZonePrefab;
        public float Duration = 5f;

        public void Apply(AbilityInstance instance, AbilityTargetData data)
        {
            if (ZonePrefab == null)
            {
                Debug.LogWarning("[SpawnZoneEffect] No prefab");
                return;
            }

            if (instance.Owner == null) return;

            var authority = instance.Owner.GetComponent<Player.Authority.PlayerAuthority>();
            if (authority == null) return;

            int sourceID = authority.PlayerID;

            EventBus<SpawnZoneEffectEvent>.Raise(
                new SpawnZoneEffectEvent
                {
                    Prefab = ZonePrefab,
                    Position = data.Position,
                    Duration = Duration,
                    SourcePlayerID = sourceID
                });
        }
    }

    // 🔵 IMPULSO (dash, knockback, etc.)
    [System.Serializable]
    public class ImpulseEffect : IAbilityEffect
    {
        public float Force = 10f;

        public void Apply(AbilityInstance instance, AbilityTargetData data)
        {
            if (instance.Owner == null) return;

            var authority = instance.Owner.GetComponent<Player.Authority.PlayerAuthority>();
            if (authority == null) return;

            int sourceID = authority.PlayerID;

            EventBus<ApplyImpulseEffectEvent>.Raise(
                new ApplyImpulseEffectEvent
                {
                    Target = instance.Owner,
                    Direction = data.Direction,
                    Force = Force,
                    SourcePlayerID = sourceID
                });
        }
    }
}