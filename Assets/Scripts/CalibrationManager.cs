using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using TMPro;

public class CalibrationManager : MonoBehaviour
{
    public string experimentSceneName = "ExperimentScene";
    public TextMeshProUGUI instructionText;

    private InputDevice _rightHand;
    private bool _prevTrigger = false;
    private int _step = 0;

    void Start()
    {
        SetText("Rétractez le bras et appuyez sur le trigger.");
    }

    void Update()
    {
        if (!_rightHand.isValid)
        {
            _rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            return;
        }

        _rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger);

        if (trigger && !_prevTrigger)
        {
            Vector3 handPos = Vector3.zero;
            _rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out handPos);

            Transform xrOrigin = Camera.main.transform.parent?.parent ?? Camera.main.transform.parent;
            Vector3 handWorldPos = xrOrigin != null ? xrOrigin.TransformPoint(handPos) : handPos;
            float dist = Vector3.Distance(Camera.main.transform.position, handWorldPos);

            if (_step == 0)
            {
                CalibrationData.MinDistance = dist;
                Debug.Log($"[Calibration] Bras rétracté : {dist:F2}m");
                _step = 1;
                SetText("Tendez le bras et appuyez sur le trigger.");
            }
            else if (_step == 1)
            {
                CalibrationData.MaxDistance = dist;
                Debug.Log($"[Calibration] Bras tendu : {dist:F2}m");
                SetText("Calibration terminée !");
                SceneManager.LoadScene(experimentSceneName);
            }
        }

        _prevTrigger = trigger;
    }

    void SetText(string msg)
    {
        if (instructionText != null)
            instructionText.text = msg;
    }
}