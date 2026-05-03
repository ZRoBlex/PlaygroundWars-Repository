using System.Collections.Generic;
using UnityEngine;

namespace AbilitySystem
{
    [CreateAssetMenu(menuName = "Ability System/Ability Definition")]
    public class AbilityDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string AbilityID;
        public string DisplayName;

        [Header("Core")]
        public float Cooldown = 1f;
        public float Cost = 0f;

        [Header("Targeting")]
        public TargetType TargetType;
        public float Range = 10f;
        public float Radius = 3f;

        [Header("Effects")]
        public List<EffectDefinition> Effects = new();

        [Header("Behavior Flags")]
        public bool RequiresLineOfSight = false;
        public bool CanBeCancelled = false;
        public bool IsChanneled = false;
    }

    public enum TargetType
    {
        Self,
        Target,
        Area,
        Direction
    }
}