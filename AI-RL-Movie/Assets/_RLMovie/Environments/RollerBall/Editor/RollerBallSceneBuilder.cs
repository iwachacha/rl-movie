using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using RLMovie.Common;
using RLMovie.Environments.RollerBall;
using Unity.MLAgents;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;

namespace RLMovie.Editor
{
    /// <summary>
    /// RollerBall シーンを自動構築するエディタメニュー。
    /// メニュー: RLMovie > Create RollerBall Scene
    /// </summary>
    public static class RollerBallSceneBuilder
    {
        [MenuItem("RLMovie/Create RollerBall Scene")]
        public static void CreateScene()
        {
            // 新規シーンを作成
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // --- 環境ルート ---
            GameObject envRoot = new GameObject("RollerBallEnvironment");

            // --- 床 ---
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(envRoot.transform);
            floor.transform.localPosition = Vector3.zero;
            floor.transform.localScale = new Vector3(1f, 1f, 1f);
            floor.AddComponent<FloorVisual>();

            // 床のマテリアル（ダークカラー）
            var floorRenderer = floor.GetComponent<Renderer>();
            var floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            floorMat.SetColor("_BaseColor", new Color(0.15f, 0.15f, 0.2f));
            floorMat.SetFloat("_Smoothness", 0.8f);
            floorMat.SetFloat("_Metallic", 0.3f);
            floorRenderer.material = floorMat;

            // --- 壁 ---
            CreateWall(envRoot.transform, "WallN", new Vector3(0, 0.5f, 5f), new Vector3(10f, 1f, 0.2f));
            CreateWall(envRoot.transform, "WallS", new Vector3(0, 0.5f, -5f), new Vector3(10f, 1f, 0.2f));
            CreateWall(envRoot.transform, "WallE", new Vector3(5f, 0.5f, 0), new Vector3(0.2f, 1f, 10f));
            CreateWall(envRoot.transform, "WallW", new Vector3(-5f, 0.5f, 0), new Vector3(0.2f, 1f, 10f));

            // --- エージェント（ボール） ---
            GameObject agent = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            agent.name = "RollerAgent";
            agent.transform.SetParent(envRoot.transform);
            agent.transform.localPosition = new Vector3(0, 0.5f, 0);

            // エージェントのマテリアル（鮮やかなブルー）
            var agentRenderer = agent.GetComponent<Renderer>();
            var agentMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            agentMat.SetColor("_BaseColor", new Color(0.2f, 0.4f, 1f));
            agentMat.SetFloat("_Smoothness", 0.9f);
            agentMat.SetFloat("_Metallic", 0.5f);
            agentMat.EnableKeyword("_EMISSION");
            agentMat.SetColor("_EmissionColor", new Color(0.1f, 0.2f, 0.5f) * 1.5f);
            agentRenderer.material = agentMat;

            // Rigidbody
            var rb = agent.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.angularDamping = 0.5f;

            // ML-Agents コンポーネント
            var rollerAgent = agent.AddComponent<RollerBallAgent>();
            var decisionRequester = agent.AddComponent<DecisionRequester>();
            decisionRequester.DecisionPeriod = 10;

            // BehaviorParameters の設定
            var behaviorParams = agent.GetComponent<BehaviorParameters>();
            if (behaviorParams != null)
            {
                behaviorParams.BehaviorName = "RollerBallAgent";
                behaviorParams.BehaviorType = BehaviorType.HeuristicOnly;

                var brainParams = behaviorParams.BrainParameters;
                brainParams.VectorObservationSize = 13;
                brainParams.ActionSpec = ActionSpec.MakeContinuous(2);
            }

            // --- ターゲット ---
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
            target.name = "Target";
            target.transform.SetParent(envRoot.transform);
            target.transform.localPosition = new Vector3(3f, 0.5f, 3f);
            target.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);

            // コライダーを削除（物理衝突不要）
            Object.DestroyImmediate(target.GetComponent<BoxCollider>());

