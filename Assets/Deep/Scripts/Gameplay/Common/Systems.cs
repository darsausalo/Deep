using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Deep.Gameplay.Common
{
    [BurstCompile]
    public partial struct LifetimeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new LifetimeJob
            {
                ECB = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged),
                DeltaTime = SystemAPI.Time.DeltaTime
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct LifetimeJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            public float DeltaTime;

            public void Execute(Entity entity, ref Lifetime lifetime)
            {
                lifetime.Value -= DeltaTime;
                if (lifetime.Value <= 0f)
                {
                    ECB.DestroyEntity(entity);
                }
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct LinearMovementSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder().WithAll<LinearMovement>().Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new LinearMovementJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        public partial struct LinearMovementJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(in LinearMovement movement, ref LocalTransform transform)
            {
                transform.Position += movement.Velocity * DeltaTime;
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct ConstantRotationSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (localTransform, constantRotation) in SystemAPI.Query<RefRW<LocalTransform>, ConstantRotation>())
            {
                localTransform.ValueRW.Rotation = math.mul(quaternion.Euler(constantRotation.RotationSpeed * SystemAPI.Time.DeltaTime), localTransform.ValueRO.Rotation);
            }
        }
    }
}
