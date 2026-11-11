using UnityEngine;
using UnityEngine.InputSystem;

namespace Brimful
{
    /// <summary>
    /// The prototype's only interface: the controls for the first moments, a prompt by the
    /// sluice wheel, and a line when the stretch is done. Also captures the mouse for looking
    /// (click to capture, Esc to release). Placeholder text, drawn with IMGUI, until the
    /// game has a real interface.
    /// </summary>
    public class StretchHud : MonoBehaviour
    {
        public HeroController hero;
        public float controlsSeconds = 8f;

        GUIStyle style;

        void Update()
        {
            var mouse = Mouse.current;
            var keys = Keyboard.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame && Application.isFocused)
                Cursor.lockState = CursorLockMode.Locked;
            if (keys != null && keys.escapeKey.wasPressedThisFrame)
                Cursor.lockState = CursorLockMode.None;
            Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        }

        void OnGUI()
        {
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = Mathf.RoundToInt(Screen.height / 38f), alignment = TextAnchor.LowerCenter, wordWrap = true };
                style.normal.textColor = new Color(1f, 0.97f, 0.9f);
            }
            string text = null;
            if (hero.ReachedGoal)
                text = "The arcade is reached. The water follows you up.";
            else if (Sluice.Near(hero.transform.position) != null && hero.Current == HeroController.State.Ground)
                text = "E  turn the wheel";
            else if (Time.timeSinceLevelLoad < controlsSeconds)
                text = "WASD move   Mouse look (click to capture)   Space jump   Shift sprint   E use\nPush into the holds to climb   A / D along a ledge   W pull up   S let go";
            if (text == null)
                return;
            var r = new Rect(Screen.width * 0.1f, Screen.height * 0.72f, Screen.width * 0.8f, Screen.height * 0.22f);
            // A soft shadow so the line reads on bright plaster.
            var shadow = new GUIStyle(style);
            shadow.normal.textColor = new Color(0.2f, 0.1f, 0.05f, 0.85f);
            GUI.Label(new Rect(r.x + 2f, r.y + 2f, r.width, r.height), text, shadow);
            GUI.Label(r, text, style);
        }
    }
}
