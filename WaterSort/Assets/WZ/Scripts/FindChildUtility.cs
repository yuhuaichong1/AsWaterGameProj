using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Child lookup utility helpers.
/// </summary>
public static class FindChildUtility
{
    /// <summary>
    /// Recursively finds a child object by name under the given GameObject.
    /// </summary>
    public static GameObject FindChild(GameObject parent, string childName)
    {
        if (parent == null) return null;
        return FindChildRecursive(parent.transform, childName);
    }

    /// <summary>
    /// Recursively finds a child object by name and returns the requested component.
    /// </summary>
    public static T FindChild<T>(GameObject parent, string childName) where T : Component
    {
        GameObject child = FindChild(parent, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    /// <summary>
    /// Recursively finds a child object by name under the given Transform.
    /// </summary>
    public static GameObject FindChild(Transform parent, string childName)
    {
        if (parent == null) return null;
        return FindChildRecursive(parent, childName);
    }

    /// <summary>
    /// Recursively finds a child object by name and returns the requested component.
    /// </summary>
    public static T FindChild<T>(Transform parent, string childName) where T : Component
    {
        GameObject child = FindChild(parent, childName);
        return child != null ? child.GetComponent<T>() : null;
    }

    private static GameObject FindChildRecursive(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child.gameObject;

            GameObject result = FindChildRecursive(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Recursively finds all child objects with the given name.
    /// </summary>
    public static List<GameObject> FindAllChildren(GameObject parent, string childName)
    {
        List<GameObject> results = new List<GameObject>();
        if (parent != null)
            FindAllChildrenRecursive(parent.transform, childName, results);

        return results;
    }

    private static void FindAllChildrenRecursive(Transform parent, string childName, List<GameObject> results)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
                results.Add(child.gameObject);

            FindAllChildrenRecursive(child, childName, results);
        }
    }

    /// <summary>
    /// Finds a child object by direct path, for example "Child/GrandChild".
    /// </summary>
    public static GameObject FindChildByPath(GameObject parent, string path)
    {
        if (parent == null) return null;
        Transform child = parent.transform.Find(path);
        return child != null ? child.gameObject : null;
    }

    /// <summary>
    /// Recursively finds a child object by name, including inactive children.
    /// </summary>
    public static GameObject FindChildIncludeInactive(GameObject parent, string childName)
    {
        if (parent == null) return null;
        return FindChildRecursiveIncludeInactive(parent.transform, childName);
    }

    private static GameObject FindChildRecursiveIncludeInactive(Transform parent, string childName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child.gameObject;

            GameObject result = FindChildRecursiveIncludeInactive(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }
}
