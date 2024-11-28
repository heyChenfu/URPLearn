using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class GeckoController : MonoBehaviour
{
    
    [SerializeField] private Transform target;
    [SerializeField] private Transform headBone;
    [SerializeField] [Range(0, 10)] private float headTurnSpeed;
    [SerializeField] private float headMaxTurnAngle;

    [SerializeField] Transform leftEyeBone;
    [SerializeField] Transform rightEyeBone;

    [SerializeField] float eyeTrackingSpeed;
    //左右眼最大最小Y旋转
    [SerializeField,Range(-180, 180)] float leftEyeMaxYRotation;
    [SerializeField,Range(-180, 180)] float leftEyeMinYRotation;
    [SerializeField,Range(-180, 180)] float rightEyeMaxYRotation;
    [SerializeField,Range(-180, 180)] float rightEyeMinYRotation;

    [SerializeField] LegStepper frontLeftLegStepper;
    [SerializeField] LegStepper frontRightLegStepper;
    [SerializeField] LegStepper backLeftLegStepper;
    [SerializeField] LegStepper backRightLegStepper;

    [SerializeField] float turnSpeed;
    [SerializeField] float moveSpeed;

    [SerializeField] float turnAcceleration;
    [SerializeField] float moveAcceleration;

    [SerializeField] float minDistToTarget;
    [SerializeField] float maxDistToTarget;

    [SerializeField] float maxAngleToTarget;

    private Vector3 currentVelocity;
    private float currentAngularVelocity;


    void Awake()
    {
        StartCoroutine(LegTracking());
    }

    void LateUpdate()
    {
        RootMotionUpdate();
        HeadTracking();
        EyesTracking();
    }

    void HeadTracking()
    {
        Vector3 targetWorldLookDir = target.position - headBone.position;
        //旋转约束
        targetWorldLookDir = Vector3.RotateTowards(transform.forward, targetWorldLookDir, 
            Mathf.Deg2Rad * headMaxTurnAngle/*允许此旋转的最大角度*/, 0);
        //四元数.LookRotation方法。此方法采用向前和向上方向，并输出一个旋转
        Quaternion targetLocalRotation = Quaternion.LookRotation(targetWorldLookDir, transform.up);
        //frame rate independent damping
        float fDamping = 1 - Mathf.Exp(-headTurnSpeed * Time.deltaTime);
        //但由于四元数表示旋转, 使用球形线性插值
        headBone.rotation = Quaternion.Slerp(headBone.rotation, targetLocalRotation, fDamping);

    }

    void EyesTracking()
    {
        Quaternion targetEyeRotation = Quaternion.LookRotation(target.position - headBone.position, Vector3.up);
        float fDamping = 1 - Mathf.Exp(-eyeTrackingSpeed * Time.deltaTime);
        leftEyeBone.rotation = Quaternion.Slerp(leftEyeBone.rotation, targetEyeRotation, fDamping);
        rightEyeBone.rotation = Quaternion.Slerp(rightEyeBone.rotation, targetEyeRotation, fDamping);
        //给眼睛添加局部旋转角度约束
        float leftEyeCurrentYRotation = leftEyeBone.localEulerAngles.y;
        float rightEyeCurrentYRotation = rightEyeBone.localEulerAngles.y;
        // Move the rotation to a -180 ~ 180 range
        if (leftEyeCurrentYRotation > 180)
            leftEyeCurrentYRotation -= 360;
        if (rightEyeCurrentYRotation > 180) 
            rightEyeCurrentYRotation -= 360;
        
        float leftEyeClampedYRotation = Mathf.Clamp(
            leftEyeCurrentYRotation,
            leftEyeMinYRotation,
            leftEyeMaxYRotation
        );
        float rightEyeClampedYRotation = Mathf.Clamp(
            rightEyeCurrentYRotation,
            rightEyeMinYRotation,
            rightEyeMaxYRotation
        );

        // Apply the clamped Y rotation without changing the X and Z rotations
        leftEyeBone.localEulerAngles = new Vector3(
            leftEyeBone.localEulerAngles.x,
            leftEyeClampedYRotation,
            leftEyeBone.localEulerAngles.z
        );
        rightEyeBone.localEulerAngles = new Vector3(
            rightEyeBone.localEulerAngles.x,
            rightEyeClampedYRotation,
            rightEyeBone.localEulerAngles.z
        );
        
    }

    void RootMotionUpdate()
    {
        Vector3 towardTarget = target.position - transform.position;
        Vector3 towardTargetProjected = Vector3.ProjectOnPlane(towardTarget, transform.up);

        float angleToTarget = Vector3.SignedAngle(transform.forward, towardTargetProjected, transform.up);
        float targetAngularVelocity = 0;

        if (Math.Abs(angleToTarget) > maxAngleToTarget)
        {
            targetAngularVelocity = angleToTarget > 0 ? turnSpeed : -turnSpeed;
        }

        currentAngularVelocity = Mathf.Lerp(currentAngularVelocity, targetAngularVelocity,
            1 - Mathf.Exp(-turnAcceleration * Time.deltaTime));

        transform.Rotate(0, Time.deltaTime * currentAngularVelocity, 0, Space.World);

        Vector3 targetVelocity = Vector3.zero;
        if (Mathf.Abs(angleToTarget) < 60)
        {
            float disToTarget = Vector3.Distance(transform.position, target.position);
            if (disToTarget > maxDistToTarget)
            {
                targetVelocity = moveSpeed * towardTargetProjected.normalized;
            }
            else if (disToTarget < minDistToTarget)
            {
                targetVelocity = moveSpeed * -towardTargetProjected.normalized;
            }
        }

        currentVelocity = Vector3.Lerp(currentVelocity, targetVelocity,
            1 - Mathf.Exp(-moveAcceleration * Time.deltaTime));

        transform.position += currentVelocity * Time.deltaTime;
    }

    IEnumerator LegTracking()
    {
        if(frontLeftLegStepper == null || backRightLegStepper == null ||
           frontRightLegStepper== null || backLeftLegStepper == null)
            yield break;
        while (true)
        {
            //避免四肢同时运动
            do
            {
                frontLeftLegStepper.MoveV1();
                backRightLegStepper.MoveV1();
                yield return null;
            }
            while (frontLeftLegStepper.Moving || backRightLegStepper.Moving);
            
            do
            {
                frontRightLegStepper.MoveV1();
                backLeftLegStepper.MoveV1();
                yield return null;
            }
            while (frontRightLegStepper.Moving || backLeftLegStepper.Moving);
        }
    }
}
