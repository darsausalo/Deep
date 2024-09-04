using Deep.Authoring.Utilities;
using UnityEngine;

namespace Deep.Authoring.Spawning
{
    public class SpawnPoint : MonoBehaviour
    {
        private Transform _transform;

        private void OnDrawGizmos()
        {
            if (_transform == null)
                _transform = transform;

            var position = _transform.position;
            Gizmos.color = Color.green;
            ExtGizmos.DrawWireCapsule(position + _transform.up, 0.5f, 2f, _transform.rotation);
            Gizmos.color = Color.cyan;
            ExtGizmos.DrawArrow(position, position + (_transform.forward * 1.5f), 0.15f);
        }
    }
}
