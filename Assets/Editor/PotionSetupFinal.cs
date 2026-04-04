using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class PotionSetup
{
    [MenuItem("Tools/Mad Doctor/Setup Potion System Final")]
    public static void Setup()
    {
        string spritePath = "Assets/Mad Doctor Assets/Sprites/User Interfaces/potion1.png";
        string prefabFolder = "Assets/Mad Doctor Assets/Prefabs";
        string prefabPath = prefabFolder + "/HealthPotion.prefab";

        if (!AssetDatabase.IsValidFolder(prefabFolder))
        {
            AssetDatabase.CreateFolder("Assets/Mad Doctor Assets", "Prefabs");
        }

        GameObject potionObj = new GameObject("HealthPotion");
        SpriteRenderer sr = potionObj.AddComponent<SpriteRenderer>();
        Sprite potionSprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (potionSprite != null) sr.sprite = potionSprite;

        CircleCollider2D col = potionObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;

        // Add component by string if it's tricky to reference cross-assembly
        System.Type type = System.Type.GetType("HealthPotion, Assembly-CSharp");
        if (type != null) potionObj.AddComponent(type);

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(potionObj, prefabPath);
        GameObject.DestroyImmediate(potionObj);

        GameObject managerObj = GameObject.Find("PotionManager");
        if (managerObj == null) managerObj = new GameObject("PotionManager");
        
        System.Type managerType = System.Type.GetType("PotionManager, Assembly-CSharp");
        Component managerComp = managerObj.GetComponent(managerType);
        if (managerComp == null && managerType != null) managerComp = managerObj.AddComponent(managerType);

        if (managerComp != null)
        {
            SerializedObject so = new SerializedObject(managerComp);
            so.FindProperty("potionPrefab").objectReferenceValue = savedPrefab;
            so.ApplyModifiedProperties();
        }
        
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("✅ Setup Potions Done!");
    }
}
