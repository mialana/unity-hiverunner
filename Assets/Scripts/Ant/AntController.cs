using NUnit.Framework;
using UnityEngine;

public class AntController : MonoBehaviour
{
    [Header("References")]
    private Rigidbody rb;
    public Camera antCamera;

    public HiveGenerator hiveGenerator;

    [Header("Configurations")]
    public float walkSpeed = 5f;
    public float runSpeed = 7f;
    public float jumpSpeed = 10f;

    [Header("Runtime")]
    Vector3 newVelocity;
    bool isGrounded = false;
    bool isJumping = false;

    public float minFov = 15f;
    public float maxFov = 180f;
    public float sensitivity = 20f;

    float pitch = 0f;

    // Orbit parameters
    float cameraDistance = 15f; // how far behind the ant
    float cameraHeight = 0f; // vertical offset above the ant

    GameObject mesh;
    GameObject controller;

    Vector3 rotation;

    private AudioSource audioSource;
    public AudioClip jumpClip;
    public AudioClip hitClip;

    void Awake()
    {
        mesh = transform.Find("ctrl_global").gameObject;
        controller = transform.Find("cuerpo_LP").gameObject;

        rotation = new Vector3(0f, 90f, 0f);

        audioSource = GetComponent<AudioSource>();

        if (hiveGenerator == null)
        {
            hiveGenerator = GameObject.Find("HiveGenerator").GetComponent<HiveGenerator>();
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.Locked;

        rb = GetComponent<Rigidbody>();
        // transform.eulerAngles = new(0, 90f, 0);
        rb.freezeRotation = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isGrounded)
        {
            if (Input.GetKeyDown(KeyCode.Space) && !isJumping)
            {
                newVelocity.y = jumpSpeed;
                isJumping = true;
                audioSource.PlayOneShot(jumpClip, 2f);

                rb.AddForce(newVelocity, ForceMode.Impulse);
            }
        }
    }

    void FixedUpdate()
    {
        // Retains vertical velocity while discarding x and z components
        newVelocity = Vector3.up * rb.linearVelocity.y;
        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        newVelocity.x = Input.GetAxis("Horizontal") * speed;
        newVelocity.z = Input.GetAxis("Vertical") * speed;

        if (newVelocity.x < 0)
        {
            rotation.y = -90;
        }
        else
        {
            rotation.y = 90;
        }

        mesh.transform.localEulerAngles = rotation;
        controller.transform.localEulerAngles = rotation;

        if (!hiveGenerator.bounds.Contains(transform.position))
        {
            newVelocity.z = 5f;
        }

        rb.linearVelocity = transform.TransformDirection(newVelocity);
    }

    void LateUpdate()
    {
        // Mouse look
        float mouseY = Input.GetAxis("Mouse Y");
        pitch -= mouseY * 5f;
        pitch = Mathf.Clamp(pitch, 0f, 40f);

        // Compute rotation from pitch and position the camera
        Quaternion rot = Quaternion.Euler(pitch, 0f, 0f);
        Vector3 orbitOffset =
            rot * new Vector3(0f, 0f, -cameraDistance) + new Vector3(0f, cameraHeight, 0f);
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
        if (col.collider is MeshCollider)
        {
            isGrounded = true;
            isJumping = false;
        }
    }

    void OnCollisionExit(Collision col)
    {
        isGrounded = false;
    }
}
