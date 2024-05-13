using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace VectorField
{
    public struct VectorFieldNode
    {
        public int X;
        public int Y;
        public int Z;
        public bool Reachable; //是否可到达

        public GameObject NodeObj;
        public BoxCollider BCollider;
        public TextMeshPro DistanceText;
        public TextMeshPro AngleText;
        public SpriteRenderer ArrowObj;

        public VectorFieldNode(int x, int y, int z, bool reachable, GameObject nodeObj, BoxCollider collider, TextMeshPro disText, TextMeshPro angleText, SpriteRenderer arrowObj)
        {
            X = x;
            Y = y;
            Z = z;
            Reachable = reachable;
            NodeObj = nodeObj;
            BCollider = collider;
            DistanceText = disText;
            AngleText = angleText;
            ArrowObj = arrowObj;
        }

        public static bool operator ==(VectorFieldNode a, VectorFieldNode b)
        {
            return a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        }

        public static bool operator !=(VectorFieldNode a, VectorFieldNode b)
        {
            return !(a == b);
        }

    }

    public class VectorFieldDataNode 
    { 
        public VectorFieldNode Node;
        public float Distance; //距离目标点距离
        public Vector3 Direction; //目标点方向
        public bool IsInit;

        public VectorFieldDataNode(VectorFieldNode node, float distance, Vector3 direction)
        {
            Node = node;
            Distance = distance;
            Direction = direction;
            IsInit = false;

        }

        public void SetDistance(float distance, float maxDistance)
        {
            Distance = distance;
            Node.DistanceText.text = Distance.ToString("0.#");
            Node.ArrowObj.color = new Color(1, Distance / maxDistance, Distance / maxDistance);

        }


    }


}
