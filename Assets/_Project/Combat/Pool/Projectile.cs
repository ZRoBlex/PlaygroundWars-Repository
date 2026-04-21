// ============================================================
//  Projectile.cs
//  Combat/Pool/Projectile.cs
//
//  RESPONSABILIDAD ÚNICA: Comportamiento de un proyectil en vuelo.
//
//  CARACTERÍSTICAS:
//  • Movimiento con velocidad + gravedad propias (sin Rigidbody)
//  • SphereCast continuo para detección a alta velocidad
//    (previene tuneling — el proyectil no "atraviesa" objetos)
//  • Colisión via OnCollisionEnter como fallback
//  • Al impactar → notifica HitDetectionSystem → retorna al pool
//  • Al expirar (lifetime) → retorna al pool
//  • NUNCA usa Instantiate/Destroy para su ciclo de vida
// ============================================================

using Combat.Systems;
using Core.Debug;
using UnityEngine;

namespace Combat.Pool
{
    [RequireComponent(typeof(Collider))]
    [DisallowMultipleComponent]
    public class Projectile : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Detección")]
        [SerializeField] private LayerMask _collisionMask = ~0;
        [SerializeField] private float     _castRadius    = 0.05f;

        [Tooltip("SphereCast en lugar de colisión nativa (mejor para alta velocidad).")]
        [SerializeField] private bool _useContinuousCast = true;

        // ── Estado ────────────────────────────────────────────

        public int  ID       { get; private set; }
        public bool IsActive { get; private set; }

        private WeaponConfig              _config;
        private int                       _shooterID;
        private Vector3                   _velocity;
        private float                     _lifeTimer;
        private Vector3                   _prevPosition;
        private System.Action<Projectile> _onReturn;

        // ── Inicialización por Pool ───────────────────────────

        public void Initialize(
            int                       id,
            WeaponConfig              config,
            int                       shooterID,
            Vector3                   position,
            Vector3                   direction,
            System.Action<Projectile> onReturn)
        {
            ID            = id;
            _config       = config;
            _shooterID    = shooterID;
            _onReturn     = onReturn;

            transform.position = position;
            transform.rotation = Quaternion.LookRotation(direction);

            _velocity     = direction.normalized * config.ProjectileSpeed;
            _lifeTimer    = config.ProjectileLifetime;
            _prevPosition = position;
            IsActive      = true;

            gameObject.SetActive(true);
        }

        // ── Update ────────────────────────────────────────────

        private void Update()
        {
            if (!IsActive) return;

            float dt = Time.deltaTime;

            // Gravedad escalable
            _velocity.y -= Physics.gravity.magnitude * _config.ProjectileGravityScale() * dt;

            Vector3 delta = _velocity * dt;

            // Detección continua (anti-tuneling)
            if (_useContinuousCast)
            {
                if (Physics.SphereCast(
                        _prevPosition, _castRadius,
                        _velocity.normalized,
                        out RaycastHit hit,
                        delta.magnitude,
                        _collisionMask,
                        QueryTriggerInteraction.Ignore))
                {
                    HandleImpact(hit.collider, hit.point, hit.normal);
                    return;
                }
            }

            transform.position += delta;
            transform.rotation  = Quaternion.LookRotation(_velocity);
            _prevPosition       = transform.position;

            // Expiración por lifetime
            _lifeTimer -= dt;
            if (_lifeTimer <= 0f)
            {
                CoreLogger.LogSystemDebug("Projectile",
                    $"[{ID}] Expirado sin impacto.");
                ReturnToPool();
            }
        }

        // ── Colisión nativa (fallback) ────────────────────────

        private void OnCollisionEnter(Collision col)
        {
            if (!IsActive) return;
            var contact = col.GetContact(0);
            HandleImpact(col.collider, contact.point, contact.normal);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsActive) return;
            HandleImpact(other, transform.position, -_velocity.normalized);
        }

        // ── Impacto ───────────────────────────────────────────

        private void HandleImpact(Collider hitCol, Vector3 point, Vector3 normal)
        {
            if (!IsActive) return;

            HitDetectionSystem_Fixed.ProcessProjectileImpact(
                _config, _shooterID, hitCol, point, normal);

            ReturnToPool();
        }

        // ── Pool ──────────────────────────────────────────────

        private void ReturnToPool()
        {
            IsActive = false;
            gameObject.SetActive(false);
            _onReturn?.Invoke(this);
        }

        /// <summary>Retorno forzado (limpieza de escena, entre rondas).</summary>
        public void ForceReturn()
        {
            IsActive = false;
            gameObject.SetActive(false);
        }
    }

    // ── Extensión de WeaponConfig ─────────────────────────────

    public static class WeaponConfigProjectileExtensions
    {
        public static float ProjectileGravityScale(this WeaponConfig cfg)
            => cfg.ProjectileGravity;
    }
}
