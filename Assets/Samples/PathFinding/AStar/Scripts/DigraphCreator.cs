using Algorithm;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace AStar
{

    public class DigraphCreator : MonoBehaviour
    {
        [SerializeField]
        public GameObject AgentObj;
        [Range(0.01f, 0.1f)]
        [SerializeField]
        public float AgentVolocity = 0.05f;
        [SerializeField]
        public int NaviMeshCount;
        [SerializeField]
        public int ColumnNumber; //多少列
        [SerializeField]
        public float NaviInterval = 1.1f;
        [SerializeField]
        public Material NaviCommonMat;
        [SerializeField]
        public Material NaviDisableMat;

        private AStarGrid _aStarGrid;
        private List<AStarNode> _pathList;
        private int _pathCurrIndex;

        private const float _reachDistance = 0.1f;

        // Start is called before the first frame update
        void Start()
        {
            GameObject naviObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/PathFinding/AStar/Prefab/NavigateQuad.prefab");
            _aStarGrid = new AStarGrid(naviObj, NaviMeshCount / ColumnNumber + 1, 1, ColumnNumber, NaviCommonMat, NaviDisableMat);

        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hitInfo;
                if (Physics.Raycast(ray, out hitInfo))
                {
                    _pathList = _aStarGrid.HitDetect(hitInfo, AgentObj.transform.position);
                    _pathCurrIndex = 0;
                }
            }

            //移动物体
            if (_pathList != null && _pathList.Count > 0 &&
                AgentObj != null)
            {
                //是否已抵达索引节点
                if (Vector3.Distance(AgentObj.transform.position, _pathList[_pathCurrIndex].NodeObj.transform.position) < _reachDistance)
                {
                    if(_pathCurrIndex + 1 < _pathList.Count)
                    {
                        _pathCurrIndex++;
                    }
                    else
                    {
                        //抵达终点
                        _pathList = null;
                    }
                }
                else
                {
                    Vector3 currentPosition = AgentObj.transform.position;
                    Vector3 direction = _pathList[_pathCurrIndex].NodeObj.transform.position - currentPosition;
                    direction.Normalize();
                    AgentObj.transform.position += direction * AgentVolocity;

                }
            }

        }

    }

}

