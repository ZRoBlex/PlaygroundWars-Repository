// ╔══════════════════════════════════════════════════════════╗
// ║  ARCHIVO: CharacterEvents.cs                             ║
// ║  CARPETA: Assets/_Project/CharacterSystem/Events/        ║
// ╚══════════════════════════════════════════════════════════╝

namespace CharacterSystem
{
    public struct OnCharacterSelectedEvt
    {
        public int    PlayerID;
        public string CharacterID;
        public string PreviousCharacterID;
    }

    public struct OnCharacterInitializedEvt
    {
        public int    PlayerID;
        public string CharacterID;
    }

    public struct OnStatsChangedEvt
    {
        public int    OwnerID;
        public string StatID;
        public float  OldValue;
        public float  NewValue;
    }

    public struct OnModifierAppliedEvt
    {
        public int    OwnerID;
        public string StatID;
        public string ModifierID;
        public float  Value;
        public float  Duration;
    }

    public struct OnModifierRemovedEvt
    {
        public int    OwnerID;
        public string StatID;
        public string ModifierID;
    }

    public struct OnLevelUpEvt
    {
        public int PlayerID;
        public int OldLevel;
        public int NewLevel;
        public int TotalXP;
    }

    public struct OnXPGainedEvt
    {
        public int    PlayerID;
        public int    Amount;
        public string Reason;
        public int    TotalXP;
    }

    public struct OnSkinChangedEvt
    {
        public int    PlayerID;
        public string CharacterID;
        public string SkinID;
    }
}