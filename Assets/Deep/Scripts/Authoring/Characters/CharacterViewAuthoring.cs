using Deep.Gameplay.Characters;
using Unity.Entities;
using UnityEngine;

namespace Deep.Authoring.Characters
{
    [DisallowMultipleComponent]
    public class CharacterViewAuthoring : MonoBehaviour
    {
        public GameObject Character;

        public class Baker : Baker<CharacterViewAuthoring>
        {
            public override void Bake(CharacterViewAuthoring authoring)
            {
                if (authoring.transform.parent != authoring.Character.transform)
                {
                    Debug.LogError("ERROR: the Character View must be a direct 1st-level child of the character authoring GameObject. Conversion will be aborted");
                    return;
                }

                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new CharacterView { Character = GetEntity(authoring.Character, TransformUsageFlags.Dynamic) });
            }
        }
    }
}
