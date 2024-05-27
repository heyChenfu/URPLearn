using RVO;
using UnityEngine;

namespace BatchRendererGroupTest
{
    public class GameManagementMono : MonoBehaviour
    {
        // Start is called before the first frame update
        void Awake()
        {
            Simulator.Instance.setTimeStep(0.15f);
            Simulator.Instance.setAgentDefaults(15.0f, 10, 10.0f, 5.0f, 0.8f, 2.0f, new RVO.Vector2(0.0f, 0.0f));
            // add Obstacles in awake
            Simulator.Instance.processObstacles();

        }

        // Update is called once per frame
        void Update()
        {

        }
    }

}
