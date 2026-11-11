using UnityEngine;

namespace Brimful
{
    /// <summary>
    /// A crate that rests on the basin floor and rides the water up as it fills, bobbing a
    /// little. Kinematic, so the hero can stand on it and be carried.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Floater : MonoBehaviour
    {
        public WaterBody water;
        public float restY;          // its centre's height on the dry floor
        public float draft = 0.25f;  // how far its centre sits below the surface when floating
        public float bob = 0.04f;

        Rigidbody body;
        Quaternion baseRotation;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            baseRotation = transform.rotation;
            if (restY == 0f)
                restY = transform.position.y;
        }

        void FixedUpdate()
        {
            float floatY = water.Level - draft;
            bool floating = floatY > restY;
            float wave = floating ? Mathf.Sin(Time.time * 1.7f) * bob : 0f;
            var p = transform.position;
            body.MovePosition(new Vector3(p.x, Mathf.Max(restY, floatY) + wave, p.z));
            float tilt = floating ? Mathf.Sin(Time.time * 1.3f) * 1.5f : 0f;
            body.MoveRotation(baseRotation * Quaternion.Euler(tilt, 0f, tilt * 0.6f));
        }
    }
}
