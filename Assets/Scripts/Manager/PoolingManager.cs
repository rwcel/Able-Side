using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct FPool
{
    public int _Count;
    public GameObject _Obj;
    public Transform parent;   // *개별 부모가 필요한 경우 넣기 null인경우 poolParent로
}

public class PoolingManager : Singleton<PoolingManager>
{
    [SerializeField] Transform poolParent;
    [SerializeField] FPool[] pools;
    [SerializeField] FPool charPoolInfo;

    private Dictionary<string, Queue<GameObject>> poolDictionary;


    protected override void AwakeInstance()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        GeneratePrefab();
        GenerateCharacterPrefab();
    }

    protected override void DestroyInstance() { }

    private void GeneratePrefab()
    {
        foreach (var item in pools)
        {
            Queue<GameObject> poolQueue = new Queue<GameObject>();
            Transform parent;
            if (item.parent == null)
            {
                parent = new GameObject(item._Obj.name + " Pool").transform;
                parent.SetParent(poolParent);
            }
            else
            {
                parent = item.parent;
            }
            for (int i = 0, length = item._Count; i < length; i++)
            {
                GameObject obj = Instantiate(item._Obj, parent);
                obj.name = item._Obj.name + "_" + i.ToString();
                obj.SetActive(false);

                poolQueue.Enqueue(obj);
            }

            poolDictionary.Add(item._Obj.name, poolQueue);
            // Debug.Log(item._Obj.name);
        }
    }

    private void GenerateCharacterPrefab()
    {
        var list = GameManager.Instance.GameCharacter.CharacterDatas;

        // Debug.Log(list.Count);

        foreach (var item in list)
        {
            Queue<GameObject> poolQueue = new Queue<GameObject>();
            Transform parent;
            if (charPoolInfo.parent == null)
            {
                parent = new GameObject("Character Pool").transform;
                parent.SetParent(poolParent);
            }
            else
            {
                parent = charPoolInfo.parent;
            }

            for (int i = 0, length = charPoolInfo._Count; i < length; i++)
            {
                GameObject obj = Instantiate(item.prefab, parent);
                obj.name = item.prefab.name + "_" + i.ToString();
                obj.SetActive(false);

                poolQueue.Enqueue(obj);
            }

            poolDictionary.Add(item.prefab.name, poolQueue);
            // Debug.Log(item._Obj.name);
        }
    }

    public GameObject Dequeue(string name, Vector3 pos, Quaternion rot, bool isLocal = false)
    {
        if (name == Values.Key_Null)
            return null;

        GameObject obj = poolDictionary[name].Dequeue();
        if(isLocal)
        {
            obj.transform.localPosition = pos;
            obj.transform.localRotation = rot;
        }
        else
        {
            obj.transform.position = pos;
            obj.transform.rotation = rot;
        }
        obj.SetActive(true);

        return obj;
    }

    public void Enqueue(GameObject obj)
    {
        obj.SetActive(false);

        string name = obj.name.Split('_')[0];

        poolDictionary[name].Enqueue(obj);
    }
}