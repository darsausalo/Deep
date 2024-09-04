using Unity.Entities;
using Unity.Mathematics;

namespace Deep.Gameplay.Common
{
    public struct InterpolationDelay : IComponentData
    {
        public uint Value;
    }

    public struct Lifetime : IComponentData
    {
        public float Value;
    }

    public struct LinearMovement : IComponentData
    {
        public float3 Velocity;
    }

    public struct SpawnPointElement : IBufferElementData
    {
        public float3 Value;
    }

    public struct ConstantRotation : IComponentData
    {
        public float3 RotationSpeed;
    }
}
