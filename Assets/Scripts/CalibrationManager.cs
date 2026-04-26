using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Handles the calibration process before the experiment.
/// Guides the user and records spatial bounds.
/// </summary>
public class CalibrationManager : MonoBehaviour
{
    public string experimentSceneName = "ExperimentScene";
    public TextMeshProUGUI instructionText;

    private InputDevice _rightHand;
    private InputDevice _leftHand;
    
    private bool _prevTriggerRight = false;
    private bool _prevTriggerLeft = false;
    
    private int _step = 0;

    /// <summary>
    /// Instructions displayed to the user.
    /// </summary>
    private readonly string[] _instructions = {
        "Tendez le bras au maximum vers l'AVANT au niveau de votre nez et appuyez sur le trigger.",
        "Ramenez le bras à proximité de votre nez et appuyez sur le trigger.",
        "Tendez le bras au maximum vers la DROITE tout en restant dans votre champ de vision et appuyez sur le trigger.",
        "Tendez le bras au maximum vers la GAUCHE tout en restant dans votre champ de vision et appuyez sur le trigger.",
        "Tendez le bras au maximum vers le HAUT tout en restant dans votre champ de vision et appuyez sur le trigger.",
        "Tendez le bras au maximum vers le BAS tout en restant dans votre champ de vision et appuyez sur le trigger.",
    };

    void Start()
    {
        SetText(_instructions[0]);
    }

    void Update()
    {
        if (!_rightHand.isValid)
            _rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
        if (!_leftHand.isValid)
            _leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);

        _rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerRight);
        _leftHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool triggerLeft);

        bool triggered = (triggerRight && !_prevTriggerRight) || (triggerLeft && !_prevTriggerLeft);

        if (triggered && _step < 6)
        {
            InputDevice activeHand = (triggerRight && !_prevTriggerRight) ? _rightHand : _leftHand;
            activeHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos);

            Transform xrOrigin = Camera.main.transform.parent?.parent ?? Camera.main.transform.parent;
            Vector3 worldPos = xrOrigin != null ? xrOrigin.TransformPoint(localPos) : localPos;

            CalibrationData.CalibrationPoints[_step] = worldPos;
            Debug.Log($"[Calibration] Step {_step} : {worldPos}");

            _step++;

            if (_step < 6)
                SetText(_instructions[_step]);
            else
            {
                CalibrationData.Compute();
                Debug.Log($"[Calibration] Centre: {CalibrationData.Center} | Depth: {CalibrationData.RadiusDepth:F2} | Width: {CalibrationData.RadiusWidth:F2} | Height: {CalibrationData.RadiusHeight:F2}");
                SetText("Calibration terminée !");
                SceneManager.LoadScene(experimentSceneName);
            }
        }

        _prevTriggerRight = triggerRight;
        _prevTriggerLeft = triggerLeft;
    }

    void SetText(string msg)
    {
        if (instructionText != null)
            instructionText.text = msg;
    }
}