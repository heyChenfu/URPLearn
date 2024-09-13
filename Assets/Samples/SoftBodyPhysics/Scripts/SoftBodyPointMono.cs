using UnityEngine;

namespace SoftBodySimulator 
{
    public class SoftBodyPointMono : MonoBehaviour
    {
        public float Radius;
        [HideInInspector]
        public Vector2 Velocity;
        [HideInInspector]
        public float Width;
        [HideInInspector]
        public float Height;

        // Start is called before the first frame update
        void Start()
        {
            var spriteRender = GetComponent<SpriteRenderer>();
            if (spriteRender != null)
            {
                float width = spriteRender.bounds.size.x;
                float height = spriteRender.bounds.size.y;
                Debug.Log($"width={width}, height={height}");
            }

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
