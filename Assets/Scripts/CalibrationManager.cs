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

    private readonly string[] _instructions = {
        "Tendez le bras au maximum vers l'AVANT au niveau de votre nez et appuyez sur le trigger.",
        "Ramenez le bras collé à votre nez et appuyez sur le trigger.",
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
        {
            _rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            return;
        }

        _rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger);

        if (trigger && !_prevTrigger && _step < 6)
        {
            _rightHand.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localPos);
            Transform xrOrigin = Camera.main.transform.parent?.parent ?? Camera.main.transform.parent;
            Vector3 worldPos = xrOrigin != null ? xrOrigin.TransformPoint(localPos) : localPos;

            CalibrationData.CalibrationPoints[_step] = worldPos;
            Debug.Log($"[Calibration] Step {_step} ({_instructions[_step].Split(' ')[7]}) : {worldPos}");

            _step++;

            if (_step < 6)
            {
                SetText(_instructions[_step]);
            }
            else
            {
                CalibrationData.Compute();
                Debug.Log($"[Calibration] Centre: {CalibrationData.Center} | Rayon: {CalibrationData.Radius:F2}m");
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