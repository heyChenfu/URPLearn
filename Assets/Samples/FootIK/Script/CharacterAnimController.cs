using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimController : MonoBehaviour
{
    [SerializeField]
    public float WalkSpeed = 1;
    [SerializeField]
    public float RotateSpeed = 1;
    [SerializeField]
    public float WalkAnimAccelerate = 1;
    [SerializeField]
    public float FootDistanceToGroud = 0; //脚部贴地所需距离
    [SerializeField]
    public float FootIKMaxDistance = 0; //脚部IK检测最大距离
    [SerializeField]
    public float MaxFootDistance = 1; //脚部最大离开地面距离初始位置大小

    public LayerMask IkContainLayer;
    public bool IsIK = true;
    public bool IsMove = true;

    Animator animator;
    Vector3 _footInitIKLeftFootPos = default;
    Vector3 _footInitIKRightFootPos = default;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {

        float horizontal = Input.GetAxis("Horizontal"); // 默认映射到左右箭头键和A、D键
        float vertical = Input.GetAxis("Vertical"); // 默认映射到上下箭头键和W、S键

        Vector3 direction = Vector3.zero;
        float fWalkBlend = animator.GetFloat("WalkBlend");
        if (Mathf.Abs(vertical) >= float.Epsilon)
        {
            fWalkBlend += WalkAnimAccelerate * Time.deltaTime;
            direction += new Vector3(0, 0, vertical);
        }
        else
        {
            fWalkBlend = 0;
        }
        animator.SetFloat("WalkBlend", fWalkBlend);
        if (!IsMove)
            return;
        transform.Translate(direction * Time.deltaTime * WalkSpeed);
        transform.Rotate(0, horizontal * RotateSpeed * Time.deltaTime, 0);

    }

    private void OnAnimatorIK(int layerIndex)
    {
        if (!IsIK)
            return;

        Tuple<float, float> weightTupple = GetFootIKWeight();
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, weightTupple.Item1);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, weightTupple.Item1);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, weightTupple.Item2);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, weightTupple.Item2);

        RaycastHit hit;
        Ray rayLeftFoot = new Ray(animator.GetIKPosition(AvatarIKGoal.LeftFoot) + Vector3.up, Vector3.down);
        if (Physics.Raycast(rayLeftFoot, out hit, FootDistanceToGroud + FootIKMaxDistance, IkContainLayer))
        {
            if (hit.transform.tag == "Walkable")
            {
                Vector3 footPos = hit.point;
                footPos.y += FootDistanceToGroud;
                animator.SetIKPosition(AvatarIKGoal.LeftFoot, footPos);
                animator.SetIKRotation(AvatarIKGoal.LeftFoot, Quaternion.LookRotation(transform.forward, hit.normal));
                //Debug.Log("IK Position Set: " + animator.GetIKPosition(AvatarIKGoal.LeftFoot));
            }
        }
        Ray rayRightFoot = new Ray(animator.GetIKPosition(AvatarIKGoal.RightFoot) + Vector3.up, Vector3.down);
        if (Physics.Raycast(rayRightFoot, out hit, FootDistanceToGroud + FootIKMaxDistance, IkContainLayer))
        {
            if (hit.transform.tag == "Walkable")
            {
                Vector3 footPos = hit.point;
                footPos.y += FootDistanceToGroud;
                animator.SetIKPosition(AvatarIKGoal.RightFoot, footPos);
                animator.SetIKRotation(AvatarIKGoal.RightFoot, Quaternion.LookRotation(transform.forward, hit.normal));
            }
        }

    }

    /// <summary>
    /// 通过和初始距离大小计算当前IK权重
    /// 即当脚一开始贴近地面, 而后移动动画的播放逐渐导致远离地面, 所以逐渐减小IK权重
    /// </summary>
    /// <returns></returns>
    private Tuple<float, float> GetFootIKWeight()
    {
        if (_footInitIKLeftFootPos == default)
            _footInitIKLeftFootPos = animator.transform.InverseTransformPoint(animator.GetIKPosition(AvatarIKGoal.LeftFoot));
        if (_footInitIKRightFootPos == default)
            _footInitIKRightFootPos = animator.transform.InverseTransformPoint(animator.GetIKPosition(AvatarIKGoal.RightFoot));
        Vector3 currentLeftFootPosition = animator.transform.InverseTransformPoint(animator.GetIKPosition(AvatarIKGoal.LeftFoot));
        Vector3 currentRightFootPosition = animator.transform.InverseTransformPoint(animator.GetIKPosition(AvatarIKGoal.RightFoot));
        float disLeftSqr = Vector3.Distance(currentLeftFootPosition, _footInitIKLeftFootPos);
        float disRightSqr = Vector3.Distance(currentRightFootPosition, _footInitIKRightFootPos);
        float leftFootIKWeight = disLeftSqr / MaxFootDistance;
        float rightFootIKWeight = disRightSqr / MaxFootDistance;
        //Debug.Log($"leftFootIKWeight = {leftFootIKWeight}, rightFootIKWeight = {rightFootIKWeight}, disLeftSqr = {disLeftSqr}, disRightSqr = {disRightSqr}");
        leftFootIKWeight = 1 - Mathf.Clamp01(leftFootIKWeight);
        rightFootIKWeight = 1 - Mathf.Clamp01(rightFootIKWeight);
        return Tuple.Create(leftFootIKWeight, rightFootIKWeight);
    }

}
