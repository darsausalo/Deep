using UnityEngine;

namespace Deep.Gameplay.Cameras
{
    [DisallowMultipleComponent]
    public class MainGameObjectCamera : MonoBehaviour
    {
        public static Camera Instance;

        private void Awake()
        {
            Instance = GetComponent<Camera>();
        }
    }
}
