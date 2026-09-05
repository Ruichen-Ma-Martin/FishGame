using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 一键把血条 UI 写进敌人预制体：新建 World Space Canvas + 背景/填充 Image + 血量数字，
// 再挂上 EnemyHealthBar 并连好引用。
// 放在 Assets/Editor 目录下，Unity 会编进编辑器专用程序集，不会打进游戏包
public static class EnemyHealthBarBuilder
{
    private const string BarRootName = "HealthBar";

    // Canvas 用 100x14 的"像素"尺寸配 0.01 的缩放，换算到世界里就是 1 x 0.14 个单位。
    // 直接把 Canvas 做成 1x0.14 会让字号只能填小数，非常难调，所以用大尺寸 + 小缩放
    private const float BarPixelWidth = 100f;
    private const float BarPixelHeight = 14f;
    private const float CanvasScale = 0.01f;
    private const float HeightAboveEnemy = 0.8f;   // 血条相对敌人中心的高度（世界单位）

    // 菜单入口：Unity 顶部菜单 Tools > 生成敌人血条
    [MenuItem("Tools/生成敌人血条")]
    public static void BuildEnemyHealthBars()
    {
        int created = 0;
        int skipped = 0;

        // 扫描全工程的预制体，只处理根物体上挂了 enemy 组件的，
        // 这样以后新增别的敌人预制体也能一次性覆盖
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset == null || asset.GetComponent<enemy>() == null)
            {
                continue;
            }

            if (AddBarToPrefab(path))
            {
                created++;
                Debug.Log("已给敌人预制体加上血条：" + path);
            }
            else
            {
                skipped++;
                Debug.Log("敌人预制体已有血条，跳过：" + path);
            }
        }

        if (created == 0 && skipped == 0)
        {
            EditorUtility.DisplayDialog("生成敌人血条",
                "没有找到挂着 enemy 组件的预制体。\n\n请确认敌人预制体的根物体上有 enemy 脚本。",
                "知道了");
            return;
        }

        EditorUtility.DisplayDialog("生成敌人血条",
            "新增 " + created + " 个，跳过 " + skipped + " 个（已存在）。\n\n" +
            "血条默认受伤才显示，3 秒无伤自动隐藏。\n想常显就在预制体的 EnemyHealthBar 上勾 Always Visible。",
            "好");
    }

    // 往单个预制体里加血条。返回 false 表示已经有了、这次跳过
    private static bool AddBarToPrefab(string path)
    {
        // 编辑预制体资源的标准做法：把内容加载成一个临时物体，改完再存回去
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            // 已经加过就跳过，避免同一只敌人头上叠出两条血条
            if (root.transform.Find(BarRootName) != null)
            {
                return false;
            }

            GameObject barRoot = CreateBarCanvas(root.transform);

            // 背景先建、填充后建：UI 按层级先后绘制，后建的盖在上面
            CreateImage(barRoot.transform, "Background", new Color(0.08f, 0.08f, 0.08f, 0.75f), Image.Type.Simple);

            Image fill = CreateImage(barRoot.transform, "Fill", new Color(0.85f, 0.15f, 0.15f), Image.Type.Filled);
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = (int)Image.OriginHorizontal.Left;   // 从左往右填
            fill.fillAmount = 1f;

            TextMeshProUGUI healthText = CreateHealthText(barRoot.transform);

            WireHealthBar(root, barRoot, fill, healthText);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return true;
        }
        finally
        {
            // 不管成功失败都要卸载临时内容，否则会泄漏一个隐藏的预制体场景
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    // 血条根节点：World Space 的 Canvas，作为敌人的子物体，跟随移动由父子关系自动完成
    private static GameObject CreateBarCanvas(Transform enemyRoot)
    {
        GameObject barRoot = new GameObject(BarRootName, typeof(RectTransform));
        barRoot.transform.SetParent(enemyRoot, false);

        Canvas canvas = barRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 10;   // 盖在敌人贴图上面，否则可能被精灵挡住

        RectTransform rect = barRoot.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(BarPixelWidth, BarPixelHeight);
        rect.localScale = Vector3.one * CanvasScale;
        rect.localPosition = new Vector3(0f, HeightAboveEnemy, 0f);
        return barRoot;
    }

    // 生成一个铺满父节点的 Image。Filled 模式需要 sprite 才能算填充，
    // 所以统一用 Unity 内置的 UISprite，不依赖项目里的美术资源
    private static Image CreateImage(Transform parent, string name, Color color, Image.Type type)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = type;
        image.color = color;
        image.raycastTarget = false;   // 血条只用来看，别参与点击判定

        StretchToParent(go.GetComponent<RectTransform>());
        return image;
    }

    // 叠在血条上的血量数字，字号相对 100x14 的画布尺寸而言
    private static TextMeshProUGUI CreateHealthText(Transform barRoot)
    {
        GameObject go = new GameObject("HealthText", typeof(RectTransform));
        go.transform.SetParent(barRoot, false);
        StretchToParent(go.GetComponent<RectTransform>());

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = "0 / 0";
        text.fontSize = 10f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    // 挂上 EnemyHealthBar 并连好三个引用。
    // 私有字段只能通过 SerializedObject 赋值，这也是编辑器连线的标准做法
    private static void WireHealthBar(GameObject root, GameObject barRoot, Image fill, TextMeshProUGUI healthText)
    {
        EnemyHealthBar bar = root.GetComponent<EnemyHealthBar>();
        if (bar == null)
        {
            bar = root.AddComponent<EnemyHealthBar>();
        }

        SerializedObject so = new SerializedObject(bar);
        so.FindProperty("_barRoot").objectReferenceValue = barRoot;
        so.FindProperty("_fill").objectReferenceValue = fill;
        so.FindProperty("_healthText").objectReferenceValue = healthText;
        so.ApplyModifiedProperties();
    }

    // 让 RectTransform 四边贴住父节点，父节点变大它就跟着变大
    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
