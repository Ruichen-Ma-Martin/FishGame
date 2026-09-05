using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 一键生成玩家 HUD：血条 / 体力条 / SAN 条 / 血肉数字，并自动把引用连到 PlayerUI 上。
// 放在 Assets/Editor 目录下，Unity 会把它编进编辑器专用程序集，不会打进游戏包，
// 所以运行时脚本里始终没有 UnityEditor 命名空间
public static class PlayerHUDBuilder
{
    // 生成时的初始布局参数：生成完可以在 Scene 视图里随意拖，改这里只影响下一次生成
    private const float BarWidth = 260f;
    private const float BarHeight = 24f;
    private const float BarSpacing = 30f;   // 相邻两条之间的垂直间距
    private const float Margin = 20f;       // 距屏幕边缘的留白

    private const string HudRootName = "PlayerHUD";

    // 菜单入口：Unity 顶部菜单 Tools > 生成玩家 HUD
    [MenuItem("Tools/生成玩家 HUD")]
    public static void BuildHUD()
    {
        // 已经生成过就直接退出，避免叠出两套一模一样的 UI 又互相盖住。
        // 想重新生成就先手动删掉场景里的 PlayerHUD 物体
        if (FindHudRoot() != null)
        {
            EditorUtility.DisplayDialog("生成玩家 HUD",
                "场景里已经存在 " + HudRootName + " 物体。\n\n想重新生成请先把它删掉，避免叠加出两套 UI。",
                "知道了");
            return;
        }

        // 复用场景里已有的 Canvas（你的血量 / 金币文本就挂在上面），没有才新建
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            canvas = CreateCanvas();
        }

        // HUD 根节点：铺满整个 Canvas，只作为容器，本身不画任何东西
        GameObject hudRoot = new GameObject(HudRootName, typeof(RectTransform));
        hudRoot.transform.SetParent(canvas.transform, false);
        StretchToParent(hudRoot.GetComponent<RectTransform>());

        // 三条状态条从上往下排：血量（红）、体力（绿）、SAN（紫）
        Image healthFill = CreateBar(hudRoot.transform, "HealthBar", new Color(0.85f, 0.15f, 0.15f), 0);
        Image staminaFill = CreateBar(hudRoot.transform, "StaminaBar", new Color(0.2f, 0.8f, 0.3f), 1);
        Image sanFill = CreateBar(hudRoot.transform, "SanBar", new Color(0.6f, 0.3f, 0.85f), 2);

        // 血量数字叠在血条上。放在填充层之后创建，UI 的绘制顺序按层级先后，
        // 这样数字才会盖在彩色填充上面而不是被它挡住
        TextMeshProUGUI healthText = CreateBarLabel(healthFill.transform.parent, "HealthText");

        // 血肉数字放右上角，和左上角的状态条分开
        TextMeshProUGUI fleshText = CreateFleshText(hudRoot.transform);

        // 建好 UI 再连线，保证 PlayerUI 的每个槽位都有值
        PlayerUI playerUI = WirePlayerUI(healthFill, staminaFill, sanFill, healthText, fleshText);

