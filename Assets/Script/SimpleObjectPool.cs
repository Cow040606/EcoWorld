using System.Collections.Generic;
using UnityEngine;

public class SimpleObjectPool : MonoBehaviour
{
    public static SimpleObjectPool Instance { get; private set; }

    private Dictionary<string, Queue<GameObject>> poolDictionary = new Dictionary<string, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
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
