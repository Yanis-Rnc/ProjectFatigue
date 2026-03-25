    using UnityEngine;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine.XR;

    namespace FittsLaw
    {
        public class FittsExperimentManager : MonoBehaviour
        {
            public Camera playerCamera;
            public GameObject targetPrefab;
            public float[] sizes = { 0.25f, 0.5f, 1f };
            public float[] distances = { 2.5f, 5f, 10f };
            public int targetsPerWave = 5;
            public float maxYawAngleFromCenter = 25;
            public float maxPitchAngleFromCenter = 65;

            private Quaternion _initialCameraRotation;
            private Vector3 _initialCameraPosition;
            private List<GameObject> _currentTargets = new List<GameObject>();
            private int _targetsHit;
            private float _waveStartTime;
            private int _totalShots;
            private int _missedShots;
            private float _waveDifficulty;

            private List<float> _waveScores = new List<float>();
            private List<float> _waveTimes = new List<float>();
            private List<float> _waveErrorRates = new List<float>();

            private Vector3 _lastTargetPos = Vector3.zero;
            private bool _experimentRunning = true;

            void Start()
            {
                if (playerCamera == null)
                    playerCamera = Camera.main;
                
                Camera cam = playerCamera;
                _initialCameraRotation = cam.transform.rotation;
                _initialCameraPosition = cam.transform.position;

                SpawnWave();
            }

            void Update()
            {
                bool desktopStop = Input.GetKeyDown(KeyCode.S);

                bool vrStop = false;

                if (XRSettings.isDeviceActive)
                {
                    InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

                    if (leftHand.isValid)
                    {
                        leftHand.TryGetFeatureValue(CommonUsages.primary2DAxisClick, out vrStop);
                    }
                }

                if (_experimentRunning && (desktopStop || vrStop))
                {
                    _experimentRunning = false;
                    StopExperiment();
                }
            }

            void SpawnWave()
            {
                _totalShots = 0;
                _missedShots = 0;
                _waveDifficulty = 0f;
                _waveStartTime = Time.time;
                _targetsHit = 0;
                _currentTargets.Clear();

                for (int i = 0; i < targetsPerWave; i++)
                    SpawnRandomTarget();
            }

            void SpawnRandomTarget()
            {
                float size = sizes[Random.Range(0, sizes.Length)];
                float distance = distances[Random.Range(0, distances.Length)];
                float yaw = Random.Range(-maxYawAngleFromCenter, maxYawAngleFromCenter);
                float pitch = Random.Range(-maxPitchAngleFromCenter, maxPitchAngleFromCenter);

                Vector3 direction = _initialCameraRotation * Quaternion.Euler(pitch, yaw, 0) * Vector3.forward;
                Vector3 spawnPos = _initialCameraPosition + direction.normalized * distance;

                GameObject target = Instantiate(targetPrefab, spawnPos, Quaternion.identity);
                target.transform.localScale = Vector3.one * size;
                target.GetComponent<FittsTarget>().Init(this);
                _currentTargets.Add(target);
            }

            public void TargetHit(FittsTarget target)
            {
                if (!_currentTargets.Contains(target.gameObject))
                    return;

                float distanceFromLast = _lastTargetPos == Vector3.zero ? 0f : Vector3.Distance(target.transform.position, _lastTargetPos);
                _lastTargetPos = target.transform.position;

                _targetsHit++;
                float distance;

                if (_lastTargetPos == Vector3.zero)
                {
                    distance = Vector3.Distance(playerCamera.transform.position, target.transform.position);
                }
                else
                {
                    distance = Vector3.Distance(_lastTargetPos, target.transform.position);
                }
                float size = target.transform.localScale.x;
                float id = Mathf.Log((2 * distance) / size, 2);
                _waveDifficulty += id;

                Debug.Log($"Target Hit | Distance from last: {distanceFromLast:F2} | Success | Size: {size} | Position: {target.transform.position}");

                _currentTargets.Remove(target.gameObject);
                Destroy(target.gameObject);

                if (_targetsHit >= targetsPerWave && _experimentRunning)
                {
                    _waveDifficulty /= targetsPerWave;
                    EndWave();
                }
            }

            public void RegisterShot(bool hit, GameObject target = null)
            {
                _totalShots++;
                if (!hit && target != null)
                {
                    _missedShots++;
                    float distanceFromLast = (_lastTargetPos == Vector3.zero) ? 0f : Vector3.Distance(target.transform.position, _lastTargetPos);
                    Debug.Log($"Target Missed | Distance from last: {distanceFromLast:F2} | Size: {target.transform.localScale.x} | Position: {target.transform.position}");
                }
            }

            void EndWave()
            {
                if (!_experimentRunning) return;

                float waveTime = Time.time - _waveStartTime;
                float averageTime = waveTime / targetsPerWave;
                float errorRate = _totalShots > 0 ? (float)_missedShots / _totalShots : 0f;
                float score = (_waveDifficulty * 100f) / (averageTime * (1f + errorRate));

                _waveScores.Add(score);
                _waveTimes.Add(averageTime);
                _waveErrorRates.Add(errorRate);

                SpawnWave();
            }

            void StopExperiment()
            {
                foreach (var target in _currentTargets)
                {
                    if (target != null)
                        Destroy(target);
                }

                _currentTargets.Clear();

                _lastTargetPos = Vector3.zero;

                Debug.Log("========== EXPERIMENT RAW DATA ==========");
            
                int waveCount = _waveScores.Count;
            
                for (int i = 0; i < waveCount; i++)
                {
                    float score = _waveScores[i];
                    float time = _waveTimes[i];
                    float error = _waveErrorRates[i];
            
                    Debug.Log(
                        $"Wave {i + 1} | Score: {score:F3} | AvgTime: {time:F3} | ErrorRate: {error:F3}"
                    );
                }
            
                Debug.Log("========== GLOBAL STATS ==========");
            
                float avgScore = _waveScores.Count > 0 ? _waveScores.Average() : 0f;
                float minScore = _waveScores.Count > 0 ? _waveScores.Min() : 0f;
                float maxScore = _waveScores.Count > 0 ? _waveScores.Max() : 0f;
            
                float avgTime = _waveTimes.Count > 0 ? _waveTimes.Average() : 0f;
                float minTime = _waveTimes.Count > 0 ? _waveTimes.Min() : 0f;
                float maxTime = _waveTimes.Count > 0 ? _waveTimes.Max() : 0f;
            
                float avgError = _waveErrorRates.Count > 0 ? _waveErrorRates.Average() : 0f;
                float minError = _waveErrorRates.Count > 0 ? _waveErrorRates.Min() : 0f;
                float maxError = _waveErrorRates.Count > 0 ? _waveErrorRates.Max() : 0f;
            
                Debug.Log($"Score | Avg: {avgScore:F3} | Min: {minScore:F3} | Max: {maxScore:F3}");
                Debug.Log($"Time  | Avg: {avgTime:F3} | Min: {minTime:F3} | Max: {maxTime:F3}");
                Debug.Log($"Error | Avg: {avgError:F3} | Min: {minError:F3} | Max: {maxError:F3}");
            
                Debug.Log("========== FATIGUE ANALYSIS ==========");
            
                if (waveCount > 1)
                {
                    float firstScore = _waveScores.First();
                    float lastScore = _waveScores.Last();
            
                    float firstTime = _waveTimes.First();
                    float lastTime = _waveTimes.Last();
            
                    float firstError = _waveErrorRates.First();
                    float lastError = _waveErrorRates.Last();
            
                    Debug.Log($"Score Evolution: {firstScore:F3} → {lastScore:F3} (Δ {lastScore - firstScore:F3})");
                    Debug.Log($"Time Evolution: {firstTime:F3} → {lastTime:F3} (Δ {lastTime - firstTime:F3})");
                    Debug.Log($"Error Evolution: {firstError:F3} → {lastError:F3} (Δ {lastError - firstError:F3})");
                }
            
                Debug.Log("========== END ==========");
            }
        }
    }