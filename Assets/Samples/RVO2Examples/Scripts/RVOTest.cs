using RVO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace RVO2Examples
{
    public class RVOTest : MonoBehaviour
    {
        public GameObject Prefab01;
        public GameObject Prefab02;

        [SerializeField]
        float speed = 3f;
        [SerializeField]
        float space = 1.1f;
        [SerializeField]
        int N = 20;

        List<GameObject> _objList = new List<GameObject>();
        IList<RVO.Vector2> goals;
        List<int> selectIndex = null;
        System.Random random;
        bool bDragging = false;
        Vector3 _mouseStartPos;
        Vector3 _mouseEndPos;
        Rect _selectionRect;

        void Start()
        {
            goals = new List<RVO.Vector2>();
            random = new System.Random();

            // 创建静态阻挡
            GameObject[] obj = FindObjectsOfType(typeof(GameObject)) as GameObject[];
            foreach (GameObject g in obj)
            {
                if (g.tag.Equals("obstacle"))
                {
                    Vector3 scale = g.transform.lossyScale;
                    Vector3 position = g.transform.position;

                    IList<RVO.Vector2> obstacle = new List<RVO.Vector2>();
                    obstacle.Add(new RVO.Vector2(position.x + scale.x / 2, position.z + scale.z / 2));
                    obstacle.Add(new RVO.Vector2(position.x - scale.x / 2, position.z + scale.z / 2));
                    obstacle.Add(new RVO.Vector2(position.x - scale.x / 2, position.z - scale.z / 2));
                    obstacle.Add(new RVO.Vector2(position.x + scale.x / 2, position.z - scale.z / 2));
                    Simulator.Instance.addObstacle(obstacle);
                }
            }
            Simulator.Instance.processObstacles();

            Simulator.Instance.setAgentDefaults(10.0f, 10, 1f, 1.0f, 0.5f, speed, new RVO.Vector2(0.0f, 0.0f));
            CreateObj(new Vector3(-30, 0, 0), Prefab01, 1f);
            CreateObj(new Vector3(30, 0, 0), Prefab02, 1f);

        }

        void CreateObj(Vector3 position, GameObject prefab, float mass)
        {
            for (int i = 0; i < N; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    // orca
                    RVO.Vector2 p = new RVO.Vector2(i * space + position.x, j * space + position.z);
                    int index = Simulator.Instance.addAgent(p);
                    //Simulator.Instance.setAgentMass(index, mass);
                    // 目标点
                    goals.Add(p);
                    // 物体
                    GameObject g = GameObject.Instantiate(prefab);
                    _objList.Add(g);
                    g.transform.localScale = g.transform.localScale * 0.5f;
                }
            }
        }

        void Update()
        {
            Simulator.Instance.setTimeStep(Time.deltaTime);
            SetPreferredVelocities();
            Simulator.Instance.doStep();

            for (int i = 0; i < Simulator.Instance.getNumAgents(); ++i)
            {
                RVO.Vector2 p = Simulator.Instance.getAgentPosition(i);
                _objList[i].transform.position = new Vector3(p.x(), 0, p.y());
            }

            if (Input.GetMouseButtonDown(0))
            {
                bDragging = true;
                _mouseStartPos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0))
            {
                bDragging = false;
                _mouseEndPos = Input.mousePosition;
                _selectionRect = CalculateSelectionRect();
                selectIndex = SelectObjectsByRect(_selectionRect);
            }

            if (Input.GetMouseButtonDown(1) && selectIndex != null)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hitInfo))
                {
                    Vector3 position = hitInfo.point;
                    for (int i = 0; i < goals.Count; i++)
                    {
                        if (selectIndex.Contains(i))
                        {
                            RVO.Vector2 p = new RVO.Vector2(position.x, position.z);
                            goals[i] = p;
                        }
                    }

                }
            }

        }

        void OnGUI()
        {
            //GUI coordinates are used by the GUI system. They are identical to Screen coordinates
            //except that they start at (0,0) in the upper left and go to (Screen. width, Screen. height) in the lower right.
            //GUI坐标原点在左上角而屏幕坐标原点在左下角
            Rect adjustedRect = new Rect(_selectionRect.x, Screen.height - _selectionRect.y - _selectionRect.height, _selectionRect.width, _selectionRect.height);
            //GUI.DrawTexture(_selectionRect, Texture2D.whiteTexture);
            DrawRectangle(adjustedRect, 1, Color.green);
        }

        void SetPreferredVelocities()
        {
            for (int i = 0; i < Simulator.Instance.getNumAgents(); ++i)
            {
                RVO.Vector2 goalVector = goals[i] - Simulator.Instance.getAgentPosition(i);

                if (RVOMath.absSq(goalVector) > 1.0f)
                {
                    goalVector = RVOMath.normalize(goalVector) * speed;
                }

                Simulator.Instance.setAgentPrefVelocity(i, goalVector);
            }
        }

        Rect CalculateSelectionRect()
        {
            float width = Mathf.Abs(_mouseEndPos.x - _mouseStartPos.x);
            float height = Mathf.Abs(_mouseEndPos.y - _mouseStartPos.y);
            return new Rect(_mouseStartPos.x, _mouseStartPos.y, width, height);
        }

        List<int> SelectObjectsByRect(Rect selectionRect)
        {
            List<int> selectIndex = new List<int>();
            var mainCamera = Camera.main;
            for (int i = 0; i < _objList.Count; ++i)
            {
                Vector3 screenPoint = mainCamera.WorldToScreenPoint(_objList[i].transform.position);
                if (selectionRect.Contains(new UnityEngine.Vector2(screenPoint.x, screenPoint.y)))
                    selectIndex.Add(i);
            }
            return selectIndex;
        }

        void DrawRectangle(Rect area, int frameWidth, Color color)
        {
            //Create a one pixel texture with the right color
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();

            // Top
            GUI.DrawTexture(new Rect(area.x, area.y, area.width, frameWidth), texture);
            // Bottom
            GUI.DrawTexture(new Rect(area.x, area.yMax - frameWidth, area.width, frameWidth), texture);
            // Left
            GUI.DrawTexture(new Rect(area.x, area.y, frameWidth, area.height), texture);
            // Right
            GUI.DrawTexture(new Rect(area.xMax - frameWidth, area.y, frameWidth, area.height), texture);
        }


    }

}

