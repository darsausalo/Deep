using Deep.Authoring.Spawning;
using Deep.Gameplay.Common;
using Unity.Entities;
using UnityEngine;

namespace Deep.Authoring.Assets.Scripts.Authoring.Spawning
{
    public class SpawnPointAuthoring : MonoBehaviour
    {
        public class Baker : Baker<SpawnPointAuthoring>
        {
            public override void Bake(SpawnPointAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                var spawnPointElements = AddBuffer<SpawnPointElement>(entity);
                var spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
                foreach (var spawnPoint in spawnPoints)
                {
                    spawnPointElements.Add(new SpawnPointElement { Value = spawnPoint.transform.position });
                }
            }
        }
    }
}
