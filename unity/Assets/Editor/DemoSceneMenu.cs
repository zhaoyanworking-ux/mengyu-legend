#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChibiARPG.EditorTools
{
    public static class DemoSceneMenu
    {
        const string ScenePath = "Assets/Scenes/QinglanTown.unity";

        [MenuItem("萌域传说/生成演示场景（青岚镇）", false, 1)]
        public static void GenerateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            DemoWorldBuilder.Build();
            WriteClassAndQuestAssets();

            Directory.CreateDirectory("Assets/Scenes");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };
            EditorBuildSettings.scenes = scenes;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("《萌域传说》演示场景已生成并保存到 " + ScenePath + "。按下 Play 即可从职业选择开始。");
        }

        [MenuItem("萌域传说/写入职业与任务数据资产", false, 2)]
        public static void WriteClassAndQuestAssets()
        {
            Directory.CreateDirectory("Assets/Resources/Data");
            SaveIfMissing(PlayerClassCatalog.CreateSwordsman(), "Assets/Resources/Data/Class_Swordsman.asset");
            SaveIfMissing(PlayerClassCatalog.CreateMage(), "Assets/Resources/Data/Class_Mage.asset");
            SaveIfMissing(PlayerClassCatalog.CreateArcher(), "Assets/Resources/Data/Class_Archer.asset");
            foreach (var q in QuestCatalog.All())
            {
                string path = "Assets/Resources/Data/Quest_" + q.questId + ".asset";
                if (AssetDatabase.LoadAssetAtPath<QuestDefinition>(path) == null)
                {
                    AssetDatabase.CreateAsset(Object.Instantiate(q), path);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void SaveIfMissing(PlayerClassDefinition def, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<PlayerClassDefinition>(path) != null)
                return;
            AssetDatabase.CreateAsset(def, path);
        }
    }
}
#endif
