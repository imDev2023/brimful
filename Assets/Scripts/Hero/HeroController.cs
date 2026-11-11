using UnityEngine;

namespace Brimful
{
    /// <summary>
    /// The hero's movement as a small state machine: run and jump on the ground, climb a
    /// ClimbSurface's face, hang from a Ledge and shimmy along it, pull up over the lip, and turn
    /// a Sluice's wheel. The code owns the state; the Animator only plays the state's clip
    /// (CrossFade by name), so the two can never disagree.
    /// </summary>
    [RequireComponent(typeof(CharacterController), typeof(HeroInput), typeof(Animator))]
    public class HeroController : MonoBehaviour
    {
        public enum State { Ground, Air, Climb, Hang, PullUp, Interact }

        [Header("Ground")]
        public float runSpeed = 4.2f;
        public float sprintSpeed = 6f;
        public float acceleration = 22f;
        public float turnSpeed = 720f;

        [Header("Air")]
        public float jumpHeight = 1.15f;
        public float gravity = 22f;
        public float airAcceleration = 7f;
        public float coyoteTime = 0.12f;
        public float jumpBuffer = 0.15f;

        [Header("Climb")]
        public float climbSpeed = 1.15f;
        public float wallOffset = 0.3f;     // root's distance from the face while climbing
        public float wallReach = 0.45f;     // how far in front of the body a wall can be grabbed
        public float chestHeight = 1.15f;

        [Header("Hang")]
        public float hangHandHeight = 1.73f; // hands above the root in Hang_Loop (HeroCheck)
        public float hangOffset = 0.26f;     // root's distance out from the lip while hanging
        public float shimmySpeed = 1.0f;
        public float grabRadius = 0.4f;
        public float pullUpSeconds = 0.95f;

        [Header("Refs")]
        public Transform cameraTransform;

        public State Current => state;
        public Vector3 Velocity => velocity;
        public Ledge HangLedge => ledge;
        public float HangT => ledgeT;
        public System.Action<State, State> StateChanged;

        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int ClimbRateHash = Animator.StringToHash("ClimbRate");

        CharacterController cc;
        Animator anim;
        HeroInput input;

        State state = State.Ground;
        float stateTime;
        Vector3 velocity;
        float lastGrounded = -10f, lastJumpPressed = -10f, grabCooldown, airTime;

        Ledge ledge;
        float ledgeT;
        Vector3 hangFrom;
        Quaternion hangFromRot;
        Vector3 wallNormal;

        Vector3 pullFrom, pullLift, pullTo;
        Sluice sluice;
        bool sluiceOpened;

        Transform platform;
        Vector3 platformLocal;

        // Hand IK while hanging: each hand holds a point on the lip and steps to keep up.
        float leftHold, rightHold;
        float leftStep = 1f, rightStep = 1f;
        float leftFrom, rightFrom;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            anim = GetComponent<Animator>();
            input = GetComponent<HeroInput>();
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        void Update()
        {
            var f = input.Frame;
            float dt = Time.deltaTime;
            if (f.jumpPressed)
                lastJumpPressed = Time.time;
            stateTime += dt;
            grabCooldown -= dt;

            switch (state)
            {
                case State.Ground: UpdateGround(f, dt); break;
                case State.Air: UpdateAir(f, dt); break;
                case State.Climb: UpdateClimb(f, dt); break;
                case State.Hang: UpdateHang(f, dt); break;
                case State.PullUp: UpdatePullUp(dt); break;
                case State.Interact: UpdateInteract(); break;
            }

            var h = new Vector3(velocity.x, 0f, velocity.z).magnitude;
            anim.SetFloat(SpeedHash, state == State.Ground ? h : 0f, 0.08f, dt);
            CheckZones();
        }

        // ---------------------------------------------------------------- ground

