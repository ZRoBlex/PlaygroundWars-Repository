using System.Collections.Generic;
using UnityEngine;

namespace AbilitySystem
{
    public struct AbilityTargetData
    {
        public List<GameObject> Targets;
        public Vector3 Position;
        public Vector3 Direction;
    }

    public static class AbilityTargetingSystem
    {
        public static AbilityTargetData ResolveTargets(AbilityInstance instance)
        {
            AbilityDefinition def = instance.Definition;

            AbilityTargetData data = new AbilityTargetData
            {
                Targets = new List<GameObject>(),
                Position = Vector3.zero,
                Direction = Vector3.forward
            };

            GameObject ownerObj = GetOwnerObject(instance.OwnerID);

            if (ownerObj == null)
                return data;

            data.Position = ownerObj.transform.position;
            data.Direction = ownerObj.transform.forward;

            switch (def.TargetType)
            {
                case TargetType.Self:
                    data.Targets.Add(ownerObj);
                    break;

                case TargetType.Direction:
                    data.Direction = ownerObj.transform.forward;
                    break;

                case TargetType.Area:
                    Collider[] hits = Physics.OverlapSphere(
                        ownerObj.transform.position,
                        def.Radius // ✅ CORREGIDO
                    );

                    foreach (var hit in hits)
                        data.Targets.Add(hit.gameObject);

                    break;

                case TargetType.Target:
                    // Placeholder simple (luego lo mejoras con raycast)
                    Ray ray = new Ray(ownerObj.transform.position, ownerObj.transform.forward);
                    if (Physics.Raycast(ray, out RaycastHit hitInfo, def.Range))
                    {
                        data.Targets.Add(hitInfo.collider.gameObject);
                    }
                    break;
            }

            return data;
        }

        // ⚠️ TEMPORAL (luego lo reemplazas por sistema real de entidades)
        private static GameObject GetOwnerObject(int ownerID)
        {
            var all = GameObject.FindObjectsOfType<Player.Authority.PlayerAuthority>();

            foreach (var p in all)
            {
                if (p.PlayerID == ownerID)
                    return p.gameObject;
            }

            return null;
        }
    }
}