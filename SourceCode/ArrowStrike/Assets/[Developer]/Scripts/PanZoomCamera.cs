using UnityEngine;

public class PanAndZoom : MonoBehaviour
{
    public Camera mainCamera; // Reference to the main camera
    public Transform player; // Reference to the player's transform
    public Vector3 offset = new Vector3(0, 10, -10); // Offset for the camera position relative to the player
    public float followSpeed = 5f; // Speed at which the camera follows the player
    public float rotationSpeed = 5f; // Speed at which the camera rotates to match the player's rotation
    public static float panSpeed = 0.5f; // Speed of panning
    public static float zoomSpeed = 5f; // Speed of zooming
    public float minZoom = 5f; // Minimum orthographic size
    public float maxZoom = 20f; // Maximum orthographic size

    public float minX = -50f; // Minimum X bound
    public float maxX = 50f;  // Maximum X bound
    public float minZ = -50f; // Minimum Z bound
    public float maxZ = 50f;  // Maximum Z bound

    private Vector3 lastPanPosition; // Last position of the pointer (mouse or touch)
    private int panFingerId; // Finger ID used for panning
    private bool isPanning; // Is panning active
    private bool isPanningOrZooming; // Indicates if the camera is being manipulated

    void Start()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }
    }

    void Update()
    {
        HandleZoom();

        HandlePan();

        // Follow player when not panning or zooming
        if (!isPanningOrZooming && player != null)
        {
            //FollowPlayer();
        }
    }

    void HandlePan()
    {
        // For touch input
        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                isPanning = true;
                isPanningOrZooming = true;
                panFingerId = touch.fingerId;
                lastPanPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved && isPanning && touch.fingerId == panFingerId)
            {
                Vector3 delta = (Vector3)touch.position - lastPanPosition;
                PanCamera(delta);
                lastPanPosition = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended && touch.fingerId == panFingerId)
            {
                isPanning = false;
                isPanningOrZooming = false; // Reset the flag
            }
        }

        // For mouse input
        if (Input.GetMouseButtonDown(1))
        {
            isPanning = true;
            isPanningOrZooming = true;
            lastPanPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButton(1) && isPanning)
        {
            Vector3 delta = Input.mousePosition - lastPanPosition;
            PanCamera(delta);
            lastPanPosition = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(1))
        {
            isPanning = false;
            isPanningOrZooming = false; // Reset the flag
        }
    }

    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            isPanningOrZooming = true; // Set the flag when zooming
            ZoomCamera(scroll * zoomSpeed);
        }

        if (Input.touchCount == 2)
        {
            isPanningOrZooming = true; // Set the flag for pinch zoom
            Touch touch1 = Input.GetTouch(0);
            Touch touch2 = Input.GetTouch(1);

            Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;
            Vector2 touch2PrevPos = touch2.position - touch2.deltaPosition;

            float prevTouchDelta = (touch1PrevPos - touch2PrevPos).magnitude;
            float currentTouchDelta = (touch1.position - touch2.position).magnitude;

            float delta = currentTouchDelta - prevTouchDelta;

            ZoomCamera(delta * zoomSpeed * 0.01f); // Adjust the zoom factor for touch
        }
    }

    void PanCamera(Vector3 delta)
    {
        Vector3 cameraRight = mainCamera.transform.right;
        Vector3 cameraForward = Vector3.Cross(cameraRight, Vector3.up);

        Vector3 move = (-delta.x * cameraRight + -delta.y * cameraForward) * panSpeed * Time.deltaTime;
        mainCamera.transform.position += move;

        Vector3 clampedPosition = mainCamera.transform.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, minX, maxX);
        clampedPosition.z = Mathf.Clamp(clampedPosition.z, minZ, maxZ);
        mainCamera.transform.position = clampedPosition;
    }

    void ZoomCamera(float zoomFactor)
    {
        if (mainCamera.orthographic)
        {
            mainCamera.orthographicSize = Mathf.Clamp(mainCamera.orthographicSize - zoomFactor, minZoom, maxZoom);
        }
        else
        {
            mainCamera.fieldOfView = Mathf.Clamp(mainCamera.fieldOfView - zoomFactor, minZoom, maxZoom);
        }
    }

    void FollowPlayer()
    {
        // Smoothly adjust position to follow the player
        Vector3 targetPosition = player.position + offset;
        mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, new Vector3(targetPosition.x,10,targetPosition.z), followSpeed * Time.deltaTime);


        // Smoothly adjust rotation to look at the player
        //Quaternion targetRotation = Quaternion.LookRotation(player.position - mainCamera.transform.position, Vector3.up);
        //mainCamera.transform.rotation = Quaternion.Slerp(mainCamera.transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
