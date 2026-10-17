using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
static class GraphicsBufferValidation
{
    const string Key="GraphicsBufferProbe.Run";
    static GraphicsBufferValidation()
    {
        EditorApplication.update+=()=>
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorApplication.isUpdating||!File.Exists("Temp/graphics_buffer_probe"))return;
            string label=File.ReadAllText("Temp/graphics_buffer_probe").Trim();File.Delete("Temp/graphics_buffer_probe");
            EditorSceneManager.SaveOpenScenes();EditorSceneManager.OpenScene(IslandSceneSetup.MainScenePath);
            SessionState.SetString(Key,label);EditorApplication.isPlaying=true;
        };
        EditorApplication.playModeStateChanged+=state=>
        {
            string label=SessionState.GetString(Key,"");if(label.Length==0)return;
            if(state==PlayModeStateChange.EnteredPlayMode)new GameObject("GraphicsBufferProbe").AddComponent<GraphicsBufferProbe>().runLabel=label;
            else if(state==PlayModeStateChange.EnteredEditMode)SessionState.EraseString(Key);
        };
    }
}
