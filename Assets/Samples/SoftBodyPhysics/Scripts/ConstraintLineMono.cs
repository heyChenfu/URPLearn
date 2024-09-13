using UnityEngine;

namespace SoftBodySimulator
{
    public class ConstraintLineMono : MonoBehaviour
    {
        //‘º ¯æ‡¿Î
        public float Distance = 1;
        //◊Ëƒ·
        public float ConstraintDamping = 1;
        //µØª…¡¶
        public float SpringForce = 1;
        public SoftBodyPointMono[] Point = new SoftBodyPointMono[2];

        LineRenderer _lineRenderer;

        // Start is called before the first frame update
        void Start()
        {
            if (Point[0] == null || Point[1] == null)
                Debug.LogError("ConstraintLineMonoµƒpoint≈‰÷√”–“≈¬©!");
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
