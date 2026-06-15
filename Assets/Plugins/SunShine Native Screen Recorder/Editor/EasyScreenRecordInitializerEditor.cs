#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(EasyScreenRecordInitializer))]
public class EasyScreenRecordInitializerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.LabelField("Premium Feature", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(EasyScreenRecordInitializer.PremiumFeatureInfo, MessageType.Error);

        // if (GUILayout.Button("Upgrade to pro"))
        //     Application.OpenURL(EasyScreenRecordInitializer.ProAssetStoreUrl);

        EditorGUILayout.Space();
        DrawDefaultInspector();
    }
}
#endif