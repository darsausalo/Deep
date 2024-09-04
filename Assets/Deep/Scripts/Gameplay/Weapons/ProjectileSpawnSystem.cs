using Deep.Gameplay.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Deep.Gameplay.Weapons
{
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct ProjectileSpawnSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder()
                .WithAll<WeaponProjectile>()
                .Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new ProjectileSpawnJob
            {
                ECB = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged),
                LocalToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct ProjectileSpawnJob : IJobEntity
        {
            public EntityCommandBuffer ECB;
            [ReadOnly] public ComponentLookup<LocalToWorld> LocalToWorldLookup;

            private void Execute(
                Entity entity,
                in WeaponProjectile weaponProjectile,
                ref DynamicBuffer<ProjectileShot> projectileShots)
            {
                for (var i = 0; i < projectileShots.Length; i++)
                {
                    var projectileShotData = projectileShots[i].Data;
                    if (LocalToWorldLookup.TryGetComponent(projectileShotData.OriginEntity, out var originLocalToWorld))
                    {
                        var visualOrigin = originLocalToWorld.Position;
                        var simulationHitOrigin = projectileShotData.SimulationOrigin +
                            projectileShotData.SimulationDirection * projectileShotData.HitDistance;
                        var visualDirection = math.normalizesafe(simulationHitOrigin - visualOrigin);

                        var projectileEntity = ECB.Instantiate(weaponProjectile.Prefab);
                        ECB.SetComponent(projectileEntity, LocalTransform.FromPositionRotation(
                            visualOrigin,
                            quaternion.LookRotationSafe(visualDirection, projectileShotData.Up)));
                        ECB.AddComponent(projectileEntity, projectileShotData);
                        if (weaponProjectile.Speed > 0f)
                        {
                            ECB.AddComponent(projectileEntity, new LinearMovement
                            {
                                Velocity = visualDirection * weaponProjectile.Speed
                            });
                        }
                    }
                }
                projectileShots.Clear();
            }
        }
    }
}
