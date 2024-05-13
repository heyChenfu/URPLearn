using Algorithm;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace VectorField
{
    public class DigraphCreator : MonoBehaviour
    {
        public VectorFieldAgent _agent;
        [SerializeField]
        public int LineNumber;
        [SerializeField]
        public int ColumnNumber; //多少列
        [SerializeField]
        private float _naviNodeInterval = 1.0f;

        private VectorFieldNode[,] _vectorFieldArr;

        // Start is called before the first frame update
        void Start()
        {
            GameObject naviObj = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Samples/PathFinding/VectorField/Prefab/VectorFieldNavigateQuad.prefab");
            _vectorFieldArr = new VectorFieldNode[LineNumber, ColumnNumber];

            int XOffset = 0, YOffset = 0, ZOffset = 0;
            for (int i = 0; i < LineNumber; ++i)
            {
                for (int j = 0; j < 1; ++j)
                {
                    for (int k = 0; k < ColumnNumber; ++k)
                    {
                        bool bUnreachable = Random.Range(0, 10) < 2;
                        GameObject obj = GameObject.Instantiate<GameObject>(naviObj);
                        obj.transform.position = new Vector3(XOffset * _naviNodeInterval, 0, ZOffset * _naviNodeInterval);
                        obj.GetComponent<MeshRenderer>().material.color = bUnreachable ? Color.black : new Color(185 / 255f, 185 / 255f, 185 / 255f);
                        obj.transform.Find("NodePosText").GetComponent<TextMeshPro>().text = $"({XOffset},{ZOffset})";
                        BoxCollider collider = obj.GetComponent<BoxCollider>();
                        TextMeshPro distancsText = obj.transform.Find("NodeValueText").GetComponent<TextMeshPro>();
                        TextMeshPro angleText = obj.transform.Find("AngleValueText ").GetComponent<TextMeshPro>();
                        var arrowObj = obj.transform.Find("ArrowObj").GetComponent<SpriteRenderer>();
                        arrowObj.gameObject.SetActive(!bUnreachable);
                        _vectorFieldArr[i, k] = new VectorFieldNode(i, j, k, !bUnreachable, obj, collider, distancsText, angleText, arrowObj);

                        ZOffset++;
                    }
                    YOffset++;
                    ZOffset = 0;
                }
                XOffset++;
                YOffset = 0;
            }

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
                    VectorFieldDataNode[,] path = HitDetect(hitInfo, out VectorFieldDataNode endNode);
                    _agent.SetPath(path, endNode);
                }
            }

        }

        public VectorFieldDataNode[,] HitDetect(RaycastHit hitInfo, out VectorFieldDataNode endNode)
        {
            VectorFieldDataNode[,] dataList = new VectorFieldDataNode[_vectorFieldArr.GetLength(0), _vectorFieldArr.GetLength(1)];
            endNode = null;
            for (int i = 0; i < _vectorFieldArr.GetLength(0); ++i)
            {
                for (int j = 0; j < _vectorFieldArr.GetLength(1); ++j)
                {
                    dataList[i, j] = new VectorFieldDataNode(_vectorFieldArr[i, j], 0, Vector3.zero);
                    if (!_vectorFieldArr[i, j].Reachable)
                        continue;
                    if (_vectorFieldArr[i, j].NodeObj != null && _vectorFieldArr[i, j].NodeObj.transform == hitInfo.transform)
                        endNode = dataList[i, j];
                }
            }
            if (endNode == null)
                return null;
            VectorFieldAlgorithm.Instance().GenerateHeatMap(endNode, dataList);
            return dataList;
        }

    }

}
