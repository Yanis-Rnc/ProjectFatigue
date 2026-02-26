using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace FittsLaw
{
    public class FittsExperimentManager : MonoBehaviour
    {
        public GameObject targetPrefab;
        public float[] sizes = { 0.25f, 0.5f, 1f };
        public float[] distances = { 2.5f, 5f, 10f };
        public int targetsPerWave = 5;
        public float maxYawAngleFromCenter = 25;
        public float maxPitchAngleFromCenter = 65;
        public int maxWaves = 10;

        private Quaternion _initialCameraRotation;
        private Vector3 _initialCameraPosition;
        private List<GameObject> _currentTargets = new List<GameObject>();
        private int _targetsHit;
        private float _waveStartTime;
        private int _totalShots;
        private int _missedShots;
        private float _waveDifficulty;
        private int _waveIndex;

        private List<float> _waveScores = new List<float>();
        private List<float> _waveTimes = new List<float>();
        private List<float> _waveErrorRates = new List<float>();

        void Start()
        {
            Camera cam = Camera.main;
            _initialCameraRotation = cam.transform.rotation;
            _initialCameraPosition = cam.transform.position;
            SpawnWave();
        }

        void SpawnWave()
        {
            if (_waveIndex >= maxWaves)
            {
                ShowSummary();
                return;
            }

            _waveIndex++;
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

            _targetsHit++;
            float distance = Vector3.Distance(Camera.main.transform.position, target.transform.position);
            float size = target.transform.localScale.x;
            float id = Mathf.Log((2 * distance) / size, 2);
            _waveDifficulty += id;

            _currentTargets.Remove(target.gameObject);
            Destroy(target.gameObject);

            if (_targetsHit >= targetsPerWave)
            {
                _waveDifficulty /= targetsPerWave;
                EndWave();
            }
        }

        public void RegisterShot(bool hit)
        {
            _totalShots++;
            if (!hit) _missedShots++;
        }

        void EndWave()
        {
            float waveTime = Time.time - _waveStartTime;
            float averageTime = waveTime / targetsPerWave;
            float errorRate = _totalShots > 0 ? (float)_missedShots / _totalShots : 0f;
            float score = (_waveDifficulty * 100f) / (averageTime * (1f + errorRate));

            _waveScores.Add(score);
            _waveTimes.Add(averageTime);
            _waveErrorRates.Add(errorRate);

            SpawnWave();
        }

        void ShowSummary()
        {
            float avgScore = _waveScores.Average();
            float avgTime = _waveTimes.Average();
            float avgErrorRate = _waveErrorRates.Average();

            Debug.Log("=== EXPERIMENT SUMMARY ===");
            Debug.Log($"Average Score: {avgScore:F2}");
            Debug.Log($"Average Time per Target: {avgTime:F2}s");
            Debug.Log($"Average Error Rate: {avgErrorRate:P1}");
        }
    }
}