#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEditor.Experimental.SceneManagement;

public static class CreateUISetup
{
    [MenuItem("Tools/Setup/Create UI (Menu + HUD)")]
    public static void CreateUI()
    {
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();

        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            var gmGO = new GameObject("GameManager");
            Undo.RegisterCreatedObjectUndo(gmGO, "Create GameManager");
            gm = gmGO.AddComponent<GameManager>();

            // Intento de auto-asignar player si existe con tag "Player"
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player) SetSerializedField(gm, "player", player.transform);
        }

        // Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
        }

        // EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.EventSystems.StandaloneInputModule));
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // Built-in font
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // === HUD Texts ===
        Text scoreText = EnsureText(canvas.transform, "ScoreText", new Vector2(10, -10), TextAnchor.UpperLeft, font, "SCORE: 0");
        Text levelText = EnsureText(canvas.transform, "LevelText", new Vector2(0, -10), TextAnchor.UpperCenter, font, "LEVEL: 1");
        Text coinsText = EnsureText(canvas.transform, "CoinText", new Vector2(-10, -10), TextAnchor.UpperRight, font, "COINS: 0/10");

        // Assign to GameManager
        SetSerializedField(gm, "scoreText", scoreText);
        SetSerializedField(gm, "levelText", levelText);
        SetSerializedField(gm, "coinsText", coinsText);

        // === Panels ===
        var menuPanel = EnsurePanel(canvas.transform, "MenuPanel", new Color(0, 0, 0, 0.6f), true);
        var levelCompletePanel = EnsurePanel(canvas.transform, "LevelCompletePanel", new Color(0, 0, 0, 0.6f), false);
        var gameCompletedPanel = EnsurePanel(canvas.transform, "GameCompletedPanel", new Color(0, 0, 0, 0.6f), false);
        var gameOverPanel = EnsurePanel(canvas.transform, "GameOverPanel", new Color(0, 0, 0, 0.6f), false);

        // MenuPanel contents
        AddCenteredLabel(menuPanel.transform, font, "PAUSED", 48);
        var playBtn = AddCenteredButton(menuPanel.transform, font, "PLAY");
        UnityEventTools.AddPersistentListener(playBtn.onClick, gm.StartGame);

        // LevelCompletePanel
        AddCenteredLabel(levelCompletePanel.transform, font, "LEVEL COMPLETED!", 48);
        var contBtn = AddCenteredButton(levelCompletePanel.transform, font, "CONTINUE");
        UnityEventTools.AddPersistentListener(contBtn.onClick, gm.NextLevel);

        // GameCompletedPanel
        AddCenteredLabel(gameCompletedPanel.transform, font, "GAME COMPLETED!", 48);
        var restartBtn1 = AddCenteredButton(gameCompletedPanel.transform, font, "RESTART");
        UnityEventTools.AddPersistentListener(restartBtn1.onClick, gm.RestartGame);

        // GameOverPanel
        AddCenteredLabel(gameOverPanel.transform, font, "GAME OVER", 48);
        var restartBtn2 = AddCenteredButton(gameOverPanel.transform, font, "TRY AGAIN");
        UnityEventTools.AddPersistentListener(restartBtn2.onClick, gm.RestartGame);

        // Wire panels to GameManager
        SetSerializedField(gm, "menuPanel", menuPanel);
        SetSerializedField(gm, "levelCompletePanel", levelCompletePanel);
        SetSerializedField(gm, "gameCompletedPanel", gameCompletedPanel);
        SetSerializedField(gm, "gameOverPanel", gameOverPanel);

        // Mark scene dirty so changes are saved
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        // Optional: save prefab if folder exists
        string prefabPath = "Assets/Prefabs/UI/Canvas_Game.prefab";
        string dir = System.IO.Path.GetDirectoryName(prefabPath);
        if (System.IO.Directory.Exists(dir))
        {
            var canvasGO = canvas.gameObject;
            PrefabUtility.SaveAsPrefabAsset(canvasGO, prefabPath);
            Debug.Log($"[CreateUISetup] Prefab guardado en {prefabPath}");
        }

        Undo.CollapseUndoOperations(group);
        Debug.Log("[CreateUISetup] Canvas + HUD + Menús creados y conectados.");
    }

    // ===== Helpers =====

    static Text EnsureText(Transform parent, string name, Vector2 margin, TextAnchor anchor, Font font, string initial)
    {
        var tGO = parent.Find(name)?.gameObject;
        if (tGO == null)
        {
            tGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            Undo.RegisterCreatedObjectUndo(tGO, "Create Text");
            tGO.transform.SetParent(parent, false);
        }
        var rt = tGO.GetComponent<RectTransform>();
        rt.anchorMin = AnchorMinFor(anchor);
        rt.anchorMax = AnchorMaxFor(anchor);
        rt.pivot = PivotFor(anchor);
        rt.sizeDelta = new Vector2(400, 60);
        rt.anchoredPosition = margin;

        var txt = tGO.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = 28;
        txt.alignment = anchor;
        txt.text = initial;
        txt.color = Color.white;
        txt.horizontalOverflow = HorizontalWrapMode.Overflow;
        txt.verticalOverflow = VerticalWrapMode.Overflow;
        return txt;
    }

    static GameObject EnsurePanel(Transform parent, string name, Color bg, bool active)
    {
        var go = parent.Find(name)?.gameObject;
        if (go == null)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            Undo.RegisterCreatedObjectUndo(go, "Create Panel");
            go.transform.SetParent(parent, false);
        }
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = bg;

        go.SetActive(active);
        return go;
    }

    static void AddCenteredLabel(Transform parent, Font font, string text, int size)
    {
        var label = new GameObject("Title", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        Undo.RegisterCreatedObjectUndo(label, "Create Label");
        label.transform.SetParent(parent, false);

        var rt = label.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(800, 120);
        rt.anchoredPosition = new Vector2(0, 120);

        var txt = label.GetComponent<Text>();
        txt.font = font;
        txt.fontSize = size;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = text;
        txt.color = Color.white;
    }

    static Button AddCenteredButton(Transform parent, Font font, string label)
    {
        var btnGO = new GameObject("Button", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(btnGO, "Create Button");
        btnGO.transform.SetParent(parent, false);

        var rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320, 96);
        rt.anchoredPosition = new Vector2(0, -40);

        var img = btnGO.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.9f);

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        Undo.RegisterCreatedObjectUndo(textGO, "Create Button Text");
        textGO.transform.SetParent(btnGO.transform, false);

        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

        var txt = textGO.GetComponent<Text>();
        txt.font = font; txt.fontSize = 36; txt.alignment = TextAnchor.MiddleCenter; txt.text = label; txt.color = Color.black;

        return btnGO.GetComponent<Button>();
    }

    static Vector2 AnchorMinFor(TextAnchor a)
    {
        switch (a)
        {
            case TextAnchor.UpperLeft: return new Vector2(0, 1);
            case TextAnchor.UpperCenter: return new Vector2(0.5f, 1);
            case TextAnchor.UpperRight: return new Vector2(1, 1);
            default: return new Vector2(0.5f, 0.5f);
        }
    }
    static Vector2 AnchorMaxFor(TextAnchor a) => AnchorMinFor(a);
    static Vector2 PivotFor(TextAnchor a)
    {
        switch (a)
        {
            case TextAnchor.UpperLeft: return new Vector2(0, 1);
            case TextAnchor.UpperCenter: return new Vector2(0.5f, 1);
            case TextAnchor.UpperRight: return new Vector2(1, 1);
            default: return new Vector2(0.5f, 0.5f);
        }
    }

    static void SetSerializedField(Object target, string fieldName, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }
        else
        {
            Debug.LogWarning($"[CreateUISetup] No se encontró el campo '{fieldName}' en {target}.");
        }
    }
}
#endif
