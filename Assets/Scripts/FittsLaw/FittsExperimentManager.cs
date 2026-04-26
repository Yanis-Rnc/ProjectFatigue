using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace FittsLaw
{
    /// <summary>
    /// Main manager of the Fitts' Law experiment.
    /// Handles target spawning, hit detection, metrics computation,
    /// and exporting results to CSV files.
    /// </summary>
    public class FittsExperimentManager : MonoBehaviour
    {
        // === Public references ===
        public Camera playerCamera;          // Player viewpoint
        public GameObject targetPrefab;      // Prefab used for targets
        public string exportPath;            // Optional export directory

        // === Runtime state ===
        private List<GameObject> _currentTargets = new List<GameObject>();
        private int _targetsHit;

        // Possible target sizes
        private readonly float[] _sizes = { 0.025f, 0.05f, .1f };

        // Timing & metrics
        private float _experimentStartTime;
        private int _totalShots;
        private int _missedShots;
        private float _totalDifficulty;

        // XR input tracking
        private InputDevice _rightHand;
        private InputDevice _leftHand;
        private bool _prevTriggerRight = false;
        private bool _prevTriggerLeft = false;

        // Hit tracking
        private float _lastHitTime;
        private Vector3 _lastTargetPos = Vector3.zero;
        private bool _experimentRunning = true;

        // Spawn sequence
        private Vector3[] _spawnSequence;
        private int _spawnIndex = 0;

        /// <summary>
        /// Represents a single hit record for data export.
        /// </summary>
        private struct HitRecord
        {
            public int Index;
            public float Time;
            public float AbsoluteTime;
            public float Distance;
            public float Size;
            public float ID;
            public Vector3 Position;
            public int SeqIndex;
            public bool IsMiss;
        }

        private List<HitRecord> _records = new List<HitRecord>();

        void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            StartCoroutine(SpawnFirstTargetDelayed());
        }

        /// <summary>
        /// Small delay before starting the experiment.
        /// </summary>
        IEnumerator SpawnFirstTargetDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            _experimentStartTime = Time.time;
            _lastHitTime = Time.time;
            BuildSpawnSequence();
            SpawnNextTarget();
        }

        void Update()
        {
            // Ensure devices are valid
            if (!_rightHand.isValid)
                _rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (!_leftHand.isValid)
                _leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

            _rightHand.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool triggerRight);
            _leftHand.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out bool triggerLeft);

            bool triggered = (triggerRight && !_prevTriggerRight) || (triggerLeft && !_prevTriggerLeft);

            // Stop experiment (keyboard or VR)
            bool desktopStop = Input.GetKeyDown(KeyCode.S);
            bool vrStop = false;

            if (triggered)
            {
                InputDevice activeHand = (triggerRight && !_prevTriggerRight) ? _rightHand : _leftHand;
                activeHand.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out vrStop);
            }

            if (_experimentRunning && (desktopStop || vrStop))
            {
                _experimentRunning = false;
                StopExperiment();
            }

            _prevTriggerRight = triggerRight;
            _prevTriggerLeft = triggerLeft;
        }

        /// <summary>
        /// Builds a deterministic sequence of target positions based on calibration.
        /// </summary>
        void BuildSpawnSequence()
        {
            Vector3 c = CalibrationData.Center;
            float rD = CalibrationData.RadiusDepth;
            float rW = CalibrationData.RadiusWidth;
            float rH = CalibrationData.RadiusHeight;

            Vector3 x0 = c + new Vector3(0, rH, 0);
            Vector3 x1 = c + new Vector3(0, 0, rD);
            Vector3 x2 = c + new Vector3(0, -rH, 0);
            Vector3 x3 = c + new Vector3(0, 0, -rD);

            Vector3 y0 = c + new Vector3(0, 0, rD);
            Vector3 y1 = c + new Vector3(rW, 0, 0);
            Vector3 y2 = c + new Vector3(0, 0, -rD);
            Vector3 y3 = c + new Vector3(-rW, 0, 0);

            Vector3 z0 = c + new Vector3(0, rH, 0);
            Vector3 z1 = c + new Vector3(rW, 0, 0);
            Vector3 z2 = c + new Vector3(0, -rH, 0);
            Vector3 z3 = c + new Vector3(-rW, 0, 0);

            _spawnSequence = new Vector3[] { x0, x1, x2, x3, y0, y1, y2, y3, z0, z1, z2, z3, c };
            _spawnIndex = 0;
        }

        /// <summary>
        /// Spawns the next target in the sequence with random size.
        /// </summary>
        void SpawnNextTarget()
        {
            Vector3 spawnPos = _spawnSequence[_spawnIndex % _spawnSequence.Length];
            _spawnIndex++;

            float size = _sizes[Random.Range(0, _sizes.Length)];

            GameObject target = Instantiate(targetPrefab, spawnPos, Quaternion.identity);
            target.transform.localScale = Vector3.one * size;
            target.GetComponent<FittsTarget>().Init(this);

            _currentTargets.Add(target);
        }

        /// <summary>
        /// Called when a target is successfully hit.
        /// Computes Fitts' Law metrics.
        /// </summary>
        public void TargetHit(FittsTarget target)
        {
            if (!_currentTargets.Contains(target.gameObject))
                return;

            float distanceFromLast = _lastTargetPos == Vector3.zero ? 0f : Vector3.Distance(target.transform.position, _lastTargetPos);
            _lastTargetPos = target.transform.position;

            _targetsHit++;

            float distance = distanceFromLast == 0f
                ? Vector3.Distance(playerCamera.transform.position, target.transform.position)
                : distanceFromLast;

            float size = target.transform.localScale.x;

            // Fitts' Law Index of Difficulty
            float id = Mathf.Log((2 * distance) / size, 2);
            _totalDifficulty += id;

            float timeSinceLast = Time.time - _lastHitTime;
            float absoluteTime = Time.time - _experimentStartTime;
            _lastHitTime = Time.time;

            _records.Add(new HitRecord
            {
                Index = _targetsHit,
                Time = timeSinceLast,
                AbsoluteTime = absoluteTime,
                Distance = distanceFromLast,
                Size = size,
                ID = id,
                Position = target.transform.position,
                SeqIndex = _spawnIndex - 1,
                IsMiss = false
            });

            _currentTargets.Remove(target.gameObject);
            Destroy(target.gameObject);

            if (_experimentRunning)
                SpawnNextTarget();
        }

        /// <summary>
        /// Registers a shot attempt (hit or miss).
        /// </summary>
        public void RegisterShot(bool hit, GameObject target = null)
        {
            _totalShots++;
            if (!hit)
                _missedShots++;
        }

        /// <summary>
        /// Stops the experiment, computes statistics, and exports results.
        /// </summary>
        void StopExperiment()
        {
            foreach (var t in _currentTargets)
                if (t != null) Destroy(t);

            _currentTargets.Clear();

            float totalTime = Time.time - _experimentStartTime;
            float avgTimePerTarget = _targetsHit > 0 ? totalTime / _targetsHit : 0f;
            float errorRate = _totalShots > 0 ? (float)_missedShots / _totalShots : 0f;
            float avgDifficulty = _targetsHit > 0 ? _totalDifficulty / _targetsHit : 0f;

            float score = avgDifficulty > 0 && avgTimePerTarget > 0
                ? (avgDifficulty * 100f) / (avgTimePerTarget * (1f + errorRate))
                : 0f;

            ExportCSV(totalTime, avgTimePerTarget, errorRate, avgDifficulty, score, 0, 0, 0);

            SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
        }

        /// <summary>
        /// Exports detailed and summary results into CSV files.
        /// </summary>
        void ExportCSV(float totalTime, float avgTime, float errorRate, float avgID, float score, float firstHalf, float secondHalf, float slowdown)
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

            string basePath = string.IsNullOrEmpty(exportPath)
                ? System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
                : exportPath;

            string folder = Path.Combine(basePath, $"FittsData_{timestamp}");
            Directory.CreateDirectory(folder);

            // Detailed hits
            var sb = new StringBuilder();
            sb.AppendLine("hit_index;absolute_time;time_since_last;distance;size;ID");

            foreach (var r in _records)
                sb.AppendLine($"{r.Index};{r.AbsoluteTime:F3};{r.Time:F3};{r.Distance:F3};{r.Size:F3};{r.ID:F3}");

            File.WriteAllText(Path.Combine(folder, $"hits_{timestamp}.csv"), sb.ToString());

            // Summary
            var sb2 = new StringBuilder();
            sb2.AppendLine("total_time;targets_hit;error_rate;avg_ID;avg_time;score");
            sb2.AppendLine($"{totalTime:F3};{_targetsHit};{errorRate:F3};{avgID:F3};{avgTime:F3};{score:F3}");

            File.WriteAllText(Path.Combine(folder, $"summary_{timestamp}.csv"), sb2.ToString());
        }
    }
}