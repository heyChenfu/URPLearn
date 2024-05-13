using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LegStepper : MonoBehaviour
{
    [SerializeField] private Transform homeTransform;
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

    public void Move()
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

    IEnumerator MoveToHome(Vector3 endPoint, Quaternion endRot)
    {
        Moving = true;

        Quaternion startRot = transform.rotation;
        Vector3 startPoint = transform.position;

        Vector3 centerPoint = (startPoint + endPoint) / 2;
        centerPoint += homeTransform.up * Vector3.Distance(startPoint, endPoint) * stepHeight;

        float timeElapsed = 0;

        do
        {
            timeElapsed += Time.deltaTime;
            float normalizedTime = Easing.EaseInOutCubic(timeElapsed / moveDuration);

            transform.position = Vector3.Lerp(
                Vector3.Lerp(startPoint, centerPoint, normalizedTime),
                Vector3.Lerp(centerPoint, endPoint, normalizedTime),
                normalizedTime);

            transform.rotation = Quaternion.Lerp(startRot, endRot, normalizedTime);

            yield return null;
        }
        while (timeElapsed < moveDuration);

        Moving = false;
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
