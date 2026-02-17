using UnityEngine;

public class DesktopPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float acceleration = 10f;

    [Header("Mouse")]
    public float mouseSensitivity = 2.5f;
    public float smoothTime = 0.05f;

    public Camera playerCamera;

    private float xRotation = 0f;
    private Vector2 currentMouseDelta;
    private Vector2 currentMouseDeltaVelocity;

    private Vector3 currentVelocity;

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
        HandleMovement();
    }

    void HandleMouseLook()
    {
        Vector2 targetMouseDelta = new Vector2(
            Input.GetAxis("Mouse X"),
            Input.GetAxis("Mouse Y")
        );

        // Smooth mouse
        currentMouseDelta = Vector2.SmoothDamp(
            currentMouseDelta,
            targetMouseDelta,
            ref currentMouseDeltaVelocity,
            smoothTime
        );

        float mouseX = currentMouseDelta.x * mouseSensitivity * 100f * Time.deltaTime;
        float mouseY = currentMouseDelta.y * mouseSensitivity * 100f * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 targetMove = (transform.right * moveX + transform.forward * moveZ) * moveSpeed;

        currentVelocity = Vector3.Lerp(currentVelocity, targetMove, acceleration * Time.deltaTime);

        transform.position += currentVelocity * Time.deltaTime;
    }
}