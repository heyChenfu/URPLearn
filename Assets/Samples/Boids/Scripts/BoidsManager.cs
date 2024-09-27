using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BoidsSimulator
{
    public class BoidsManager : MonoBehaviour
    {
        [SerializeField]
        public GameObject BoidObj;
        [SerializeField]
        public int InstantiateCount = 10;
        [SerializeField]
        public GameObject TargetObj;

        BoidsDataMono _boidsData;
        BoidsMono[] _boidsArr;

        // Start is called before the first frame update
        void Start()
        {
            _boidsData = GetComponent<BoidsDataMono>();
            _boidsArr = new BoidsMono[InstantiateCount];

            for (int i = 0; i < InstantiateCount; ++i)
            {
                var boidObj = GameObject.Instantiate(BoidObj);
                float posX = Random.Range(-5f, 5f);
                float posY = Random.Range(-5f, 5f);
                float posZ = Random.Range(-5f, 5f);
                boidObj.transform.position = new Vector3(posX, posY, posZ);
                _boidsArr[i] = boidObj.GetComponent<BoidsMono>();
                _boidsArr[i].Initialize(_boidsData, TargetObj.transform);
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (_boidsArr == null || _boidsArr.Length == 0)
                return;

            for (int i = 0; i < _boidsArr.Length; ++i)
            {
                for (int j = 0; j < _boidsArr.Length; ++j)
                {
                    if (i == j)
                        continue;
                    Vector3 offset = _boidsArr[j].Position - _boidsArr[i].Position;
                    float sqrDst = offset.sqrMagnitude;

                    if (sqrDst < _boidsData.perceptionRadius * _boidsData.perceptionRadius)
                    {
                        _boidsArr[i].NumFlockmates += 1;
                        _boidsArr[i].FlockHeading += _boidsArr[j].Forward;
                        _boidsArr[i].FlockCentre += _boidsArr[j].Position;

                        if (sqrDst < _boidsData.avoidanceRadius * _boidsData.avoidanceRadius)
                            _boidsArr[i].AvoidanceHeading -= offset / sqrDst;
                    }
                }
                _boidsArr[i].UpdateBoid();
            }

        }
    }

}
