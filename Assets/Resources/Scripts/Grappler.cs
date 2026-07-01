using UnityEngine;
using System.Collections.Generic;

public class Grappler : MonoBehaviour
{
    [Header("References")]
    public PlayerController playerController;
    public LineRenderer wireRenderer;
    public Transform wireStartVisual;
    public Transform wireEndVisual;

    [Header("Grappling Hook Settings")]
    public float grappleMaxDistance = 30f;
    public float wireShootSpeed = 60f;
    public float grapplePullSpeed = 20f;
    public float grappleCooldown = 2f;

    [Header("Momentum & Safety Settings")]
    public float fallDampenRate = 5f;
    public float extraTimeoutBuffer = 1.0f;

    [Header("Wire Rope Physics")]
    [Range(4, 60)] public int maxWireSegments = 30;
    public float wireSegmentSpacing = 1.2f;
    public float wireSag = 0.6f;
    public float wireFollowSpeed = 30f;

    private bool isWireFlying = false;
    private Vector3 grappleTargetPos;
    private Vector3 wireTipPos;
    private Vector3 inheritedVelocity;
    private Vector3 lastSwingVelocity;
    private float cooldownTimer = 0f;

    private float maxSwingTime;
    private float swingTimer = 0f;
    private float previousSwingDistance = float.MaxValue;
    private List<Vector3> ropePoints;

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
                        InitRope(GetWireOrigin(), wireTipPos);

                        float distance = Vector3.Distance(cam.transform.position, grappleTargetPos);

                        float wireFlyingTime = distance / wireShootSpeed;
                        float playerTravelTime = distance / grapplePullSpeed;

                        float currentSpeed = playerController.GetCurrentVelocity().magnitude;
                        float momentumBonusTime = currentSpeed * 0.15f;

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

                // 슬라이딩/앉기 취소 로직 삭제 완료
                inheritedVelocity = playerController.GetCurrentVelocity();
                previousSwingDistance = float.MaxValue;
            }
        }

        if (playerController.isSwing)
        {
            if (inheritedVelocity.y < 0)
            {
                inheritedVelocity.y = Mathf.Lerp(inheritedVelocity.y, 0f, fallDampenRate * Time.deltaTime);
            }

            Vector3 playerCenter = playerController.characterController.bounds.center;
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

            float currentDistanceToTarget = Vector3.Distance(playerCenter, grappleTargetPos);

            if (currentDistanceToTarget < 1.5f || currentDistanceToTarget > previousSwingDistance)
            {
                StopGrapple();
            }
            else
            {
                previousSwingDistance = currentDistanceToTarget;
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

        if (isWireFlying)
        {
            Vector3 origin = GetWireOrigin();
            wireRenderer.enabled = true;
            SimulateRope(origin, wireTipPos, Time.deltaTime);
            wireRenderer.positionCount = ropePoints.Count;
            wireRenderer.SetPositions(ropePoints.ToArray());
            SetTipVisuals(true, origin, wireTipPos);
        }
        else if (playerController.isSwing)
        {
            Vector3 origin = GetWireOrigin();
            wireRenderer.positionCount = 2;
            wireRenderer.enabled = true;
            wireRenderer.SetPosition(0, origin);
            wireRenderer.SetPosition(1, grappleTargetPos);
            SetTipVisuals(true, origin, grappleTargetPos);
        }
        else
        {
            wireRenderer.enabled = false;
            SetTipVisuals(false, Vector3.zero, Vector3.zero);
        }
    }

    private void SetTipVisuals(bool visible, Vector3 startPos, Vector3 endPos)
    {
        if (wireStartVisual != null)
        {
            wireStartVisual.gameObject.SetActive(visible);
            if (visible) wireStartVisual.position = startPos;
        }

        if (wireEndVisual != null)
        {
            wireEndVisual.gameObject.SetActive(visible);
            if (visible) wireEndVisual.position = endPos;
        }
    }


    private Vector3 GetWireOrigin()
    {
        return cam.transform.position + (cam.transform.forward * 0.4f) + (cam.transform.right * 0.3f) - (cam.transform.up * 0.2f);
    }

    private void InitRope(Vector3 origin, Vector3 tip)
    {
        ropePoints = new List<Vector3> { origin, tip };
    }

    private void SimulateRope(Vector3 origin, Vector3 tip, float dt)
    {
        if (ropePoints == null) InitRope(origin, tip);

        ropePoints[0] = origin;
        ropePoints[ropePoints.Count - 1] = tip;

        while (ropePoints.Count < maxWireSegments && Vector3.Distance(ropePoints[ropePoints.Count - 2], tip) > wireSegmentSpacing)
        {
            int insertIndex = ropePoints.Count - 1;
            Vector3 straightPoint = Vector3.Lerp(ropePoints[ropePoints.Count - 2], tip, 0.5f);
            Vector3 saggedPoint = straightPoint + Vector3.down * wireSag;
            ropePoints.Insert(insertIndex, saggedPoint);
        }

        int last = ropePoints.Count - 1;
        float followRate = 1f - Mathf.Exp(-wireFollowSpeed * dt);

        for (int i = 1; i < last; i++)
        {
            float t = (float)i / last;

            Vector3 idealPos = Vector3.Lerp(origin, tip, t);
            Vector3 targetPos = idealPos + Vector3.down * wireSag;

            ropePoints[i] = Vector3.Lerp(ropePoints[i], targetPos, followRate);

            // like a rope in hand: the closer a point is to either end, the less slack
            // it's allowed to have, so the wire can never visibly detach no matter the speed
            float maxLag = Mathf.Min(i, last - i) * wireSegmentSpacing;
            Vector3 offset = ropePoints[i] - targetPos;
            if (offset.magnitude > maxLag)
            {
                ropePoints[i] = targetPos + offset.normalized * maxLag;
            }
        }
    }
}