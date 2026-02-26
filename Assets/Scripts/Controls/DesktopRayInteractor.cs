using FittsLaw;
using UnityEngine;

namespace Controls
{
    public class DesktopRayInteractor : MonoBehaviour
    {
        public float rayDistance = 1000f;
        public FittsExperimentManager manager;

        private Camera _playerCamera;

        void Start()
        {
            _playerCamera = GetComponentInChildren<Camera>();
            if (_playerCamera == null)
                _playerCamera = Camera.main;
        }

        private void Update()
        {
            HandleClick();
        }

        void HandleClick()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = new Ray(_playerCamera.transform.position, _playerCamera.transform.forward);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, rayDistance))
                {
                    hit.collider.SendMessage("OnTargetHit", SendMessageOptions.DontRequireReceiver);
                }
                else
                {
                    manager.RegisterShot(false);
                }
            }
        }
    }
}