        // 支持 Ctrl+Z 整体撤销，生成错了不用手动删一堆物体
        Undo.RegisterCreatedObjectUndo(hudRoot, "生成玩家 HUD");
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        // 生成后直接选中，方便立刻在 Inspector 里核对连线结果
        Selection.activeGameObject = playerUI.gameObject;
        Debug.Log("玩家 HUD 生成完成：血条 / 体力条 / SAN 条 / 血肉数字已创建并连线。记得 Ctrl+S 保存场景。", playerUI);
    }

    // 在场景里找已经生成过的 HUD 根节点，用来防止重复生成
    private static GameObject FindHudRoot()
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            Transform existing = canvas.transform.Find(HudRootName);
            if (existing != null)
            {
                return existing.gameObject;
            }
        }

        return null;
    }

    // 新建一个 Screen Space - Overlay 的 Canvas，并按 1920x1080 等比缩放，
    // 这样换分辨率时 HUD 不会错位
    private static Canvas CreateCanvas()
    {
        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform));
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // HUD 不需要点击，但 Canvas 缺了它某些 UI 组件会报警告，一并加上
        canvasGo.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    // 生成一条状态条：容器负责定位，里面叠两层 Image —— 深色背景打底 + 彩色填充。
    // 返回填充层，交给 PlayerUI 改它的 fillAmount
    private static Image CreateBar(Transform parent, string name, Color fillColor, int index)
    {
        GameObject barRoot = new GameObject(name, typeof(RectTransform));
        barRoot.transform.SetParent(parent, false);

        // 锚点和轴心都设到左上角，这样坐标就是"距左边多少、距顶部多少"，换分辨率不跑位
        RectTransform rt = barRoot.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(BarWidth, BarHeight);
        rt.anchoredPosition = new Vector2(Margin, -(Margin + index * BarSpacing));

        // 背景：半透明深色，空槽部分也能看出这条总共有多长
        CreateImage(barRoot.transform, "Background", new Color(0.08f, 0.08f, 0.08f, 0.75f), Image.Type.Simple);

        // 填充：必须是 Filled + Horizontal，否则 fillAmount 不起作用
        Image fill = CreateImage(barRoot.transform, "Fill", fillColor, Image.Type.Filled);
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;   // 从左往右填
        fill.fillAmount = 1f;
        return fill;
    }

    // 生成一个铺满父节点的 Image。Filled 模式需要 sprite 才能正确计算填充，
    // 所以统一用 Unity 内置的 UISprite，不依赖项目里的美术资源
    private static Image CreateImage(Transform parent, string name, Color color, Image.Type type)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = type;
        image.color = color;
        image.raycastTarget = false;   // HUD 只用来看，别挡住底下的点击

        StretchToParent(go.GetComponent<RectTransform>());
        return image;
    }

    // 在状态条上叠一层居中的数字，铺满整条，字号比右上角的血肉数字小一号
    private static TextMeshProUGUI CreateBarLabel(Transform barRoot, string name)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(barRoot, false);
        StretchToParent(go.GetComponent<RectTransform>());

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = "0 / 0";
        text.fontSize = 16f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    // 生成右上角的血肉数字
    private static TextMeshProUGUI CreateFleshText(Transform parent)
    {
        GameObject go = new GameObject("FleshText", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(220f, 48f);
        rt.anchoredPosition = new Vector2(-Margin, -Margin);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = "0";
        text.fontSize = 36f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Right;   // 右对齐，数字变长时不会往右溢出
        text.raycastTarget = false;
        return text;
    }

    // 创建（或复用）PlayerUI 并连好全部 6 个引用。
    // 私有字段只能通过 SerializedObject 赋值，这也是编辑器里连线的标准做法，
    // 好处是写进去的值会被正确标脏并存到场景文件里
    private static PlayerUI WirePlayerUI(Image healthFill, Image staminaFill, Image sanFill,
        TextMeshProUGUI healthText, TextMeshProUGUI fleshText)
    {
        PlayerUI playerUI = Object.FindFirstObjectByType<PlayerUI>();
        if (playerUI == null)
        {
            GameObject go = new GameObject("PlayerUI");
            playerUI = go.AddComponent<PlayerUI>();
            Undo.RegisterCreatedObjectUndo(go, "生成玩家 HUD");
        }

        // 数据来源从场景里自动找，找不到就报出来，免得运行后才发现条不动
        player playerData = Object.FindFirstObjectByType<player>();
        playerController controller = Object.FindFirstObjectByType<playerController>();
        if (playerData == null)
        {
            Debug.LogWarning("场景里找不到 player 组件，PlayerUI 的 _player 槽位没连上，血量 / 血肉 / SAN 不会更新。", playerUI);
        }

        if (controller == null)
        {
            Debug.LogWarning("场景里找不到 playerController 组件，PlayerUI 的 _controller 槽位没连上，体力条不会更新。", playerUI);
        }

        SerializedObject so = new SerializedObject(playerUI);
        so.FindProperty("_healthBarFill").objectReferenceValue = healthFill;
        so.FindProperty("_staminaBarFill").objectReferenceValue = staminaFill;
        so.FindProperty("_sanBarFill").objectReferenceValue = sanFill;
        so.FindProperty("_healthText").objectReferenceValue = healthText;
        so.FindProperty("_fleshText").objectReferenceValue = fleshText;
        so.FindProperty("_player").objectReferenceValue = playerData;
        so.FindProperty("_controller").objectReferenceValue = controller;
        so.ApplyModifiedProperties();

        return playerUI;
    }

    // 让 RectTransform 四边都贴住父节点，父节点变大它就跟着变大
    private static void StretchToParent(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
