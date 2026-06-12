using UnityEditor;
using UnityEngine;

public static class DigitalHumanEditorTools
{
    [MenuItem("Tools/Digital Human/Rebuild SampleScene", false, 2)]
    public static void RebuildScene()
    {
        try
        {
            DigitalHumanSampleSceneBuilder.RebuildSampleScene();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[DigitalHumanEditorTools] Scene rebuild failed: " + ex);
            EditorUtility.DisplayDialog("重建失败", ex.Message, "好的");
        }
    }
}
