using System.Collections.Generic;
using UnityEngine;

namespace Brimful
{
    /// <summary>
    /// A lip the hero can hang from: a straight segment from A to B. The hero hangs on the side
    /// the transform's forward points to (Outward), facing back into the wall, and can pull up
    /// wherever the top behind the lip has room to stand.
    /// </summary>
    public class Ledge : MonoBehaviour
    {
        public static readonly List<Ledge> All = new List<Ledge>();

        public Vector3 localA = new Vector3(-1f, 0f, 0f);
        public Vector3 localB = new Vector3(1f, 0f, 0f);
        public bool canPullUp = true;

        public Vector3 A => transform.TransformPoint(localA);
        public Vector3 B => transform.TransformPoint(localB);
        public Vector3 Direction => (B - A).normalized;
        public float Length => Vector3.Distance(A, B);
        public Vector3 Outward => Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public Vector3 PointAt(float t) => A + Direction * Mathf.Clamp(t, 0f, Length);

        public float ClosestT(Vector3 p) => Mathf.Clamp(Vector3.Dot(p - A, Direction), 0f, Length);

        void OnDrawGizmos()
        {
            Gizmos.color = canPullUp ? Color.green : Color.yellow;
            Gizmos.DrawLine(A, B);
            Gizmos.DrawRay((A + B) / 2f, Outward * 0.3f);
        }
    }
}
