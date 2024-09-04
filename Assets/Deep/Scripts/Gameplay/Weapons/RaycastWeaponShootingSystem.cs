using Deep.Gameplay.Common;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Transforms;

namespace Deep.Gameplay.Weapons
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(WeaponPredictionUpdateGroup))]
    [UpdateAfter(typeof(WeaponFiringSystem))]
    [BurstCompile]
    public partial struct RaycastWeaponShootingSystem : ISystem
    {
        private NativeList<RaycastHit> _hits;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate(SystemAPI.QueryBuilder()
                .WithAll<RaycastWeapon, Weapon, WeaponFireState>()
                .Build());

            _hits = new NativeList<RaycastHit>(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_hits.IsCreated)
            {
                _hits.Dispose();
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new RaycastWeaponFiringJob
            {
                IsServer = state.WorldUnmanaged.IsServer(),
                NetworkTime = SystemAPI.GetSingleton<NetworkTime>(),
                PhysicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld,
                PhysicsWorldHistory = SystemAPI.GetSingleton<PhysicsWorldHistorySingleton>(),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true),
                Hits = _hits,
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(RaycastWeapon))]
        private partial struct RaycastWeaponFiringJob : IJobEntity
        {
            public bool IsServer;
            public NetworkTime NetworkTime;
            [ReadOnly] public PhysicsWorld PhysicsWorld;
            [ReadOnly] public PhysicsWorldHistorySingleton PhysicsWorldHistory;
            // TODO: public ComponentLookup<Health> HealthLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;
            public NativeList<RaycastHit> Hits;

            public void Execute(
                ref Weapon weapon,
                ref WeaponFeedback weaponFeedback,
                ref DynamicBuffer<ProjectileShot> projectileShotsBuffer,
                in InterpolationDelay interpolationDelay,
                in WeaponFireState fireState,
                in WeaponSimulationShotOrigin weaponSimulationShotOrigin,
                in DynamicBuffer<WeaponShotIgnoredEntity> weaponShotIgnoredEntities)
            {
                PhysicsWorldHistory.GetCollisionWorldFromTick(NetworkTime.ServerTick, interpolationDelay.Value,
                    ref PhysicsWorld, out var collisionWorld);
                var computeProjectileShots = !IsServer && NetworkTime.IsFirstTimeFullyPredictingTick;

                for (var i = 0; i < fireState.ShotsToFire; i++)
                {
                    WeaponUtilities.ComputeShot(
                      ref weapon,
                      ref LocalTransformLookup,
                      ref ParentLookup,
                      ref PostTransformMatrixLookup,
                      ref Hits,
                      in weaponSimulationShotOrigin,
                      in collisionWorld,
                      in weaponShotIgnoredEntities,
                      out var hitFound,
                      out var closestValidHit,
                      out var projectileShotData);

                    // Damage
                    if (IsServer && hitFound)
                    {
                        //if (HealthLookup.TryGetComponent(closestValidHit.Entity, out Health health))
                        //{
                        //    health.CurrentHealth -= weapon.Damage;
                        //    HealthLookup[closestValidHit.Entity] = health;
                        //}
                    }

                    if (computeProjectileShots)
                    {
                        projectileShotsBuffer.Add(new ProjectileShot { Data = projectileShotData });
                    }

                    if (IsServer)
                    {
                        weapon.RemoteShotsCount++;
                    }
                    else if (NetworkTime.IsFirstTimeFullyPredictingTick)
                    {
                        weaponFeedback.ShotFeedbackRequests++;
                    }
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct RaycastWeaponClientShootingSystem : ISystem
    {
        private NativeList<RaycastHit> _hits;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<NetworkTime>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate(SystemAPI.QueryBuilder()
                .WithAll<RaycastWeapon, Weapon, WeaponFireState>().Build());

            _hits = new NativeList<RaycastHit>(Allocator.Persistent);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (_hits.IsCreated)
            {
                _hits.Dispose();
            }
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new RaycastWeaponRemoteShotsJob
            {
                CollisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld,
                StoredKinematicCharacterDataLookup = SystemAPI.GetComponentLookup<StoredKinematicCharacterData>(true),
                Hits = _hits,
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
                ParentLookup = SystemAPI.GetComponentLookup<Parent>(true),
                PostTransformMatrixLookup = SystemAPI.GetComponentLookup<PostTransformMatrix>(true),
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithNone(typeof(GhostOwnerIsLocal))]
        [WithAll(typeof(RaycastWeapon))]
        public partial struct RaycastWeaponRemoteShotsJob : IJobEntity
        {
            [ReadOnly] public CollisionWorld CollisionWorld;
            [ReadOnly] public ComponentLookup<StoredKinematicCharacterData> StoredKinematicCharacterDataLookup;
            [ReadOnly] public ComponentLookup<LocalTransform> LocalTransformLookup;
            [ReadOnly] public ComponentLookup<Parent> ParentLookup;
            [ReadOnly] public ComponentLookup<PostTransformMatrix> PostTransformMatrixLookup;
            public NativeList<RaycastHit> Hits;

            void Execute(
                Entity entity,
                ref Weapon weapon,
                ref WeaponFeedback weaponFeedback,
                ref DynamicBuffer<ProjectileShot> projectileShotsBuffer,
                in WeaponSimulationShotOrigin weaponSimulationShotOrigin,
                in DynamicBuffer<WeaponShotIgnoredEntity> weaponShotIgnoredEntities)
            {
                // TODO: should handle the case where a weapon goes out of client's area-of-interest
                // and then comes back later with a high shots count diff
                uint shotsToProcess = weapon.RemoteShotsCount - weapon.LastRemoteShotsCount;
                weapon.LastRemoteShotsCount = weapon.RemoteShotsCount;

                for (int i = 0; i < shotsToProcess; i++)
                {
                    WeaponUtilities.ComputeShot(
                      ref weapon,
                      ref LocalTransformLookup,
                      ref ParentLookup,
                      ref PostTransformMatrixLookup,
                      ref Hits,
                      in weaponSimulationShotOrigin,
                      in CollisionWorld,
                      in weaponShotIgnoredEntities,
                      out var hitFound,
                      out var closestValidHit,
                      out var projectileShotData);

                    projectileShotsBuffer.Add(new ProjectileShot { Data = projectileShotData });
                    weaponFeedback.ShotFeedbackRequests++;
                }
            }
        }
    }
}
