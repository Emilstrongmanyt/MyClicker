using System.IO;
using UnityEditor;
using UnityEngine;

namespace MyClicker.Editor
{
    public static class LogoIconBuilder
    {
        const string IconPath = "Assets/MyClicker/Icons/AppIcon.png";

        [MenuItem("MyClicker/Assign App Icon")]
        public static void Assign()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null)
            {
                Debug.LogWarning("[MyClicker] App icon not found at " + IconPath);
                return;
            }

            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, new[] { icon });
            EditorUtility.SetDirty(icon);
            AssetDatabase.SaveAssets();
            Debug.Log("[MyClicker] Assigned app icon from " + IconPath);
        }
    }
}