        void UpdateGround(HeroInputFrame f, float dt)
        {
            CarryWithPlatform();
            var wish = CameraRelative(f.move);
            float speed = f.sprint ? sprintSpeed : runSpeed;
            var target = wish * speed;
            var h = Vector3.MoveTowards(new Vector3(velocity.x, 0f, velocity.z), target, acceleration * dt);
            if (wish.sqrMagnitude > 0.01f)
                Face(wish, dt);
            velocity = h + Vector3.down * 4f;
            cc.Move(velocity * dt);

            if (cc.isGrounded)
            {
                lastGrounded = Time.time;
                RememberPlatform();
            }

            if (Time.time - lastJumpPressed < jumpBuffer)
            {
                Jump();
                return;
            }
            if (f.interactPressed && (sluice = Sluice.Near(transform.position)) != null)
            {
                Enter(State.Interact);
                return;
            }
            if (wish.sqrMagnitude > 0.25f && TryFindWall(wish, out var hit))
            {
                EnterClimb(hit);
                return;
            }
            if (!cc.isGrounded && Time.time - lastGrounded > coyoteTime)
            {
                velocity.y = 0f;
                Enter(State.Air);
                anim.CrossFadeInFixedTime("Fall", 0.2f);
            }
        }

        void Jump()
        {
            lastJumpPressed = -10f;
            lastGrounded = -10f;
            velocity.y = Mathf.Sqrt(2f * gravity * jumpHeight);
            platform = null;
            Enter(State.Air);
            // Jump_Start opens with a deep crouch; start past it so the jump answers at once.
            anim.CrossFadeInFixedTime("JumpStart", 0.08f, 0, 0.16f);
        }

        // ---------------------------------------------------------------- air

        void UpdateAir(HeroInputFrame f, float dt)
        {
            airTime += dt;
            var wish = CameraRelative(f.move);
            var h = new Vector3(velocity.x, 0f, velocity.z);
            if (wish.sqrMagnitude > 0.01f)
            {
                h = Vector3.MoveTowards(h, wish * Mathf.Max(h.magnitude, runSpeed), airAcceleration * dt);
                Face(wish, dt * 0.5f);
            }
            velocity = new Vector3(h.x, velocity.y - gravity * dt, h.z);
            cc.Move(velocity * dt);

            if (Time.time - lastJumpPressed < jumpBuffer && Time.time - lastGrounded < coyoteTime)
            {
                Jump();
                return;
            }
            if (grabCooldown <= 0f && velocity.y < 1.5f && TryGrabLedge(out var l, out var t))
            {
                EnterHang(l, t);
                return;
            }
            if (grabCooldown <= 0f && wish.sqrMagnitude > 0.25f && TryFindWall(transform.forward, out var hit))
            {
                EnterClimb(hit);
                return;
            }
            if (cc.isGrounded && velocity.y <= 0f)
            {
                lastGrounded = Time.time;
                bool hard = airTime > 0.75f;
                Enter(State.Ground);
                if (hard)
                    anim.CrossFadeInFixedTime("Land", 0.06f, 0, 0.1f);
                else
                    anim.CrossFadeInFixedTime("Locomotion", 0.12f);
                return;
            }
            if (velocity.y < -0.5f && !anim.GetCurrentAnimatorStateInfo(0).IsName("Fall") && !anim.IsInTransition(0))
                anim.CrossFadeInFixedTime("Fall", 0.25f);
        }

        // ---------------------------------------------------------------- climb

        bool TryFindWall(Vector3 dir, out RaycastHit hit)
        {
            dir = Vector3.ProjectOnPlane(dir, Vector3.up).normalized;
            var from = transform.position + Vector3.up * chestHeight;
            if (Physics.Raycast(from, dir, out hit, cc.radius + wallReach, ~0, QueryTriggerInteraction.Ignore)
                && ClimbSurface.Is(hit.collider)
                && Mathf.Abs(hit.normal.y) < 0.3f
                && Vector3.Dot(dir, -hit.normal) > 0.6f)
                return true;
            return false;
        }

