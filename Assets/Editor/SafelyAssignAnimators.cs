using UnityEngine;
using UnityEditor;

public class SafelyAssignAnimators
{
    [MenuItem("Tools/Mad Doctor/Safely Assign Enemy Animators")]
    public static void Assign()
    {
        string[] prefabNames = { "Enemy_1", "Enemy02", "Enemy03", "Enemy04", "Enemy05", "Enemy06" };
        string[] folders = { "Enemy Character01", "Enemy Character02", "Enemy Character03", "Enemy Character04", "Enemy Character05", "Enemy Character06" };
        
        for (int i = 0; i < prefabNames.Length; i++)
        {
            string prefabPath = "Assets/Mad Doctor Assets/Prefabs/" + prefabNames[i] + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            EditorUtility.SetDirty(prefab);
            
            string controllerPath = $"Assets/Mad Doctor Assets/Animations/Enemy/{folders[i]}/{prefabNames[i]}_Controller.controller";
            RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(controllerPath);

            if (prefab != null && controller != null)
            {
                Animator anim = prefab.GetComponent<Animator>();
                if (anim == null) anim = prefab.AddComponent<Animator>();
                anim.runtimeAnimatorController = controller;
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"Successfully assigned {controller.name} to {prefab.name}");
            }
            else
            {
                Debug.LogError($"Failed to load prefab or controller. Prefab: {prefab != null}, Controller: {controller != null} - {controllerPath}");
            }
        }
    }
}
