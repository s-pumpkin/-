using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceLoad<T> where T : UnityEngine.Object
{
    public Dictionary<string, T> Pool = new Dictionary<string, T>();

    public void Info(string path)
    {
        T[] objects = Resources.LoadAll<T>(path);
        foreach (T o in objects)
        {
            if (!Pool.ContainsKey(o.name))
                Pool.Add(o.name, o);
        }
    }

    public T GetObject(string naem)
    {
        T o;
        if (Pool.TryGetValue(naem, out o))
            return o;

        return null;
    }
}
