using UnityEngine;
using UnityEditor;

public class MenuItems
{
    [MenuItem("Tools/ApplyAllSelectedPrefabs")]
    private static void NewMenuOption()
    {
        foreach(GameObject g in Selection.gameObjects)
        {
            PrefabUtility.ReplacePrefab(g, PrefabUtility.GetPrefabParent(g));
        }
    }
}