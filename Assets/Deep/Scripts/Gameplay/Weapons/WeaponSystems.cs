using Deep.Gameplay.Characters;
using Deep.Gameplay.Common;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Deep.Gameplay.Weapons
{
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(PredictedFixedStepSimulationSystemGroup))]
    [BurstCompile]
    public partial struct ActiveWeaponSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new ActiveWeaponJob
            {
                IsServer = state.WorldUnmanaged.IsServer(),
                ECB = SystemAPI.GetSingletonRW<PostPredictionPreTransformsECBSystem.Singleton>().ValueRW.CreateCommandBuffer(state.WorldUnmanaged),
                WeaponLookup = SystemAPI.GetComponentLookup<Weapon>(),
                CharacterVisualsLookup = SystemAPI.GetComponentLookup<CharacterVisuals>(true),
                GhostOwnerIsLocalLookup = SystemAPI.GetComponentLookup<GhostOwnerIsLocal>(true),
                LinkedEntityGroupLookup = SystemAPI.GetBufferLookup<LinkedEntityGroup>(),
                WeaponShotIgnoredEntityLookup = SystemAPI.GetBufferLookup<WeaponShotIgnoredEntity>()
            }.Schedule(state.Dependency);

            state.Dependency = new SetupActiveWeaponJob
            {
                WeaponLookup = SystemAPI.GetComponentLookup<Weapon>(),
                WeaponVisual1PLookup = SystemAPI.GetComponentLookup<WeaponVisual1P>(true)
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        private partial struct ActiveWeaponJob : IJobEntity
        {
            public bool IsServer;
            public EntityCommandBuffer ECB;
            public ComponentLookup<Weapon> WeaponLookup;
            [ReadOnly] public ComponentLookup<CharacterVisuals> CharacterVisualsLookup;
            [ReadOnly] public ComponentLookup<GhostOwnerIsLocal> GhostOwnerIsLocalLookup;
            public BufferLookup<LinkedEntityGroup> LinkedEntityGroupLookup;
            public BufferLookup<WeaponShotIgnoredEntity> WeaponShotIgnoredEntityLookup;

            public void Execute(Entity entity, ref ActiveWeapon activeWeapon)
            {
                if (activeWeapon.Value != activeWeapon.Previous)
                {
                    if (WeaponLookup.TryGetComponent(activeWeapon.Value, out var weapon))
                    {
                        if (CharacterVisualsLookup.TryGetComponent(entity, out var characterVisuals))
                        {
                            if (GhostOwnerIsLocalLookup.HasComponent(entity))
                            {
                                var weapon1P = ECB.Instantiate(weapon.VisualPrefab1P);
                                ECB.AddComponent(weapon1P, new Parent { Value = characterVisuals.WeaponSocket1P });
                                ECB.AddComponent(entity, new CharacterLinkedWeapon { Value = weapon1P });
                            }
                            ECB.AddComponent(activeWeapon.Value, new Parent { Value = characterVisuals.WeaponSocket3P });
                            ECB.AddComponent(activeWeapon.Value, new WeaponOwner { Owner = entity });
                            ECB.SetComponent(activeWeapon.Value, new WeaponSimulationShotOrigin { Value = characterVisuals.Visuals1P });

                            // Make weapon linked to the character
                            var linkedEntityBuffer = LinkedEntityGroupLookup[entity];
                            linkedEntityBuffer.Add(activeWeapon.Value);

                            // Add character as ignored shot entities
                            if (WeaponShotIgnoredEntityLookup.TryGetBuffer(activeWeapon.Value, out var ignoredEntities))
                            {
                                ignoredEntities.Add(new WeaponShotIgnoredEntity { Value = entity });
                            }
                        }
                    }

                    // TODO: Un-setup previous weapon
                    // if (WeaponControlLookup.HasComponent(activeWeapon.PreviousEntity))
                    // {
                    // Disable weapon update, reset owner, reset data, unparent, etc...
                    // }

                    activeWeapon.Previous = activeWeapon.Value;
                }
            }
        }

        [BurstCompile]
        [WithAll(typeof(GhostOwnerIsLocal))]
        private partial struct SetupActiveWeaponJob : IJobEntity
        {
            public ComponentLookup<Weapon> WeaponLookup;
            [ReadOnly] public ComponentLookup<WeaponVisual1P> WeaponVisual1PLookup;

            public void Execute(in CharacterLinkedWeapon characterLinkedWeapon, in ActiveWeapon activeWeapon)
            {
                // TODO: use Weapon.Visuals1P
                if (WeaponVisual1PLookup.TryGetComponent(characterLinkedWeapon.Value, out var weapon1P) &&
                    WeaponLookup.TryGetComponent(activeWeapon.Value, out var weapon))
                {
                    weapon.ShotOrigin = weapon1P.ShotOrigin;
                    WeaponLookup[activeWeapon.Value] = weapon;
                }
            }
        }
    }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(WeaponPredictionUpdateGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct WeaponFiringSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate(SystemAPI.QueryBuilder()
                .WithAll<WeaponControl, WeaponFireState>()
                .Build());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new WeaponFiringJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime
            }.Schedule(state.Dependency);
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        private partial struct WeaponFiringJob : IJobEntity
        {
            public float DeltaTime;

            public void Execute(ref WeaponControl control, ref WeaponFireState fireState)
            {
                fireState.ShotsToFire = 0;
                fireState.ShotTimer += DeltaTime;

                if (control.FirePressed)
                    fireState.IsFiring = true;

                if (fireState.Rate > 0f)
                {
                    var delayBetweenShots = 1f / fireState.Rate;

                    // Clamp shot timer in order to shoot at most the maximum amount of shots
                    // that can be shot in one frame based on the firing rate.
                    // This also prevents needlessly dirtying the timer ghostfield (saves bandwidth).
                    fireState.ShotTimer = math.clamp(fireState.ShotTimer, 0f, math.max(delayBetweenShots + 0.01f, DeltaTime));

                    while (fireState.IsFiring && fireState.ShotTimer > delayBetweenShots)
                    {
                        fireState.ShotsToFire++;

                        fireState.ShotTimer -= delayBetweenShots;

                        if (!fireState.Automatic)
                            fireState.IsFiring = false;
                    }
                }

                if (!fireState.Automatic || control.FireReleased)
                    fireState.IsFiring = false;
            }
        }
    }
}
