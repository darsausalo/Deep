using System;
using System.Collections.Generic;
using UnityEngine;

namespace Deep.Authoring.Spawning
{
    public class RandomPositionGenerator : MonoBehaviour
    {
        private readonly List<Vector3> _positions = new List<Vector3>();

        [Range(5, 25)]
        public int DistancePerPoint = 20;

        [Range(5, 64)]
        public int MaxPoints = 14;

        [Range(10, 200)]
        public float Radius = 30f;

        public bool ShowFill;

        [ContextMenu("GeneratePoints")]
        private void GeneratePoints()
        {
            _positions.Clear();
            var children = GetComponentsInChildren<Transform>(true);
            foreach (var child in children)
            {
                if (child != transform)
                    DestroyImmediate(child.gameObject);
            }

            for (int i = 0; i < MaxPoints; i++)
            {
                var position2d = UnityEngine.Random.insideUnitCircle * Radius;
                var position = new Vector3(position2d.x, 0, position2d.y);
                position += transform.position;
                // Check distance between points
                bool isValid = true;
                foreach (var existingPosition in _positions)
                {
                    if (Vector3.Distance(position, existingPosition) < DistancePerPoint)
                    {
                        isValid = false;
                        break;
                    }
                }

                // If the point is valid, add it to the list with a random Y rotation
                if (isValid)
                {
                    _positions.Add(position);
                }
            }

            for (int i = 0; i < _positions.Count; i++)
            {
                var position = _positions[i];

                var newPoint = new GameObject($"Point {i + 1}");
                newPoint.AddComponent<SpawnPoint>();
                newPoint.transform.SetParent(transform);
                newPoint.transform.position = position;
                newPoint.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);

            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            if (ShowFill)
                Gizmos.DrawSphere(transform.position, Radius);
            else
                Gizmos.DrawWireSphere(transform.position, Radius);
        }
    }
}
