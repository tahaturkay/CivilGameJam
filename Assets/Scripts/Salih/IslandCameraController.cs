using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Orthographic navigation for the island scene. The island rectangle is measured on world X/Z
/// at boundsCenter.y, so navigation also works on the island's raised ground.
/// </summary>
[RequireComponent(typeof(Camera))]
public class IslandCameraController : MonoBehaviour
{
    [Header("Island bounds (world X/Z)")]
    public Vector3 boundsCenter = Vector3.zero;
    public Vector2 boundsHalfExtents = new Vector2(90f, 72.5f);

    [Header("Orthographic zoom")]
    [Min(1f)] public float minOrthographicSize = 18f;
    [Min(1f)] public float maxOrthographicSize = 140f;
    [Min(1f)] public float homeOrthographicSize = 100f;
    [Min(0.01f)] public float zoomSensitivity = 0.2f;

    [Header("Keyboard pan")]
    public bool keyboardPanEnabled = true;
    [Min(1f)] public float keyboardPanSpeed = 45f;

    [Header("Scene references")]
    public DisasterController disasterController;

    private Camera cachedCamera;
    private bool dragging;
    private Vector2 lastPointerPosition;

    private void Awake()
    {
        cachedCamera = GetComponent<Camera>();
        if (disasterController == null)
        {
            disasterController = GetComponent<DisasterController>();
            if (disasterController == null)
                disasterController = FindAnyObjectByType<DisasterController>();
        }

        NormalizeZoomSettings();
    }

    private void OnDisable()
    {
        dragging = false;
    }

    private void Update()
    {
        if (cachedCamera == null || !cachedCamera.orthographic) return;

        if (ControlsBlocked() || IsPointerOverUi())
        {
            dragging = false;
            return;
        }

        if (HomePressed())
        {
            FrameHome();
            dragging = false;
            return;
        }

        Vector2 pointerPosition;
        float wheel;
        bool middlePressed, middleHeld, rightPressed, rightHeld;
        bool hasMouse = ReadMouse(out pointerPosition, out wheel,
            out middlePressed, out middleHeld, out rightPressed, out rightHeld);

        if (hasMouse)
        {
            bool panPressed = middlePressed || rightPressed;
            bool panHeld = middleHeld || rightHeld;
            if (panPressed)
            {
                dragging = true;
                lastPointerPosition = pointerPosition;
            }

            if (dragging && panHeld)
            {
                Vector3 previousGroundPoint;
                Vector3 currentGroundPoint;
                if (TryGetGroundPoint(lastPointerPosition, out previousGroundPoint) &&
                    TryGetGroundPoint(pointerPosition, out currentGroundPoint))
                {
                    Vector3 pan = previousGroundPoint - currentGroundPoint;
                    pan.y = 0f;
                    MoveCamera(pan);
                    ClampCameraCenterToBounds();
                }

                lastPointerPosition = pointerPosition;
            }
            else if (!panHeld)
            {
                dragging = false;
            }

            if (Mathf.Abs(wheel) > 0.001f)
                ZoomAt(pointerPosition, wheel);
        }

        if (keyboardPanEnabled)
        {
            Vector2 keyboardDirection = ReadKeyboardPan();
            if (keyboardDirection.sqrMagnitude > 0f)
            {
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                if (right.sqrMagnitude < 0.001f) right = Vector3.right;

                Vector3 movement = right * keyboardDirection.x + forward * keyboardDirection.y;
                if (movement.sqrMagnitude > 1f) movement.Normalize();
                MoveCamera(movement * keyboardPanSpeed * Time.unscaledDeltaTime);
                ClampCameraCenterToBounds();
            }
        }
    }

    /// <summary>
    /// Sets the island rectangle and zoom framing, then centers the camera on a full-island view.
    /// halfExtents are the positive X/Z half-widths around center.
    /// </summary>
    public void Configure(Vector3 center, Vector2 halfExtents, float minimumZoom,
        float maximumZoom, float homeZoom)
    {
        boundsCenter = center;
        boundsHalfExtents = new Vector2(Mathf.Abs(halfExtents.x), Mathf.Abs(halfExtents.y));
        minOrthographicSize = Mathf.Max(1f, minimumZoom);
        maxOrthographicSize = Mathf.Max(minOrthographicSize, maximumZoom);
        homeOrthographicSize = Mathf.Clamp(homeZoom, minOrthographicSize, maxOrthographicSize);
        NormalizeZoomSettings();
        FrameHome();
    }

    /// <summary>Returns the camera to a centered view that fits the configured island bounds.</summary>
    public void FrameHome()
    {
        EnsureCameraReference();
        if (cachedCamera == null || !cachedCamera.orthographic) return;

        NormalizeZoomSettings();
        float fitSize = CalculateIslandFitSize();
        cachedCamera.orthographicSize = Mathf.Clamp(
            Mathf.Max(homeOrthographicSize, fitSize), minOrthographicSize, maxOrthographicSize);
        SetCameraCenter(boundsCenter);
    }

    private void ZoomAt(Vector2 screenPosition, float wheel)
    {
        Vector3 anchorBefore;
        bool hasAnchor = TryGetGroundPoint(screenPosition, out anchorBefore);

        float newSize = cachedCamera.orthographicSize * Mathf.Exp(-wheel * zoomSensitivity);
        cachedCamera.orthographicSize = Mathf.Clamp(newSize, minOrthographicSize, maxOrthographicSize);

        Vector3 anchorAfter;
        if (hasAnchor && TryGetGroundPoint(screenPosition, out anchorAfter))
        {
            Vector3 correction = anchorBefore - anchorAfter;
            correction.y = 0f;
            MoveCamera(correction);
        }

        ClampCameraCenterToBounds();
    }

