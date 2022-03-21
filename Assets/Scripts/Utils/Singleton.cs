using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance = null;
    public static T Instance
    {
        get
        {
            instance = instance ?? (FindObjectOfType(typeof(T)) as T);
            instance = instance ?? new GameObject(typeof(T).ToString(), typeof(T)).GetComponent<T>();
            return instance;
        }
    }

    private void Awake()
    {
        if(instance == null)
        {
            instance = this as T;
        }
        AwakeInstance();
    }

    private void OnDestroy()
    {
        instance = null;
        DestroyInstance();
    }

    protected abstract void AwakeInstance();
    protected abstract void DestroyInstance();

    private void OnApplicationQuit()
    {
        instance = null;
    }
}