using Unity.Entities;
using Unity.Entities.Graphics;
using Unity.Rendering;
using Unity.Transforms;

namespace Deep.Gameplay.Common
{
    public static class CommonUtilities
    {
        public static void EnableRenderingInHierarchy(
            EntityManager entityManager,
            EntityCommandBuffer ecb,
            Entity entity,
            BufferLookup<Child> childBufferFromEntity)
        {
            if (entityManager.HasComponent<RenderFilterSettings>(entity) && entityManager.HasComponent<DisableRendering>(entity))
            {
                ecb.RemoveComponent<DisableRendering>(entity);
            }

            if (childBufferFromEntity.HasBuffer(entity))
            {
                var childBuffer = childBufferFromEntity[entity];
                for (int i = 0; i < childBuffer.Length; i++)
                {
                    EnableRenderingInHierarchy(entityManager, ecb, childBuffer[i].Value, childBufferFromEntity);
                }
            }
        }

        public static void DisableRenderingInHierarchy(
            EntityManager entityManager,
            EntityCommandBuffer ecb,
            Entity entity,
            BufferLookup<Child> childBufferFromEntity)
        {
            if (entityManager.HasComponent<RenderFilterSettings>(entity) && !entityManager.HasComponent<DisableRendering>(entity))
            {
                ecb.AddComponent<DisableRendering>(entity);
            }

            if (childBufferFromEntity.HasBuffer(entity))
            {
                var childBuffer = childBufferFromEntity[entity];
                for (int i = 0; i < childBuffer.Length; i++)
                {
                    DisableRenderingInHierarchy(entityManager, ecb, childBuffer[i].Value, childBufferFromEntity);
                }
            }
        }

        public static void SetLayerInHierarchy(
            EntityManager entityManager,
            EntityCommandBuffer ecb,
            Entity onEntity,
            BufferLookup<Child> childBufferFromEntity,
            int layer)
        {
            if (entityManager.HasComponent<RenderFilterSettings>(onEntity))
            {
                RenderFilterSettings renderFilterSettings = entityManager.GetSharedComponent<RenderFilterSettings>(onEntity);
                renderFilterSettings.Layer = layer;
                ecb.SetSharedComponent(onEntity, renderFilterSettings);
            }

            if (childBufferFromEntity.HasBuffer(onEntity))
            {
                DynamicBuffer<Child> childBuffer = childBufferFromEntity[onEntity];
                for (int i = 0; i < childBuffer.Length; i++)
                {
                    SetLayerInHierarchy(entityManager, ecb, childBuffer[i].Value, childBufferFromEntity, layer);
                }
            }
        }
    }
}
