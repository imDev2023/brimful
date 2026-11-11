using System.Collections.Generic;
using UnityEngine;

namespace Brimful
{
    /// <summary>Kinds of box zone in a stretch.</summary>
    public enum ZoneKind { Checkpoint, Kill, Goal }

    /// <summary>
    /// A box zone the hero is tested against each frame by position, with no physics callbacks:
    /// a checkpoint remembers where to respawn, a kill zone sends the hero back to the last
    /// checkpoint, and the goal ends the stretch.
    /// </summary>
    public class Zone : MonoBehaviour
    {
        public static readonly List<Zone> All = new List<Zone>();

        public ZoneKind kind;
        public Vector3 size = Vector3.one;
        public Transform spawn;        // checkpoints only: where the hero comes back, facing its forward

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public bool Contains(Vector3 p)
        {
            var local = transform.InverseTransformPoint(p);
            return Mathf.Abs(local.x) <= size.x / 2f && Mathf.Abs(local.y) <= size.y / 2f && Mathf.Abs(local.z) <= size.z / 2f;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = kind == ZoneKind.Kill ? new Color(1f, 0.2f, 0.2f, 0.25f) : kind == ZoneKind.Goal ? new Color(1f, 0.85f, 0.2f, 0.25f) : new Color(0.2f, 0.6f, 1f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(Vector3.zero, size);
        }
    }
}
