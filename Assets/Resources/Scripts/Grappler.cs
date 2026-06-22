using UnityEngine;

public class Grappler : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public LineRenderer wireRenderer;

    [Header("Grappling Hook Settings")]
    public float grappleMaxDistance = 30f;
    public float wireShootSpeed = 60f;
    public float grapplePullSpeed = 20f;
    public float grappleCooldown = 2f;

    [Header("Momentum & Safety Settings")]
    public float fallDampenRate = 5f;
    public float extraTimeoutBuffer = 1.0f;

    private bool isWireFlying = false;
    private Vector3 grappleTargetPos;
    private Vector3 wireTipPos;
    private Vector3 inheritedVelocity;
    private Vector3 lastSwingVelocity;
    private float cooldownTimer = 0f;

    private float maxSwingTime;
    private float swingTimer = 0f;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        if (wireRenderer != null) wireRenderer.enabled = false;

        if (playerController == null) playerController = GetComponent<PlayerController>();
    }

    void Update()
    {
        if (cooldownTimer > 0 && !isWireFlying && !playerController.isSwing)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (!isWireFlying && !playerController.isSwing && cooldownTimer <= 0)
            {
                RaycastHit hit;
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, grappleMaxDistance))
                {
                    if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Ground"))
                    {
                        grappleTargetPos = hit.point;
                        isWireFlying = true;
                        wireTipPos = cam.transform.position;

                        float distance = Vector3.Distance(cam.transform.position, grappleTargetPos);

                        float wireFlyingTime = distance / wireShootSpeed;
                        float playerTravelTime = distance / grapplePullSpeed;

                        // --- [핵심 추가] 관성에 비례한 추가 유예 시간 계산 ---
                        // 플레이어가 현재 가진 속도가 빠를수록, 방향 전환에 시간이 더 필요하므로 제한 시간을 늘려줍니다.
                        float currentSpeed = playerController.GetCurrentVelocity().magnitude;
                        float momentumBonusTime = currentSpeed * 0.15f; // 속도 10당 1.5초의 추가 유예 시간 부여
                                                                        // -----------------------------------------------------

                        maxSwingTime = wireFlyingTime + playerTravelTime + extraTimeoutBuffer + momentumBonusTime;
                        swingTimer = 0f;
                    }
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isWireFlying || playerController.isSwing)
            {
                StopGrapple();
            }
        }

        if (isWireFlying || playerController.isSwing)
        {
            swingTimer += Time.deltaTime;
            if (swingTimer >= maxSwingTime)
            {
                StopGrapple();
                return;
            }
        }

        if (isWireFlying)
        {
            wireTipPos = Vector3.MoveTowards(wireTipPos, grappleTargetPos, wireShootSpeed * Time.deltaTime);

            if (Vector3.Distance(wireTipPos, grappleTargetPos) < 0.1f)
            {
                isWireFlying = false;
                playerController.isSwing = true;

                playerController.CancelLowPostures();
                inheritedVelocity = playerController.GetCurrentVelocity();
            }
        }

        if (playerController.isSwing)
        {
            if (inheritedVelocity.y < 0)
            {
                inheritedVelocity.y = Mathf.Lerp(inheritedVelocity.y, 0f, fallDampenRate * Time.deltaTime);
            }

            Vector3 playerCenter = transform.position + Vector3.up * (playerController.currentHeight / 2f);
            Vector3 pullDir = (grappleTargetPos - playerCenter).normalized;

            Vector3 swingVelocity = (pullDir * grapplePullSpeed) + inheritedVelocity;

            CollisionFlags flags = playerController.MoveByGrappler(swingVelocity);

            if ((flags & CollisionFlags.Sides) != 0)
            {
                swingVelocity = Vector3.ProjectOnPlane(swingVelocity, playerController.latestWallNormal);
                inheritedVelocity = Vector3.ProjectOnPlane(inheritedVelocity, playerController.latestWallNormal);
            }

            lastSwingVelocity = swingVelocity;

            if ((flags & CollisionFlags.Above) != 0)
            {
                if (lastSwingVelocity.y > 0)
                {
                    lastSwingVelocity.y = 0f;
                }
                StopGrapple();
                return;
            }

            if (Vector3.Distance(playerCenter, grappleTargetPos) < 1.5f)
            {
                StopGrapple();
            }
        }

        DrawWire();
    }

    void StopGrapple()
    {
        if (playerController.isSwing)
        {
            playerController.SetVelocityFromGrapple(lastSwingVelocity);
        }

        isWireFlying = false;
        playerController.isSwing = false;
        cooldownTimer = grappleCooldown;
    }

    void DrawWire()
    {
        if (wireRenderer == null) return;

        if (isWireFlying || playerController.isSwing)
        {
            wireRenderer.enabled = true;
            wireRenderer.SetPosition(0, cam.transform.position + (transform.right * 0.3f) - (transform.up * 0.2f));
            wireRenderer.SetPosition(1, isWireFlying ? wireTipPos : grappleTargetPos);
        }
        else
        {
            wireRenderer.enabled = false;
        }
    }
}