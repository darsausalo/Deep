using Unity.CharacterController;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

namespace Deep.Gameplay.Characters
{
    public static class CharacterUtilities
    {
        public static Entity SpawnCharacter(ref EntityCommandBuffer ecb, in Entity characterPrefab, float3 position, int networkId)
        {
            var character = ecb.Instantiate(characterPrefab);

            ecb.SetComponent(character, new GhostOwner { NetworkId = networkId });
            ecb.SetComponent(character, LocalTransform.FromPosition(position));

            return character;
        }

        public static quaternion ComputeRotationFromYAngleAndUp(float rotationYDegrees, float3 transformUp)
        {
            return math.mul(MathUtilities.CreateRotationWithUpPriority(transformUp, math.forward()), quaternion.Euler(0f, math.radians(rotationYDegrees), 0f));
        }

        public static quaternion CalculateLocalViewRotation(float viewPitchDegrees, float viewRollDegrees)
        {
            // Pitch
            quaternion viewLocalRotation = quaternion.AxisAngle(-math.right(), math.radians(viewPitchDegrees));

            // Roll
            viewLocalRotation = math.mul(viewLocalRotation, quaternion.AxisAngle(math.forward(), math.radians(viewRollDegrees)));

            return viewLocalRotation;
        }

        public static void ComputeFinalRotationsFromRotationDelta(
            ref float viewPitchDegrees,
            ref float characterRotationYDegrees,
            float3 characterTransformUp,
            float2 yawPitchDeltaDegrees,
            float viewRollDegrees,
            float minPitchDegrees,
            float maxPitchDegrees,
            out quaternion characterRotation,
            out float canceledPitchDegrees,
            out quaternion viewLocalRotation)
        {
            // Yaw
            characterRotationYDegrees += yawPitchDeltaDegrees.x;
            characterRotation = ComputeRotationFromYAngleAndUp(characterRotationYDegrees, characterTransformUp);

            // Pitch
            viewPitchDegrees += yawPitchDeltaDegrees.y;
            float viewPitchAngleDegreesBeforeClamp = viewPitchDegrees;
            viewPitchDegrees = math.clamp(viewPitchDegrees, minPitchDegrees, maxPitchDegrees);
            canceledPitchDegrees = yawPitchDeltaDegrees.y - (viewPitchAngleDegreesBeforeClamp - viewPitchDegrees);

            viewLocalRotation = CalculateLocalViewRotation(viewPitchDegrees, viewRollDegrees);
        }
    }
}
