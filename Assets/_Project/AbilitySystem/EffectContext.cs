using System.Collections.Generic;
using UnityEngine;

namespace AbilitySystem
{
    public struct EffectContext
    {
        public int SourceID;
        public GameObject SourceObject;

        public List<GameObject> Targets;

        public Vector3 Origin;
        public Vector3 Direction;
    }
}