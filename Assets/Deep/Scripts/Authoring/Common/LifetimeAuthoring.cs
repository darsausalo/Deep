using Deep.Gameplay.Common;
using Unity.Entities;
using UnityEngine;

namespace Deep.Authoring.Common
{
    public class LifetimeAuthoring : MonoBehaviour
    {
        public float Lifetime;

        public class Baker : Baker<LifetimeAuthoring>
        {
            public override void Bake(LifetimeAuthoring authoring)
            {
                AddComponent(GetEntity(TransformUsageFlags.Dynamic), new Lifetime
                {
                    Value = authoring.Lifetime
                });
            }
        }
    }
}
