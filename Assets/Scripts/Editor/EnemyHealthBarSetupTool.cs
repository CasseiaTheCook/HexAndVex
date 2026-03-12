#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class EnemyHealthBarSetupTool
{
    [MenuItem("Tools/Setup Enemy Health Bars")]
    static void SetupHealthBars()
    {
        string[] prefabPaths = {
            "Assets/Prefabs/Orc.prefab",
            "Assets/Prefabs/TelegraphAoe.prefab",
            "Assets/Prefabs/Warloc.prefab",
            "Assets/Prefabs/Boss.prefab",
            "Assets/Prefabs/Totem.prefab"
        };

        int count = 0;

        foreach (string path in prefabPaths)
        {
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefabAsset == null)
            {
                Debug.LogWarning($"Prefab bulunamadı: {path}");
                continue;
            }

            // Prefab'ı düzenleme modunda aç
            string assetPath = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);

            HealthScript hs = prefabRoot.GetComponentInChildren<HealthScript>();
            if (hs == null)
            {
                Debug.LogWarning($"{path} üzerinde HealthScript bulunamadı.");
                PrefabUtility.UnloadPrefabContents(prefabRoot);
                continue;
            }

            // Eski HealthBarCanvas'ları temizle
            for (int i = hs.transform.childCount - 1; i >= 0; i--)
            {
                Transform child = hs.transform.GetChild(i);
                if (child.name == "HealthBarCanvas")
                    Object.DestroyImmediate(child.gameObject);
            }

            // Eski EnemyHealthBar component'ini temizle
            EnemyHealthBar oldBar = hs.GetComponent<EnemyHealthBar>();
            if (oldBar != null) Object.DestroyImmediate(oldBar);

            // Düşman tipine göre boyutlar
            EnemyAI ai = prefabRoot.GetComponent<EnemyAI>();
            float barW = 0.55f, barH = 0.1f, offY = 0.18f, pad = 0.012f;
            if (ai != null && ai.enemyBehavior == EnemyAI.EnemyBehavior.Boss)
            {
                barW = 0.8f; barH = 0.12f; offY = 0.6f; pad = 0.015f;
            }
            else if (ai != null && ai.enemyBehavior == EnemyAI.EnemyBehavior.Totem)
            {
                barW = 0.4f; barH = 0.07f; offY = 0.25f; pad = 0.008f;
            }

            int uiLayer = LayerMask.NameToLayer("UI");

            // === Canvas ===
            GameObject canvasGO = new GameObject("HealthBarCanvas");
            canvasGO.transform.SetParent(hs.transform, false);
            canvasGO.layer = uiLayer;

            RectTransform canvasRT = canvasGO.AddComponent<RectTransform>();
            canvasRT.localPosition = new Vector3(0f, offY, 0f);
            canvasRT.sizeDelta = new Vector2(barW, barH);
            canvasRT.localScale = Vector3.one;

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            // === BG ===
            GameObject bgGO = new GameObject("BG", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bgGO.transform.SetParent(canvasGO.transform, false);
            bgGO.layer = uiLayer;
            RectTransform bgRT = bgGO.GetComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero;
            bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero;
            bgRT.offsetMax = Vector2.zero;
            Image bgImg = bgGO.GetComponent<Image>();
            bgImg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            bgImg.raycastTarget = false;

            // === Trail (Souls-style shadow bar) ===
            GameObject trailGO = new GameObject("Trail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            trailGO.transform.SetParent(canvasGO.transform, false);
            trailGO.layer = uiLayer;
            RectTransform trailRT = trailGO.GetComponent<RectTransform>();
            trailRT.anchorMin = Vector2.zero;
            trailRT.anchorMax = Vector2.one;
            trailRT.pivot = new Vector2(0f, 0.5f);
            trailRT.offsetMin = new Vector2(pad, pad);
            trailRT.offsetMax = new Vector2(-pad, -pad);
            Image trailImg = trailGO.GetComponent<Image>();
            trailImg.color = new Color(0.6f, 0.6f, 0.6f, 0.8f);
            trailImg.raycastTarget = false;

            // === Fill ===
            GameObject fillGO = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fillGO.transform.SetParent(canvasGO.transform, false);
            fillGO.layer = uiLayer;
            RectTransform fillRT = fillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero;
            fillRT.anchorMax = Vector2.one;
            fillRT.pivot = new Vector2(0f, 0.5f);
            fillRT.offsetMin = new Vector2(pad, pad);
            fillRT.offsetMax = new Vector2(-pad, -pad);
            Image fillImg = fillGO.GetComponent<Image>();
            fillImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            fillImg.raycastTarget = false;

            // === HP Text ===
            GameObject textGO = new GameObject("HPText", typeof(RectTransform), typeof(CanvasRenderer));
            textGO.transform.SetParent(canvasGO.transform, false);
            textGO.layer = uiLayer;
            RectTransform textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;
            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "0/0";
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 0.1f;
            tmp.fontSizeMax = 10f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            tmp.fontStyle = FontStyles.Bold;
            tmp.outlineWidth = 0.3f;
            tmp.outlineColor = Color.black;

            // === EnemyHealthBar component ===
            EnemyHealthBar bar = hs.gameObject.AddComponent<EnemyHealthBar>();
            bar.barCanvas = canvas;
            bar.bgImage = bgImg;
            bar.trailImage = trailImg;
            bar.fillImage = fillImg;
            bar.hpLabel = tmp;

            // Prefab'ı kaydet ve kapat
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            PrefabUtility.UnloadPrefabContents(prefabRoot);

            count++;
            Debug.Log($"HealthBar hierarchy eklendi: {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Toplam {count} prefab'a HealthBar eklendi.");
    }
}
#endif
