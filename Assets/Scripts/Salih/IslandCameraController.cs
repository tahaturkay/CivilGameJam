using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Orthographic navigation for the island scene. The island rectangle is measured on world X/Z
/// at boundsCenter.y, so navigation also works on the island's raised ground.
/// </summary>
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(-100)]
public class IslandCameraController : MonoBehaviour
{
    [Header("Island bounds (world X/Z)")]
    public Vector3 boundsCenter = Vector3.zero;
    public Vector2 boundsHalfExtents = new Vector2(90f, 72.5f);

    [Header("Orthographic zoom")]
    [Min(1f)] public float minOrthographicSize = 18f;
    [Min(1f)] public float maxOrthographicSize = 140f;
    [Min(1f)] public float homeOrthographicSize = 100f;
    [Min(1f)] public float startOrthographicSize = 58f;
    [Min(0.01f)] public float zoomSensitivity = 0.2f;

    [Header("Keyboard pan")]
    public bool keyboardPanEnabled = true;
    [Min(1f)] public float keyboardPanSpeed = 45f;
    [Min(0.01f)] public float keyboardPanSmoothTime = 0.09f;
    [Min(1f)] public float keyboardZoomSpeed = 36f;

    [Header("Input smoothing")]
    [Min(0.01f)] public float zoomSmoothTime = 0.12f;
    [Min(1f)] public float maxPointerDeltaPerFrame = 140f;
    [Min(1f)] public float trackpadPanPixelsPerUnit = 36f;

    [Header("Scene references")]
    public DisasterController disasterController;

    private Camera cachedCamera;
    private bool dragging;
    private Vector2 lastPointerPosition;
    private Vector3 keyboardPanVelocity;
    private Vector3 keyboardPanDampingVelocity;
    private float targetOrthographicSize;
    private float zoomVelocity;
    private bool zoomAnchorActive;
    private Vector3 zoomAnchorWorld;
    private Vector2 zoomAnchorScreen;

