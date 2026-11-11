using UnityEngine;

namespace Brimful
{
    /// <summary>
    /// The third-person camera from the mood stills (stylised-2): behind the hero and a little
    /// above, about 3.4 m back. The mouse or right stick orbits it; after a moment without
    /// look input it swings back behind the hero as she moves, and it pulls in rather than
    /// passing through walls.
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class OrbitCamera : MonoBehaviour
    {
        public HeroController hero;
        public HeroInput input;
        public float distance = 3.4f;
        public float focusHeight = 1.45f;
        public float pitch = 16f;
        public float minPitch = -25f, maxPitch = 70f;
        public float mouseDegreesPerPixel = 0.12f;
        public float stickDegreesPerSecond = 160f;
        public float recenterDelay = 1.2f;
        public float recenterSpeed = 100f;
        public float collisionRadius = 0.22f;

        readonly RaycastHit[] hits = new RaycastHit[16];
        float yaw;
        float lastLook = -10f;
        Vector3 focus;
        float currentDistance;

        void Start()
        {
            yaw = hero.transform.eulerAngles.y;
            focus = hero.transform.position + Vector3.up * focusHeight;
            currentDistance = distance;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            var f = input.Frame;
            if (f.look.sqrMagnitude > 0.0001f)
            {
                var d = f.lookIsDelta ? f.look * mouseDegreesPerPixel : f.look * stickDegreesPerSecond * dt;
                yaw += d.x;
                pitch = Mathf.Clamp(pitch - d.y, minPitch, maxPitch);
                lastLook = Time.time;
            }
            else if (Time.time - lastLook > recenterDelay)
            {
                bool onWall = hero.Current == HeroController.State.Climb || hero.Current == HeroController.State.Hang || hero.Current == HeroController.State.PullUp;
                var v = hero.Velocity;
                bool moving = new Vector3(v.x, 0f, v.z).sqrMagnitude > 0.5f;
                // Ease back behind her: quick when far off, gentle as it arrives, capped so a
                // sharp turn never whips the view (run 2's landing swung at a flat 100 deg/s).
                if (onWall || moving)
                {
                    float gap = Mathf.DeltaAngle(yaw, hero.transform.eulerAngles.y);
                    float step = gap * (1f - Mathf.Exp(-(onWall ? 2.5f : 1.6f) * dt));
                    yaw += Mathf.Clamp(step, -recenterSpeed * dt, recenterSpeed * dt);
                }
            }

            // Follow across at once, but up and down gently while she is in the air, so a jump
            // does not lurch the whole view (run 2's gap jump did); catch up fast on landing.
            var targetFocus = hero.transform.position + Vector3.up * focusHeight;
            bool airborne = hero.Current == HeroController.State.Air;
            float across = 1f - Mathf.Exp(-12f * dt);
            float vertical = 1f - Mathf.Exp(-(airborne ? 2.5f : 8f) * dt);
            focus = new Vector3(Mathf.Lerp(focus.x, targetFocus.x, across), Mathf.Lerp(focus.y, targetFocus.y, vertical), Mathf.Lerp(focus.z, targetFocus.z, across));
            var rot = Quaternion.Euler(pitch, yaw, 0f);
            var back = rot * Vector3.back;

            float want = distance;
            int n = Physics.SphereCastNonAlloc(focus, collisionRadius, back, hits, distance, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (hits[i].collider.GetComponentInParent<HeroController>() == null && hits[i].distance > 0f)
                    want = Mathf.Min(want, Mathf.Max(0.6f, hits[i].distance - 0.05f));
            // Pull in at once, ease back out.
            currentDistance = want < currentDistance ? want : Mathf.Lerp(currentDistance, want, 1f - Mathf.Exp(-4f * dt));

            transform.position = focus + back * currentDistance;
            transform.rotation = Quaternion.LookRotation(focus - transform.position);
        }
    }
}