        void EnterClimb(RaycastHit hit)
        {
            wallNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
            velocity = Vector3.zero;
            cc.enabled = false;
            platform = null;
            var p = transform.position;
            var onFace = hit.point + wallNormal * wallOffset;
            transform.position = new Vector3(onFace.x, Mathf.Max(p.y, p.y + 0.05f), onFace.z);
            transform.rotation = Quaternion.LookRotation(-wallNormal);
            Enter(State.Climb);
            anim.CrossFadeInFixedTime("Climb", 0.2f);
        }

        void UpdateClimb(HeroInputFrame f, float dt)
        {
            var right = Vector3.Cross(Vector3.up, -wallNormal);
            var move = (right * f.move.x + Vector3.up * f.move.y);
            if (move.sqrMagnitude > 1f)
                move.Normalize();
            var next = transform.position + move * climbSpeed * dt;

            if (Time.time - lastJumpPressed < jumpBuffer)
            {
                // Leap away from the wall.
                lastJumpPressed = -10f;
                cc.enabled = true;
                velocity = wallNormal * 3.2f + Vector3.up * 4.5f;
                transform.rotation = Quaternion.LookRotation(wallNormal);
                grabCooldown = 0.35f;
                Enter(State.Air);
                anim.CrossFadeInFixedTime("JumpStart", 0.1f, 0, 0.16f);
                return;
            }

            // Reaching the top: the hands find a ledge.
            if (f.move.y > 0.1f && TryGrabLedge(out var l, out var t, 0.06f))
            {
                EnterHang(l, t);
                return;
            }

            // Stay on the face: probe it at chest height from the next position.
            var probe = next + Vector3.up * chestHeight + wallNormal * 0.4f;
            if (Physics.Raycast(probe, -wallNormal, out var hit, 0.4f + wallOffset + 0.4f, ~0, QueryTriggerInteraction.Ignore)
                && ClimbSurface.Is(hit.collider) && Mathf.Abs(hit.normal.y) < 0.3f)
            {
                wallNormal = Vector3.ProjectOnPlane(hit.normal, Vector3.up).normalized;
                var onFace = hit.point + wallNormal * wallOffset;
                next = new Vector3(onFace.x, next.y, onFace.z);
                transform.position = next;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(-wallNormal), 10f * dt);
                anim.SetFloat(ClimbRateHash, move.magnitude);
            }
            else
            {
                anim.SetFloat(ClimbRateHash, 0f);   // off the climbable part: hold still
            }

            // Climbing down onto the ground.
            if (f.move.y < -0.1f && Physics.Raycast(transform.position + Vector3.up * 0.2f, Vector3.down, 0.3f, ~0, QueryTriggerInteraction.Ignore))
            {
                cc.enabled = true;
                transform.position += wallNormal * 0.15f;
                Enter(State.Ground);
                anim.CrossFadeInFixedTime("Locomotion", 0.2f);
            }
        }

        // ---------------------------------------------------------------- hang

        // `below`: how far under the lip the hands may be and still grab. Climbing grabs only
        // when the hands have reached the lip, so the body does not jump up to it.
        bool TryGrabLedge(out Ledge best, out float bestT, float below = 0.3f)
        {
            best = null;
            bestT = 0f;
            float bestD = float.MaxValue;
            var hands = transform.position + Vector3.up * hangHandHeight;
            foreach (var l in Ledge.All)
            {
                float t = Mathf.Clamp(l.ClosestT(hands), 0.25f, l.Length - 0.25f);
                var p = l.PointAt(t);
                var d = hands - p;
                if (d.y > grabRadius || d.y < -below)
                    continue;
                float outDist = Vector3.Dot(Vector3.ProjectOnPlane(d, Vector3.up), l.Outward);
                float sideDist = Mathf.Abs(Vector3.Dot(d, l.Direction));
                if (outDist < -0.15f || outDist > 0.75f || sideDist > 0.3f)
                    continue;
                if (Vector3.Dot(transform.forward, -l.Outward) < 0.3f)
                    continue;
                if (d.magnitude < bestD)
                {
                    bestD = d.magnitude;
                    best = l;
                    bestT = t;
                }
            }
            return best != null;
        }

