using Unity.Entities;
using Unity.NetCode;
using Unity.Physics.Systems;

namespace Deep.Gameplay.Weapons
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(PhysicsSystemGroup))]
    public partial class WeaponPredictionUpdateGroup : ComponentSystemGroup { }
}
