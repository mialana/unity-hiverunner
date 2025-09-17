using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    private Rigidbody rb;
    public Camera antCamera;

    [Header("Configurations")]
    public float walkSpeed;
    public float runSpeed;
    public float jumpSpeed;

    [Header("Runtime")]
    Vector3 newVelocity;
    bool isGrounded = false;
    bool isJumping = false;

    public float minFov = 15f;
    public float maxFov = 90f;
    public float sensitivity = 20f;

    float pitch = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Locked;

        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
    }

    // Update is called once per frame
    void Update()
    {
        // Retains vertical velocity while discarding x and z components
        newVelocity = Vector3.up * rb.linearVelocity.y;
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        newVelocity.x = Input.GetAxis("Horizontal") * speed;
        newVelocity.z = Input.GetAxis("Vertical") * speed;

        if (isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
            {
                newVelocity.y = jumpSpeed;
                isJumping = true;
            }
        }

        rb.linearVelocity = transform.TransformDirection(newVelocity);
        transform.rotation = Quaternion.identity;
    }

    void LateUpdate()
    {
        // Mouse look
        float mouseY = Input.GetAxis("Mouse Y");
        pitch -= mouseY * 5f;
        pitch = Mathf.Clamp(pitch, 0f, 40f);

        // Orbit parameters
        float distance = 5f; // how far behind the ant
        float height = 1f; // vertical offset above the ant

        // Compute rotation from pitch and position the camera
        Quaternion rot = Quaternion.Euler(pitch, 0f, 0f);
        Vector3 orbitOffset = rot * new Vector3(0f, 0f, -distance) + new Vector3(0f, height, 0f);
        antCamera.transform.position = transform.position + orbitOffset;

        // Make camera look at the ant, factoring in its height
        antCamera.transform.LookAt(transform.position);

        // FOV handling
        float fov = antCamera.fieldOfView;
        fov += Input.GetAxis("Mouse ScrollWheel") * sensitivity;
        antCamera.fieldOfView = Mathf.Clamp(fov, minFov, maxFov);
    }

    //  A helper function
    //  Clamp the vertical head rotation (prevent bending backwards)
    public static float RestrictAngle(float angle, float angleMin, float angleMax)
    {
        if (angle > 180) // finalVal += noise;
            angle -= 360;
        else if (angle < -180)
            angle += 360;

        if (angle > angleMax)
            angle = angleMax;
        if (angle < angleMin)
            angle = angleMin;

        return angle;
    }

    void OnCollisionEnter(Collision col)
    {
        isGrounded = true;
        isJumping = false;
    }

    void OnCollisionStay(Collision col)
    {
        isGrounded = true;
        isJumping = false;
    }

    void OnCollisionExit(Collision col)
    {
        isGrounded = false;
    }
}
