using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Vector3 target = Vector3.zero;
    public float panSpeed = 0.1f;
    public float zoomSpeed = 20f;
    public float rotationSpeed = 50f;
    public float flySpeed = 0.5f;
    public bool flyMode = false;

    private Vector3 originalPosition;

    void Start()
    {
        originalPosition = target;
    }

    void Update()
    {
        // Check if we are in flyMode
        if (Input.GetKeyDown("/"))
        {
            flyMode = !flyMode;
        }

        if (flyMode)
        {
            FlyThrough();
        }
        else
        {
            RotateZoomPan();
        }

        if (Input.GetKey("escape"))
        {
            Application.Quit();
        }
    }

    void RotateZoomPan()
    {
        if (Input.GetMouseButtonDown(2) && Input.GetKey(KeyCode.LeftAlt))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                target = hit.transform.position;
            }
        }

        if (Input.GetMouseButton(2))
        {
            if (Input.GetKey(KeyCode.LeftShift))
            {
                // Pan
                transform.Translate(-Input.GetAxis("Mouse X") * panSpeed, -Input.GetAxis("Mouse Y") * panSpeed, 0);
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                // Zoom
                transform.Translate(0, 0, Input.GetAxis("Mouse Y") * zoomSpeed * Time.deltaTime);
            }
            else
            {
                // Rotate around object
                transform.RotateAround(target, Vector3.up, Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime);
                transform.RotateAround(target, transform.right, -Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime);
            }
        }

        // Zoom with scroll wheel
        float scrollData = Input.GetAxis("Mouse ScrollWheel");
        transform.Translate(0, 0, scrollData * zoomSpeed * Time.deltaTime);
    }

    void FlyThrough()
    {
        // Use the arrow keys or ZQSD keys to move
        float horizontal = Input.GetAxis("Horizontal") * flySpeed;
        float vertical = Input.GetAxis("Vertical") * flySpeed;

        // Move horizontally and vertically in local space
        transform.Translate(new Vector3(horizontal, 0, vertical), Space.Self);

        // Use A and E to move up and down
        if (Input.GetKey("a"))
        {
            // Ascend in global space
            transform.position += Vector3.up * flySpeed;
        }
        else if (Input.GetKey("e"))
        {
            // Descend in global space
            transform.position -= Vector3.up * flySpeed;
        }

        // Look around with /*Alt +*/ Right mouse button
        if (/*Input.GetKey(KeyCode.LeftAlt) &&*/ Input.GetMouseButton(1))
        {
            Vector3 rotation = new Vector3(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"), 0) * rotationSpeed * Time.deltaTime;
            rotation.z = 0; // Freeze rotation around the Z-axis
            transform.Rotate(rotation, Space.Self);

            // Force Z rotation to be zero
            Vector3 eulerAngles = transform.eulerAngles;
            eulerAngles.z = 0;
            transform.eulerAngles = eulerAngles;
        }

        // Look around with Alt + Right mouse button (a little bit unnatural)
        //if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftAlt))
        //{
        //    // Look around with Alt + Right mouse button
        //    // Horizontal mouse movement results in rotation around the camera's local Y-axis
        //    transform.Rotate(0, Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime, 0, Space.Self);

        //    // Vertical mouse movement results in rotation around the global X-axis
        //    transform.Rotate(-Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime, 0, 0, Space.World);

        //    // Force Z rotation to be zero
        //    Vector3 eulerAngles = transform.eulerAngles;
        //    eulerAngles.z = 0;
        //    transform.eulerAngles = eulerAngles;
        //}
    }
}
