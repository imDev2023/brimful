using UnityEngine;

namespace Brimful
{
    /// <summary>
    /// A body of water whose surface rises from emptyLevel to fullLevel once filling starts.
    /// The surface mesh is this transform's child; it is hidden while the basin is dry.
    /// </summary>
    public class WaterBody : MonoBehaviour
    {
        public float emptyLevel = 0f;
        public float fullLevel = 3.6f;
        public float fillSeconds = 9f;
        public Transform surface;
        public Bounds area = new Bounds(Vector3.zero, Vector3.one);   // world-space footprint

        public float Level { get; private set; }
        public bool Filling { get; private set; }
        public bool Full => Level >= fullLevel - 0.01f;

        float fillTime;

        void Awake()
        {
            Level = emptyLevel;
            Apply();
        }

        public void StartFilling() => Filling = true;

        void Update()
        {
            if (!Filling || Full)
                return;
            fillTime += Time.deltaTime;
            float u = Mathf.Clamp01(fillTime / fillSeconds);
            // Fast at first, easing as the basin brims.
            Level = Mathf.Lerp(emptyLevel, fullLevel, 1f - (1f - u) * (1f - u));
            Apply();
        }

        void Apply()
        {
            if (surface == null)
                return;
            surface.gameObject.SetActive(Level > emptyLevel + 0.02f);
            var p = surface.position;
            surface.position = new Vector3(p.x, Level, p.z);
        }

        public bool Contains(Vector3 p) =>
            p.x > area.min.x && p.x < area.max.x && p.z > area.min.z && p.z < area.max.z;
    }
}
