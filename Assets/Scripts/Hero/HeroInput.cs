using UnityEngine;
using UnityEngine.InputSystem;

namespace Brimful
{
    /// <summary>One frame of the hero's input, from the player or from a scripted pilot.</summary>
    public struct HeroInputFrame
    {
        public Vector2 move;          // left stick or WASD, x right and y forward, length up to 1
        public Vector2 look;          // mouse delta in pixels, or right stick in -1..1
        public bool lookIsDelta;      // true for a mouse, whose look is a per-frame delta
        public bool jumpPressed;
        public bool interactPressed;
        public bool sprint;
    }

    /// <summary>A source of input other than the player: the test pilot.</summary>
    public interface IHeroInputSource
    {
        HeroInputFrame Next();
    }

    /// <summary>
    /// Reads the project's Player actions (Assets/InputSystem_Actions.inputactions) once per
    /// frame, before anything that uses them, unless a pilot has taken over.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class HeroInput : MonoBehaviour
    {
        public InputActionAsset actions;
        public IHeroInputSource Override { get; set; }
        public HeroInputFrame Frame { get; private set; }

        InputAction move, look, jump, interact, sprint;

        void Awake()
        {
            if (actions == null)
                actions = InputSystem.actions;
            var map = actions.FindActionMap("Player", throwIfNotFound: true);
            move = map.FindAction("Move", true);
            look = map.FindAction("Look", true);
            jump = map.FindAction("Jump", true);
            interact = map.FindAction("Interact", true);
            sprint = map.FindAction("Sprint", true);
            map.Enable();
        }

        void Update()
        {
            if (Override != null)
            {
                Frame = Override.Next();
                return;
            }
            Frame = new HeroInputFrame
            {
                move = Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f),
                look = look.ReadValue<Vector2>(),
                lookIsDelta = look.activeControl?.device is Pointer,
                jumpPressed = jump.WasPressedThisFrame(),
                interactPressed = interact.WasPressedThisFrame(),
                sprint = sprint.IsPressed(),
            };
        }
    }
}
