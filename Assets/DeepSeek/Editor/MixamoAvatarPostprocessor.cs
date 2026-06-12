using UnityEngine;
using UnityEditor;

namespace DeepSeek.DigitalHuman.Editor
{
    /// <summary>
    /// Auto-configures LittleWitch + Ch46 Mixamo FBX files as Humanoid on import.
    /// </summary>
    public class MixamoAvatarPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            bool isLittleWitch = assetPath.Contains("little witch");
            bool isCh46 = assetPath.Contains("Ch46_nonPBR");

            if (!isLittleWitch && !isCh46)
                return;

            ModelImporter importer = assetImporter as ModelImporter;
            if (importer == null) return;

            if (importer.animationType == ModelImporterAnimationType.Human)
                return;

            bool isBaseModel = isLittleWitch || assetPath.Contains("@Waving");
            bool isAnimationOnly = assetPath.Contains("@Standing Clap") || assetPath.Contains("@Cheering");

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importBlendShapes = false;
            importer.importVisibility = isBaseModel && !isAnimationOnly;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

            Debug.Log($"[MixamoAvatarPostprocessor] Auto-configured {System.IO.Path.GetFileName(assetPath)} as Humanoid.");
        }
    }
}
