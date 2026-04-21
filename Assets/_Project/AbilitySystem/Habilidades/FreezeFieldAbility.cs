using Abilities;
using UnityEngine;

public class FreezeFieldAbility : AbilityBase
{
     protected override void OnActivate(AbilityTarget target)
    {
        // Los efectos ya los aplica AbilityEffectSystem en TryActivate()
        // Aquí solo añadir lógica extra: partículas, sonido, etc.
    }
}
