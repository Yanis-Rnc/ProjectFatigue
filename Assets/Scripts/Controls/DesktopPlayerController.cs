using UnityEngine;

namespace Controls
{
    public class DesktopPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        public float moveSpeed = 4f;
        public float acceleration = 10f;

        [Header("Mouse")]
        public float mouseSensitivity = 2.5f;
        public float smoothTime = 0.05f;

        public Camera playerCamera;

        private float _xRotation = 0f;
        private Vector2 _currentMouseDelta;
        private Vector2 _currentMouseDeltaVelocity;

        private Vector3 _currentVelocity;

        void Start()
        {
            if (playerCamera == null)
                playerCamera = GetComponentInChildren<Camera>();

            // 🔒 Lock & hide cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            HandleMouseLook();
        }

        void HandleMouseLook()
        {
            Vector2 targetMouseDelta = new Vector2(
                Input.GetAxis("Mouse X"),
                Input.GetAxis("Mouse Y")
            );

            // Smooth mouse
            _currentMouseDelta = Vector2.SmoothDamp(
                _currentMouseDelta,
                targetMouseDelta,
                ref _currentMouseDeltaVelocity,
                smoothTime
            );

            float mouseX = _currentMouseDelta.x * mouseSensitivity * 100f * Time.deltaTime;
            float mouseY = _currentMouseDelta.y * mouseSensitivity * 100f * Time.deltaTime;

            _xRotation -= mouseY;
            _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

            playerCamera.transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }
    }
}