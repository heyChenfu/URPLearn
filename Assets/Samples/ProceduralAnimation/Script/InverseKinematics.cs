using UnityEngine;

//Two-Bone IK
public class InverseKinematics : MonoBehaviour
{
    //骨骼链末端需要到达的目标位置
    [SerializeField] Transform target;
    //极点, 通过极点（如肘部或膝盖的指向）来引导中间关节的弯曲方向
    [SerializeField] Transform pole;

    //firstBone/secondBone/thirdBone骨骼链的三个关节（如肩膀、肘部、手腕）
    [SerializeField] Transform firstBone;
    [SerializeField] Vector3 firstBoneEulerAngleOffset;
    [SerializeField] Transform secondBone;
    [SerializeField] Vector3 secondBoneEulerAngleOffset;
    [SerializeField] Transform thirdBone;
    [SerializeField] Vector3 thirdBoneEulerAngleOffset;
    //末端骨骼（如手腕）是否对齐目标的旋转
    [SerializeField] bool alignThirdBoneWithTargetRotation = true;

    void OnEnable()
    {
        // Prevent null ref spam in case we didn't link up the bones
        if (
            firstBone == null ||
            secondBone == null ||
            thirdBone == null ||
            pole == null ||
            target == null
        )
        {
            Debug.LogError("IK bones not initialized", this);
            enabled = false;
            return;
        }
    }

    void LateUpdate()
    {
        Vector3 towardPole = pole.position - firstBone.position;
        Vector3 towardTarget = target.position - firstBone.position;

        //根骨骼到第二个骨骼距离
        float rootBoneLength = Vector3.Distance(firstBone.position, secondBone.position);
        //第二个骨骼到第三个骨骼距离
        float secondBoneLength = Vector3.Distance(secondBone.position, thirdBone.position);
        float totalChainLength = rootBoneLength + secondBoneLength;

        // 将根骨骼朝向目标
        firstBone.rotation = Quaternion.LookRotation(towardTarget, towardPole);
        firstBone.localRotation *= Quaternion.Euler(firstBoneEulerAngleOffset);

        Vector3 towardSecondBone = secondBone.position - firstBone.position;
        //根骨骼到目标距离
        var targetDistance = Vector3.Distance(firstBone.position, target.position);

        // Limit hypotenuse to under the total bone distance to prevent invalid triangles
        //根骨骼到目标需要满足骨骼链的长度限制, 限制斜边小于另外两边距离，以防止无效三角形
        targetDistance = Mathf.Min(targetDistance, totalChainLength * 0.9999f);

        // 将三条边组成三角形, 根骨骼所需的旋转角度, 需要满足骨骼链长度的几何约束
        // 余弦定律(已知三角形三条边长度,则可以推算出任意一个角的角度) https://en.wikipedia.org/wiki/Law_of_cosines
        var adjacent =
            (
                (rootBoneLength * rootBoneLength) +
                (targetDistance * targetDistance) -
                (secondBoneLength * secondBoneLength)
            ) / (2 * targetDistance * rootBoneLength);
        var angle = Mathf.Acos(adjacent) * Mathf.Rad2Deg;

        // We rotate around the vector orthogonal to both pole and second bone
        // 找到一个旋转轴，该轴垂直于平面（由 towardPole 和 towardSecondBone 构成）
        Vector3 cross = Vector3.Cross(towardPole, towardSecondBone);

        if (!float.IsNaN(angle))
        {
            //通过旋转angle，我们让根骨骼的指向满足三角形的几何约束（即目标的距离合法性）
            firstBone.RotateAround(firstBone.position, cross, -angle);
        }

        // We've rotated the root bone to the right place, so we just 
        // look at the target from the elbow to get the final rotation
        var secondBoneTargetRotation = Quaternion.LookRotation(target.position - secondBone.position, cross);
        secondBoneTargetRotation *= Quaternion.Euler(secondBoneEulerAngleOffset); //四元数相乘表示将一个旋转叠加到另一个旋转上
        secondBone.rotation = secondBoneTargetRotation;

        if (alignThirdBoneWithTargetRotation)
        {
            thirdBone.rotation = target.rotation;
            thirdBone.localRotation *= Quaternion.Euler(thirdBoneEulerAngleOffset);
        }
    }
}
