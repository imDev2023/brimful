using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Brimful
{
    /// <summary>
    /// A sluice the hero opens with its wheel: the wheel turns, the gate leaf lifts in its frame,
    /// the falling water shows, and the water body below starts to fill.
    /// </summary>
    public class Sluice : MonoBehaviour
    {
        public static readonly List<Sluice> All = new List<Sluice>();

        public Transform wheel;
        public float wheelRadius = 0.42f;
        public Transform gate;
        public float gateLift = 1.1f;
        public Transform standPoint;      // where the hero stands to turn the wheel, facing it
        public GameObject outflow;        // the falling water, shown once the gate is up
        public WaterBody water;
        public float reach = 1.3f;

        public bool Opened { get; private set; }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        void Awake()
        {
            if (outflow != null)
                outflow.SetActive(false);
        }

        public static Sluice Near(Vector3 p)
        {
            foreach (var s in All)
                if (!s.Opened && Vector3.Distance(Flat(p), Flat(s.standPoint.position)) < s.reach)
                    return s;
            return null;
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);

        public void Open()
        {
            if (Opened)
                return;
            Opened = true;
            StartCoroutine(Opening());
        }

        IEnumerator Opening()
        {
            var gateFrom = gate.localPosition;
            var gateTo = gateFrom + Vector3.up * gateLift;
            for (float t = 0f; t < 1.6f; t += Time.deltaTime)
            {
                float u = Mathf.SmoothStep(0f, 1f, t / 1.6f);
                gate.localPosition = Vector3.Lerp(gateFrom, gateTo, u);
                wheel.Rotate(Vector3.forward, 240f * Time.deltaTime, Space.Self);
                if (t > 0.5f && outflow != null && !outflow.activeSelf)
                {
                    outflow.SetActive(true);
                    water.StartFilling();
                }
                yield return null;
            }
            gate.localPosition = gateTo;
        }
    }
}
