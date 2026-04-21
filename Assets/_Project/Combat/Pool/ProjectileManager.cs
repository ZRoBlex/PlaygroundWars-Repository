// ============================================================
//  ProjectileManager.cs
//  Combat/Pool/ProjectileManager.cs
//
//  RESPONSABILIDAD ÚNICA: Object Pool de proyectiles.
//
//  REGLAS:
//  • Un pool por tipo de prefab (clave = InstanceID del prefab)
//  • Prewarm al inicio para evitar spike de primera partida
//  • Si el pool se agota, expande automáticamente
//  • Proyectiles inactivos en hijos organizados por tipo
//  • Singleton seguro con DontDestroyOnLoad
// ============================================================

using System.Collections.Generic;
using Core.Debug;
using UnityEngine;

namespace Combat.Pool
{
    [DisallowMultipleComponent]
    public class ProjectileManager : MonoBehaviour
    {
        // ── Singleton ─────────────────────────────────────────

        public static ProjectileManager Instance { get; private set; }

        // ── Inspector ─────────────────────────────────────────

        [Header("Pool Settings")]
        [Tooltip("Número de proyectiles por tipo al inicializar.")]
        [SerializeField] private int _defaultSize   = 20;

        [Tooltip("Proyectiles extra a crear cuando el pool se agota.")]
        [SerializeField] private int _expandAmount  = 5;

        // ── Datos de Pool ─────────────────────────────────────

        // key = prefab InstanceID
        private readonly Dictionary<int, Queue<Projectile>> _pools     = new();
        private readonly Dictionary<int, Transform>          _roots     = new();

        private int _nextID;

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Saca un proyectil del pool, lo inicializa y lo activa.
        /// </summary>
        public Projectile Spawn(
            WeaponConfig config,
            int          shooterID,
            Vector3      position,
            Vector3      direction)
        {
            if (config?.ProjectilePrefab == null)
            {
                CoreLogger.LogError("[ProjectileManager] WeaponConfig o ProjectilePrefab nulo.");
                return null;
            }

            int key = config.ProjectilePrefab.GetInstanceID();
            EnsurePool(config, key);

            Projectile proj = Dequeue(config, key);

            proj.Initialize(
                id:        _nextID++,
                config:    config,
                shooterID: shooterID,
                position:  position,
                direction: direction,
                onReturn:  p => Return(p, key)
            );

            return proj;
        }

        /// <summary>
        /// Pre-crea proyectiles para un arma (llamar al cargar el nivel).
        /// </summary>
        public void Prewarm(WeaponConfig config, int count = -1)
        {
            if (config?.ProjectilePrefab == null) return;

            int key    = config.ProjectilePrefab.GetInstanceID();
            int amount = count > 0 ? count : _defaultSize;

            EnsurePool(config, key);

            for (int i = 0; i < amount; i++)
                CreateOne(config, key);

            CoreLogger.LogSystemDebug("ProjectileManager",
                $"Prewarm: {amount}x '{config.ProjectilePrefab.name}'");
        }

        /// <summary>
        /// Retorna todos los proyectiles activos al pool (cambio de escena, fin de ronda).
        /// </summary>
        public void ReturnAll()
        {
            foreach (var root in _roots.Values)
            {
                if (root == null) continue;
                foreach (Transform child in root)
                    child.GetComponent<Projectile>()?.ForceReturn();
            }
            CoreLogger.LogSystemDebug("ProjectileManager", "Todos los proyectiles retornados.");
        }

        // ── Gestión interna ───────────────────────────────────

        private void EnsurePool(WeaponConfig config, int key)
        {
            if (_pools.ContainsKey(key)) return;

            _pools[key] = new Queue<Projectile>();

            var root = new GameObject($"[Pool] {config.ProjectilePrefab.name}");
            root.transform.SetParent(transform);
            _roots[key] = root.transform;
        }

        private Projectile Dequeue(WeaponConfig config, int key)
        {
            var pool = _pools[key];

            if (pool.Count == 0)
            {
                CoreLogger.LogSystemDebug("ProjectileManager",
                    $"Pool '{config.ProjectilePrefab.name}' agotado, expandiendo +{_expandAmount}");

                for (int i = 0; i < _expandAmount; i++)
                    CreateOne(config, key);
            }

            return pool.Dequeue();
        }

        private void Return(Projectile proj, int key)
        {
            if (!_pools.ContainsKey(key)) return;

            if (_roots.TryGetValue(key, out var root) && root != null)
                proj.transform.SetParent(root);

            _pools[key].Enqueue(proj);
        }

        private void CreateOne(WeaponConfig config, int key)
        {
            var go   = Instantiate(config.ProjectilePrefab, _roots[key]);
            var proj = go.GetComponent<Projectile>();

            if (proj == null)
            {
                CoreLogger.LogError(
                    $"[ProjectileManager] Prefab '{config.ProjectilePrefab.name}' " +
                    "no contiene el componente Projectile.cs");
                Destroy(go);
                return;
            }

            go.SetActive(false);
            _pools[key].Enqueue(proj);
        }
    }
}
