using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoidsSimulator
{
    public class BoidsDataMono : MonoBehaviour
    {
        public float minSpeed = 2;
        public float maxSpeed = 5;
        public float perceptionRadius = 2.5f; //伙伴判定半径
        public float avoidanceRadius = 1; //规避判定半径
        public float maxSteerForce = 3; //最大转向力

        public float alignWeight = 1;
        public float cohesionWeight = 1;
        public float seperateWeight = 1;
        public float targetWeight = 1;

        public float boundsRadius = .27f;
        public float avoidCollisionWeight = 10;
        public float collisionAvoidDst = 5;

        public LayerMask obstacleMask;
    }

}
