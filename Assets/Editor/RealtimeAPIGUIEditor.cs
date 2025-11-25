using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class RealtimeAPIGUIEditor
{
    static RealtimeAPIGUIEditor()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static RealtimeAPIConnection tool;

    private static void OnSceneGUI(SceneView sceneView)
    {
        // Only show during play mode
        if (!Application.isPlaying)
            return;

        if (tool == null)
            tool = GameObject.FindObjectOfType<RealtimeAPIConnection>(); // Assumes one instance in scene

        if (tool == null)
            return;

        Handles.BeginGUI();

        GUILayout.BeginArea(new Rect(10, 10, 220, 100), GUI.skin.window);
        GUILayout.Space(-10);
        GUILayout.Label("Realtime API Settings", EditorStyles.boldLabel);
        GUILayout.Space(5);

        GUILayout.Label("Connection", EditorStyles.boldLabel);
        GUILayout.Label("Status: " + tool.connectionStatus, EditorStyles.label);

        bool newConnectionState = GUILayout.Toggle(tool.isConnected, tool.connectionButtonString, "Button");
        if (newConnectionState != tool.isConnected)
        {
            tool.isConnected = newConnectionState;
            tool.sceneGUIButtonPressed();
        }

        GUILayout.EndArea();

        Handles.EndGUI();
    }
}