    private float CalculateIslandFitSize()
    {
        float halfWidth = Mathf.Abs(boundsHalfExtents.x);
        float halfDepth = Mathf.Abs(boundsHalfExtents.y);
        Vector3 cameraRight = transform.right;
        Vector3 cameraUp = transform.up;

        float projectedWidth = Mathf.Abs(cameraRight.x) * halfWidth + Mathf.Abs(cameraRight.z) * halfDepth;
        float projectedHeight = Mathf.Abs(cameraUp.x) * halfWidth + Mathf.Abs(cameraUp.z) * halfDepth;
        float aspect = cachedCamera.aspect > 0f ? cachedCamera.aspect : 1f;
        return Mathf.Max(projectedHeight, projectedWidth / aspect) + 1f;
    }

    private void ClampCameraCenterToBounds()
    {
        Vector3 focus;
        if (!TryGetGroundPoint(new Vector2(cachedCamera.pixelWidth * 0.5f, cachedCamera.pixelHeight * 0.5f), out focus))
            focus = new Vector3(transform.position.x, boundsCenter.y, transform.position.z);

        Vector3 clamped = new Vector3(
            Mathf.Clamp(focus.x, boundsCenter.x - Mathf.Abs(boundsHalfExtents.x), boundsCenter.x + Mathf.Abs(boundsHalfExtents.x)),
            boundsCenter.y,
            Mathf.Clamp(focus.z, boundsCenter.z - Mathf.Abs(boundsHalfExtents.y), boundsCenter.z + Mathf.Abs(boundsHalfExtents.y)));
        Vector3 correction = clamped - focus;
        correction.y = 0f;
        if (correction.sqrMagnitude > 0.000001f) MoveCamera(correction);
    }

    private void SetCameraCenter(Vector3 worldPoint)
    {
        Vector3 focus;
        if (!TryGetGroundPoint(new Vector2(cachedCamera.pixelWidth * 0.5f, cachedCamera.pixelHeight * 0.5f), out focus))
            focus = new Vector3(transform.position.x, boundsCenter.y, transform.position.z);

        Vector3 correction = new Vector3(worldPoint.x - focus.x, 0f, worldPoint.z - focus.z);
        MoveCamera(correction);
    }

    private bool TryGetGroundPoint(Vector2 screenPosition, out Vector3 point)
    {
        Ray ray = cachedCamera.ScreenPointToRay(screenPosition);
        float distance;
        if (new Plane(Vector3.up, new Vector3(0, boundsCenter.y, 0)).Raycast(ray, out distance))
        {
            point = ray.GetPoint(distance);
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    private void MoveCamera(Vector3 worldDelta)
    {
        worldDelta.y = 0f;
        if (worldDelta.sqrMagnitude > 0.000001f)
            transform.position += worldDelta;
    }

    private bool ControlsBlocked()
    {
        GameManager manager = GameManager.Instance;
        return PauseMenu.DurdurulduMu ||
               (manager != null && (manager.oyunDurduMu || manager.afetOlduMu || manager.oyunBittiMi)) ||
               (disasterController != null && disasterController.AfetDevamEdiyorMu);
    }

    private static bool IsPointerOverUi()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    private static bool ReadMouse(out Vector2 position, out float wheel,
        out bool middlePressed, out bool middleHeld, out bool rightPressed, out bool rightHeld)
    {
        position = Vector2.zero;
        wheel = 0f;
        middlePressed = middleHeld = rightPressed = rightHeld = false;

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            position = mouse.position.ReadValue();
            wheel = mouse.scroll.ReadValue().y / 120f;
            middlePressed = mouse.middleButton.wasPressedThisFrame;
            middleHeld = mouse.middleButton.isPressed;
            rightPressed = mouse.rightButton.wasPressedThisFrame;
            rightHeld = mouse.rightButton.isPressed;
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        position = Input.mousePosition;
        wheel = Input.GetAxisRaw("Mouse ScrollWheel") * 10f;
        middlePressed = Input.GetMouseButtonDown(2);
        middleHeld = Input.GetMouseButton(2);
        rightPressed = Input.GetMouseButtonDown(1);
        rightHeld = Input.GetMouseButton(1);
        return true;
#else
        return false;
#endif
    }

    private static Vector2 ReadKeyboardPan()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                      (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            float y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                      (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y).normalized;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        float legacyX = (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1f : 0f) -
                        (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1f : 0f);
        float legacyY = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1f : 0f) -
                        (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1f : 0f);
        return new Vector2(legacyX, legacyY).normalized;
#else
        return Vector2.zero;
#endif
    }

    private static bool HomePressed()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.homeKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Home)) return true;
#endif
        return false;
    }

    private void NormalizeZoomSettings()
    {
        minOrthographicSize = Mathf.Max(1f, minOrthographicSize);
        maxOrthographicSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
        homeOrthographicSize = Mathf.Clamp(homeOrthographicSize, minOrthographicSize, maxOrthographicSize);
        boundsHalfExtents = new Vector2(Mathf.Abs(boundsHalfExtents.x), Mathf.Abs(boundsHalfExtents.y));
    }

    private void EnsureCameraReference()
    {
        if (cachedCamera == null) cachedCamera = GetComponent<Camera>();
    }
}
