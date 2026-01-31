using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(GraphManager))]
public class GraphManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        using (new EditorGUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("Editor Tools", EditorStyles.boldLabel);

            using (new EditorGUI.DisabledScope(EditorApplication.isPlaying))
            {
                if (GUILayout.Button("Build Level (Editor)"))
                {
                    var mgr = (GraphManager)target;
                    Undo.RegisterFullObjectHierarchyUndo(mgr.gameObject, "Build Level");
                    mgr.BuildLevel();
                    EditorUtility.SetDirty(mgr);
                }

                if (GUILayout.Button("Clear Level (Editor)"))
                {
                    var mgr = (GraphManager)target;
                    Undo.RegisterFullObjectHierarchyUndo(mgr.gameObject, "Clear Level");
                    mgr.ClearLevel();
                    EditorUtility.SetDirty(mgr);
                }
            }
        }
    }
}