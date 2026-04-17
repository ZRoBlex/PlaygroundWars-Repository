// ============================================================
//  EventBus.cs
//  Core/Events/EventBus.cs
//
//  Bus de eventos genérico y type-safe.
//  Permite comunicación totalmente desacoplada entre sistemas.
//
//  CARACTERÍSTICAS:
//  • Genérico (un bus por tipo de evento struct)
//  • Thread-safe para subscribe/unsubscribe durante Raise()
//  • Captura excepciones individuales para no romper la cadena
//  • Sin allocations en el hot path (HashSet, sin LINQ)
//
//  USO:
//    Suscribirse   → EventBus<MyEvent>.Subscribe(OnMyEvent);
//    Desuscribirse → EventBus<MyEvent>.Unsubscribe(OnMyEvent);
//    Disparar      → EventBus<MyEvent>.Raise(new MyEvent { ... });
//
//  REGLA: Siempre desuscribirse en OnDestroy / OnDisable.
// ============================================================

using System;
using System.Collections.Generic;
using Core.Debug;

namespace Core.Events
{
    public static class EventBus<T> where T : struct
    {
        // Conjunto principal de listeners activos
        private static readonly HashSet<Action<T>> _listeners    = new();

        // Buffers para modificaciones seguras durante Raise()
        private static readonly HashSet<Action<T>> _pendingAdd    = new();
        private static readonly HashSet<Action<T>> _pendingRemove = new();

        private static bool _isRaising;

        // ── Suscripción ───────────────────────────────────────

        public static void Subscribe(Action<T> listener)
        {
            if (listener == null)
            {
                CoreLogger.LogWarning($"[EventBus<{typeof(T).Name}>] Intentando suscribir listener nulo.");
                return;
            }

            if (_isRaising)
                _pendingAdd.Add(listener);
            else
                _listeners.Add(listener);
        }

        public static void Unsubscribe(Action<T> listener)
        {
            if (listener == null) return;

            if (_isRaising)
                _pendingRemove.Add(listener);
            else
                _listeners.Remove(listener);
        }

        // ── Dispatch ──────────────────────────────────────────

        public static void Raise(T eventData)
        {
            _isRaising = true;

            foreach (var listener in _listeners)
            {
                try
                {
                    listener.Invoke(eventData);
                }
                catch (Exception e)
                {
                    // No interrumpir la cadena si un listener explota
                    CoreLogger.LogError(
                        $"[EventBus<{typeof(T).Name}>] Excepción en listener '{listener.Method.Name}': {e.Message}\n{e.StackTrace}"
                    );
                }
            }

            _isRaising = false;

            // Aplicar cambios pendientes después del ciclo
            foreach (var l in _pendingRemove) _listeners.Remove(l);
            foreach (var l in _pendingAdd)    _listeners.Add(l);
            _pendingAdd.Clear();
            _pendingRemove.Clear();
        }

        // ── Utilidades ────────────────────────────────────────

        /// <summary>
        /// Elimina todos los listeners. Llamar al hacer scene reload completo
        /// o en tests para limpiar estado global.
        /// </summary>
        public static void Clear()
        {
            _listeners.Clear();
            _pendingAdd.Clear();
            _pendingRemove.Clear();
            CoreLogger.LogDebug($"[EventBus<{typeof(T).Name}>] Limpiado.");
        }

        /// <summary>Cantidad de listeners activos. Útil para debug.</summary>
        public static int ListenerCount => _listeners.Count;
    }
}
