// ============================================================
//  WeaponConfig.cs
//  Combat/Config/WeaponConfig.cs
//
//  ScriptableObject con todos los datos de un arma.
//  Un asset por arma. Cero hardcoding.
//
//  CREAR: Assets → Right Click → Create → Combat → WeaponConfig
// ============================================================

using UnityEngine;

namespace Combat
{
    public enum ShootingType { HitScan, Projectile, Continuous }

    [CreateAssetMenu(fileName = "NewWeapon", menuName = "Combat/WeaponConfig", order = 0)]
    public class WeaponConfig : ScriptableObject
    {
        // ── Identidad ─────────────────────────────────────────

        [Header("Identidad")]
        [Tooltip("ID único. Usado en eventos y sistemas. Ej: 'pistol_01'")]
        public string WeaponID      = "weapon_default";
        public string DisplayName   = "Arma";

        // ── Tipo ──────────────────────────────────────────────

        [Header("Tipo de Disparo")]
        public ShootingType ShootingType  = ShootingType.HitScan;
        public bool         IsAutomatic   = false;

        // ── Daño ──────────────────────────────────────────────

        [Header("Daño")]
        [Range(1f, 500f)]
        public float BaseDamage           = 25f;

        [Range(1f, 5f)]
        public float HeadshotMultiplier   = 2f;

        [Range(0f, 200f)]
        public float DamageFalloffStart   = 20f;

        [Range(0f, 300f)]
        public float DamageFalloffEnd     = 80f;

        [Range(0f, 1f)]
        public float MinDamageFraction    = 0.1f;

        // ── Cadencia ──────────────────────────────────────────

        [Header("Cadencia")]
        [Range(0.05f, 5f)]
        [Tooltip("Tiempo mínimo entre disparos en segundos.")]
        public float FireRate             = 0.15f;

        // ── Munición ──────────────────────────────────────────

        [Header("Munición")]
        [Range(1, 200)]
        public int  MagazineSize          = 30;

        [Range(0, 500)]
        public int  MaxReserveAmmo        = 120;

        public bool InfiniteReserve       = false;

        // ── Recarga ───────────────────────────────────────────

        [Header("Recarga")]
        [Range(0.5f, 6f)]
        public float ReloadTime           = 2f;

        public bool AutoReload            = true;

        // ── HitScan ───────────────────────────────────────────

        [Header("HitScan")]
        [Range(1f, 500f)]
        public float    MaxRange          = 100f;
        public LayerMask HitLayers        = ~0;

        [Range(1, 12)]
        public int  PelletsPerShot        = 1;

        [Range(0f, 15f)]
        public float SpreadAngle          = 0f;

        // ── Proyectil ─────────────────────────────────────────

        [Header("Proyectil")]
        [Range(1f, 150f)]
        public float    ProjectileSpeed   = 20f;

        [Range(0f, 5f)]
        public float    ProjectileGravity = 1f;

        [Range(0.5f, 30f)]
        public float    ProjectileLifetime = 5f;

        public GameObject ProjectilePrefab;

        // ── Continuo ──────────────────────────────────────────

        [Header("Continuo (agua / láser)")]
        [Range(1f, 50f)]
        public float DamagePerSecond      = 15f;

        [Range(1f, 50f)]
        public float ContinuousRange      = 15f;

        // ── Recoil ────────────────────────────────────────────

        [Header("Recoil")]
        [Range(0f, 8f)]
        public float  RecoilPitch         = 1f;

        [Range(0f, 4f)]
        public float  RecoilYawVariance   = 0.5f;

        [Range(0f, 1f)]
        public float  RecoilRecoveryRate  = 0.6f;

        [Tooltip("Patrón fijo (X=yaw, Y=pitch por disparo). Vacío = aleatorio.")]
        public Vector2[] RecoilPattern;

        // ── Red ───────────────────────────────────────────────

        [Header("Red")]
        public bool UseNetworking         = false;

        // ── Helpers ───────────────────────────────────────────

        /// <summary>Daño final con falloff por distancia y headshot.</summary>
        public float CalculateDamage(float distance, bool isHeadshot = false)
        {
            float dmg = BaseDamage;

            if (distance > DamageFalloffStart && DamageFalloffEnd > DamageFalloffStart)
            {
                float t   = Mathf.InverseLerp(DamageFalloffStart, DamageFalloffEnd, distance);
                float min = BaseDamage * MinDamageFraction;
                dmg       = Mathf.Lerp(BaseDamage, min, t);
            }

            if (isHeadshot) dmg *= HeadshotMultiplier;
            return Mathf.Max(1f, dmg);
        }
    }
}