            // ターゲットのマテリアル（グリーンのグロー）
            var targetRenderer = target.GetComponent<Renderer>();
            var targetMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            targetMat.SetColor("_BaseColor", new Color(0.2f, 1f, 0.4f));
            targetMat.SetFloat("_Smoothness", 0.9f);
            targetMat.EnableKeyword("_EMISSION");
            targetMat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.4f) * 2f);
            targetRenderer.material = targetMat;

            // TargetVisual
            target.AddComponent<TargetVisual>();

            // --- 環境マネージャー ---
            GameObject envMgrObj = new GameObject("EnvironmentManager");
            envMgrObj.transform.SetParent(envRoot.transform);
            envMgrObj.AddComponent<EnvironmentManager>();

            // --- RollerBallAgent の参照をセット ---
            SerializedObject so = new SerializedObject(rollerAgent);
            so.FindProperty("target").objectReferenceValue = target.transform;
            so.FindProperty("envManager").objectReferenceValue = envMgrObj.GetComponent<EnvironmentManager>();
            so.FindProperty("moveForce").floatValue = 1.0f;
            so.FindProperty("goalDistance").floatValue = 1.42f;
            so.FindProperty("startPosition").vector3Value = new Vector3(0, 0.5f, 0);
            so.ApplyModifiedProperties();

            // --- カメラ設定 ---
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.position = new Vector3(0, 12f, -8f);
                mainCam.transform.rotation = Quaternion.Euler(55f, 0, 0);
                mainCam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
            }

            // --- ライティング ---
            var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (var light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    light.color = new Color(1f, 0.95f, 0.9f);
                    light.intensity = 1.2f;
                    light.transform.rotation = Quaternion.Euler(50f, -30f, 0);
                }
            }

            // --- Training Visualizer ---
            GameObject vizObj = new GameObject("TrainingVisualizer");
            var viz = vizObj.AddComponent<TrainingVisualizer>();
            SerializedObject vizSo = new SerializedObject(viz);
            vizSo.FindProperty("targetAgent").objectReferenceValue = rollerAgent;
            vizSo.ApplyModifiedProperties();

            // --- Recording Helper ---
            GameObject recObj = new GameObject("RecordingHelper");
            recObj.AddComponent<RecordingHelper>();

            // --- シーンを保存 ---
            string scenePath = "Assets/_RLMovie/Environments/RollerBall/Scenes/RollerBall.unity";
            // フォルダ作成
            if (!AssetDatabase.IsValidFolder("Assets/_RLMovie/Environments/RollerBall/Scenes"))
            {
                // パスのフォルダが存在するか確認して作成
                if (!AssetDatabase.IsValidFolder("Assets/_RLMovie"))
                    AssetDatabase.CreateFolder("Assets", "_RLMovie");
                if (!AssetDatabase.IsValidFolder("Assets/_RLMovie/Environments"))
                    AssetDatabase.CreateFolder("Assets/_RLMovie", "Environments");
                if (!AssetDatabase.IsValidFolder("Assets/_RLMovie/Environments/RollerBall"))
                    AssetDatabase.CreateFolder("Assets/_RLMovie/Environments", "RollerBall");
                if (!AssetDatabase.IsValidFolder("Assets/_RLMovie/Environments/RollerBall/Scenes"))
                    AssetDatabase.CreateFolder("Assets/_RLMovie/Environments/RollerBall", "Scenes");
            }

            EditorSceneManager.SaveScene(scene, scenePath);

            // マテリアルを保存
            string matFolder = "Assets/_RLMovie/Environments/RollerBall/Materials";
            if (!AssetDatabase.IsValidFolder(matFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_RLMovie/Environments/RollerBall/Materials"))
                    AssetDatabase.CreateFolder("Assets/_RLMovie/Environments/RollerBall", "Materials");
            }
            AssetDatabase.CreateAsset(floorMat, matFolder + "/FloorMat.mat");
            AssetDatabase.CreateAsset(agentMat, matFolder + "/AgentMat.mat");
            AssetDatabase.CreateAsset(targetMat, matFolder + "/TargetMat.mat");

            // 再保存
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log("🎱 RollerBall scene created successfully at: " + scenePath);
            EditorUtility.DisplayDialog("RollerBall Scene",
                "RollerBall シーンを作成しました！\n\n" +
                "Play ボタンを押して矢印キーでエージェントを操作できます。",
                "OK");
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.localPosition = position;
            wall.transform.localScale = scale;

            var wallRenderer = wall.GetComponent<Renderer>();
            var wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            wallMat.SetColor("_BaseColor", new Color(0.3f, 0.3f, 0.35f));
            wallMat.SetFloat("_Smoothness", 0.5f);
            wallRenderer.material = wallMat;
        }
    }
}
