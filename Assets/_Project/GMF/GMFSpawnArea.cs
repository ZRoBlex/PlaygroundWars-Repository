// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: GMF_SpawnArea.cs                               ║
// ║  CLASE ÚNICA                                             ║
// ║                                                          ║
// ║  RESPONSABILIDAD:                                        ║
// ║    Zona de reaparición por equipo.                       ║
// ║    Encuentra posiciones libres (sin colisión) dentro     ║
// ║    del área para spawnear jugadores sin solapamiento.    ║
// ║                                                          ║
// ║  AÑADIR: Un GameObject por equipo en la escena          ║
// ║  SETUP:  BoxCollider → isTrigger = true                  ║
// ║          TeamID = mismo que el equipo                    ║
// ╚══════════════════════════════════════════════════════════╝

using System.Collections.Generic;
using Core.Debug;
using UnityEngine;

namespace GMF
{
    [RequireComponent(typeof(BoxCollider))]
    [DisallowMultipleComponent]
    public class GMFSpawnArea : MonoBehaviour
    {
        // ── Inspector ─────────────────────────────────────────

        [Header("Equipo")]
        [Tooltip("TeamID al que pertenece esta zona (0=Red, 1=Blue, etc.)")]
        [SerializeField] public int TeamID = 0;

        [Header("Colocación libre de colisiones")]
        [Tooltip("Radio de la cápsula de verificación de espacio libre.")]
        [SerializeField] private float _checkRadius     = 0.4f;
        [Tooltip("Altura de la cápsula (usa la altura del CharacterController).")]
        [SerializeField] private float _checkHeight     = 1.8f;
        [Tooltip("LayerMask de objetos que bloquean el spawn.")]
        [SerializeField] private LayerMask _blockingMask = ~0;
        [Tooltip("Intentos máximos antes de usar el centro del área.")]
        [SerializeField] private int _maxAttempts       = 30;

        [Header("Debug")]
        [SerializeField] private bool _drawGizmos = true;

        // ── Referencias ───────────────────────────────────────

        private BoxCollider _box;

        // ── Posiciones reservadas en el frame actual ──────────

        private readonly List<Vector3> _reservedPositions = new();

        // ── Lifecycle ─────────────────────────────────────────

        private void Awake()
        {
            _box = GetComponent<BoxCollider>();
            _box.isTrigger = true;
        }

        // ── API Pública ───────────────────────────────────────

        /// <summary>
        /// Devuelve una posición libre dentro del área para spawnear.
        /// Llama a ClearReservations() antes de un batch de spawns si quieres
        /// que los jugadores no aparezcan en el mismo punto.
        /// </summary>
        public bool TryGetSpawnPosition(out Vector3 position)
        {
            Vector3 center = transform.position;
            Vector3 size   = Vector3.Scale(_box.size, transform.lossyScale);
            float   halfH  = _checkHeight * 0.5f;

            for (int i = 0; i < _maxAttempts; i++)
            {
                // Punto aleatorio dentro del volumen del BoxCollider
                Vector3 candidate = center + new Vector3(
                    Random.Range(-size.x * 0.5f, size.x * 0.5f),
                    0f,
                    Random.Range(-size.z * 0.5f, size.z * 0.5f)
                );

                // Elevar el punto para que el jugador esté de pie
                candidate.y = center.y + _checkHeight * 0.5f;

                // ¿Hay espacio libre?
                bool overlaps = Physics.CheckCapsule(
                    candidate + Vector3.down  * (halfH - _checkRadius),
                    candidate + Vector3.up    * (halfH - _checkRadius),
                    _checkRadius,
                    _blockingMask,
                    QueryTriggerInteraction.Ignore);

                if (overlaps) continue;

                // ¿Está cerca de una posición reservada en este frame?
                bool tooClose = false;
                foreach (var res in _reservedPositions)
                {
                    if (Vector3.Distance(candidate, res) < _checkRadius * 2.5f)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                // ✅ Posición válida
                _reservedPositions.Add(candidate);
                position = candidate;
                return true;
            }

            // Fallback: centro del área (puede solaparse)
            CoreLogger.LogWarning(
                $"[SpawnArea T{TeamID}] No encontró posición libre en {_maxAttempts} intentos. " +
                "Usando el centro.");
            position = center + Vector3.up * _checkHeight;
            return false;
        }

        /// <summary>Limpia las posiciones reservadas al inicio de un batch.</summary>
        public void ClearReservations() => _reservedPositions.Clear();

        // ── Gizmos ────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            if (!_drawGizmos) return;

            if (_box == null) _box = GetComponent<BoxCollider>();
            if (_box == null) return;

            Color c = TeamID == 0 ? new Color(1f, 0.2f, 0.2f, 0.25f)
                    : TeamID == 1 ? new Color(0.2f, 0.4f, 1f, 0.25f)
                    : TeamID == 2 ? new Color(0.2f, 1f, 0.2f, 0.25f)
                    : new Color(1f, 1f, 0.2f, 0.25f);

            Gizmos.color = c;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawCube(_box.center, _box.size);

            c.a = 0.8f;
            Gizmos.color = c;
            Gizmos.DrawWireCube(_box.center, _box.size);
            Gizmos.matrix = Matrix4x4.identity;

            // Label
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f,
                $"SPAWN T{TeamID}", new GUIStyle
                {
                    normal  = { textColor = Color.white },
                    fontSize = 12,
                    fontStyle = FontStyle.Bold
                });
#endif
        }
    }
}