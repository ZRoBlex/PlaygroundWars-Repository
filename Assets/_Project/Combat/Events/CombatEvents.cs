// ============================================================
//  CombatEvents.cs
//  Combat/Events/CombatEvents.cs
//
//  Todos los event structs del sistema de combate.
//  Comunicación desacoplada via EventBus<T> del Core.
//  Solo tipos de valor — sin referencias a MonoBehaviours.
// ============================================================

using UnityEngine;

namespace Combat.Events
{
    // ── Disparo ───────────────────────────────────────────────

    /// <summary>Un arma disparó exitosamente.</summary>
    public struct OnShootEvent
    {
        public int         ShooterID;
        public string      WeaponID;
        public ShootingType Type;
        public Vector3     Origin;
        public Vector3     Direction;
    }

    /// <summary>Intento de disparo fallido.</summary>
    public struct OnShootFailedEvent
    {
        public int    ShooterID;
        public string WeaponID;
        public string Reason;   // "NoAmmo", "Reloading", "Cooldown"
    }

    // ── Impacto ───────────────────────────────────────────────

    /// <summary>Algo fue golpeado (HitScan o proyectil).</summary>
    public struct OnHitEvent
    {
        public int     ShooterID;
        public string  WeaponID;
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public bool    HitPlayer;
        public int     TargetID;
        public float   Distance;
    }

    // ── Daño ──────────────────────────────────────────────────

    /// <summary>Daño calculado y aplicado.</summary>
    public struct OnDamageDealtEvent
    {
        public int    SourceID;
        public int    TargetID;
        public float  Amount;
        public bool   IsCritical;
        public string WeaponID;
        public Vector3 HitPoint;
    }

    /// <summary>Este jugador recibió daño.</summary>
    public struct OnDamageReceivedEvent
    {
        public int    VictimID;
        public int    AttackerID;
        public float  Amount;
        public float  RemainingHealth;
    }

    // ── Munición ──────────────────────────────────────────────

    /// <summary>La munición del cargador cambió.</summary>
    public struct OnAmmoChangedEvent
    {
        public int    OwnerID;
        public string WeaponID;
        public int    Current;
        public int    Max;
        public int    Reserve;
    }

    /// <summary>Sin munición al intentar disparar.</summary>
    public struct OnAmmoEmptyEvent
    {
        public int    OwnerID;
        public string WeaponID;
    }

    // ── Recarga ───────────────────────────────────────────────

    public struct OnReloadStartEvent
    {
        public int    OwnerID;
        public string WeaponID;
        public float  Duration;
    }

    public struct OnReloadCompleteEvent
    {
        public int    OwnerID;
        public string WeaponID;
        public int    NewAmmo;
    }

    public struct OnReloadCancelledEvent
    {
        public int    OwnerID;
        public string WeaponID;
    }

    // ── Armas ─────────────────────────────────────────────────

    public struct OnWeaponEquippedEvent
    {
        public int    OwnerID;
        public string WeaponID;
        public int    Slot;
    }

    public struct OnWeaponUnequippedEvent
    {
        public int    OwnerID;
        public string WeaponID;
    }

    public struct OnWeaponSwitchedEvent
    {
        public int    OwnerID;
        public string PreviousWeaponID;
        public string NewWeaponID;
        public int    NewSlot;
    }

    // ── Recoil ────────────────────────────────────────────────

    /// <summary>Aplica recoil a la cámara del propietario.</summary>
    public struct OnRecoilEvent
    {
        public int   OwnerID;
        public float PitchDelta;
        public float YawDelta;
    }
}
