using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

[InitializeOnLoad]
public class SugiHandiSetupTools : EditorWindow
{
    private const string SETUP_VERSION_KEY = "SugiHandi_Setup_Completed_v1";

    static SugiHandiSetupTools()
    {
        EditorApplication.delayCall += () =>
        {
            if (!EditorPrefs.GetBool(SETUP_VERSION_KEY, false))
            {
                RunFullSetup();
                EditorPrefs.SetBool(SETUP_VERSION_KEY, true);
            }
        };
    }

    [MenuItem("SugiHandi Tools/Run Full Setup (PPU, Sorting, Folders & Prefabs)")]
    public static void RunFullSetup()
    {
        Debug.Log("<color=#4CAF50><b>[SugiHandi Setup]</b> Starting Complete Project Setup...</color>");

        ConfigureGraphicsTransparencySort();
        ConfigurePixelArtImportSettings();
        ConfigureCharacterSlices();
        EnsureScriptableObjects();
        GenerateAllPrefabs();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("<color=#4CAF50><b>[SugiHandi Setup]</b> All Prefabs and Asset Import Settings Successfully Applied!</color>");
    }

    [MenuItem("SugiHandi Tools/Configure Top-Down Graphics Sorting")]
    public static void ConfigureGraphicsTransparencySort()
    {
        // Custom Axis X:0, Y:1, Z:0 for Stardew-style top-down Y sorting
        GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
        GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);
        Debug.Log("[SugiHandi Setup] GraphicsSettings TransparencySortMode set to CustomAxis (0, 1, 0).");
    }

    [MenuItem("SugiHandi Tools/Apply 16 PPU & Point Filter to Tileset & Sprites")]
    public static void ConfigurePixelArtImportSettings()
    {
        string assetPackDir = "Assets/3rd-party Assets/Pixel Art Top Down - Basic v1.2.3";
        if (!Directory.Exists(assetPackDir))
        {
            Debug.LogWarning("[SugiHandi Setup] Asset pack folder not found at: " + assetPackDir);
            return;
        }

        string[] textures = Directory.GetFiles(assetPackDir, "*.png", SearchOption.AllDirectories);
        foreach (string texPath in textures)
        {
            string unityPath = texPath.Replace("\\", "/");
            if (unityPath.EndsWith("Scene Overview.png")) continue;

            TextureImporter importer = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spritePixelsPerUnit != 16)
            {
                importer.spritePixelsPerUnit = 16;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            TextureImporterPlatformSettings defaultSettings = importer.GetDefaultPlatformTextureSettings();
            if (defaultSettings.textureCompression != TextureImporterCompression.Uncompressed)
            {
                defaultSettings.textureCompression = TextureImporterCompression.Uncompressed;
                defaultSettings.format = TextureImporterFormat.RGBA32;
                importer.SetPlatformTextureSettings(defaultSettings);
                changed = true;
            }

            foreach (string platform in new string[] { "Standalone", "Android", "WebGL" })
            {
                TextureImporterPlatformSettings ps = importer.GetPlatformTextureSettings(platform);
                if (ps.overridden || ps.textureCompression != TextureImporterCompression.Uncompressed)
                {
                    ps.textureCompression = TextureImporterCompression.Uncompressed;
                    ps.format = TextureImporterFormat.RGBA32;
                    importer.SetPlatformTextureSettings(ps);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();
            }
        }

        Debug.Log("[SugiHandi Setup] Configured 16 PPU, Point Filter, and Uncompressed settings for all tiles and sprites.");
    }

    [MenuItem("SugiHandi Tools/Slice Character Sprites (16x32 with Bottom Pivot)")]
    public static void ConfigureCharacterSlices()
    {
        // 1. TX Player.png in 3rd-party folder
        string txPlayerPath = "Assets/3rd-party Assets/Pixel Art Top Down - Basic v1.2.3/Characters/TX Player.png";
        SliceTextureGrid16x32(txPlayerPath);

        // 2. Player_16x32.png in SugiHandi Asset Pack
        string player16x32Path = "Assets/Assets/2D SugiHandi Asset Pack v1.0/Characters/Player Sprite/Player_16x32.png";
        UpdatePlayerPivotsToBottom(player16x32Path);
    }

    private static void SliceTextureGrid16x32(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.isReadable = true;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;

        int cellWidth = 16;
        int cellHeight = 32;
        int cols = tex.width / cellWidth;
        int rows = tex.height / cellHeight;

        List<SpriteMetaData> metaList = new List<SpriteMetaData>();
        int index = 0;

        for (int r = rows - 1; r >= 0; r--)
        {
            for (int c = 0; c < cols; c++)
            {
                SpriteMetaData smd = new SpriteMetaData
                {
                    name = Path.GetFileNameWithoutExtension(path) + "_" + index,
                    rect = new Rect(c * cellWidth, r * cellHeight, cellWidth, cellHeight),
                    alignment = (int)SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0.0f) // Anchor at feet for Stardew-style top-down Y sorting
                };
                metaList.Add(smd);
                index++;
            }
        }

        importer.spritesheet = metaList.ToArray();
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        Debug.Log($"[SugiHandi Setup] Sliced {path} into {metaList.Count} sprites (16x32, Bottom Pivot).");
    }

    private static void UpdatePlayerPivotsToBottom(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        SpriteMetaData[] sheet = importer.spritesheet;
        if (sheet != null && sheet.Length > 0)
        {
            for (int i = 0; i < sheet.Length; i++)
            {
                sheet[i].alignment = (int)SpriteAlignment.BottomCenter;
                sheet[i].pivot = new Vector2(0.5f, 0.0f); // Anchor at feet
            }
            importer.spritesheet = sheet;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            Debug.Log($"[SugiHandi Setup] Updated pivots of {sheet.Length} sprites in {path} to Bottom.");
        }
    }

    [MenuItem("SugiHandi Tools/Generate Missing ScriptableObjects")]
    public static void EnsureScriptableObjects()
    {
        string dataPath = "Assets/Resources/Data";
        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
        }

        // Clay Jar Artifact
        string clayJarPath = $"{dataPath}/ArtifactData_ClayJar.asset";
        ArtifactData clayJar = AssetDatabase.LoadAssetAtPath<ArtifactData>(clayJarPath);
        if (clayJar == null)
        {
            clayJar = ScriptableObject.CreateInstance<ArtifactData>();
            clayJar.artifactId = "ClayJar";
            clayJar.artifactName = "Karaang Duyan sa Yuta (Clay Jar)";
            clayJar.butuanonInscription = "Kini ang sudlanan sa mga handumanan sa suba.";
            clayJar.historicalNote = "Pre-colonial Butuan trade earthenware jar recovered near the Agusan River delta.";
            clayJar.kodeksWordKey = "Duyan";
            AssetDatabase.CreateAsset(clayJar, clayJarPath);
        }

        // Gold Ornament Artifact
        string goldPath = $"{dataPath}/ArtifactData_GoldOrnament.asset";
        ArtifactData gold = AssetDatabase.LoadAssetAtPath<ArtifactData>(goldPath);
        if (gold == null)
        {
            gold = ScriptableObject.CreateInstance<ArtifactData>();
            gold.artifactId = "GoldOrnament";
            gold.artifactName = "Bulawang Hiyas (Gold Ornament)";
            gold.butuanonInscription = "Bulawan sa kabukiran, gabay sa mga anito.";
            gold.historicalNote = "Intricately hammered gold ornament reflecting the advanced metallurgy of pre-colonial Butuan.";
            gold.kodeksWordKey = "Bulawan";
            AssetDatabase.CreateAsset(gold, goldPath);
        }

        // Fisherman Quest
        string questPath = $"{dataPath}/QuestData_Fisherman.asset";
        QuestData fishermanQuest = AssetDatabase.LoadAssetAtPath<QuestData>(questPath);
        if (fishermanQuest == null)
        {
            fishermanQuest = ScriptableObject.CreateInstance<QuestData>();
            fishermanQuest.questId = "fisherman_lost_jar";
            fishermanQuest.title = "Lost River Trade Jar";
            fishermanQuest.questGiverNpcId = "Fisherman";
            fishermanQuest.synopsis = "Help the local fisherman locate the ancient clay trade jar near the riverbanks.";
            fishermanQuest.rewardNote = "Received fisherman's blessing and new entry in the Kodeks.";

            QuestStep step1 = new QuestStep
            {
                stepId = "step_find_jar",
                objective = "Find the missing clay trade jar by the docks.",
                completionTriggerNote = "Examine the Clay Jar artifact.",
                dialogueOnCompletion = "You found it! Salamat, Manaog."
            };
            fishermanQuest.steps.Add(step1);
            AssetDatabase.CreateAsset(fishermanQuest, questPath);
        }

        AssetDatabase.SaveAssets();
    }

    [MenuItem("SugiHandi Tools/Generate All Core Prefabs")]
    public static void GenerateAllPrefabs()
    {
        string prefabsDir = "Assets/Prefabs";
        if (!Directory.Exists(prefabsDir))
        {
            Directory.CreateDirectory(prefabsDir);
        }

        EnsureScriptableObjects();

        CreatePlayerPrefab($"{prefabsDir}/Player.prefab");
        CreateNPCPrefabs(prefabsDir);
        CreateArtifactPrefabs(prefabsDir);
        CreatePuzzlePrefabs(prefabsDir);
        CreateManagersPrefab($"{prefabsDir}/_CoreManagers.prefab");
        CreateGridPrefab($"{prefabsDir}/EnvironmentGrid.prefab");
        CreateUIPrefabs(prefabsDir);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=#4CAF50><b>[SugiHandi Setup]</b> Successfully Generated All Prefabs in Assets/Prefabs/!</color>");
    }

    private static Sprite GetDefaultCharacterSprite()
    {
        string[] candidatePaths = new string[]
        {
            "Assets/Assets/2D SugiHandi Asset Pack v1.0/Characters/Player Sprite/Player_16x32.png",
            "Assets/3rd-party Assets/Pixel Art Top Down - Basic v1.2.3/Characters/TX Player.png"
        };

        foreach (string path in candidatePaths)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object a in assets)
            {
                if (a is Sprite s) return s;
            }
        }
        return null;
    }

    private static Sprite GetDefaultWorldSprite()
    {
        string path = "Assets/3rd-party Assets/Pixel Art Top Down - Basic v1.2.3/Props/TX Props.png";
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object a in assets)
        {
            if (a is Sprite s) return s;
        }
        return null;
    }

    private static void CreatePlayerPrefab(string path)
    {
        GameObject go = new GameObject("Player");
        go.tag = "Player";

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultCharacterSprite();
        sr.sortingLayerName = "Player";
        sr.spriteSortPoint = SpriteSortPoint.Pivot; // Pivot sorting for Stardew effect

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D col = go.AddComponent<CapsuleCollider2D>();
        col.direction = CapsuleDirection2D.Horizontal;
        col.size = new Vector2(0.8f, 0.4f);
        col.offset = new Vector2(0f, 0.2f); // Feet level

        Animator anim = go.AddComponent<Animator>();
        string animControllerPath = "Assets/Animations/Player.controller";
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(animControllerPath);
        if (controller != null) anim.runtimeAnimatorController = controller;

        PlayerMovement pm = go.AddComponent<PlayerMovement>();
        pm.moveSpeed = 5.0f;
        pm.arrivalThreshold = 0.08f;
        pm.animator = anim;

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        Debug.Log("[SugiHandi Setup] Created Player Prefab: " + path);
    }

    private static void CreateNPCPrefabs(string prefabsDir)
    {
        var npcs = new (string id, string name, string trigger)[]
        {
            ("LolaTising", "Lola Tising", "Elder_Greeting"),
            ("Fisherman", "Fisherman", "Fisherman_Quest"),
            ("Merchant", "Merchant", "Merchant_Trade"),
            ("AnitoSpirit", "Anito Spirit", "")
        };

        foreach (var (id, npcName, trigger) in npcs)
        {
            string prefabPath = $"{prefabsDir}/NPC_{id}.prefab";
            GameObject go = new GameObject($"NPC_{id}");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultCharacterSprite();
            sr.sortingLayerName = "Player";
            sr.spriteSortPoint = SpriteSortPoint.Pivot;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.5f;

            NPCInteractable npc = go.AddComponent<NPCInteractable>();
            npc.npcId = id;
            npc.npcName = npcName;
            npc.interactionRadius = 1.5f;

            if (!string.IsNullOrEmpty(trigger))
            {
                string diagPath = $"Assets/Resources/Data/Dialogue_{trigger}.asset";
                npc.dialogueData = AssetDatabase.LoadAssetAtPath<DialogueData>(diagPath);
            }

            if (id == "Fisherman")
            {
                string qPath = "Assets/Resources/Data/QuestData_Fisherman.asset";
                npc.questToOffer = AssetDatabase.LoadAssetAtPath<QuestData>(qPath);
            }

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            DestroyImmediate(go);
            Debug.Log("[SugiHandi Setup] Created NPC Prefab: " + prefabPath);
        }
    }

    private static void CreateArtifactPrefabs(string prefabsDir)
    {
        var artifacts = new (string id, string dataName)[]
        {
            ("ClayJar", "ArtifactData_ClayJar"),
            ("GoldOrnament", "ArtifactData_GoldOrnament"),
            ("WayMarker", "")
        };

        foreach (var (id, dataName) in artifacts)
        {
            string prefabPath = $"{prefabsDir}/Artifact_{id}.prefab";
            GameObject go = new GameObject($"Artifact_{id}");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultWorldSprite();
            sr.sortingLayerName = "Ground-Obstacle";
            sr.spriteSortPoint = SpriteSortPoint.Pivot;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 1.2f;

            ArtifactInteractable artifact = go.AddComponent<ArtifactInteractable>();
            artifact.interactionRadius = 1.2f;

            if (!string.IsNullOrEmpty(dataName))
            {
                string dataPath = $"Assets/Resources/Data/{dataName}.asset";
                artifact.artifactData = AssetDatabase.LoadAssetAtPath<ArtifactData>(dataPath);
            }

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            DestroyImmediate(go);
            Debug.Log("[SugiHandi Setup] Created Artifact Prefab: " + prefabPath);
        }
    }

    private static void CreatePuzzlePrefabs(string prefabsDir)
    {
        // 1. Single Marker Prefab
        string markerPrefabPath = $"{prefabsDir}/PuzzleMarker.prefab";
        GameObject markerGo = new GameObject("PuzzleMarker");

        SpriteRenderer sr = markerGo.AddComponent<SpriteRenderer>();
        sr.sprite = GetDefaultWorldSprite();
        sr.sortingLayerName = "Ground-Obstacle";
        sr.spriteSortPoint = SpriteSortPoint.Pivot;

        CircleCollider2D col = markerGo.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.8f;

        PuzzleMarker pm = markerGo.AddComponent<PuzzleMarker>();
        pm.markerRenderer = sr;
        pm.tappedColor = new Color(0.6f, 0.9f, 0.6f, 1f);

        GameObject savedMarkerPrefab = PrefabUtility.SaveAsPrefabAsset(markerGo, markerPrefabPath);
        DestroyImmediate(markerGo);

        // 2. Composite StoneMarkerPuzzle Prefab with 8 sequential markers
        string puzzlePrefabPath = $"{prefabsDir}/StoneMarkerPuzzle.prefab";
        GameObject puzzleGo = new GameObject("StoneMarkerPuzzle");
        EnvironmentalPuzzle puzzle = puzzleGo.AddComponent<EnvironmentalPuzzle>();
        puzzle.puzzleId = "puzzle_sacred_stones";
        puzzle.markersInOrder = new List<PuzzleMarker>();

        float radius = 2.5f;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * (Mathf.PI * 2f / 8f);
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);

            GameObject childMarker = (GameObject)PrefabUtility.InstantiatePrefab(savedMarkerPrefab, puzzleGo.transform);
            childMarker.name = $"StoneMarker_{i + 1}";
            childMarker.transform.localPosition = pos;

            PuzzleMarker markerComp = childMarker.GetComponent<PuzzleMarker>();
            markerComp.markerIndex = i;
            markerComp.parentPuzzle = puzzle;
            puzzle.markersInOrder.Add(markerComp);
        }

        PrefabUtility.SaveAsPrefabAsset(puzzleGo, puzzlePrefabPath);
        DestroyImmediate(puzzleGo);
        Debug.Log("[SugiHandi Setup] Created Puzzle Prefabs: " + puzzlePrefabPath);
    }

    private static void CreateManagersPrefab(string path)
    {
        GameObject go = new GameObject("_CoreManagers");

        go.AddComponent<WorldManager>();
        go.AddComponent<DialogueHandler>();
        go.AddComponent<InteractionHandler>();
        go.AddComponent<ProgressTracker>();

        PrefabUtility.SaveAsPrefabAsset(go, path);
        DestroyImmediate(go);
        Debug.Log("[SugiHandi Setup] Created Core Managers Prefab: " + path);
    }

    private static void CreateGridPrefab(string path)
    {
        GameObject gridGo = new GameObject("Grid");
        Grid grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f); // 16 PPU with 16x16 tiles = 1x1 Cell Size

        var layers = new (string name, string sortingLayer, int order, bool hasCollider)[]
        {
            ("Ground", "Ground-Base", 0, false),
            ("Water", "Sea", 1, false),
            ("Objects", "Ground-Obstacle", 2, false),
            ("Collision", "Default", 0, true),
            ("Overhead", "Default", 10, false)
        };

        foreach (var (name, sortingLayer, order, hasCollider) in layers)
        {
            GameObject tilemapChild = new GameObject(name);
            tilemapChild.transform.SetParent(gridGo.transform, false);

            Tilemap tm = tilemapChild.AddComponent<Tilemap>();
            TilemapRenderer tmr = tilemapChild.AddComponent<TilemapRenderer>();
            tmr.sortingLayerName = sortingLayer;
            tmr.sortingOrder = order;
            tmr.mode = TilemapRenderer.Mode.Individual; // Sorts each tile individually
            tmr.sortOrder = TilemapRenderer.SortOrder.BottomLeft;

            if (hasCollider)
            {
                TilemapCollider2D tc = tilemapChild.AddComponent<TilemapCollider2D>();
                tc.usedByComposite = true;
                Rigidbody2D rb = tilemapChild.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static;
                tilemapChild.AddComponent<CompositeCollider2D>();
            }
        }

        PrefabUtility.SaveAsPrefabAsset(gridGo, path);
        DestroyImmediate(gridGo);
        Debug.Log("[SugiHandi Setup] Created Environment Grid Prefab: " + path);
    }

    private static void CreateUIPrefabs(string prefabsDir)
    {
        // 1. Option Button Sub-Prefab
        string btnPrefabPath = $"{prefabsDir}/DialogueOptionButton.prefab";
        GameObject btnGo = new GameObject("DialogueOptionButton");
        RectTransform btnRect = btnGo.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(400, 48);
        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.15f, 0.18f, 0.95f);
        Button btn = btnGo.AddComponent<Button>();

        GameObject btnTextGo = new GameObject("Text");
        btnTextGo.transform.SetParent(btnGo.transform, false);
        RectTransform btnTextRect = btnTextGo.AddComponent<RectTransform>();
        btnTextRect.anchorMin = Vector2.zero;
        btnTextRect.anchorMax = Vector2.one;
        btnTextRect.sizeDelta = Vector2.zero;
        TextMeshProUGUI btnTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.fontSize = 18;
        btnTmp.color = Color.white;
        btnTmp.text = "Dialogue Option";

        GameObject savedBtnPrefab = PrefabUtility.SaveAsPrefabAsset(btnGo, btnPrefabPath);
        DestroyImmediate(btnGo);

        // 2. Kodeks Row Sub-Prefab
        string rowPrefabPath = $"{prefabsDir}/KodeksEntryRow.prefab";
        GameObject rowGo = new GameObject("KodeksEntryRow");
        RectTransform rowRect = rowGo.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(600, 60);
        HorizontalLayoutGroup hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;

        GameObject wordGo = new GameObject("WordText");
        wordGo.transform.SetParent(rowGo.transform, false);
        TextMeshProUGUI wordTmp = wordGo.AddComponent<TextMeshProUGUI>();
        wordTmp.fontSize = 20;
        wordTmp.color = new Color(1f, 0.85f, 0.3f);
        wordTmp.text = "Word";

        GameObject contextGo = new GameObject("ContextText");
        contextGo.transform.SetParent(rowGo.transform, false);
        TextMeshProUGUI contextTmp = contextGo.AddComponent<TextMeshProUGUI>();
        contextTmp.fontSize = 16;
        contextTmp.color = Color.white;
        contextTmp.text = "Encountered context...";

        GameObject savedRowPrefab = PrefabUtility.SaveAsPrefabAsset(rowGo, rowPrefabPath);
        DestroyImmediate(rowGo);

        // 3. Full Game UI Canvas Prefab
        string canvasPrefabPath = $"{prefabsDir}/GameUICanvas.prefab";
        GameObject canvasGo = new GameObject("GameUICanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        GameInterface gi = canvasGo.AddComponent<GameInterface>();

        gi.dialogueOptionButtonPrefab = savedBtnPrefab.GetComponent<Button>();
        gi.kodeksEntryRowPrefab = savedRowPrefab;

        // Dialogue Panel
        GameObject diagPanel = CreateUIPanel(canvasGo.transform, "DialoguePanel", new Vector2(900, 260), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 160f));
        gi.dialoguePanelGroup = diagPanel.GetComponent<CanvasGroup>();

        GameObject npcNameGo = CreateUIText(diagPanel.transform, "NPCName", "NPC Name", 22, new Vector2(800, 36), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(420f, -24f));
        gi.dialogueNpcNameText = npcNameGo.GetComponent<TextMeshProUGUI>();
        gi.dialogueNpcNameText.color = new Color(1f, 0.85f, 0.3f);

        GameObject npcBodyGo = CreateUIText(diagPanel.transform, "NPCBody", "Dialogue body text goes here...", 18, new Vector2(820, 100), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f));
        gi.dialogueNpcBodyText = npcBodyGo.GetComponent<TextMeshProUGUI>();

        GameObject optionsContainer = new GameObject("OptionsContainer");
        optionsContainer.transform.SetParent(diagPanel.transform, false);
        RectTransform optRect = optionsContainer.AddComponent<RectTransform>();
        optRect.sizeDelta = new Vector2(820, 60);
        optRect.anchoredPosition = new Vector2(0f, -80f);
        HorizontalLayoutGroup optHlg = optionsContainer.AddComponent<HorizontalLayoutGroup>();
        optHlg.spacing = 16;
        optHlg.childAlignment = TextAnchor.MiddleCenter;
        gi.dialogueOptionsContainer = optionsContainer.transform;

        // Quest HUD
        GameObject questHud = CreateUIPanel(canvasGo.transform, "QuestObjectiveHUD", new Vector2(360, 80), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(200f, -60f));
        gi.questObjectiveHudGroup = questHud.GetComponent<CanvasGroup>();
        GameObject questTxt = CreateUIText(questHud.transform, "ObjectiveText", "Current Objective: Explore the village", 16, new Vector2(320, 60), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        gi.questObjectiveText = questTxt.GetComponent<TextMeshProUGUI>();

        // Quest Offer Panel
        GameObject questOffer = CreateUIPanel(canvasGo.transform, "QuestOfferPanel", new Vector2(700, 320), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        gi.questOfferPanelGroup = questOffer.GetComponent<CanvasGroup>();
        gi.questOfferPanelGroup.alpha = 0f;
        gi.questOfferPanelGroup.blocksRaycasts = false;
        GameObject questTitle = CreateUIText(questOffer.transform, "QuestTitle", "New Quest Available", 24, new Vector2(600, 40), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f));
        gi.questOfferTitleText = questTitle.GetComponent<TextMeshProUGUI>();
        GameObject questSyn = CreateUIText(questOffer.transform, "QuestSynopsis", "Synopsis...", 18, new Vector2(600, 140), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f));
        gi.questOfferSynopsisText = questSyn.GetComponent<TextMeshProUGUI>();

        // Artifact Examine Panel
        GameObject artifactExamine = CreateUIPanel(canvasGo.transform, "ArtifactExaminePanel", new Vector2(800, 500), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        gi.artifactExaminePanelGroup = artifactExamine.GetComponent<CanvasGroup>();
        gi.artifactExaminePanelGroup.alpha = 0f;
        gi.artifactExaminePanelGroup.blocksRaycasts = false;
        GameObject artTitle = CreateUIText(artifactExamine.transform, "ArtifactName", "Artifact Name", 26, new Vector2(700, 40), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f));
        gi.artifactNameText = artTitle.GetComponent<TextMeshProUGUI>();
        GameObject artInsc = CreateUIText(artifactExamine.transform, "ArtifactInscription", "Butuanon Inscription", 20, new Vector2(700, 60), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 60f));
        gi.artifactInscriptionText = artInsc.GetComponent<TextMeshProUGUI>();
        gi.artifactInscriptionText.color = new Color(1f, 0.85f, 0.3f);
        GameObject artHist = CreateUIText(artifactExamine.transform, "HistoricalNote", "Historical Note...", 16, new Vector2(700, 160), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -60f));
        gi.artifactHistoricalNoteText = artHist.GetComponent<TextMeshProUGUI>();

        // Summary Screen Panel
        GameObject summaryScreen = CreateUIPanel(canvasGo.transform, "SummaryScreen", new Vector2(1000, 700), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);
        gi.summaryScreenGroup = summaryScreen.GetComponent<CanvasGroup>();
        gi.summaryScreenGroup.alpha = 0f;
        gi.summaryScreenGroup.blocksRaycasts = false;
        GameObject sumTitle = CreateUIText(summaryScreen.transform, "SummaryTitle", "Kodeks (Cultural Codex)", 28, new Vector2(800, 50), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f));
        gi.summaryTitleText = sumTitle.GetComponent<TextMeshProUGUI>();

        GameObject kodeksContainer = new GameObject("KodeksEntriesContainer");
        kodeksContainer.transform.SetParent(summaryScreen.transform, false);
        RectTransform kcRect = kodeksContainer.AddComponent<RectTransform>();
        kcRect.sizeDelta = new Vector2(800, 450);
        kcRect.anchoredPosition = new Vector2(0f, -40f);
        VerticalLayoutGroup vlg = kodeksContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        gi.kodeksEntriesContainer = kodeksContainer.transform;

        PrefabUtility.SaveAsPrefabAsset(canvasGo, canvasPrefabPath);
        DestroyImmediate(canvasGo);
        Debug.Log("[SugiHandi Setup] Created UI Canvas Prefab: " + canvasPrefabPath);
    }

    private static GameObject CreateUIPanel(Transform parent, string name, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        go.AddComponent<CanvasGroup>();
        return go;
    }

    private static GameObject CreateUIText(Transform parent, string name, string text, float fontSize, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }
}
