using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// demo使用，创建大量单位
/// </summary>
public class SpawnManager : MonoBehaviour {

    #region 字段

    public GameObject spawnPrefab;
    public int gridWidth;
    public int gridHeight;
    public Vector3 initPos;

    #endregion


    #region 方法

    void Start()
    {
        for(var i = 0; i < gridWidth; i++)
        {
            for(var j = 0; j < gridHeight; j++)
            {
                Instantiate<GameObject>(spawnPrefab, initPos + new Vector3(i, 0, j), Quaternion.identity);
            }
        }
    }


    #endregion

}