        void EnterHang(Ledge l, float t)
        {
            hangFrom = transform.position;
            hangFromRot = transform.rotation;
            ledge = l;
            ledgeT = t;
            velocity = Vector3.zero;
            cc.enabled = false;
            platform = null;
            leftHold = leftFrom = HandT(-1f);
            rightHold = rightFrom = HandT(1f);
            leftStep = rightStep = 1f;
            Enter(State.Hang);   // before placing, so the placement blends from the grab
            PlaceOnLedge();
            anim.CrossFadeInFixedTime("Hang", 0.12f);
        }

        // The hero's right along the lip, as +1 or -1 against the ledge's own direction.
        float SideSign => Mathf.Sign(Vector3.Dot(Vector3.Cross(Vector3.up, -ledge.Outward), ledge.Direction));

        float HandT(float side) => ledgeT + side * SideSign * 0.2f;

        void PlaceOnLedge()
        {
            var p = ledge.PointAt(ledgeT) + ledge.Outward * hangOffset - Vector3.up * hangHandHeight;
            var r = Quaternion.LookRotation(-ledge.Outward);
            // Blend into the hang over its first moments, so the grab does not pop the body up
            // to the lip in one frame (run 2's arcade grab moved it 0.3 m at once).
            float u = state == State.Hang ? Mathf.SmoothStep(0f, 1f, stateTime / 0.15f) : 1f;
            transform.position = Vector3.Lerp(hangFrom, p, u);
            transform.rotation = Quaternion.Slerp(hangFromRot, r, u);
        }

        void UpdateHang(HeroInputFrame f, float dt)
        {
            // Shimmy: the body slides along the lip and the hands step after it.
            float before = ledgeT;
            ledgeT = Mathf.Clamp(ledgeT + f.move.x * SideSign * shimmySpeed * dt, 0.25f, ledge.Length - 0.25f);
            PlaceOnLedge();
            StepHands(dt, ledgeT - before);

            bool wantsUp = f.move.y > 0.6f || Time.time - lastJumpPressed < jumpBuffer;
            if (wantsUp && stateTime > 0.25f && CanPullUp(out var top))
            {
                lastJumpPressed = -10f;
                pullFrom = transform.position;
                pullTo = top;
                // ClimbUp_1m starts with the hands 0.98 m above the root: rise to that first.
                pullLift = ledge.PointAt(ledgeT) + ledge.Outward * (hangOffset + 0.08f) - Vector3.up * 0.98f;
                Enter(State.PullUp);
                anim.CrossFadeInFixedTime("Hang", 0.05f);
                return;
            }
            if (f.move.y < -0.6f && stateTime > 0.25f)
            {
                // Let go.
                cc.enabled = true;
                velocity = ledge.Outward * 0.8f;
                grabCooldown = 0.45f;
                Enter(State.Air);
                anim.CrossFadeInFixedTime("Fall", 0.2f);
            }
        }

        void StepHands(float dt, float moved)
        {
            const float stepSeconds = 0.16f, slack = 0.12f;
            leftStep = Mathf.Min(1f, leftStep + dt / stepSeconds);
            rightStep = Mathf.Min(1f, rightStep + dt / stepSeconds);
            // Only one hand moves at a time, and only when it has fallen behind.
            if (leftStep >= 1f && rightStep >= 1f)
            {
                float dl = Mathf.Abs(leftHold - HandT(-1f)), dr = Mathf.Abs(rightHold - HandT(1f));
                if (dl > slack && dl >= dr) { leftFrom = leftHold; leftHold = HandT(-1f) + Mathf.Sign(moved) * 0.05f; leftStep = 0f; }
                else if (dr > slack) { rightFrom = rightHold; rightHold = HandT(1f) + Mathf.Sign(moved) * 0.05f; rightStep = 0f; }
            }
        }

