using System.Collections.Generic;
using UnityEngine;

public class SimpleObjectPool : MonoBehaviour
{
    private static SimpleObjectPool _instance;
    public static SimpleObjectPool Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<SimpleObjectPool>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("SimpleObjectPool");
                    _instance = obj.AddComponent<SimpleObjectPool>();
                }
            }
            return _instance;
        }
    }

    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public GameObject SpawnFromPool(string poolTag, GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        if (!poolDictionary.ContainsKey(poolTag))
        {
            poolDictionary[poolTag] = new Queue<GameObject>();
        }

        GameObject objectToSpawn = null;
        if (poolDictionary[poolTag].Count > 0)
        {
            objectToSpawn = poolDictionary[poolTag].Dequeue();
        }

        if (objectToSpawn == null)
        {
            objectToSpawn = Instantiate(prefab);
            objectToSpawn.name = prefab.name;
        }

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;
        if (parent != null)
        {
            objectToSpawn.transform.SetParent(parent, false);
        }
        else
        {
            objectToSpawn.transform.SetParent(null);
        }

        return objectToSpawn;
    }

    public void ReturnToPool(string poolTag, GameObject objectToReturn)
    {
        if (objectToReturn == null) return;
        
        objectToReturn.SetActive(false);

        if (!poolDictionary.ContainsKey(poolTag))
        {
            poolDictionary[poolTag] = new Queue<GameObject>();
        }

        if (!poolDictionary[poolTag].Contains(objectToReturn))
        {
            poolDictionary[poolTag].Enqueue(objectToReturn);
        }
    }
}
