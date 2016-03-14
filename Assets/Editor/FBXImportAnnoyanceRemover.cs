using UnityEngine;
using UnityEditor;
using System;

public class FBXImportAnnoyanceRemover : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        ModelImporter importer = assetImporter as ModelImporter;
        String name = importer.assetPath.ToLower();
        if (name.Substring(name.Length - 4, 4) == ".fbx" && importer.globalScale == 100.0f)
        {
            importer.globalScale = 50.0F;
            importer.generateAnimations = ModelImporterGenerateAnimations.None;
            importer.animationType = ModelImporterAnimationType.None;
            importer.optimizeMesh = false;
            importer.importBlendShapes = false;
            importer.importMaterials = false;
        }
    }
}