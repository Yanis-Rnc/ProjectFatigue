using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace FittsLaw
{
    public class FittsExperimentManager : MonoBehaviour
    {
        public Camera playerCamera;
        public GameObject targetPrefab;
        public float maxYawAngleFromCenter = 30f;
        public float maxPitchAngleFromCenter = 20f;

        private List<GameObject> _currentTargets = new List<GameObject>();
        private int _targetsHit;
        private readonly float[] _sizes = { 0.025f, 0.05f, .1f };
        private float _experimentStartTime;
        private int _totalShots;
        private int _missedShots;
        private float _totalDifficulty;

        private List<float> _hitTimes = new List<float>();
        private float _lastHitTime;

        private Vector3 _lastTargetPos = Vector3.zero;
        private bool _experimentRunning = true;

        private Vector3 _spawnCameraPosition;
        private Quaternion _spawnCameraRotation;

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
            _spawnCameraPosition = playerCamera.transform.position;
            _spawnCameraRotation = playerCamera.transform.rotation;
            SpawnRandomTarget();
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

        void SpawnRandomTarget()
        {
            _spawnCameraPosition = playerCamera.transform.position;
            _spawnCameraRotation = playerCamera.transform.rotation;

            _currentTargets.Clear();

            float size = _sizes[Random.Range(0, _sizes.Length)];
            float distance = Random.Range(CalibrationData.MinDistance, CalibrationData.MaxDistance);
            float yaw = Random.Range(-maxYawAngleFromCenter, maxYawAngleFromCenter);
            float pitch = Random.Range(-maxPitchAngleFromCenter, maxPitchAngleFromCenter);

            Vector3 direction = _spawnCameraRotation * Vector3.forward;
            direction = Quaternion.AngleAxis(yaw, _spawnCameraRotation * Vector3.up) * direction;
            direction = Quaternion.AngleAxis(pitch, _spawnCameraRotation * Vector3.right) * direction;

            Vector3 spawnPos = _spawnCameraPosition + direction.normalized * distance;

            GameObject target = Instantiate(targetPrefab, spawnPos, Quaternion.identity);
            target.transform.localScale = Vector3.one * size;
            target.GetComponent<FittsTarget>().Init(this);
            _currentTargets.Add(target);

            Debug.Log($"[SPAWN] #{_targetsHit + 1} | Pos: {spawnPos} | Size: {size} | Distance: {distance:F2} | Yaw: {yaw:F1} | Pitch: {pitch:F1} | CamPos: {_spawnCameraPosition} | CamFwd: {_spawnCameraRotation * Vector3.forward}");
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
            _hitTimes.Add(timeSinceLast);
            _lastHitTime = Time.time;

            Debug.Log($"[HIT] #{_targetsHit} | Pos: {target.transform.position} | Size: {size} | Distance: {distanceFromLast:F2} | ID: {id:F2} | Time: {timeSinceLast:F2}s | CamPos: {playerCamera.transform.position} | CamFwd: {playerCamera.transform.forward}");

            _currentTargets.Remove(target.gameObject);
            Destroy(target.gameObject);

            if (_experimentRunning)
                SpawnRandomTarget();
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

            Debug.Log("========== HIT TIMES ==========");
            for (int i = 0; i < _hitTimes.Count; i++)
                Debug.Log($"Hit #{i + 1} | Time: {_hitTimes[i]:F3}s");

            Debug.Log("========== EXPERIMENT RESULTS ==========");
            Debug.Log($"Total Time    : {totalTime:F2}s");
            Debug.Log($"Targets Hit   : {_targetsHit}");
            Debug.Log($"Total Shots   : {_totalShots}");
            Debug.Log($"Missed Shots  : {_missedShots}");
            Debug.Log($"Error Rate    : {errorRate:F3}");
            Debug.Log($"Avg ID        : {avgDifficulty:F3}");
            Debug.Log($"Avg Time/Hit  : {avgTimePerTarget:F3}s");
            Debug.Log($"Score         : {score:F3}");

            if (_hitTimes.Count > 1)
            {
                int half = _hitTimes.Count / 2;
                float firstHalf = _hitTimes.Take(half).Average();
                float secondHalf = _hitTimes.Skip(half).Average();
                Debug.Log("========== FATIGUE ANALYSIS ==========");
                Debug.Log($"Avg Time First Half  : {firstHalf:F3}s");
                Debug.Log($"Avg Time Second Half : {secondHalf:F3}s");
                Debug.Log($"Slowdown             : {secondHalf - firstHalf:F3}s");
            }

            Debug.Log("========== END ==========");

            SceneManager.LoadScene("MenuScene", LoadSceneMode.Single);
        }
    }
}