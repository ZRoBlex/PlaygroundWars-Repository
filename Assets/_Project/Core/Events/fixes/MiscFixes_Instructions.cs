// ============================================================
//  MiscFixes.cs
//  Fixes/MiscFixes.cs
//
//  ERRORES ADICIONALES QUE CORRIGE:
//
//  ─────────────────────────────────────────────────────────
//  FIX A: PlayerController.cs línea 190
//  Error: 'ApplyDamageRequestEvent' is ambiguous
//
//  SOLUCIÓN: Buscar en PlayerController.cs la línea:
//
//      EventBus<ApplyDamageRequestEvent>.Raise(new ApplyDamageRequestEvent
//      {
//          TargetPlayerID = ...  ← campo viejo
//          SourcePlayerID = ...  ← campo viejo
//          Amount         = ...  ← campo viejo
//      });
//
//  REEMPLAZAR con (campos canónicos):
//
//      EventBus<Core.Events.ApplyDamageRequestEvent>.Raise(
//          new Core.Events.ApplyDamageRequestEvent
//          {
//              TargetID   = targetPlayerID,
//              AttackerID = sourcePlayerID,
//              Damage     = amount,
//              HitPoint   = hitPoint,
//              HitNormal  = hitNormal
//          });
//
//  ─────────────────────────────────────────────────────────
//  FIX B: DamageSystem.cs línea 66
//  Error: 'ApplyDamageRequestEvent' is ambiguous
//
//  SOLUCIÓN: Añadir al principio de DamageSystem.cs:
//      using Core.Events;
//  Y usar campos canónicos (TargetID, AttackerID, Damage).
//
//  ─────────────────────────────────────────────────────────
//  FIX C: CaptureTheFlagMode.Awake() override
//  Error: 'CaptureTheFlagMode.Awake()': no suitable method to override
//
//  CAUSA: GameModeBase no declara 'protected virtual void Awake()'
//  SOLUCIÓN: Dos opciones (elige una):
//
//  Opción 1 — Cambiar 'protected override void Awake()' por
//             simplemente 'protected override void Awake()' NO funciona
//             porque Unity gestiona Awake especialmente.
//
//             Usar 'private void Awake()' o mejor 'protected virtual void Awake()'
//             en GameModeBase, como se muestra abajo.
//
//  Opción 2 — En CaptureTheFlagMode: cambiar
//             'protected override void Awake()' por
//             'private new void Awake()'
//             y llamar a la inicialización base manualmente.
//  ─────────────────────────────────────────────────────────

// Este archivo es SOLO documentación/instrucciones.
// No contiene código compilable por sí solo.
// Sigue las instrucciones en los comentarios anteriores.
