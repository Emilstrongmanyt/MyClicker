using MyClicker.Data;
using UnityEngine;

namespace MyClicker.Character
{
    public static class HeroPrefabLoader
    {
        const string EditorPath = "Assets/HeroEditor/FantasyHeroes/Prefabs/Human.prefab";

        public static GameObject Load(GameConfig config)
        {
            if (config != null && config.character != null && config.character.heroPrefab != null)
                return config.character.heroPrefab;

            var fromResources = Resources.Load<GameObject>("Human");
            if (fromResources != null)
                return fromResources;

#if UNITY_EDITOR
            var fromAssets = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(EditorPath);
            if (fromAssets != null)
                return fromAssets;
#endif
            Debug.LogError("[MyClicker] Human hero prefab is missing. Expected Assets/HeroEditor/FantasyHeroes/Prefabs/Human.prefab");
            return null;
        }
    }
}
