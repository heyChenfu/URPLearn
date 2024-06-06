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
    public float FootDistanceToGroud = 0;
    [SerializeField]
    public float FootIKMaxDistance = 0;

    public LayerMask IkContainLayer;

    Animator animator;

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();

    }

    // Update is called once per frame
    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal"); // Ĭ��ӳ�䵽���Ҽ�ͷ����A��D��
        float vertical = Input.GetAxis("Vertical"); // Ĭ��ӳ�䵽���¼�ͷ����W��S��

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
        transform.Translate(direction * Time.deltaTime * WalkSpeed);
        transform.Rotate(0, horizontal * RotateSpeed * Time.deltaTime, 0);

    }

    private void OnAnimatorIK(int layerIndex)
    {
        animator.SetIKPositionWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.LeftFoot, 1);
        animator.SetIKPositionWeight(AvatarIKGoal.RightFoot, 1);
        animator.SetIKRotationWeight(AvatarIKGoal.RightFoot, 1);

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

}
