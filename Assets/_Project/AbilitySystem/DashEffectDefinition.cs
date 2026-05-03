using UnityEngine;

namespace AbilitySystem
{
    [CreateAssetMenu(menuName = "Ability System/Effects/Dash")]
    public class DashEffectDefinition : EffectDefinition
    {
        public float Distance = 5f;

        public override void Execute(EffectContext context)
        {
            var owner = context.SourceObject;
            if (owner == null) return;

            var controller = owner.GetComponent<CharacterController>();
            if (controller == null)
            {
                Debug.LogWarning("[DashEffect] No CharacterController");
                return;
            }

            Vector3 dir = context.Direction.normalized;

            if (dir == Vector3.zero)
                dir = owner.transform.forward;

            controller.Move(dir * Distance);
        }
    }
}