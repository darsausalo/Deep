using Deep.Gameplay.Common;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Deep.Authoring.Common
{
    [DisallowMultipleComponent]
    public class ConstantRotationAuthoring : MonoBehaviour
    {
        public float RotationSpeed;

        public class Baker : Baker<ConstantRotationAuthoring>
        {
            public override void Bake(ConstantRotationAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new ConstantRotation { RotationSpeed = new float3(0, math.radians(authoring.RotationSpeed), 0) });
            }
        }
    }
}
