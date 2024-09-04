using Deep.Gameplay.Characters;
using Deep.Gameplay.Weapons;
using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Deep.Authoring.Assets.Scripts.Authoring.Characters
{
    [DisallowMultipleComponent]
    public class CharacterAuthoring : MonoBehaviour
    {
        [Header("Visuals")]
        public GameObject Visuals1P;
        public GameObject Visuals3P;

        public GameObject WeaponSocket1P;
        public GameObject WeaponSocket3P;

        [Header("View")]
        public float MinViewAngle = -90f;
        public float MaxViewAngle = 90f;
        public float ViewRollAmount;
        public float ViewRollSharpness;

        [Header("Body")]
        public float GroundMaxSpeed = 10f;
        public float GroundedMovementSharpness = 15f;
        public float AirAcceleration = 50f;
        public float AirMaxSpeed = 10f;
        public float AirDrag = 0f;
        public float JumpSpeed = 10f;
        public float3 Gravity = math.up() * -30f;
        public bool PreventAirAccelerationAgainstUngroundedHits = true;
        public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling = BasicStepAndSlopeHandlingParameters.GetDefault();

        public AuthoringKinematicCharacterProperties CharacterProperties = AuthoringKinematicCharacterProperties.GetDefault();

        public class Baker : Baker<CharacterAuthoring>
        {
            public override void Bake(CharacterAuthoring authoring)
            {
                KinematicCharacterUtilities.BakeCharacter(this, authoring, authoring.CharacterProperties);

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                // TODO: check nullables for GameObject (visuals, sockets etcs)!!!

                AddComponent(entity, new CharacterComponent
                {
                    GroundMaxSpeed = authoring.GroundMaxSpeed,
                    GroundedMovementSharpness = authoring.GroundedMovementSharpness,
                    AirAcceleration = authoring.AirAcceleration,
                    AirMaxSpeed = authoring.AirMaxSpeed,
                    AirDrag = authoring.AirDrag,
                    JumpSpeed = authoring.JumpSpeed,
                    Gravity = authoring.Gravity,
                    PreventAirAccelerationAgainstUngroundedHits = authoring.PreventAirAccelerationAgainstUngroundedHits,
                    StepAndSlopeHandling = authoring.StepAndSlopeHandling,

                    MinViewAngle = authoring.MinViewAngle,
                    MaxViewAngle = authoring.MaxViewAngle,
                    ViewRollAmount = authoring.ViewRollAmount,
                    ViewRollSharpness = authoring.ViewRollSharpness,
                });
                AddComponent(entity, new CharacterVisuals
                {
                    Visuals1P = GetEntity(authoring.Visuals1P, TransformUsageFlags.Dynamic),
                    Visuals3P = GetEntity(authoring.Visuals3P, TransformUsageFlags.Dynamic),

                    WeaponSocket1P = GetEntity(authoring.WeaponSocket1P, TransformUsageFlags.Dynamic),
                    WeaponSocket3P = GetEntity(authoring.WeaponSocket3P, TransformUsageFlags.Dynamic),
                });
                AddComponent<CharacterControl>(entity);
                AddComponent<ActiveWeapon>(entity);
                AddComponent<CharacterWeaponFeedback>(entity);
            }
        }
    }
}
