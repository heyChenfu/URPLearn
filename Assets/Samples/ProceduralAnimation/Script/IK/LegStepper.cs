using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegStepper : MonoBehaviour
{
    [SerializeField] private Transform homeTransform;
    //和Honme多长距离后移动
    [SerializeField] private float wantStepAtDistance;
    [SerializeField] private float moveDuration;
    [SerializeField] float stepOvershootFraction;

    [SerializeField] LayerMask groundRaycastMask = ~0;
    [SerializeField] float heightOverGround = 0.0f;
    [SerializeField] float stepHeight = 0.5f;

    [SerializeField] float wantStepAtAngle = 45.0f;
    [SerializeField] bool overshootFromHome = false;

    public bool Moving { get; private set; }
    public Vector3 EndPoint { get; private set; }

    private void Start()
    {
        transform.position = homeTransform.position;
        EndPoint = transform.position;
    }

    public void Setup(Transform homeTransform, float wantStepAtDistance, float wantStepAtAngle, float moveDuration, float stepOvershootFraction, LayerMask groundRaycastMask, float heightOverGround, float stepHeight, bool overshootFromHome)
    {
        this.homeTransform = homeTransform;
        this.wantStepAtDistance = wantStepAtDistance;
        this.moveDuration = moveDuration;
        this.stepOvershootFraction = stepOvershootFraction;
        this.groundRaycastMask = groundRaycastMask;
        this.heightOverGround = heightOverGround;
        this.stepHeight = stepHeight;
        this.overshootFromHome = overshootFromHome;
        this.wantStepAtAngle = wantStepAtAngle;
    }
    
    public void MoveV1()
    {
        // If we are already moving, don't start another move
        if (Moving) return;

        float distFromHome = Vector3.Distance(transform.position, homeTransform.position);

        // If we are too far off in position or rotation
        if (distFromHome > wantStepAtDistance)
        {
            // Start the step coroutine
            StartCoroutine(MoveCoroutine());
        }
    }

    IEnumerator MoveToHome(Vector3 endPoint, Quaternion endRot)
    {
        // Indicate we're moving (used later)
        Moving = true;

        // Store the initial conditions
        Quaternion startRot = transform.rotation;
        Vector3 startPoint = transform.position;

        // Time since step started
        float timeElapsed = 0;

        // Here we use a do-while loop so the normalized time goes past 1.0 on the last iteration,
        // placing us at the end position before ending.
        do
        {
            // Add time since last frame to the time elapsed
            timeElapsed += Time.deltaTime;

            float normalizedTime = timeElapsed / moveDuration;

            // Interpolate position and rotation
            transform.position = Vector3.Lerp(startPoint, endPoint,normalizedTime);
            transform.rotation = Quaternion.Slerp(startRot, endRot, normalizedTime);

            // Wait for one frame
            yield return null;
        }
        while (timeElapsed < moveDuration);

        // Done moving
        Moving = false;
    }
    
    IEnumerator MoveCoroutine()
    {
        Moving = true;

        Vector3 startPoint = transform.position;
        Quaternion startRot = transform.rotation;

        Quaternion endRot = homeTransform.rotation;

        // Directional vector from the foot to the home position
        Vector3 towardHome = (homeTransform.position - transform.position);
        // Total distnace to overshoot by   
        float overshootDistance = wantStepAtDistance * stepOvershootFraction;
        Vector3 overshootVector = towardHome * overshootDistance;
        // Since we don't ground the point in this simplified implementation,
        // we restrict the overshoot vector to be level with the ground
        // by projecting it on the world XZ plane.
        overshootVector = Vector3.ProjectOnPlane(overshootVector, Vector3.up);

        // Apply the overshoot
        Vector3 endPoint = homeTransform.position + overshootVector;

        // We want to pass through the center point
        Vector3 centerPoint = (startPoint + endPoint) / 2;
        // But also lift off, so we move it up by half the step distance (arbitrarily)
        centerPoint += homeTransform.up * Vector3.Distance(startPoint, endPoint) * 2 / 3;

        float timeElapsed = 0;
        do
        {
            timeElapsed += Time.deltaTime;
            float normalizedTime = timeElapsed / moveDuration;
            normalizedTime = Easing.EaseInOutCubic(normalizedTime);
            
            // 两次线性插值间接地实现二次贝塞尔曲线的效果
            transform.position =
                Vector3.Lerp(
                    Vector3.Lerp(startPoint, centerPoint, normalizedTime),
                    Vector3.Lerp(centerPoint, endPoint, normalizedTime),
                    normalizedTime
                );

            transform.rotation = Quaternion.Slerp(startRot, endRot, normalizedTime);

            yield return null;
        }
        while (timeElapsed < moveDuration);

        Moving = false;
    }

    public void MoveV2()
    {
        if (!Moving)
        {
            Vector3 pos = Vector3.ProjectOnPlane(transform.position, homeTransform.up);
            Vector3 homePos = Vector3.ProjectOnPlane(homeTransform.position, homeTransform.up);
            float sqrDist = Vector3.SqrMagnitude(pos - homePos);
            float angleFromHome = Quaternion.Angle(transform.rotation, homeTransform.rotation);
    
            if (sqrDist > wantStepAtDistance * wantStepAtDistance || angleFromHome > wantStepAtAngle)
            {
                Vector3 endPos;
                Vector3 endNormal;
                if(GetGroundedEndPosition(out endPos, out endNormal))
                {
                    EndPoint = endPos;
                    Quaternion endRot = Quaternion.LookRotation(Vector3.ProjectOnPlane(homeTransform.forward, endNormal), endNormal);
                    StartCoroutine(MoveToHome(endPos, endRot));
                }
            }
        }
    }
    
    bool GetGroundedEndPosition(out Vector3 position, out Vector3 normal)
    {
        Vector3 homeDir;
        if(overshootFromHome)
        {
            homeDir = homeTransform.forward;
        } 
        else
        {
            homeDir = (homeTransform.position - transform.position).normalized;
        }

        float overshootDistance = wantStepAtDistance * stepOvershootFraction;
        Vector3 overshootVector = homeDir * overshootDistance;

        Vector3 raycastOrigin = homeTransform.position + overshootVector + homeTransform.up * 5f;

        if (Physics.Raycast(
            raycastOrigin,
            -homeTransform.up,
            out RaycastHit hit,
            Mathf.Infinity,
            groundRaycastMask
        ))
        {
            position = hit.point + homeTransform.up * heightOverGround;
            normal = hit.normal;
            return true;
        }
        position = Vector3.zero;
        normal = Vector3.zero;
        return false;
    }
}
