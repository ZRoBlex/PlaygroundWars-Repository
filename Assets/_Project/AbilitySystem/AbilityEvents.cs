// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: AbilityEvents.cs                               ║
// ║  CARPETA: Assets/_Project/AbilitySystem/Events/          ║
// ║  CLASE ÚNICA — no separar                                ║
// ║                                                          ║
// ║  ESTE ARCHIVO RESUELVE:                                  ║
// ║    • CS0246 'PlayerAbilityInputEvt' not found            ║
// ║    • CS0246 'OnCooldownEndEvt' not found                 ║
// ║    • CS0246 'OnCooldownStartEvt' not found               ║
// ║    • CS0246 'OnCooldownTickEvt' not found                ║
// ║    • CS0246 'OnAbilityActivatedEvt' not found            ║
// ║    • CS0246 'OnAbilityFailedEvt' not found               ║
// ║    • CS0246 'OnEffectAppliedEvt' not found               ║
// ║    • CS0246 'OnEffectRemovedEvt' not found               ║
// ║    • CS0246 'OnEffectStackedEvt' not found               ║
// ╚══════════════════════════════════════════════════════════╝

using UnityEngine;

namespace AbilitySystem
{
    // ── Habilidades ───────────────────────────────────────────

    public struct OnAbilityActivatedEvt
    {
        public int     OwnerID;
        public string  AbilityID;
        public int     TargetID;
        public Vector3 Position;
    }

    public struct OnAbilityFailedEvt
    {
        public int    OwnerID;
        public string AbilityID;
        public string Reason;       // "OnCooldown","ConditionFailed","NoTarget"
    }

    // ── Cooldown ──────────────────────────────────────────────

    public struct OnCooldownStartEvt
    {
        public int    OwnerID;
        public string AbilityID;
        public float  Duration;
    }

    public struct OnCooldownEndEvt
    {
        public int    OwnerID;
        public string AbilityID;
    }

    public struct OnCooldownTickEvt
    {
        public int    OwnerID;
        public string AbilityID;
        public float  Remaining;
        public float  Total;
    }

    // ── Status Effects ────────────────────────────────────────

    public struct OnEffectAppliedEvt
    {
        public int    TargetID;
        public string EffectID;
        public int    SourceOwnerID;
        public float  Duration;
        public float  Intensity;
        public int    StackCount;
    }

    public struct OnEffectRemovedEvt
    {
        public int    TargetID;
        public string EffectID;
        public bool   Expired;
    }

    public struct OnEffectStackedEvt
    {
        public int    TargetID;
        public string EffectID;
        public int    NewStacks;
    }

    // ── Input ─────────────────────────────────────────────────

    /// <summary>
    /// Publicado por PlayerInput al presionar una tecla de habilidad.
    /// AbilityManager escucha este evento y activa el slot.
    /// Añadir a PlayerInput.cs:
    ///   if (Input.GetKeyDown(KeyCode.Q))
    ///       EventBus&lt;PlayerAbilityInputEvt&gt;.Raise(new PlayerAbilityInputEvt
    ///           { PlayerID = _authority.PlayerID, SlotIndex = 0 });
    /// </summary>
    public struct PlayerAbilityInputEvt
    {
        public int PlayerID;
        public int SlotIndex;   // 0=Q, 1=E, 2=R, 3=F, etc.
    }

    // ── Networking ────────────────────────────────────────────

    public struct RequestActivateAbilityEvt
    {
        public int    RequesterID;
        public string AbilityID;
        public float  Timestamp;
    }

    // ── Manager ───────────────────────────────────────────────

    public struct OnAbilityAddedEvt
    {
        public int    OwnerID;
        public string AbilityID;
        public int    SlotIndex;
    }

    public struct OnAbilityRemovedEvt
    {
        public int    OwnerID;
        public string AbilityID;
    }
}