using FittsLaw;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Handles ray-based interaction in VR.
/// Casts a short ray to detect and hit targets.
/// </summary>
public class VRRayInteractor : MonoBehaviour
{
    public float rayDistance = 0.025f;
    public FittsExperimentManager manager;
    public Transform rayOrigin;

    private InputDevice _rightHand;
    private InputDevice _leftHand;

    private bool _prevTriggerRight = false;
    private bool _prevTriggerLeft = false;

    private LineRenderer _line;

    void Start()
    {
        if (rayOrigin == null)
            rayOrigin = transform.Find("Poke Interactor") ?? transform;

        // Visual ray
        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = 0.002f;
        _line.endWidth = 0.002f;
        _line.material = new Material(Shader.Find("Sprites/Default"));
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

        Vector3 origin = rayOrigin.position - rayOrigin.forward * .025f;
        Vector3 direction = rayOrigin.forward;

        // Draw ray
        Vector3 endPoint = Physics.Raycast(origin, direction, out RaycastHit previewHit, rayDistance)
            ? previewHit.point
            : origin + direction * rayDistance;

        _line.SetPosition(0, origin);
        _line.SetPosition(1, endPoint);

        if (triggered)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, rayDistance))
            {
                FittsTarget t = hit.collider.GetComponent<FittsTarget>();

                if (t != null)
                {
                    manager.RegisterShot(true);
                    t.OnTargetHit();
                }
                else manager.RegisterShot(false);
            }
            else manager.RegisterShot(false);
        }

        _prevTriggerRight = triggerRight;
        _prevTriggerLeft = triggerLeft;
    }
}