        bool CanPullUp(out Vector3 top)
        {
            top = default;
            if (!ledge.canPullUp)
                return false;
            var lip = ledge.PointAt(ledgeT);
            var probe = lip - ledge.Outward * 0.45f + Vector3.up * 0.6f;
            if (!Physics.Raycast(probe, Vector3.down, out var floor, 0.9f, ~0, QueryTriggerInteraction.Ignore))
                return false;
            top = floor.point;
            float r = cc.radius;
            var bottom = top + Vector3.up * (r + 0.05f);
            var head = top + Vector3.up * (cc.height - r);
            return !Physics.CheckCapsule(bottom, head, r * 0.9f, ~0, QueryTriggerInteraction.Ignore);
        }

        // ---------------------------------------------------------------- pull up

        void UpdatePullUp(float dt)
        {
            const float lift = 0.22f;   // seconds to rise from the hang to ClimbUp_1m's start
            if (stateTime < lift)
            {
                float u = Mathf.SmoothStep(0f, 1f, stateTime / lift);
                transform.position = Vector3.Lerp(pullFrom, pullLift, u);
                if (stateTime + dt >= lift)
                    anim.CrossFadeInFixedTime("PullUp", 0.08f);
                return;
            }
            float v = Mathf.Clamp01((stateTime - lift) / pullUpSeconds);
            // Up first, then over the lip onto the top.
            float up = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(v / 0.6f));
            float over = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((v - 0.35f) / 0.65f));
            var p = new Vector3(Mathf.Lerp(pullLift.x, pullTo.x, over), Mathf.Lerp(pullLift.y, pullTo.y, up), Mathf.Lerp(pullLift.z, pullTo.z, over));
            transform.position = p;
            if (v >= 1f)
            {
                transform.position = pullTo;
                cc.enabled = true;
                velocity = Vector3.zero;
                Enter(State.Ground);
                anim.CrossFadeInFixedTime("Locomotion", 0.2f);
            }
        }

        // ---------------------------------------------------------------- sluice

        void UpdateInteract()
        {
            if (stateTime < 0.05f)
            {
                cc.enabled = false;
                transform.position = new Vector3(sluice.standPoint.position.x, transform.position.y, sluice.standPoint.position.z);
                transform.rotation = Quaternion.LookRotation(Vector3.ProjectOnPlane(sluice.standPoint.forward, Vector3.up));
                cc.enabled = true;
                velocity = Vector3.zero;
                sluiceOpened = false;
                anim.CrossFadeInFixedTime("Interact", 0.15f);
            }
            if (!sluiceOpened && stateTime > 0.35f)
            {
                sluiceOpened = true;
                sluice.Open();
            }
            if (stateTime > 2.2f)
            {
                Enter(State.Ground);
                anim.CrossFadeInFixedTime("Locomotion", 0.25f);
            }
        }

        // ---------------------------------------------------------------- shared

        void Enter(State next)
        {
            var prev = state;
            state = next;
            stateTime = 0f;
            if (next == State.Air)
                airTime = 0f;
            StateChanged?.Invoke(prev, next);
        }

        Vector3 CameraRelative(Vector2 move)
        {
            if (move.sqrMagnitude < 0.0001f)
                return Vector3.zero;
            var cam = cameraTransform != null ? cameraTransform : transform;
            var fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, fwd);
            return Vector3.ClampMagnitude(fwd * move.y + right * move.x, 1f);
        }

        void Face(Vector3 dir, float dt)
        {
            var target = Quaternion.LookRotation(Vector3.ProjectOnPlane(dir, Vector3.up));
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * dt);
        }

        void RememberPlatform()
        {
            platform = null;
            if (Physics.SphereCast(transform.position + Vector3.up * 0.4f, cc.radius * 0.8f, Vector3.down, out var hit, 0.5f, ~0, QueryTriggerInteraction.Ignore)
                && hit.collider.GetComponentInParent<Floater>() != null)
            {
                platform = hit.collider.transform;
                platformLocal = platform.InverseTransformPoint(transform.position);
            }
        }

        void CarryWithPlatform()
        {
            if (platform == null)
                return;
            var carried = platform.TransformPoint(platformLocal);
            var delta = carried - transform.position;
            if (delta.sqrMagnitude > 0.000001f && delta.sqrMagnitude < 1f)
                cc.Move(delta);
        }

        void CheckZones()
        {
            foreach (var z in Zone.All)
            {
                if (!z.Contains(transform.position))
                    continue;
                if (z.kind == ZoneKind.Checkpoint)
                    LastCheckpoint = z;
                else if (z.kind == ZoneKind.Goal)
                    ReachedGoal = true;
                else if (z.kind == ZoneKind.Kill)
                    Respawn();
            }
            foreach (var s in Sluice.All)
                if (s.water != null && s.water.Contains(transform.position) && transform.position.y < s.water.Level - 1.1f)
                    Respawn();
        }

        public Zone LastCheckpoint { get; private set; }
        public bool ReachedGoal { get; private set; }
        public int Respawns { get; private set; }

        public void Respawn()
        {
            if (LastCheckpoint == null || LastCheckpoint.spawn == null)
                return;
            Respawns++;
            cc.enabled = false;
            transform.SetPositionAndRotation(LastCheckpoint.spawn.position, LastCheckpoint.spawn.rotation);
            cc.enabled = true;
            velocity = Vector3.zero;
            platform = null;
            Enter(State.Ground);
            anim.CrossFadeInFixedTime("Locomotion", 0f);
        }

        // ---------------------------------------------------------------- IK

        void OnAnimatorIK(int layer)
        {
            if (state == State.Hang || (state == State.PullUp && stateTime < 0.22f))
            {
                PinHand(AvatarIKGoal.LeftHand, leftFrom, leftHold, leftStep);
                PinHand(AvatarIKGoal.RightHand, rightFrom, rightHold, rightStep);
            }
            else if (state == State.Interact && sluice != null)
            {
                // Hands on the wheel's rim, hand over hand, while it turns.
                float w = Mathf.Clamp01((stateTime - 0.15f) / 0.2f) * Mathf.Clamp01((2.1f - stateTime) / 0.25f);
                float swing = Mathf.Sin(stateTime * Mathf.PI * 2f * 1.1f) * 35f;
                GripRim(AvatarIKGoal.LeftHand, 150f + swing, w);
                GripRim(AvatarIKGoal.RightHand, 30f - swing, w);
            }
            else
            {
                anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                anim.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            }
        }

        void GripRim(AvatarIKGoal goal, float degrees, float weight)
        {
            // The wheel turns about the sluice's z axis; its rim, unrotated, lies in x and y.
            var frame = sluice.transform;
            float a = degrees * Mathf.Deg2Rad;
            var p = sluice.wheel.position + (frame.right * Mathf.Cos(a) + frame.up * Mathf.Sin(a)) * sluice.wheelRadius - frame.forward * 0.04f;
            anim.SetIKPositionWeight(goal, weight);
            anim.SetIKPosition(goal, p);
        }

        void PinHand(AvatarIKGoal goal, float from, float to, float step)
        {
            float t = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, step));
            // A stepping hand lifts a little off the lip.
            var p = ledge.PointAt(t) + Vector3.up * (0.03f + Mathf.Sin(step * Mathf.PI) * 0.06f) + ledge.Outward * 0.04f;
            anim.SetIKPositionWeight(goal, 1f);
            anim.SetIKPosition(goal, p);
        }
    }
}
