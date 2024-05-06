using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolObject : SingletonDontDestroy<PoolObject>
{
    private Dictionary<string, Queue<GameObject>> pooledObjects = new Dictionary<string, Queue<GameObject>>();
    public GameObject GetObject(GameObject gameObject)
    {
        if (pooledObjects.TryGetValue(gameObject.name, out Queue<GameObject> objectList))
        {
            if (objectList.Count == 0)
            {
                return CreateNewObject(gameObject);
            }
            else
            {
                GameObject _object = objectList.Dequeue();
                _object.SetActive(true);
                return _object;
            }
        }
        else
        {
            return CreateNewObject(gameObject);
        }
    }
    private GameObject CreateNewObject(GameObject gameObject)
    {
        GameObject newGO = Instantiate(gameObject);
        newGO.name = gameObject.name;
        return newGO;
    }
    public void ReturnGameObject(GameObject gameObject)
    {
        if (pooledObjects.TryGetValue(gameObject.name, out Queue<GameObject> objectList))
        {
            objectList.Enqueue(gameObject);
        }
        else
        {
            Queue<GameObject> newObjectQueue = new Queue<GameObject>();
            newObjectQueue.Enqueue(gameObject);
            pooledObjects.Add(gameObject.name, newObjectQueue);
        }
        gameObject.transform.SetParent(transform);
        gameObject.SetActive(false);
    }
}
