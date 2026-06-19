using UnityEngine;

public class Grappler : MonoBehaviour
{
    [Header("References")]
    public PlayerControler fpsController; // 분리된 기본 이동 스크립트를 연결할 곳
    public LineRenderer wireRenderer;

    [Header("Grappling Hook Settings")]
    public float grappleMaxDistance = 30f;
    public float wireShootSpeed = 60f;
    public float grapplePullSpeed = 20f;
    public float grappleCooldown = 2f;

    private bool isWireFlying = false;
    private Vector3 grappleTargetPos;
    private Vector3 wireTipPos;
    private Vector3 inheritedVelocity;
    private Vector3 lastSwingVelocity;
    private float cooldownTimer = 0f;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
        if (wireRenderer != null) wireRenderer.enabled = false;

        // 깜빡하고 스크립트 연결 안 했을 경우를 대비한 자동 연결
        if (fpsController == null) fpsController = GetComponent<PlayerControler>();
    }

    void Update()
    {
        // 1. 쿨타임 계산 (완전히 끊어진 상태에서만 감소)
        if (cooldownTimer > 0 && !isWireFlying && !fpsController.isSwing)
        {
            cooldownTimer -= Time.deltaTime;
        }

        // 2. Q 키 입력: 발사 및 해제
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // 발사 가능 조건
            if (!isWireFlying && !fpsController.isSwing && cooldownTimer <= 0)
            {
                RaycastHit hit;
                if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, grappleMaxDistance))
                {
                    if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Ground"))
                    {
                        grappleTargetPos = hit.point;
                        isWireFlying = true;
                        wireTipPos = cam.transform.position;
                    }
                }
            }
            // 이미 날아가고 있거나 스윙 중이면 해제
            else if (isWireFlying || fpsController.isSwing)
            {
                StopGrapple();
            }
        }

        // 3. 와이어 뻗어나가는 로직
        if (isWireFlying)
        {
            wireTipPos = Vector3.MoveTowards(wireTipPos, grappleTargetPos, wireShootSpeed * Time.deltaTime);

            // 와이어가 벽에 딱 닿았을 때!
            if (Vector3.Distance(wireTipPos, grappleTargetPos) < 0.1f)
            {
                isWireFlying = false;
                fpsController.isSwing = true; // 이동 스크립트 기본 제어권 뺏기

                fpsController.CancelLowPostures(); // 앉기, 슬라이딩 강제 취소
                inheritedVelocity = fpsController.GetCurrentVelocity(); // 달리던 속도 저장
            }
        }

        // 4. 플레이어가 공중으로 끌려가는 스윙 로직
        if (fpsController.isSwing)
        {
            // 배꼽 위치 계산
            Vector3 playerCenter = transform.position + Vector3.up * (fpsController.currentHeight / 2f);
            Vector3 pullDir = (grappleTargetPos - playerCenter).normalized;

            // 잡아당기는 힘 + 원래 가지고 있던 관성
            Vector3 swingVelocity = (pullDir * grapplePullSpeed) + inheritedVelocity;

            // 캐릭터 컨트롤러 강제 이동 (조향 및 에어스트레이프는 잠김)
            fpsController.MoveByGrappler(swingVelocity);

            lastSwingVelocity = swingVelocity;

            // 목표물에 거의 다 도달하면 자동 해제
            if (Vector3.Distance(playerCenter, grappleTargetPos) < 1.5f)
            {
                StopGrapple();
            }
        }

        DrawWire();
    }

    void StopGrapple()
    {
        if (fpsController.isSwing)
        {
            // 끊어지는 순간, 비행하면서 얻은 최종 속도를 플레이어의 기본 속도로 이관 (관성 유지)
            fpsController.SetVelocityFromGrapple(lastSwingVelocity);
        }

        isWireFlying = false;
        fpsController.isSwing = false; // 제어권 돌려줌
        cooldownTimer = grappleCooldown; // 쿨타임 시작
    }

    void DrawWire()
    {
        if (wireRenderer == null) return;

        if (isWireFlying || fpsController.isSwing)
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