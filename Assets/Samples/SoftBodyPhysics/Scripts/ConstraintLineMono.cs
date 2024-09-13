using UnityEngine;

namespace SoftBodySimulator
{
    public class ConstraintLineMono : MonoBehaviour
    {
        //Լ������
        public float Distance = 1;
        //����
        public float ConstraintDamping = 1;
        //������
        public float SpringForce = 1;
        public SoftBodyPointMono[] Point = new SoftBodyPointMono[2];

        LineRenderer _lineRenderer;

        // Start is called before the first frame update
        void Start()
        {
            if (Point[0] == null || Point[1] == null)
                Debug.LogError("ConstraintLineMono��point��������©!");
            _lineRenderer = GetComponent<LineRenderer>();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            _lineRenderer.SetPosition(0, Point[0].transform.localPosition);
            _lineRenderer.SetPosition(1, Point[1].transform.localPosition);

        }

    }

}
