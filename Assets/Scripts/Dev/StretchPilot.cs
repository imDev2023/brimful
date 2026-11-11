using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace Brimful
{
    /// <summary>
    /// Plays the prototype stretch through with scripted input, the way a player would, and
    /// writes what happened (each step, each state change, the time) to a log. It steers by the
    /// markers under the scene's "Pilot" object, which StretchBuilder places, so a change to the
    /// level that breaks the route fails the run instead of passing unseen.
    /// </summary>
    public class StretchPilot : MonoBehaviour, IHeroInputSource
    {
        public HeroController hero;
        public WaterBody water;
        public string logPath = "Screenshots/pilot-log.txt";

        public bool Finished { get; private set; }
        public bool Passed { get; private set; }

        bool quitWhenDone;

        // A built player started with -pilot plays the stretch with this pilot and quits with
        // exit code 0 when it passed, 1 when it failed; -pilotLog <file> says where the log
        // goes. This tests the shipped build itself, headless:
        //   Brimful.app/Contents/MacOS/Brimful -batchmode -nographics -pilot -pilotLog run.txt
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void StartFromCommandLine()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-pilot");
            if (i < 0)
                return;
            var pilot = FindAnyObjectByType<StretchPilot>(FindObjectsInactive.Include);
            if (pilot == null)
            {
                Debug.LogError("[Pilot] no StretchPilot in the scene");
                Application.Quit(2);
                return;
            }
            int j = Array.IndexOf(args, "-pilotLog");
            if (j >= 0 && j + 1 < args.Length)
                pilot.logPath = args[j + 1];
            pilot.quitWhenDone = true;
            pilot.enabled = true;
            Time.captureFramerate = 60;   // fixed steps of game time, however fast the machine runs
        }

        struct Step
        {
            public string name;
            public float timeout;
            public Func<HeroInputFrame> drive;
            public Func<bool> done;
        }

        readonly List<Step> steps = new List<Step>();
        readonly StringBuilder log = new StringBuilder();
        int index;
        float stepStart;
        Transform cam;

        void Start()
        {
            cam = Camera.main.transform;
            hero.GetComponent<HeroInput>().Override = this;
            hero.StateChanged += (a, b) => Log($"state {a} -> {b} at {Fmt(hero.transform.position)}");
            Build();
            stepStart = Time.time;
            Log($"step {steps[0].name}");
        }

        Vector3 M(string name)
        {
            var t = transform.Find(name);
            if (t == null)
                throw new Exception($"no pilot marker {name}");
            return t.position;
        }

        void Build()
        {
            var S = HeroController.State.Ground;
            Add("run to the wall", 8f, () => Toward(M("WallBase")), () => Near(M("WallBase"), 0.35f));
            Add("grab the wall", 3f, () => Toward(M("WallBase") + Forward(M("WallBase"), M("WallTop")) * 2f), () => hero.Current == HeroController.State.Climb);
            Add("climb", 8f, () => new HeroInputFrame { move = Vector2.up }, () => hero.Current == HeroController.State.Hang);
            Add("shimmy to the open lip", 10f, () => new HeroInputFrame { move = Vector2.right },
                () => Vector3.Dot(hero.transform.position - M("PullUpSpot"), hero.transform.right) > -0.05f);
            Add("pull up", 4f, () => new HeroInputFrame { move = Vector2.up }, () => hero.Current == S && hero.transform.position.y > M("PullUpSpot").y - 0.3f);
            Add("run to the gap", 6f, () => Toward(M("GapRunup")), () => Near(M("GapRunup"), 0.4f));
            Add("jump the gap", 4f, () =>
            {
                var f = Toward(M("GapLanding"), sprint: true);
                f.jumpPressed = hero.Current == S && Vector3.Dot(hero.transform.position - M("GapEdge"), Forward(M("GapRunup"), M("GapLanding"))) > -0.1f;
                return f;
            }, () => hero.Current == S && Near(M("GapLanding"), 1.2f));
            Add("walk to the wheel", 6f, () => Toward(M("SluiceStand"), walk: true), () => Near(M("SluiceStand"), 0.3f));
            Add("open the sluice", 4f, () => new HeroInputFrame { interactPressed = hero.Current == S }, () => hero.Current == HeroController.State.Interact);
            Add("wait for the water", 16f, () => new HeroInputFrame(), () => water.Full && hero.Current == S);
            Add("walk to the edge", 6f, () => Toward(M("CrateEdge"), walk: true), () => Near(M("CrateEdge"), 0.3f));
            Add("jump onto the crate", 4f, () =>
            {
                var f = Toward(M("Crate"));
                f.jumpPressed = hero.Current == S && Time.time - stepStart < 0.1f;
                return f;
            }, () => hero.Current == S && Near(M("Crate"), 0.6f) && Time.time - stepStart > 0.3f);
            Add("settle on the crate", 2f, () => Toward(M("Crate"), walk: true), () => Near(M("Crate"), 0.2f) || Time.time - stepStart > 1f);
            Add("leap for the high ledge", 4f, () =>
            {
                var f = Toward(M("HighLedge"), sprint: true);
                f.jumpPressed = hero.Current == S && Time.time - stepStart > 0.15f && Time.time - stepStart < 0.3f;
                return f;
            }, () => hero.Current == HeroController.State.Hang);
            Add("pull up onto the arcade", 4f, () => new HeroInputFrame { move = Vector2.up }, () => hero.Current == S);
            Add("walk to the goal", 6f, () => Toward(M("Goal"), walk: true), () => hero.ReachedGoal);
        }

        void Add(string name, float timeout, Func<HeroInputFrame> drive, Func<bool> done) =>
            steps.Add(new Step { name = name, timeout = timeout, drive = drive, done = done });

        public HeroInputFrame Next()
        {
            if (Finished)
                return new HeroInputFrame();
            var step = steps[index];
            if (step.done())
            {
                Log($"done {step.name} in {Time.time - stepStart:0.00} s");
                index++;
                stepStart = Time.time;
                if (index >= steps.Count)
                {
                    Finish(true, $"passed, {hero.Respawns} respawns");
                    return new HeroInputFrame();
                }
                Log($"step {steps[index].name}");
                step = steps[index];
            }
            else if (Time.time - stepStart > step.timeout)
            {
                Finish(false, $"failed at '{step.name}' after {step.timeout} s, hero {hero.Current} at {Fmt(hero.transform.position)}");
                return new HeroInputFrame();
            }
            return step.drive();
        }

        HeroInputFrame Toward(Vector3 target, bool walk = false, bool sprint = false)
        {
            var d = Vector3.ProjectOnPlane(target - hero.transform.position, Vector3.up);
            float dist = d.magnitude;
            if (dist < 0.05f)
                return new HeroInputFrame();
            var fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            var right = Vector3.Cross(Vector3.up, fwd);
            var dir = d / dist;
            var move = new Vector2(Vector3.Dot(dir, right), Vector3.Dot(dir, fwd));
            // Slow down on the approach so the hero stops on the mark.
            float amount = Mathf.Clamp01(dist / 0.8f) * (walk ? 0.45f : 1f);
            return new HeroInputFrame { move = move.normalized * Mathf.Max(amount, 0.2f), sprint = sprint };
        }

        static Vector3 Forward(Vector3 from, Vector3 to) => Vector3.ProjectOnPlane(to - from, Vector3.up).normalized;

        bool Near(Vector3 p, float r) => Vector3.ProjectOnPlane(hero.transform.position - p, Vector3.up).magnitude < r;

        void Finish(bool passed, string message)
        {
            Finished = true;
            Passed = passed;
            Log(message);
            Debug.Log($"[Pilot] {message}");
            var dir = Path.GetDirectoryName(Path.GetFullPath(logPath));
            Directory.CreateDirectory(dir);
            File.WriteAllText(logPath, log.ToString());
            if (quitWhenDone)
                Application.Quit(passed ? 0 : 1);
        }

        void Log(string s) => log.AppendLine($"{Time.time,7:0.00}  {s}");

        static string Fmt(Vector3 v) => $"({v.x:0.00}, {v.y:0.00}, {v.z:0.00})";
    }
}
