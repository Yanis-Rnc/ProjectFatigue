using FittsLaw;
using UnityEngine;
using UnityEngine.XR;

public class VRRayInteractor : MonoBehaviour
{
    public float rayDistance = 0.025f;
    public FittsExperimentManager manager;
    public Transform rayOrigin;

    private InputDevice _rightHand;
    private bool _prevTrigger = false;
    private LineRenderer _line;

    void Start()
    {
        if (rayOrigin == null)
            rayOrigin = transform.Find("Poke Interactor") ?? transform;

        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = 0.002f;
        _line.endWidth = 0.002f;
        _line.material = new Material(Shader.Find("Sprites/Default"));
        _line.startColor = Color.red;
        _line.endColor = Color.red;
    }

    void Update()
    {
        if (!_rightHand.isValid)
        {
            _rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            _line.enabled = false;
            return;
        }

        Vector3 worldPos = rayOrigin.position - rayOrigin.forward * .025f;
        Vector3 worldDir = rayOrigin.forward;

        Vector3 endPoint = Physics.Raycast(new Ray(worldPos, worldDir), out RaycastHit previewHit, rayDistance)
            ? previewHit.point
            : worldPos + worldDir * rayDistance;

        _line.enabled = true;
        _line.SetPosition(0, worldPos);
        _line.SetPosition(1, endPoint);

        _rightHand.TryGetFeatureValue(CommonUsages.triggerButton, out bool trigger);

        if (trigger && !_prevTrigger)
        {
            if (Physics.Raycast(new Ray(worldPos, worldDir), out RaycastHit hit, rayDistance))
            {
                FittsTarget t = hit.collider.GetComponent<FittsTarget>();
                if (t != null) { manager.RegisterShot(true); t.OnTargetHit(); }
                else manager.RegisterShot(false);
            }
            else manager.RegisterShot(false);
        }

        _prevTrigger = trigger;
    }
}