    /// <summary>True in frames where a camera pan gesture owns the pointer, preventing accidental building clicks.</summary>
    static bool gestureOwnsPointer;
    public static bool SuppressWorldClick { get=>gestureOwnsPointer||SpaceHeld(); private set=>gestureOwnsPointer=value; }

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
        targetOrthographicSize = cachedCamera != null ? cachedCamera.orthographicSize : startOrthographicSize;
    }

    private void OnDisable()
    {
        dragging = false;
        keyboardPanVelocity = Vector3.zero;
        keyboardPanDampingVelocity = Vector3.zero;
        zoomVelocity = 0f;
        zoomAnchorActive = false;
        SuppressWorldClick = false;
    }

    private void Start(){FrameCity();}

    /// <summary>Readable city view at startup; Home still fits the complete island.</summary>
    public void FrameCity()
    {
        EnsureCameraReference();if(cachedCamera==null)return;
        cachedCamera.orthographicSize=Mathf.Clamp(startOrthographicSize,minOrthographicSize,maxOrthographicSize);
        keyboardPanVelocity = Vector3.zero;
        keyboardPanDampingVelocity = Vector3.zero;
        ResetZoomTarget();
        SetCameraCenter(boundsCenter+new Vector3(0,0,-7));
    }

    private void Update()
    {
        SuppressWorldClick = false;
        if (cachedCamera == null || !cachedCamera.orthographic) return;

        if (ControlsBlocked())
        {
            ResetNavigationMotion();
            return;
        }

        bool textInputFocused = IsTextInputFocused();
        if (!textInputFocused && HomePressed())
        {
            FrameHome();
            ResetNavigationMotion();
            return;
        }

        Vector2 pointerPosition, scrollDelta;
        bool middlePressed, middleHeld, rightPressed, rightHeld, leftPressed, leftHeld;
        bool hasMouse = ReadMouse(out pointerPosition, out scrollDelta,
            out middlePressed, out middleHeld, out rightPressed, out rightHeld, out leftPressed, out leftHeld);
        bool pointerOverUi = IsPointerOverUi();
        bool shiftHeld = ShiftHeld();
        bool spaceHeld = SpaceHeld();

        if (hasMouse)
        {
            bool panHeld = middleHeld || rightHeld || (spaceHeld && leftHeld);
            bool panStarted = middlePressed || rightPressed || (spaceHeld && leftPressed);
            if (panStarted && !pointerOverUi)
            {
                dragging = true;
                lastPointerPosition = pointerPosition;
            }

            if (dragging && panHeld)
            {
                SuppressWorldClick = true;
                if (!pointerOverUi) ApplyPointerPan(lastPointerPosition, pointerPosition);
                lastPointerPosition = pointerPosition;
            }
            else
            {
                dragging = false;
                lastPointerPosition = pointerPosition;
            }

            if (!pointerOverUi && scrollDelta.sqrMagnitude > 0.00000001f)
                ApplyScrollDelta(scrollDelta, pointerPosition, shiftHeld);
        }
        else dragging = false;

        Vector2 keyboardDirection = keyboardPanEnabled && !textInputFocused ? ReadKeyboardPan() : Vector2.zero;
        ApplyKeyboardPan(keyboardDirection, Time.unscaledDeltaTime);
        float keyboardZoom = textInputFocused ? 0f : ReadKeyboardZoom();
        if (Mathf.Abs(keyboardZoom) > 0.001f)
            targetOrthographicSize = Mathf.Clamp(targetOrthographicSize - keyboardZoom * keyboardZoomSpeed * Time.unscaledDeltaTime,
                minOrthographicSize, maxOrthographicSize);
        UpdateSmoothZoom(Time.unscaledDeltaTime);
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
        keyboardPanVelocity = Vector3.zero;
        keyboardPanDampingVelocity = Vector3.zero;
        ResetZoomTarget();
        SetCameraCenter(boundsCenter);
    }

    /// <summary>Applies normalized WASD/arrow input. Exposed so navigation can be verified without device input.</summary>
    public void ApplyKeyboardPan(Vector2 direction, float deltaTime)
    {
        EnsureCameraReference();
        if (cachedCamera == null || deltaTime <= 0f) return;
        if (direction.sqrMagnitude > 1f) direction.Normalize();
        if(direction.sqrMagnitude>.0001f)zoomAnchorActive=false;

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        if (right.sqrMagnitude < 0.001f) right = Vector3.right;
        Vector3 desiredVelocity = (right * direction.x + forward * direction.y) * keyboardPanSpeed;
        if (desiredVelocity.sqrMagnitude > keyboardPanSpeed * keyboardPanSpeed)
            desiredVelocity = desiredVelocity.normalized * keyboardPanSpeed;
        keyboardPanVelocity = Vector3.SmoothDamp(keyboardPanVelocity, desiredVelocity,
            ref keyboardPanDampingVelocity, Mathf.Max(0.01f, keyboardPanSmoothTime), float.PositiveInfinity, deltaTime);
        MoveCamera(keyboardPanVelocity * deltaTime);
        ClampCameraCenterToBounds();
    }

    /// <summary>Applies a bounded screen-space drag. The island point under the pointer follows the drag.</summary>
    public void ApplyPointerPan(Vector2 pointerFrom, Vector2 pointerTo)
    {
        EnsureCameraReference();
        if (cachedCamera == null) return;
        SuppressWorldClick = true;
        zoomAnchorActive=false;
        keyboardPanVelocity=keyboardPanDampingVelocity=Vector3.zero;
        Vector2 delta = pointerTo - pointerFrom;
        float maximum = Mathf.Max(1f, maxPointerDeltaPerFrame);
        if (delta.sqrMagnitude > maximum * maximum) delta = delta.normalized * maximum;
        Vector3 previousGroundPoint, currentGroundPoint;
        if (TryGetGroundPoint(pointerFrom, out previousGroundPoint) &&
            TryGetGroundPoint(pointerFrom + delta, out currentGroundPoint))
        {
            MoveCamera(previousGroundPoint - currentGroundPoint);
            ClampCameraCenterToBounds();
        }
    }

    /// <summary>
    /// Consumes normalized scroll input: vertical scroll zooms toward the cursor; Shift+scroll pans both axes.
    /// Horizontal trackpad scroll pans horizontally. Exposed for deterministic navigation tests.
    /// </summary>
    public void ApplyScrollDelta(Vector2 scrollDelta, Vector2 screenPosition, bool shiftPan)
    {
        EnsureCameraReference();
        if (cachedCamera == null) return;
        scrollDelta.x = Mathf.Clamp(scrollDelta.x, -3f, 3f);
        scrollDelta.y = Mathf.Clamp(scrollDelta.y, -3f, 3f);
        if (shiftPan)
        {
            Vector2 pixels = scrollDelta * Mathf.Max(1f, trackpadPanPixelsPerUnit);
            float maximum = Mathf.Max(1f, maxPointerDeltaPerFrame);
            if (pixels.sqrMagnitude > maximum * maximum) pixels = pixels.normalized * maximum;
            ApplyPointerPan(screenPosition, screenPosition + pixels);
            return;
        }

        // Pick the dominant axis for ordinary two-finger scrolls. Small horizontal noise
        // from a vertical gesture should not move the map while the user is zooming.
        if (Mathf.Abs(scrollDelta.x) > Mathf.Abs(scrollDelta.y))
        {
            if (Mathf.Abs(scrollDelta.x) < 0.0001f) return;
            float pixels = Mathf.Clamp(scrollDelta.x * trackpadPanPixelsPerUnit,
                -maxPointerDeltaPerFrame, maxPointerDeltaPerFrame);
            ApplyPointerPan(screenPosition, screenPosition + new Vector2(pixels, 0f));
            return;
        }

        if (Mathf.Abs(scrollDelta.y) < 0.0001f) return;
        Vector3 anchorBefore;
        if (!TryGetGroundPoint(screenPosition, out anchorBefore)) return;
        zoomAnchorWorld = anchorBefore;
        zoomAnchorScreen = screenPosition;
        targetOrthographicSize = Mathf.Clamp(targetOrthographicSize * Mathf.Exp(-scrollDelta.y * zoomSensitivity),
            minOrthographicSize, maxOrthographicSize);
        zoomAnchorActive = Mathf.Abs(targetOrthographicSize - cachedCamera.orthographicSize) > 0.0001f;
        if (!zoomAnchorActive) zoomAnchorWorld = Vector3.zero;
    }

    private void UpdateSmoothZoom(float deltaTime)
    {
        if (cachedCamera == null || deltaTime <= 0f) return;
        float current = Mathf.SmoothDamp(cachedCamera.orthographicSize, targetOrthographicSize,
            ref zoomVelocity, Mathf.Max(0.01f, zoomSmoothTime), float.PositiveInfinity, deltaTime);
        cachedCamera.orthographicSize = Mathf.Clamp(current, minOrthographicSize, maxOrthographicSize);

        Vector3 anchorAfter;
        if (zoomAnchorActive && TryGetGroundPoint(zoomAnchorScreen, out anchorAfter))
        {
            MoveCamera(zoomAnchorWorld - anchorAfter);
        }

        ClampCameraCenterToBounds();
        if (Mathf.Abs(cachedCamera.orthographicSize - targetOrthographicSize) < 0.001f)
        {
            cachedCamera.orthographicSize = targetOrthographicSize;
            zoomVelocity = 0f;
            zoomAnchorActive = false;
        }
    }

    private void ResetZoomTarget()
    {
        if (cachedCamera == null) return;
        targetOrthographicSize = cachedCamera.orthographicSize;
        zoomVelocity = 0f;
        zoomAnchorActive = false;
    }

    private void ResetNavigationMotion()
    {
        dragging = false;
        keyboardPanVelocity = Vector3.zero;
        keyboardPanDampingVelocity = Vector3.zero;
        zoomVelocity = 0f;
        targetOrthographicSize = cachedCamera != null ? cachedCamera.orthographicSize : targetOrthographicSize;
        zoomAnchorActive = false;
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

    private static bool ReadMouse(out Vector2 position, out Vector2 scrollDelta,
        out bool middlePressed, out bool middleHeld, out bool rightPressed, out bool rightHeld,
        out bool leftPressed, out bool leftHeld)
    {
        position = Vector2.zero;
        scrollDelta = Vector2.zero;
        middlePressed = middleHeld = rightPressed = rightHeld = leftPressed = leftHeld = false;

#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            position = mouse.position.ReadValue();
            scrollDelta = mouse.scroll.ReadValue();
            // The Input System normalizes scroll to [-1, 1] by default. Preserve support
            // for projects opting into Windows' native 120-units-per-notch range.
            if (InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange &&
                (Application.platform == RuntimePlatform.WindowsEditor || Application.platform == RuntimePlatform.WindowsPlayer))
                scrollDelta /= 120f;
            middlePressed = mouse.middleButton.wasPressedThisFrame;
            middleHeld = mouse.middleButton.isPressed;
            rightPressed = mouse.rightButton.wasPressedThisFrame;
            rightHeld = mouse.rightButton.isPressed;
            leftPressed = mouse.leftButton.wasPressedThisFrame;
            leftHeld = mouse.leftButton.isPressed;
            return true;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        position = Input.mousePosition;
        scrollDelta = new Vector2(0f, Input.GetAxisRaw("Mouse ScrollWheel") * 10f);
        middlePressed = Input.GetMouseButtonDown(2);
        middleHeld = Input.GetMouseButton(2);
        rightPressed = Input.GetMouseButtonDown(1);
        rightHeld = Input.GetMouseButton(1);
        leftPressed = Input.GetMouseButtonDown(0);
        leftHeld = Input.GetMouseButton(0);
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

    private static float ReadKeyboardZoom()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
            return (keyboard.eKey.isPressed ? 1f : 0f) - (keyboard.qKey.isPressed ? 1f : 0f);
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return (Input.GetKey(KeyCode.E) ? 1f : 0f) - (Input.GetKey(KeyCode.Q) ? 1f : 0f);
#else
        return 0f;
#endif
    }

    private static bool ShiftHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
            return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#else
        return false;
#endif
    }

    private static bool SpaceHeld()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.isPressed) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKey(KeyCode.Space);
