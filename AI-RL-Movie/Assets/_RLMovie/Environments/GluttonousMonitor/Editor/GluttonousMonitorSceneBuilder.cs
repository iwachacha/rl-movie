using UnityEngine;
using UnityEditor;
using RLMovie.Common;
using RLMovie.Environments.GluttonousMonitor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace RLMovie.Editor
{
    /// <summary>
    /// 暴食モニターシーンを自動構築するスクリプト。
    /// あほっぽいペンギンと巨大なモニターを配置します。
    /// </summary>
    public static class GluttonousMonitorSceneBuilder
    {
        [MenuItem("RLMovie/Create GluttonousMonitor Scene")]
        public static void CreateScene()
        {
            string scenePath = CreateSceneSilently();
            Debug.Log($"Scene created at: {scenePath}");
        }

        public static string CreateSceneSilently()
        {
            // 1. 新規シーン作成
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            string folderPath = "Assets/_RLMovie/Environments/GluttonousMonitor/Scenes";
            string scenePath = $"{folderPath}/GluttonousMonitor.unity";
            
            // 2. 基本の床と部屋の構築
            GameObject environment = new GameObject("Environment");
            
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "StationFloor";
            floor.transform.SetParent(environment.transform);
            floor.transform.localScale = new Vector3(5, 1, 5);
            
            // 床のマテリアルを少し暗くする（標準マテリアルがない場合は色だけでも）
            var floorRenderer = floor.GetComponent<Renderer>();
            floorRenderer.material.color = new Color(0.1f, 0.1f, 0.15f); // 宇宙ステーションの暗い床

            // 壁の作成（4方）
            CreateWall("Wall_Back", new Vector3(0, 5, 25), new Vector3(50, 10, 1), environment.transform);
            CreateWall("Wall_Front", new Vector3(0, 5, -25), new Vector3(50, 10, 1), environment.transform);
            CreateWall("Wall_Left", new Vector3(-25, 5, 0), new Vector3(1, 10, 50), environment.transform);
            CreateWall("Wall_Right", new Vector3(25, 5, 0), new Vector3(1, 10, 50), environment.transform);

            // 3. ライティング（雰囲気作り）
            GameObject sun = GameObject.Find("Directional Light");
            if (sun != null) sun.GetComponent<Light>().intensity = 0.2f; // 全体を暗く

            GameObject roomLight = new GameObject("RoomLight");
            roomLight.transform.position = new Vector3(0, 8, 0);
            var light = roomLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 40f;
            light.intensity = 2f;
            light.color = new Color(0.7f, 0.8f, 1f); // 少し青白い

            // 4. 巨大暴食モニター
            string monitorPrefabPath = "Assets/ThirdParty/Cosmic_Retro_Station_Props_FREE/Prefabs/CR_Monitor_Small_1.prefab";
            GameObject monitorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(monitorPrefabPath);
            if (monitorPrefab != null)
            {
                GameObject monitor = (GameObject)PrefabUtility.InstantiatePrefab(monitorPrefab);
                monitor.name = "GluttonousMonitor";
                monitor.transform.position = new Vector3(0, 0, 15); // 少し奥へ
                monitor.transform.localScale = new Vector3(15, 15, 15); // さらに巨大化
                monitor.transform.rotation = Quaternion.Euler(0, 180, 0);

                // 口（トリガー）の作成
                GameObject mouth = new GameObject("MouthTrigger");
                mouth.transform.SetParent(monitor.transform);
                mouth.transform.localPosition = new Vector3(0, 0.5f, 0.2f);
                var trigger = mouth.AddComponent<BoxCollider>();
                trigger.isTrigger = true;
                trigger.size = new Vector3(1.5f, 1.5f, 1.5f);
                
                // モニターを照らすスポットライト（口の方を向く）
                GameObject spot = new GameObject("MonitorSpotlight");
                spot.transform.position = new Vector3(0, 10, 10);
                spot.transform.LookAt(mouth.transform);
                var spotLight = spot.AddComponent<Light>();
                spotLight.type = LightType.Spot;
                spotLight.range = 20f;
                spotLight.spotAngle = 60f;
                spotLight.intensity = 5f;
                spotLight.color = Color.cyan;
            }

            // 5. あほっぽいエージェント（ペンギン）
            string penguinPrefabPath = "Assets/ThirdParty/ithappy/Animals_FREE/Prefabs/Pinguin_001.prefab";
            GameObject penguinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(penguinPrefabPath);
            if (penguinPrefab != null)
            {
                GameObject penguin = (GameObject)PrefabUtility.InstantiatePrefab(penguinPrefab);
                penguin.name = "SillyPenguinAgent";
                penguin.transform.position = new Vector3(0, 0, 0); // 地地へ
                
                // Rigidbodyの確認
                var rb = penguin.GetComponent<Rigidbody>();
                if (rb == null) rb = penguin.AddComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

                // エージェントスクリプトの追加
                var agent = penguin.AddComponent<GluttonousMonitorAgent>();
                
                var so = new SerializedObject(agent);
                so.FindProperty("enableHeatmap").boolValue = true;
                
                GameObject monitor = GameObject.Find("GluttonousMonitor");
                if (monitor != null)
                {
                    Transform mouth = monitor.transform.Find("MouthTrigger");
                    if (mouth != null) so.FindProperty("monitorMouth").objectReferenceValue = mouth;
                }
                so.FindProperty("itemLayer").intValue = (1 << 0); 
                so.ApplyModifiedProperties();
            }

            // 6. お供え物（プロップ）
            string propPrefabPath = "Assets/ThirdParty/Cosmic_Retro_Station_Props_FREE/Prefabs/CR_Tool_Hammer.prefab";
            GameObject propPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(propPrefabPath);
            if (propPrefab != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    GameObject prop = (GameObject)PrefabUtility.InstantiatePrefab(propPrefab);
                    prop.name = $"Sacrifice_{i}";
                    prop.transform.position = new Vector3(Random.Range(-10f, 10f), 1f, Random.Range(-10f, 10f));
                    if (prop.GetComponent<Rigidbody>() == null) prop.AddComponent<Rigidbody>();
                    prop.tag = "Goal";
                }
            }

            // 7. 診断マネージャ
            if (Object.FindFirstObjectByType<HeatmapManager>() == null)
            {
                GameObject heatmapObj = new GameObject("HeatmapManager");
                heatmapObj.AddComponent<HeatmapManager>();
            }

            // シーン保存
            EditorSceneManager.SaveScene(newScene, scenePath);
            return scenePath;
        }

        private static void CreateWall(string name, Vector3 pos, Vector3 scale, Transform parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = pos;
            wall.transform.localScale = scale;
            wall.transform.SetParent(parent);
            wall.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.25f);
        }
    }
}

            // シーン保存
            EditorSceneManager.SaveScene(newScene, scenePath);
            return scenePath;
        }
    }
}
