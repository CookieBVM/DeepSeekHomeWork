using UnityEngine;
using UnityEditor;

namespace DeepSeek.DigitalHuman.Editor
{
    public class MixamoAvatarPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            bool isCh46 = assetPath.Contains("Ch46_nonPBR");
            bool isAnimFbx = assetPath.Contains("Standing Idle") ||
                             assetPath.Contains("Waving") ||
                             assetPath.Contains("Clapping") ||
                             assetPath.Contains("Cheering");

            if (!isCh46 && !isAnimFbx)
                return;

            ModelImporter importer = assetImporter as ModelImporter;
            if (importer == null) return;

            if (importer.animationType == ModelImporterAnimationType.Human)
                return;

            bool isBaseModel = isCh46 || assetPath.Contains("Standing Idle") ||
                               assetPath.Contains("Waving");
            bool isAnimationOnly = assetPath.Contains("Clapping") ||
                                   assetPath.Contains("Cheering");

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importBlendShapes = false;
            importer.importVisibility = isBaseModel && !isAnimationOnly;
            importer.importCameras = false;
            importer.importLights = false;

            Debug.Log("[MixamoAvatarPostprocessor] Auto-configured " +
                      System.IO.Path.GetFileName(assetPath) + " as Humanoid.");
        }
    }
}