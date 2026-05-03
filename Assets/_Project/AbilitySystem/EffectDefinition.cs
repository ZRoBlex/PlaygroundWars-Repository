using UnityEngine;

namespace AbilitySystem
{
    public abstract class EffectDefinition : ScriptableObject
    {
        public string EffectID;

        public abstract void Execute(EffectContext context);
    }
}