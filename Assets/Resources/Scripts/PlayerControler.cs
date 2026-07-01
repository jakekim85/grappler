using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [HideInInspector] public CharacterController characterController;
    public Transform cameraTransform;

    [Header("Movement Speeds")]
    public float defaultWalkSpeed = 6f;
    public float runSpeedMultiplier = 1.4f;

    [Header("Inertia & Steering")]
    public float acceleration = 10f;
    public float deceleration = 8f;
    public float groundSteeringSpeed = 360f;
    [Range(0f, 1f)]
    public float airControlRate = 0.35f;

    [Header("Jump Setting")]
    public float jumpHeight = 2.5f;

    [Header("Mouse Sensitivity")]
    public float mouseSensitivity = 2f;

    [HideInInspector] public bool isSwing = false;
    [HideInInspector] public Vector3 latestWallNormal;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private Vector3 currentHorizontalVelocity;
    private float verticalVelocity = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        HandleMouseRotation();

        // 와이어 스윙 중에는 기본 이동 조작 막기
        if (isSwing) return;

        HandleJump();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = (transform.right * horizontal + transform.forward * vertical).normalized;

        float speedMagnitude = currentHorizontalVelocity.magnitude;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && inputDirection.magnitude > 0;

        HandleSteeringInertia(inputDirection, speedMagnitude);
        HandleVelocityMovement(inputDirection, isSprinting);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Application.Quit();
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (Mathf.Abs(hit.normal.y) < 0.5f)
        {
            latestWallNormal = hit.normal;
        }
    }

    public Vector3 GetCurrentVelocity()
    {
        return new Vector3(currentHorizontalVelocity.x, verticalVelocity, currentHorizontalVelocity.z);
    }

    public void SetVelocityFromGrapple(Vector3 newVelocity)
    {
        currentHorizontalVelocity = new Vector3(newVelocity.x, 0, newVelocity.z);
        verticalVelocity = newVelocity.y;
    }

    public CollisionFlags MoveByGrappler(Vector3 swingVelocity)
    {
        return characterController.Move(swingVelocity * Time.deltaTime);
    }

    void HandleMouseRotation()
    {
        float mouseX = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    void HandleJump()
    {
        if (characterController.isGrounded)
        {
            if (verticalVelocity < 0) verticalVelocity = -2f;
            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y);
            }
        }
        else
        {
            verticalVelocity += Physics.gravity.y * Time.deltaTime;
        }
    }

    void HandleSteeringInertia(Vector3 inputDirection, float speedMagnitude)
    {
        if (inputDirection.magnitude > 0.01f && speedMagnitude > 0.1f)
        {
            float activeSteeringSpeed = groundSteeringSpeed;
            if (!characterController.isGrounded) activeSteeringSpeed *= airControlRate;

            currentHorizontalVelocity = Vector3.RotateTowards(
                currentHorizontalVelocity.normalized, inputDirection,
                activeSteeringSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f
            );
            currentHorizontalVelocity *= speedMagnitude;
        }
    }

    void HandleVelocityMovement(Vector3 inputDirection, bool isSprinting)
    {
        float targetMaxSpeed = 0f;

        if (inputDirection.magnitude > 0)
        {
            targetMaxSpeed = isSprinting ? defaultWalkSpeed * runSpeedMultiplier : defaultWalkSpeed;
        }

        Vector3 targetVelocity = inputDirection * targetMaxSpeed;

        float currentAccel = acceleration;
        float currentDecel = deceleration;

        if (!characterController.isGrounded)
        {
            currentAccel *= airControlRate;
            currentDecel *= airControlRate;
        }

        float effectRate = targetVelocity.magnitude > 0.01f ? currentAccel : currentDecel;
        currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetVelocity, Time.deltaTime * effectRate);

        Vector3 finalMoveVelocity = currentHorizontalVelocity;
        finalMoveVelocity.y = verticalVelocity;

        CollisionFlags flags = characterController.Move(finalMoveVelocity * Time.deltaTime);

        if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0)
        {
            verticalVelocity = 0f;
        }

        if ((flags & CollisionFlags.Sides) != 0 && !characterController.isGrounded)
        {
            Vector3 projectedVel = Vector3.ProjectOnPlane(currentHorizontalVelocity, latestWallNormal);
            currentHorizontalVelocity = new Vector3(projectedVel.x, 0f, projectedVel.z);
        }
    }
}