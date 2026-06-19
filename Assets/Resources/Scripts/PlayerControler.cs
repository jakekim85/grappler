using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class HyperFPSController : MonoBehaviour
{
    public Transform cameraTransform;
    private CharacterController characterController;

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

    [Header("Sliding Settings (슬라이딩)")]
    public float slideBoost = 12f;
    public float slideDeceleration = 2.5f;
    public float minSlideSpeed = 4f; // 이 속도 이하로 떨어지면 자동으로 일어남
    [Range(0f, 1f)]
    public float slideSteeringRate = 0.15f;

    [Header("Crouch / Stand Speeds (앉기 및 일어서기 속도)")]
    public float crouchSpeed = 12f;
    public float standUpSpeed = 10f;

    [Header("Character Height")]
    private float normalHeight;
    public float lowHeight = 0.8f;
    private float currentHeight;

    [Header("Jump Setting")]
    public float jumpHeight = 2.5f;

    [Header("Mouse Sensitivity")]
    public float mouseSensitivity = 2f;

    private float xRotation = 0f;
    private float yRotation = 0f;

    private Vector3 currentHorizontalVelocity;
    private float verticalVelocity = 0f;

    // --- [핵심] 상태 변수 ---
    private bool isSliding = false;   // 슬라이딩 중인가?
    private bool isCrouching = false; // 그냥 앉아있는 중인가?

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
        HandleJump();

        // 1. 기본 입력 및 속도 체크
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = (transform.right * horizontal + transform.forward * vertical).normalized;

        float speedMagnitude = currentHorizontalVelocity.magnitude;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && inputDirection.magnitude > 0;

        // ---------------------------------------------------------
        // 2. [핵심] C키 입력 로직 (토글 및 조건 분기)
        // ---------------------------------------------------------
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (isCrouching)
            {
                // 이미 그냥 앉아있는 상태였다면 -> 일어서기
                isCrouching = false;
            }
            else if (isSliding)
            {
                // 슬라이딩 중에 C를 한 번 더 누르면 -> 슬라이딩 취소하고 일어서기
                isSliding = false;
            }
            else // 완전히 서 있는 상태일 때
            {
                if (characterController.isGrounded)
                {
                    // [조건 1] 충분히 달리는 속도일 때 C를 누르면 -> 슬라이딩!
                    if (isSprinting && speedMagnitude > defaultWalkSpeed * 0.6f)
                    {
                        isSliding = true;
                        currentHorizontalVelocity = currentHorizontalVelocity.normalized * slideBoost;
                    }
                    // [조건 2] 속도가 느리거나 멈춰있을 때 C를 누르면 -> 일반 앉기!
                    else
                    {
                        isCrouching = true;
                    }
                }
            }
        }

        // ---------------------------------------------------------
        // 3. 슬라이딩 자동 해제 체크
        // ---------------------------------------------------------
        if (isSliding)
        {
            // 속도가 기준치(minSlideSpeed)보다 떨어지거나 공중에 뜨면 슬라이딩 해제 (자동으로 일어남)
            if (speedMagnitude < minSlideSpeed || !characterController.isGrounded)
            {
                isSliding = false;
                //isCrouching은 여전히 false이므로 캐릭터는 서 있는 상태(normalHeight)로 돌아갑니다.
            }
        }

        // ---------------------------------------------------------
        // 4. 실시간 콜라이더 높이 및 카메라 Lerp 연산
        // ---------------------------------------------------------
        bool isInLowState = isSliding || isCrouching;
        float targetHeight = isInLowState ? lowHeight : normalHeight;
        float currentHeightChangeSpeed = isInLowState ? crouchSpeed : standUpSpeed;

        currentHeight = Mathf.MoveTowards(currentHeight, targetHeight, currentHeightChangeSpeed * Time.deltaTime);

        characterController.height = currentHeight;
        characterController.center = new Vector3(0, currentHeight / 2f, 0);
        cameraTransform.localPosition = new Vector3(0, currentHeight * 0.9f, 0);

        // 5. 이동 속도 제어 및 관성 (Steering)
        HandleSteeringInertia(inputDirection, speedMagnitude);
        HandleVelocityMovement(inputDirection, isSprinting);

        // 커서 해제
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // --- 이하 로직 헬퍼 함수들 ---

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
                isCrouching = false; // 점프하면 모든 앉기/슬라이딩 상태 해제
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
            else if (isCrouching) targetMaxSpeed = crouchWalkSpeed; // 앉은 상태의 이동속도 적용
            else targetMaxSpeed = isSprinting ? defaultWalkSpeed * runSpeedMultiplier : defaultWalkSpeed;
        }

        Vector3 targetVelocity = inputDirection * targetMaxSpeed;

        float currentDecel = deceleration;
        if (!characterController.isGrounded) currentDecel *= airControlRate;
        else if (isSliding) currentDecel = slideDeceleration;

        float effectRate = (targetVelocity.magnitude > 0.01f && !isSliding) ? acceleration : currentDecel;
        currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, targetVelocity, Time.deltaTime * effectRate);

        Vector3 finalMoveVelocity = currentHorizontalVelocity;
        finalMoveVelocity.y = verticalVelocity;
        characterController.Move(finalMoveVelocity * Time.deltaTime);
    }
}