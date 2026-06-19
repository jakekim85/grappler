using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerControler : MonoBehaviour
{
    [HideInInspector] public CharacterController characterController;
    public Transform cameraTransform;

    [Header("Movement Speeds")]
    public float defaultWalkSpeed = 6f;
    public float runSpeedMultiplier = 1.4f;
    public float crouchWalkSpeed = 2.5f;

    [Header("Inertia & Steering")]
    public float acceleration = 10f;
    public float deceleration = 8f;
    public float groundSteeringSpeed = 360f;
    [Range(0f, 1f)]
    public float airControlRate = 0.35f;

    [Header("Sliding Settings")]
    public float slideBoost = 12f;
    public float slideDeceleration = 2.5f;
    public float minSlideSpeed = 4f;
    [Range(0f, 1f)]
    public float slideSteeringRate = 0.15f;

    [Header("Crouch / Stand Speeds")]
    public float crouchSpeed = 12f;
    public float standUpSpeed = 10f;

    [Header("Character Height")]
    [HideInInspector] public float normalHeight;
    public float lowHeight = 0.8f;
    [HideInInspector] public float currentHeight;

    [Header("Jump Setting")]
    public float jumpHeight = 2.5f;

    [Header("Mouse Sensitivity")]
    public float mouseSensitivity = 2f;

    [HideInInspector] public bool isSwing = false;
    private bool isSliding = false;
    private bool isCrouching = false;

    private float xRotation = 0f;
    private float yRotation = 0f;
    private Vector3 currentHorizontalVelocity;
    private float verticalVelocity = 0f;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        characterController = GetComponent<CharacterController>();
        normalHeight = characterController.height;
        currentHeight = normalHeight;
    }

    void Update()
    {
        HandleMouseRotation();

        if (isSwing)
        {
            AdjustHeight(normalHeight, standUpSpeed);
            return;
        }

        HandleJump();

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = (transform.right * horizontal + transform.forward * vertical).normalized;

        float speedMagnitude = currentHorizontalVelocity.magnitude;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && inputDirection.magnitude > 0;

        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isCrouching) isCrouching = false;
            else if (isSliding) isSliding = false;
            else if (characterController.isGrounded)
            {
                if (isSprinting && speedMagnitude > defaultWalkSpeed * 0.6f)
                {
                    isSliding = true;
                    currentHorizontalVelocity = currentHorizontalVelocity.normalized * slideBoost;
                }
                else isCrouching = true;
            }
        }

        if (isSliding && (speedMagnitude < minSlideSpeed || !characterController.isGrounded))
        {
            isSliding = false;
        }

        bool isInLowState = isSliding || isCrouching;
        float targetHeight = isInLowState ? lowHeight : normalHeight;
        float currentHeightChangeSpeed = isInLowState ? crouchSpeed : standUpSpeed;
        AdjustHeight(targetHeight, currentHeightChangeSpeed);

        HandleSteeringInertia(inputDirection, speedMagnitude);
        HandleVelocityMovement(inputDirection, isSprinting);

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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

    public void CancelLowPostures()
    {
        isSliding = false;
        isCrouching = false;
    }

    public void MoveByGrappler(Vector3 swingVelocity)
    {
        characterController.Move(swingVelocity * Time.deltaTime);
    }

    void AdjustHeight(float targetH, float changeSpeed)
    {
        currentHeight = Mathf.MoveTowards(currentHeight, targetH, changeSpeed * Time.deltaTime);
        characterController.height = currentHeight;
        characterController.center = new Vector3(0, currentHeight / 2f, 0);
        cameraTransform.localPosition = new Vector3(0, currentHeight * 0.9f, 0);
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
                isSliding = false;
                isCrouching = false;
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
            else if (isSliding) activeSteeringSpeed *= slideSteeringRate;

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
            if (isSliding) targetMaxSpeed = 0f;
            else if (isCrouching) targetMaxSpeed = crouchWalkSpeed;
            else targetMaxSpeed = isSprinting ? defaultWalkSpeed * runSpeedMultiplier : defaultWalkSpeed;
        }

        Vector3 targetVelocity = inputDirection * targetMaxSpeed;

        // --- [핵심 수정 부분] ---
        // 지상 가속도와 감속도를 베이스로 가져옴
        float currentAccel = acceleration;
        float currentDecel = deceleration;

        // 공중에 있을 경우, 가속도와 감속도 모두에 airControlRate를 곱해버림!
        if (!characterController.isGrounded)
        {
            currentAccel *= airControlRate;
            currentDecel *= airControlRate;
        }
        else if (isSliding)
        {
            currentDecel = slideDeceleration;
        }

        // 입력이 있으면 가속도(currentAccel)를, 입력이 없거나 슬라이딩 중이면 감속도(currentDecel)를 적용
        float effectRate = (targetVelocity.magnitude > 0.01f && !isSliding) ? currentAccel : currentDecel;

        currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetVelocity, Time.deltaTime * effectRate);
        // ------------------------

        Vector3 finalMoveVelocity = currentHorizontalVelocity;
        finalMoveVelocity.y = verticalVelocity;
        characterController.Move(finalMoveVelocity * Time.deltaTime);
    }
}