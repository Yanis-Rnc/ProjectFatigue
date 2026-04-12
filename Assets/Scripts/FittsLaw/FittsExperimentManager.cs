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
    public class FittsExperimentManager : MonoBehaviour
    {
        public Camera playerCamera;
        public GameObject targetPrefab;
        public string exportPath;

        private List<GameObject> _currentTargets = new List<GameObject>();
        private int _targetsHit;
        private readonly float[] _sizes = { 0.025f, 0.05f, .1f };
        private float _experimentStartTime;
        private int _totalShots;
        private int _missedShots;
        private float _totalDifficulty;

        private float _lastHitTime;
        private Vector3 _lastTargetPos = Vector3.zero;
        private bool _experimentRunning = true;

        private Vector3[] _spawnSequence;
        private int _spawnIndex = 0;

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
            bool desktopStop = Input.GetKeyDown(KeyCode.S);
            bool vrStop = false;

            if (XRSettings.isDeviceActive)
            {
                InputDevice rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
                if (rightHand.isValid)
                    rightHand.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out vrStop);
            }

            if (_experimentRunning && (desktopStop || vrStop))
            {
                _experimentRunning = false;
                StopExperiment();
            }
        }

        void BuildSpawnSequence()
        {
            Vector3 c = CalibrationData.Center;
            float r = CalibrationData.Radius;

            Vector3 x0 = c + new Vector3(0, r, 0);
            Vector3 x1 = c + new Vector3(0, 0, r);
            Vector3 x2 = c + new Vector3(0, -r, 0);
            Vector3 x3 = c + new Vector3(0, 0, -r);

            Vector3 y0 = c + new Vector3(0, 0, r);
            Vector3 y1 = c + new Vector3(r, 0, 0);
            Vector3 y2 = c + new Vector3(0, 0, -r);
            Vector3 y3 = c + new Vector3(-r, 0, 0);

            Vector3 z0 = c + new Vector3(0, r, 0);
            Vector3 z1 = c + new Vector3(r, 0, 0);
            Vector3 z2 = c + new Vector3(0, -r, 0);
            Vector3 z3 = c + new Vector3(-r, 0, 0);

            _spawnSequence = new Vector3[] { x0, x1, x2, x3, y0, y1, y2, y3, z0, z1, z2, z3, c };
            _spawnIndex = 0;
        }

        void SpawnNextTarget()
        {
            _currentTargets.Clear();

            Vector3 spawnPos = _spawnSequence[_spawnIndex % _spawnSequence.Length];
            _spawnIndex++;

            float size = _sizes[Random.Range(0, _sizes.Length)];

            GameObject target = Instantiate(targetPrefab, spawnPos, Quaternion.identity);
            target.transform.localScale = Vector3.one * size;
            target.GetComponent<FittsTarget>().Init(this);
            _currentTargets.Add(target);

            Debug.Log($"[SPAWN] #{_targetsHit + 1} | Pos: {spawnPos} | Size: {size} | SeqIndex: {_spawnIndex - 1}");
        }

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

            Debug.Log($"[HIT] #{_targetsHit} | Pos: {target.transform.position} | Size: {size} | Distance: {distanceFromLast:F2} | ID: {id:F2} | Time: {timeSinceLast:F2}s");

            _currentTargets.Remove(target.gameObject);
            Destroy(target.gameObject);

            if (_experimentRunning)
                SpawnNextTarget();
        }

        public void RegisterShot(bool hit, GameObject target = null)
        {
            _totalShots++;
            if (!hit)
            {
                _missedShots++;
                Debug.Log($"[MISS] Miss #{_missedShots} | Shot #{_totalShots}");
            }
        }

        void StopExperiment()
        {
            foreach (var t in _currentTargets)
                if (t != null) Destroy(t);
            _currentTargets.Clear();
            _lastTargetPos = Vector3.zero;

            float totalTime = Time.time - _experimentStartTime;
            float avgTimePerTarget = _targetsHit > 0 ? totalTime / _targetsHit : 0f;
            float errorRate = _totalShots > 0 ? (float)_missedShots / _totalShots : 0f;
            float avgDifficulty = _targetsHit > 0 ? _totalDifficulty / _targetsHit : 0f;
            float score = avgDifficulty > 0 && avgTimePerTarget > 0
                ? (avgDifficulty * 100f) / (avgTimePerTarget * (1f + errorRate))
                : 0f;

            var hitTimes = _records.Select(r => r.Time).ToList();
            float firstHalfAvg = 0f, secondHalfAvg = 0f, slowdown = 0f;
            if (hitTimes.Count > 1)
            {
                int half = hitTimes.Count / 2;
                firstHalfAvg = hitTimes.Take(half).Average();
                secondHalfAvg = hitTimes.Skip(half).Average();
                slowdown = secondHalfAvg - firstHalfAvg;
            }

            Debug.Log("========== EXPERIMENT RESULTS ==========");
            Debug.Log($"Total Time    : {totalTime:F2}s");
            Debug.Log($"Targets Hit   : {_targetsHit}");
            Debug.Log($"Total Shots   : {_totalShots}");
            Debug.Log($"Missed Shots  : {_missedShots}");
            Debug.Log($"Error Rate    : {errorRate:F3}");
            Debug.Log($"Avg ID        : {avgDifficulty:F3}");
            Debug.Log($"Avg Time/Hit  : {avgTimePerTarget:F3}s");
            Debug.Log($"Score         : {score:F3}");
            Debug.Log($"First Half Avg: {firstHalfAvg:F3}s | Second Half Avg: {secondHalfAvg:F3}s | Slowdown: {slowdown:F3}s");

            ExportCSV(totalTime, avgTimePerTarget, errorRate, avgDifficulty, score, firstHalfAvg, secondHalfAvg, slowdown);

            Debug.Log("========== END ==========");

            SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
        }

        void ExportCSV(float totalTime, float avgTime, float errorRate, float avgID, float score, float firstHalf, float secondHalf, float slowdown)
        {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string basePath = string.IsNullOrEmpty(exportPath)
                ? System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop)
                : exportPath;
            string folder = Path.Combine(basePath, $"FittsData_{timestamp}");
            
            Directory.CreateDirectory(folder);

            var sb = new StringBuilder();
            sb.AppendLine("hit_index;absolute_time;time_since_last;distance;size;ID;pos_x;pos_y;pos_z;seq_index");
            foreach (var r in _records)
            {
                sb.AppendLine($"{r.Index};{r.AbsoluteTime:F3};{r.Time:F3};{r.Distance:F3};{r.Size:F3};{r.ID:F3};{r.Position.x:F3};{r.Position.y:F3};{r.Position.z:F3};{r.SeqIndex}");
            }
            string hitsPath = Path.Combine(folder, $"hits_{timestamp}.csv");
            File.WriteAllText(hitsPath, sb.ToString());
            Debug.Log($"[EXPORT] Hits CSV: {hitsPath}");

            var sb2 = new StringBuilder();
            sb2.AppendLine("timestamp;total_time;targets_hit;total_shots;missed_shots;error_rate;avg_ID;avg_time_per_hit;score;first_half_avg;second_half_avg;slowdown;calibration_radius;calibration_center_x;calibration_center_y;calibration_center_z");
            sb2.AppendLine($"{timestamp};{totalTime:F3};{_targetsHit};{_totalShots};{_missedShots};{errorRate:F3};{avgID:F3};{avgTime:F3};{score:F3};{firstHalf:F3};{secondHalf:F3};{slowdown:F3};{CalibrationData.Radius:F3};{CalibrationData.Center.x:F3};{CalibrationData.Center.y:F3};{CalibrationData.Center.z:F3}");
            string summaryPath = Path.Combine(folder, $"summary_{timestamp}.csv");
            File.WriteAllText(summaryPath, sb2.ToString());
            Debug.Log($"[EXPORT] Summary CSV: {summaryPath}");
        }
    }
}