#else
        return false;
#endif
    }

    private static bool IsTextInputFocused()
    {
        if (EventSystem.current == null || EventSystem.current.currentSelectedGameObject == null) return false;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        return selected.GetComponent<InputField>() != null || selected.GetComponent<TMP_InputField>() != null ||
               selected.GetComponentInParent<InputField>() != null || selected.GetComponentInParent<TMP_InputField>() != null;
    }

    private void NormalizeZoomSettings()
    {
        minOrthographicSize = Mathf.Max(1f, minOrthographicSize);
        maxOrthographicSize = Mathf.Max(minOrthographicSize, maxOrthographicSize);
        homeOrthographicSize = Mathf.Clamp(homeOrthographicSize, minOrthographicSize, maxOrthographicSize);
        boundsHalfExtents = new Vector2(Mathf.Abs(boundsHalfExtents.x), Mathf.Abs(boundsHalfExtents.y));
        maxPointerDeltaPerFrame = Mathf.Max(1f, maxPointerDeltaPerFrame);
        trackpadPanPixelsPerUnit = Mathf.Max(1f, trackpadPanPixelsPerUnit);
        zoomSmoothTime = Mathf.Max(0.01f, zoomSmoothTime);
        keyboardPanSmoothTime = Mathf.Max(0.01f, keyboardPanSmoothTime);
        keyboardZoomSpeed = Mathf.Max(1f, keyboardZoomSpeed);
    }

    private void EnsureCameraReference()
    {
        if (cachedCamera == null) cachedCamera = GetComponent<Camera>();
    }
}
