using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace Deep.Gameplay.Weapons
{
    public static class WeaponUtilities
    {
        public static void ComputeShot(
            ref Weapon weapon,
            ref ComponentLookup<LocalTransform> localTransformLookup,
            ref ComponentLookup<Parent> parentLookup,
            ref ComponentLookup<PostTransformMatrix> postTransformMatrixLookup,
            ref NativeList<RaycastHit> hits,
            in WeaponSimulationShotOrigin weaponSimulationShotOrigin,
            in CollisionWorld collisionWorld,
            in DynamicBuffer<WeaponShotIgnoredEntity> ignoredEntities,
            out bool hitFound,
            out RaycastHit closestValidHit,
            out ProjectileShotData projectileShotData)
        {
            hitFound = default;
            closestValidHit = default;
            projectileShotData = default;

            var shotOriginEntity = weaponSimulationShotOrigin.Value != Entity.Null ? weaponSimulationShotOrigin.Value : weapon.ShotOrigin;
            TransformHelpers.ComputeWorldTransformMatrix(shotOriginEntity,
                out var shotOriginTransform,
                ref localTransformLookup,
                ref parentLookup,
                ref postTransformMatrixLookup);
            var shotOrigin = shotOriginTransform.Translation();

            for (int s = 0; s < weapon.ProjectilesCount; s++)
            {
                var shotSpreadRotation = quaternion.identity;
                if (weapon.Spread > 0f)
                {
                    shotSpreadRotation = math.slerp(weapon.Random.NextQuaternionRotation(), quaternion.identity,
                        (math.PI - math.clamp(weapon.Spread, 0f, math.PI)) / math.PI);
                }

                var shotDirection = math.rotate(shotSpreadRotation, shotOriginTransform.Forward());

                hits.Clear();
                var rayInput = new RaycastInput
                {
                    Start = shotOrigin,
                    End = shotOrigin + (shotDirection * weapon.Range),
                    Filter = weapon.HitCollisionFilter,
                };
                collisionWorld.CastRay(rayInput, ref hits);
                hitFound = GetClosestRaycastHit(in hits, in ignoredEntities, out closestValidHit);

                // Hit processing
                float hitDistance = weapon.Range;
                if (hitFound)
                {
                    hitDistance = closestValidHit.Fraction * weapon.Range;
                    hitFound = true;
                }

                projectileShotData = new ProjectileShotData
                {
                    OriginEntity = weapon.ShotOrigin,
                    SimulationOrigin = shotOrigin,
                    SimulationDirection = shotDirection,
                    Up = shotOriginTransform.Up(),
                    HitDistance = hitDistance
                };
            }
        }

        public static bool GetClosestRaycastHit(
            in NativeList<RaycastHit> hits,
            in DynamicBuffer<WeaponShotIgnoredEntity> ignoredEntities,
            out RaycastHit closestValidHit)
        {
            closestValidHit = default;
            closestValidHit.Fraction = float.MaxValue;
            for (int j = 0; j < hits.Length; j++)
            {
                var tmpHit = hits[j];

                // Check closest so far
                if (tmpHit.Fraction < closestValidHit.Fraction)
                {
                    // Check collidable
                    if (PhysicsUtilities.IsCollidable(tmpHit.Material))
                    {
                        // Check entity ignore
                        bool entityValid = true;
                        for (int k = 0; k < ignoredEntities.Length; k++)
                        {
                            if (tmpHit.Entity == ignoredEntities[k].Value)
                            {
                                entityValid = false;
                                break;
                            }
                        }

                        // Final hit
                        if (entityValid)
                        {
                            closestValidHit = tmpHit;
                        }
                    }
                }
            }

            return closestValidHit.Entity != Entity.Null;
        }
    }
}
