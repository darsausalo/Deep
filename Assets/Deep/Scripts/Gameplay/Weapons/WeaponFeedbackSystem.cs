using Deep.Gameplay.Cameras;
using Deep.Gameplay.Characters;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace Deep.Gameplay.Weapons
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct WeaponFeedbackSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        { }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.Dependency = new WeaponFeedbackJob
            {
                DeltaTime = SystemAPI.Time.DeltaTime,
                ElapsedTime = (float)SystemAPI.Time.ElapsedTime,
                WeaponControlLookup = SystemAPI.GetComponentLookup<WeaponControl>(true),
                WeaponFeedbackLookup = SystemAPI.GetComponentLookup<WeaponFeedback>(false),
                LocalTransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false),
                MainEntityCameraLookup = SystemAPI.GetComponentLookup<MainEntityCamera>(false),
            }.Schedule(state.Dependency);
            state.Dependency.Complete(); // note: temporary solution
        }

        [BurstCompile]
        //[WithAll(typeof(GhostOwnerIsLocal))]
        private partial struct WeaponFeedbackJob : IJobEntity
        {
            public float DeltaTime;
            public float ElapsedTime;
            [ReadOnly] public ComponentLookup<WeaponControl> WeaponControlLookup;
            public ComponentLookup<WeaponFeedback> WeaponFeedbackLookup;
            public ComponentLookup<LocalTransform> LocalTransformLookup;
            public ComponentLookup<MainEntityCamera> MainEntityCameraLookup;

            private void Execute(
                ref CharacterWeaponFeedback characterWeaponFeedback,
                in CharacterComponent character,
                in CharacterVisuals characterVisuals,
                in KinematicCharacterBody characterBody,
                in ActiveWeapon activeWeapon)
            {
                if (WeaponFeedbackLookup.TryGetComponent(activeWeapon.Value, out var weaponFeedback))
                {
                    // Shot feedback
                    for (var i = 0; i < weaponFeedback.ShotFeedbackRequests; i++)
                    {
                        characterWeaponFeedback.TargetRecoilFOVKick += weaponFeedback.RecoilFOVKick;
                    }
                    weaponFeedback.ShotFeedbackRequests = 0;
                    WeaponFeedbackLookup[activeWeapon.Value] = weaponFeedback;

                    var isAiming = false;
                    var characterMaxSpeed = characterBody.IsGrounded ? character.GroundMaxSpeed : character.AirMaxSpeed;
                    var characterVelocityRatio = math.length(characterBody.RelativeVelocity) / characterMaxSpeed;

                    // Weapon bob
                    {
                        float3 targetBobPos = default;
                        if (characterBody.IsGrounded)
                        {
                            var bobSpeedMultiplier = isAiming ? weaponFeedback.BobAimRatio : 1f;
                            var hBob = math.sin(ElapsedTime * weaponFeedback.BobFrequency) *
                                weaponFeedback.BobHAmount * bobSpeedMultiplier * characterVelocityRatio;
                            var vBob = ((math.sin(ElapsedTime * weaponFeedback.BobFrequency * 2f) * 0.5f) + 0.5f) *
                                weaponFeedback.BobVAmount * bobSpeedMultiplier * characterVelocityRatio;
                            targetBobPos = new float3(hBob, vBob, 0f);
                        }

                        characterWeaponFeedback.WeaponLocalPosBob = math.lerp(characterWeaponFeedback.WeaponLocalPosBob,
                            targetBobPos, math.saturate(weaponFeedback.BobSharpness * DeltaTime));
                    }

                    // FoV modifications
                    if (MainEntityCameraLookup.TryGetComponent(characterVisuals.Visuals1P, out var entityCamera))
                    {
                        // FoV kick
                        {
                            // Clamp current
                            characterWeaponFeedback.TargetRecoilFOVKick = math.clamp(characterWeaponFeedback.TargetRecoilFOVKick,
                                0f, weaponFeedback.RecoilMaxFOVKick);

                            // FoV go towards recoil
                            if (characterWeaponFeedback.CurrentRecoilFOVKick <= characterWeaponFeedback.TargetRecoilFOVKick * 0.99f)
                            {
                                characterWeaponFeedback.CurrentRecoilFOVKick = math.lerp(characterWeaponFeedback.CurrentRecoilFOVKick,
                                    characterWeaponFeedback.TargetRecoilFOVKick,
                                    math.saturate(weaponFeedback.RecoilFOVKickSharpness * DeltaTime));
                            }
                            // FoV go towards restitution
                            else
                            {
                                characterWeaponFeedback.CurrentRecoilFOVKick = math.lerp(characterWeaponFeedback.CurrentRecoilFOVKick,
                                    0f, math.saturate(weaponFeedback.RecoilFOVKickRestitutionSharpness * DeltaTime));
                                characterWeaponFeedback.TargetRecoilFOVKick = characterWeaponFeedback.CurrentRecoilFOVKick;
                            }
                        }

                        // Aiming
                        if (WeaponControlLookup.TryGetComponent(activeWeapon.Value, out WeaponControl weaponControl))
                        {
                            float targetFOV = weaponControl.AimHeld
                                ? (entityCamera.BaseFoV * weaponFeedback.AimFOVRatio)
                                : entityCamera.BaseFoV;
                            entityCamera.CurrentFoV = math.lerp(entityCamera.CurrentFoV,
                                targetFOV + characterWeaponFeedback.CurrentRecoilFOVKick,
                                math.saturate(weaponFeedback.AimFOVSharpness * DeltaTime));
                        }

                        MainEntityCameraLookup[characterVisuals.Visuals1P] = entityCamera;
                    }
                }
            }
        }
    }
}
