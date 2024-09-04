using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

namespace Deep.Gameplay.Characters
{
    [GhostComponent]
    public struct CharacterComponent : IComponentData
    {
        public float GroundMaxSpeed;
        public float GroundedMovementSharpness;
        public float AirAcceleration;
        public float AirMaxSpeed;
        public float AirDrag;
        public float JumpSpeed;
        public float3 Gravity;
        public bool PreventAirAccelerationAgainstUngroundedHits;
        public BasicStepAndSlopeHandlingParameters StepAndSlopeHandling;

        public float MinViewAngle;
        public float MaxViewAngle;
        public float ViewRollAmount;
        public float ViewRollSharpness;

        [GhostField(Smoothing = SmoothingAction.InterpolateAndExtrapolate)]
        public float CharacterYDegrees;
        [GhostField(Smoothing = SmoothingAction.InterpolateAndExtrapolate)]
        public float ViewPitchDegrees;
        public quaternion ViewLocalRotation;
        public float ViewRollDegrees;
    }

    public struct CharacterVisuals : IComponentData
    {
        public Entity Visuals1P;
        public Entity Visuals3P;

        public Entity WeaponSocket1P;
        public Entity WeaponSocket3P;
    }

    public struct CharacterControl : IComponentData
    {
        public float3 MoveVector;
        public float2 LookYawPitchDegrees;
        public bool Jump;
    }

    public struct CharacterView : IComponentData
    {
        public Entity Character;
    }

    public struct CharacterClientCleanup : ICleanupComponentData
    {
        // TODO: DeathVFX?
    }
}
