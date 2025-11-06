using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private float dragSpeed = 0.02f;
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;

    private Camera cam;
    private Vector2 lastMousePosition;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }
    }

    void Update()
    {
        HandlePan();
        HandleZoom();
    }

    private void HandlePan()
    {

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = transform.forward;
        forward.y = 0;
        forward.Normalize();

        Vector3 right = transform.right;
        right.y = 0;
        right.Normalize();

        Vector3 movement = (right * horizontal + forward * vertical) * panSpeed * Time.deltaTime;
        transform.position += movement;

        if (Input.GetMouseButtonDown(2))
        {
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButton(2))
        {
            Vector2 currentMousePosition = Input.mousePosition;
            Vector2 delta = lastMousePosition - currentMousePosition;

            Vector3 dragMovement = (right * delta.x + forward * delta.y) * dragSpeed;
            transform.position += dragMovement;

            lastMousePosition = currentMousePosition;
        }
    }

    private void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            Vector3 position = transform.position;
            position.y -= scroll * zoomSpeed;
            position.y = Mathf.Clamp(position.y, minZoom, maxZoom);
            transform.position = position;
        }
    }
}
