using UnityEngine;
using Core.Events;

// ════════════════════════════════════════════════════════
    //  ServerDamageProcessor — LA ÚNICA AUTORIDAD DE DAÑO
    // ════════════════════════════════════════════════════════
 
    /// <summary>
    /// Componente que procesa ApplyDamageRequestEvent con autoridad de servidor.
    ///
    /// OFFLINE:     Añadir al mismo GO que el GameManager. Siempre procesa.
    /// HOST:        Añadir al host. Solo el host valida y aplica daño.
    /// CLIENT:      NO añadir a clientes. Reciben DamageAppliedEvent via red.
    /// DEDICATED:   Añadir al servidor dedicado.
    ///
    /// SETUP: 1 instancia en escena por sesión.
    /// </summary>
    public class ServerDamageProcessor : MonoBehaviour
    {
        [Header("Modo de Red")]
        [SerializeField] private bool _isAuthority = true;   // false en clientes puros
 
        [Header("Anti-Cheat básico")]
        [SerializeField] private float _maxDamagePerShot = 500f;
        [SerializeField] private float _maxRangeBonus    = 10f;  // Margen sobre MaxRange del config
 
        private void OnEnable()
        {
            EventBus<ApplyDamageRequestEvent>.Subscribe(OnDamageRequested);
        }
 
        private void OnDisable()
        {
            EventBus<ApplyDamageRequestEvent>.Unsubscribe(OnDamageRequested);
        }
 
        private void OnDamageRequested(ApplyDamageRequestEvent req)
        {
            // ✅ Solo la autoridad procesa
            if (!_isAuthority) return;
 
            // ── Validaciones anti-cheat ────────────────────────
 
            // 1. El daño no puede exceder el máximo permitido
            if (req.Damage > _maxDamagePerShot)
            {
                // CoreLogger.LogWarning(
                //     $"[ServerDmg] ANTI-CHEAT: Daño excesivo {req.Damage:F0} " +
                //     $"de P{req.AttackerID}. Rechazado.");
 
                EventBus<DamageRequestRejectedEvent>.Raise(new DamageRequestRejectedEvent
                {
                    AttackerID = req.AttackerID,
                    TargetID   = req.TargetID,
                    Reason     = "AntiCheat_ExcessiveDamage"
                });
                return;
            }
 
            // 2. El target debe ser válido (no muerto ya, no mismo equipo en CTF, etc.)
            // TODO: integrar con TeamManager para friendly fire
            if (req.TargetID < 0)
            {
                EventBus<DamageRequestRejectedEvent>.Raise(new DamageRequestRejectedEvent
                {
                    AttackerID = req.AttackerID,
                    TargetID   = req.TargetID,
                    Reason     = "InvalidTarget"
                });
                return;
            }
 
            // ── Cálculo de daño real ───────────────────────────
 
            float finalDamage = req.Damage;
 
            // Headshot multiplier (el config del arma lo define, no el cliente)
            if (req.IsHeadshot)
                finalDamage *= GetHeadshotMult(req.WeaponID);
 
            // Falloff por distancia
            finalDamage = ApplyFalloff(finalDamage, req.Distance, req.WeaponID);
            finalDamage = Mathf.Max(1f, Mathf.Round(finalDamage));
 
            // CoreLogger.LogSystemDebug("ServerDmg",
            //     $"P{req.AttackerID}→P{req.TargetID}: {req.Damage:F0}→{finalDamage:F0}dmg " +
            //     $"head={req.IsHeadshot}");
 
            // ── Aplicar daño al target ─────────────────────────
 
            // Publicar DamageAppliedEvent → PlayerHealth lo recibe y reduce HP
            EventBus<DamageAppliedEvent>.Raise(new DamageAppliedEvent
            {
                AttackerID      = req.AttackerID,
                TargetID        = req.TargetID,
                FinalDamage     = finalDamage,
                RemainingHealth = 0f,   // PlayerHealth actualiza esto al procesar
                WasLethal       = false,
                HitPoint        = req.HitPoint,
                WeaponID        = req.WeaponID
            });
        }
 
        // ── Helpers (en producción: leer de WeaponConfigRegistry) ─
 
        private float GetHeadshotMult(string weaponID) => 2f;     // Expandir con registro
 
        private float ApplyFalloff(float dmg, float dist, string weaponID)
            => dmg;  // Expandir: buscar WeaponConfig en registry por weaponID
    }
