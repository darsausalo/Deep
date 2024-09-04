using Unity.Entities;

namespace Deep.Gameplay.Cameras
{
    public struct MainEntityCamera : IComponentData
    {
        public float BaseFoV;
        public float CurrentFoV;
    }
}
