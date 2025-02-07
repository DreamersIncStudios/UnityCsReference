// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEditor.TerrainTools;

namespace UnityEditor
{
    internal class TerrainMenus
    {
        [MenuItem("GameObject/3D Object/Terrain", false, 3000)]
        static void CreateTerrain(MenuCommand menuCommand)
        {
            // Create the storage for the terrain in the project
            // (So we can reuse it in multiple scenes)
            TerrainData terrainData = new TerrainData();
            terrainData.heightmapResolution = 513;
            terrainData.size = new Vector3(1000, 600, 1000);
            terrainData.baseMapResolution = 1024;
            terrainData.SetDetailResolution(1024, terrainData.detailResolutionPerPatch);

            AssetDatabase.CreateAsset(terrainData, AssetDatabase.GenerateUniqueAssetPath("Assets/New Terrain.asset"));
            var parent = menuCommand.context as GameObject;
            GameObject terrain = Terrain.CreateTerrainGameObject(terrainData);
            terrain.name = "Terrain";

            GameObjectUtility.SetParentAndAlign(terrain, parent);
            StageUtility.PlaceGameObjectInCurrentStage(terrain);
            GameObjectUtility.EnsureUniqueNameForSibling(terrain);
            Selection.activeObject = terrain;
            Undo.RegisterCreatedObjectUndo(terrain, "Create terrain");
        }

        internal static void ImportRaw()
        {
            string saveLocation = EditorUtility.OpenFilePanel("Import Raw Heightmap", "", "raw");
            if (saveLocation != "")
            {
                ImportRawHeightmap wizard = TerrainWizard.DisplayTerrainWizard<ImportRawHeightmap>("Import Heightmap", "Import");
                wizard.InitializeImportRaw(GetActiveTerrain(), saveLocation);
            }
        }

        internal static void ExportHeightmapRaw()
        {
            ExportRawHeightmap wizard = TerrainWizard.DisplayTerrainWizard<ExportRawHeightmap>("Export Heightmap", "Export");
            wizard.InitializeDefaults(GetActiveTerrain());
        }

        internal static void MassPlaceTrees()
        {
            PlaceTreeWizard wizard = TerrainWizard.DisplayTerrainWizard<PlaceTreeWizard>("Place Trees", "Place");
            wizard.InitializeDefaults(GetActiveTerrain());
        }

        internal static void Flatten()
        {
            FlattenHeightmap wizard = TerrainWizard.DisplayTerrainWizard<FlattenHeightmap>("Flatten Heightmap", "Flatten");
            wizard.InitializeDefaults(GetActiveTerrain());
        }

        internal static void RefreshPrototypes()
        {
            GetActiveTerrainData().RefreshPrototypes();
            GetActiveTerrain().Flush();
            EditorApplication.SetSceneRepaintDirty();
        }

        static Terrain GetActiveTerrain()
        {
            Object[] selection = Selection.GetFiltered(typeof(Terrain), SelectionMode.Editable);

            if (selection.Length != 0)
                return selection[0] as Terrain;
            else
                return Terrain.activeTerrain;
        }

        static TerrainData GetActiveTerrainData()
        {
            if (GetActiveTerrain())
                return GetActiveTerrain().terrainData;
            else
                return null;
        }
    }

    class TerrainDetailContextMenus
    {
        [MenuItem("CONTEXT/TerrainEngineDetails/Add Detail Mesh")]
        static internal void AddDetailMesh(MenuCommand item)
        {
            TerrainWizard.DisplayTerrainWizard<DetailMeshWizard>("Add Detail Mesh", "Add").InitializeDefaults((Terrain)item.context, -1);
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Add Grass Texture")]
        static internal void AddDetailTexture(MenuCommand item)
        {
            TerrainWizard.DisplayTerrainWizard<DetailTextureWizard>("Add Grass Texture", "Add").InitializeDefaults((Terrain)item.context, -1);
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Add Grass Texture", validate = true)]
        static internal bool AddDetailTextureValidate(MenuCommand item)
        {
            return UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null
                || UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.terrainDetailGrassBillboardShader != null
                || UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline.terrainDetailGrassShader != null;
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Edit")]
        static internal void EditDetail(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            DetailPrototype prototype = terrain.terrainData.detailPrototypes[item.userData];

            if (prototype.usePrototypeMesh)
            {
                TerrainWizard.DisplayTerrainWizard<DetailMeshWizard>("Edit Detail Mesh", "Apply").InitializeDefaults((Terrain)item.context, item.userData);
            }
            else
            {
                TerrainWizard.DisplayTerrainWizard<DetailTextureWizard>("Edit Grass Texture", "Apply").InitializeDefaults((Terrain)item.context, item.userData);
            }
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Edit", true)]
        static internal bool EditDetailCheck(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            return item.userData >= 0 && item.userData < terrain.terrainData.detailPrototypes.Length;
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Remove")]
        static internal void RemoveDetail(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            TerrainEditorUtility.RemoveDetail(terrain, item.userData);
        }

        [MenuItem("CONTEXT/TerrainEngineDetails/Remove", true)]
        static internal bool RemoveDetailCheck(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            return item.userData >= 0 && item.userData < terrain.terrainData.detailPrototypes.Length;
        }
    }
    class TerrainTreeContextMenus
    {
        [MenuItem("CONTEXT/TerrainEngineTrees/Add Tree")]
        static internal void AddTree(MenuCommand item)
        {
            TreeWizard wizard = TerrainWizard.DisplayTerrainWizard<TreeWizard>("Add Tree", "Add");
            wizard.InitializeDefaults((Terrain)item.context, -1);
        }

        [MenuItem("CONTEXT/TerrainEngineTrees/Edit Tree")]
        static internal void EditTree(MenuCommand item)
        {
            TreeWizard wizard = TerrainWizard.DisplayTerrainWizard<TreeWizard>("Edit Tree", "Apply");
            wizard.InitializeDefaults((Terrain)item.context, item.userData);
        }

        [MenuItem("CONTEXT/TerrainEngineTrees/Edit Tree", true)]
        static internal bool EditTreeCheck(MenuCommand item)
        {
            return PaintTreesTool.instance.selectedTree >= 0;
        }

        [MenuItem("CONTEXT/TerrainEngineTrees/Remove Tree")]
        static internal void RemoveTree(MenuCommand item)
        {
            Terrain terrain = (Terrain)item.context;
            TerrainEditorUtility.RemoveTree(terrain, item.userData);
        }

        [MenuItem("CONTEXT/TerrainEngineTrees/Remove Tree", true)]
        static internal bool RemoveTreeCheck(MenuCommand item)
        {
            return PaintTreesTool.instance.selectedTree >= 0;
        }
    }

    /*
        [MenuItem ("Terrain/Import Heightmap - Texture...")]
        static void ImportHeightmap () {
            ImportTextureHeightmap wizard = ScriptableWizard.DisplayWizard<ImportTextureHeightmap>("Import Heightmap", "Import");
            wizard.InitializeDefaults(GetActiveTerrain());
        }
    */
} //namespace
