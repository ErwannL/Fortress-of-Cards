using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

namespace FortressOfCards.EditorTools
{
    public static class IsometricSceneSetup
    {
        [MenuItem("Tools/Fortress/Create Fresh Sample Scene")]
        public static void CreateFreshSampleScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetupSceneInternal(false);
            PopulateStarterTilesInternal(false);

            const string scenesFolder = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Scenes");
            }

            EditorSceneManager.SaveScene(newScene, "Assets/Scenes/SampleScene.unity");
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Sample Scene Ready",
                "A fresh SampleScene was created with Grid, Tilemaps, Camera, and starter tiles.",
                "OK");
        }

        [MenuItem("Tools/Fortress/Setup Isometric Tilemap Scene")]
        public static void SetupScene()
        {
            SetupSceneInternal(true);
        }

        private static void SetupSceneInternal(bool showDialog)
        {
            CleanupOldObjects();

            GameObject gridObject = GameObject.Find("Grid");
            if (gridObject == null)
            {
                gridObject = new GameObject("Grid");
                Undo.RegisterCreatedObjectUndo(gridObject, "Create Grid");
            }

            Grid grid = gridObject.GetComponent<Grid>();
            if (grid == null)
            {
                grid = Undo.AddComponent<Grid>(gridObject);
            }

            grid.cellLayout = GridLayout.CellLayout.Isometric;
            grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
            grid.cellSize = new Vector3(1f, 0.5f, 1f);

            SetupTilemap(gridObject.transform, "GroundTilemap", 0);
            SetupTilemap(gridObject.transform, "PathTilemap", 10);
            SetupTilemap(gridObject.transform, "PropsTilemap", 20);

            SetupMainCamera();

            Selection.activeObject = gridObject;
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Fortress Setup",
                    "Isometric Tilemap scene is ready. Open Window > 2D > Tile Palette to paint your ground/path tiles.",
                    "OK");
            }
        }

        [MenuItem("Tools/Fortress/Paint Target/Ground")]
        public static void SetGroundPaintTarget()
        {
            SetPaintTarget("GroundTilemap");
        }

        [MenuItem("Tools/Fortress/Paint Target/Path")]
        public static void SetPathPaintTarget()
        {
            SetPaintTarget("PathTilemap");
        }

        [MenuItem("Tools/Fortress/Paint Target/Props")]
        public static void SetPropsPaintTarget()
        {
            SetPaintTarget("PropsTilemap");
        }

        [MenuItem("Tools/Fortress/Paint Ready/Ground")]
        public static void PrepareGroundPainting()
        {
            PreparePainting("GroundTilemap");
        }

        [MenuItem("Tools/Fortress/Paint Ready/Path")]
        public static void PreparePathPainting()
        {
            PreparePainting("PathTilemap");
        }

        [MenuItem("Tools/Fortress/Populate Starter Tiles")]
        public static void PopulateStarterTiles()
        {
            PopulateStarterTilesInternal(true);
        }

        [MenuItem("Tools/Fortress/Debug/Paint Test Tile (Ground)")]
        public static void PaintTestTileOnGround()
        {
            SetupSceneInternal(false);

            GameObject groundObject = GameObject.Find("GroundTilemap");
            if (groundObject == null)
            {
                EditorUtility.DisplayDialog("Tilemap Missing", "GroundTilemap not found.", "OK");
                return;
            }

            Tilemap groundTilemap = groundObject.GetComponent<Tilemap>();
            TileBase groundTile = GetOrCreateTileAsset("sol-exterieur", "Ground_Auto");
            if (groundTilemap == null || groundTile == null)
            {
                EditorUtility.DisplayDialog("Test Failed", "Could not access GroundTilemap or Ground_Auto tile.", "OK");
                return;
            }

            Undo.RecordObject(groundTilemap, "Paint Test Tile");
            groundTilemap.SetTile(Vector3Int.zero, groundTile);
            groundTilemap.RefreshAllTiles();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            SetPaintTarget("GroundTilemap");
            EditorUtility.DisplayDialog("Test Tile Painted", "Placed one ground tile at cell (0,0).", "OK");
        }

        private static void PopulateStarterTilesInternal(bool showDialog)
        {
            GameObject groundObject = GameObject.Find("GroundTilemap");
            GameObject pathObject = GameObject.Find("PathTilemap");
            if (groundObject == null || pathObject == null)
            {
                SetupSceneInternal(false);
                groundObject = GameObject.Find("GroundTilemap");
                pathObject = GameObject.Find("PathTilemap");
                if (groundObject == null || pathObject == null)
                {
                    EditorUtility.DisplayDialog(
                        "Tilemap Missing",
                        "GroundTilemap/PathTilemap still not found after setup.",
                        "OK");
                    return;
                }
            }

            Tilemap groundTilemap = groundObject.GetComponent<Tilemap>();
            Tilemap pathTilemap = pathObject.GetComponent<Tilemap>();
            if (groundTilemap == null || pathTilemap == null)
            {
                EditorUtility.DisplayDialog(
                    "Tilemap Missing",
                    "Tilemap components are missing.",
                    "OK");
                return;
            }

            TileBase groundTile = GetOrCreateTileAsset("sol-exterieur", "Ground_Auto");
            TileBase pathTile = GetOrCreateTileAsset("sol-path", "Path_Auto");
            if (groundTile == null || pathTile == null)
            {
                EditorUtility.DisplayDialog(
                    "Tiles Not Found",
                    "Could not create/read tile assets from Assets/ground/sol-exterieur(.ase/.png) and sol-path(.ase/.png).",
                    "OK");
                return;
            }

            Undo.RecordObject(groundTilemap, "Populate Ground Tilemap");
            Undo.RecordObject(pathTilemap, "Populate Path Tilemap");

            groundTilemap.ClearAllTiles();
            pathTilemap.ClearAllTiles();

            // Small starter area so user always sees something in scene/game.
            for (int x = -8; x <= 8; x++)
            {
                for (int y = -5; y <= 5; y++)
                {
                    groundTilemap.SetTile(new Vector3Int(x, y, 0), groundTile);
                }
            }

            for (int x = -7; x <= 7; x++)
            {
                pathTilemap.SetTile(new Vector3Int(x, 0, 0), pathTile);
            }

            groundTilemap.RefreshAllTiles();
            pathTilemap.RefreshAllTiles();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Selection.activeObject = pathObject;
            GridPaintingState.scenePaintTarget = pathObject;
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");

            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "Starter Tiles Ready",
                    "Ground and path starter tiles were painted. You can now keep painting in Tile Palette.",
                    "OK");
            }
        }

        private static void SetPaintTarget(string tilemapName)
        {
            GameObject target = GameObject.Find(tilemapName);
            if (target == null)
            {
                EditorUtility.DisplayDialog(
                    "Tilemap Missing",
                    $"Could not find '{tilemapName}'. Run Tools > Fortress > Setup Isometric Tilemap Scene first.",
                    "OK");
                return;
            }

            Selection.activeObject = target;
            GridPaintingState.scenePaintTarget = target;
            EditorApplication.ExecuteMenuItem("Window/2D/Tile Palette");
            if (SceneView.lastActiveSceneView != null)
            {
                SceneView.lastActiveSceneView.in2DMode = true;
                SceneView.lastActiveSceneView.Repaint();
            }
            SceneView.lastActiveSceneView?.Focus();
        }

        private static void PreparePainting(string tilemapName)
        {
            SetupSceneInternal(false);

            TileBase groundTile = GetOrCreateTileAsset("sol-exterieur", "Ground_Auto");
            TileBase pathTile = GetOrCreateTileAsset("sol-path", "Path_Auto");
            if (groundTile == null || pathTile == null)
            {
                EditorUtility.DisplayDialog(
                    "Tiles Not Found",
                    "Could not create/read tile assets from Assets/ground/sol-exterieur(.ase/.png) and sol-path(.ase/.png).",
                    "OK");
                return;
            }

            RenamePaletteIfNeeded();
            SetPaintTarget(tilemapName);

            // Keep the tilemap as active selection so Scene clicks paint reliably.
            EditorGUIUtility.PingObject(groundTile);
            EditorGUIUtility.PingObject(pathTile);
            SetPaintTarget(tilemapName);

            EditorUtility.DisplayDialog(
                "Paint Ready",
                "Scene and paint target are ready. In Tile Palette, click one tile, keep Brush tool active, then paint in Scene view.",
                "OK");
        }

        private static void RenamePaletteIfNeeded()
        {
            const string oldPath = "Assets/Tiles/FortressIso .prefab";
            const string newPath = "Assets/Tiles/FortressIso.prefab";
            Object oldPalette = AssetDatabase.LoadAssetAtPath<Object>(oldPath);
            Object newPalette = AssetDatabase.LoadAssetAtPath<Object>(newPath);
            if (oldPalette == null || newPalette != null)
            {
                return;
            }

            string error = AssetDatabase.RenameAsset(oldPath, "FortressIso");
            if (string.IsNullOrEmpty(error))
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        private static TileBase GetOrCreateTileAsset(string spriteBaseName, string tileAssetName)
        {
            const string tileDir = "Assets/Tiles";
            if (!AssetDatabase.IsValidFolder(tileDir))
            {
                AssetDatabase.CreateFolder("Assets", "Tiles");
            }

            string tilePath = $"{tileDir}/{tileAssetName}.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile != null)
            {
                return tile;
            }

            Sprite sprite = LoadGroundSprite(spriteBaseName);
            if (sprite == null)
            {
                return null;
            }

            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sprite;
            tile.color = Color.white;
            tile.colliderType = Tile.ColliderType.None;

            AssetDatabase.CreateAsset(tile, tilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return tile;
        }

        private static Sprite LoadGroundSprite(string spriteBaseName)
        {
            Sprite aseSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ground/{spriteBaseName}.ase");
            if (aseSprite != null)
            {
                return aseSprite;
            }

            Sprite pngSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/ground/{spriteBaseName}.png");
            if (pngSprite != null)
            {
                return pngSprite;
            }

            return null;
        }

        private static void CleanupOldObjects()
        {
            string[] oldNames =
            {
                "TowerDefenseMegaMap",
                "Environment",
                "EnemyPath",
                "TowerSlots",
                "Landmarks",
                "Ground",
                "Path"
            };

            for (int i = 0; i < oldNames.Length; i++)
            {
                GameObject oldObject = GameObject.Find(oldNames[i]);
                if (oldObject != null)
                {
                    Undo.DestroyObjectImmediate(oldObject);
                }
            }
        }

        private static void SetupTilemap(Transform parent, string tilemapName, int sortingOrder)
        {
            Transform existing = parent.Find(tilemapName);
            GameObject tilemapObject;

            if (existing == null)
            {
                tilemapObject = new GameObject(tilemapName);
                Undo.RegisterCreatedObjectUndo(tilemapObject, "Create Tilemap");
                tilemapObject.transform.SetParent(parent, false);
            }
            else
            {
                tilemapObject = existing.gameObject;
            }

            if (tilemapObject.GetComponent<Tilemap>() == null)
            {
                Undo.AddComponent<Tilemap>(tilemapObject);
            }

            TilemapRenderer renderer = tilemapObject.GetComponent<TilemapRenderer>();
            if (renderer == null)
            {
                renderer = Undo.AddComponent<TilemapRenderer>(tilemapObject);
            }

            renderer.sortOrder = TilemapRenderer.SortOrder.TopLeft;
            renderer.sortingOrder = sortingOrder;
            renderer.mode = TilemapRenderer.Mode.Individual;
        }

        private static void SetupMainCamera()
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new GameObject("Main Camera");
                Undo.RegisterCreatedObjectUndo(cameraObject, "Create Main Camera");
                camera = Undo.AddComponent<Camera>(cameraObject);
                camera.tag = "MainCamera";
            }

            camera.orthographic = true;
            camera.orthographicSize = 25f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 1000f;
            camera.transform.position = new Vector3(0f, 60f, -60f);
            camera.transform.rotation = Quaternion.Euler(35.264f, 45f, 0f);
        }
    }
}
