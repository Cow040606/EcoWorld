#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;
using DialogueEditor;

namespace EcoWorld.Editor
{
    [InitializeOnLoad]
    public class FixWizardDialogue
    {
        private const string PREFAB_PATH = "Assets/tantest/pre/Character_Male_Wizard_01 Variant.prefab";

        static FixWizardDialogue()
        {
            // Auto run on load/compilation
            EditorApplication.delayCall += () => {
                FixNow();
            };
        }

        [MenuItem("Tools/Fix Wizard Dialogue")]
        public static void FixNow()
        {
            try
            {
                Debug.Log("<color=cyan>[FixWizardDialogue]</color> Starting dialogue fix process...");

                // 1. Load Quest Assets
                QuestSO daoquang = LoadQuestAsset("4451efa8b92cfc04ab7aff92a3a59bc9");
                QuestSO craft2 = LoadQuestAsset("dbfa2653e33b4974aa11b16391db07ea");
                QuestSO bossfinal = LoadQuestAsset("02b9e432cd5119d44a604af5dbcc1fc0");

                if (daoquang == null || craft2 == null || bossfinal == null)
                {
                    Debug.LogError("<color=red>[FixWizardDialogue]</color> One or more QuestSO assets could not be loaded!");
                    return;
                }

                // 2. Fix the Prefab Asset
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                if (prefab == null)
                {
                    Debug.LogError("<color=red>[FixWizardDialogue]</color> Prefab not found at path: " + PREFAB_PATH);
                    return;
                }

                NPC_DialogueTrigger trigger = prefab.GetComponentInChildren<NPC_DialogueTrigger>();
                if (trigger == null)
                {
                    Debug.LogError("<color=red>[FixWizardDialogue]</color> NPC_DialogueTrigger component not found in prefab!");
                    return;
                }

                NPC_QuestBridge bridge = prefab.GetComponentInChildren<NPC_QuestBridge>();
                if (bridge == null)
                {
                    Debug.LogError("<color=red>[FixWizardDialogue]</color> NPC_QuestBridge component not found in prefab!");
                    return;
                }

                // Update NPC ID
                SerializedObject soTrigger = new SerializedObject(trigger);
                soTrigger.Update();
                soTrigger.FindProperty("npcID").intValue = 11;
                soTrigger.ApplyModifiedProperties();
                Debug.Log("<color=green>[FixWizardDialogue]</color> NPC ID set to 11.");

                // Fix persistent calls for prefab NodeEventHolders
                NodeEventHolder[] holders = prefab.GetComponentsInChildren<NodeEventHolder>(true);
                foreach (var holder in holders)
                {
                    SerializedObject soHolder = new SerializedObject(holder);
                    soHolder.Update();
                    
                    SerializedProperty calls = soHolder.FindProperty("Event.m_PersistentCalls.m_Calls");
                    
                    if (holder.NodeID == 5) // Give daoquang (ores)
                    {
                        SetupPersistentCall(calls, bridge, "GiaoViecChoPlayer", daoquang);
                    }
                    else if (holder.NodeID == 8) // Reclaim daoquang
                    {
                        SetupPersistentCall(calls, bridge, "ThuHoiViecTuPlayer", daoquang);
                    }
                    else if (holder.NodeID == 11) // Give craft2 (potions)
                    {
                        SetupPersistentCall(calls, bridge, "GiaoViecChoPlayer", craft2);
                    }
                    else if (holder.NodeID == 13) // Reclaim craft2
                    {
                        SetupPersistentCall(calls, bridge, "ThuHoiViecTuPlayer", craft2);
                    }
                    else if (holder.NodeID == 17) // Give bossfinal (Orc Hammer)
                    {
                        SetupPersistentCall(calls, bridge, "GiaoViecChoPlayer", bossfinal);
                    }
                    else if (holder.NodeID == 20) // Reclaim bossfinal
                    {
                        SetupPersistentCall(calls, bridge, "ThuHoiViecTuPlayer", bossfinal);
                    }
                    
                    soHolder.ApplyModifiedProperties();
                }

                // Save Prefab changes
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[FixWizardDialogue]</color> Prefab asset updated successfully.");

                // 3. Fix active Scene instances
                NPC_DialogueTrigger[] sceneTriggers = GameObject.FindObjectsByType<NPC_DialogueTrigger>(FindObjectsSortMode.None);
                int sceneFixCount = 0;
                
                foreach (var sceneTrig in sceneTriggers)
                {
                    if (sceneTrig.gameObject.name.Contains("Character_Male_Wizard_01"))
                    {
                        // Revert overrides on both components
                        PrefabUtility.RevertObjectOverride(sceneTrig, InteractionMode.AutomatedAction);
                        
                        NPC_QuestBridge sceneBridge = sceneTrig.GetComponent<NPC_QuestBridge>();
                        if (sceneBridge != null)
                        {
                            PrefabUtility.RevertObjectOverride(sceneBridge, InteractionMode.AutomatedAction);
                        }
                        
                        NodeEventHolder[] sceneHolders = sceneTrig.GetComponentsInChildren<NodeEventHolder>(true);
                        foreach (var sh in sceneHolders)
                        {
                            PrefabUtility.RevertObjectOverride(sh, InteractionMode.AutomatedAction);
                        }

                        // Force NPC ID directly
                        SerializedObject soSceneTrig = new SerializedObject(sceneTrig);
                        soSceneTrig.Update();
                        soSceneTrig.FindProperty("npcID").intValue = 11;
                        soSceneTrig.ApplyModifiedProperties();
                        
                        EditorUtility.SetDirty(sceneTrig.gameObject);
                        sceneFixCount++;
                    }
                }

                if (sceneFixCount > 0)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    Debug.Log($"<color=green>[FixWizardDialogue]</color> Reverted overrides and updated {sceneFixCount} scene instances.");
                }
                else
                {
                    Debug.LogWarning("<color=orange>[FixWizardDialogue]</color> No Wizard instances found in the active scene.");
                }

                Debug.Log("<color=green>[FixWizardDialogue]</color> All Wizard fixes applied successfully!");
            }
            catch (Exception ex)
            {
                Debug.LogError("<color=red>[FixWizardDialogue]</color> Exception during dialogue fix: " + ex.ToString());
            }
        }

        private static QuestSO LoadQuestAsset(string guid)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;
            return AssetDatabase.LoadAssetAtPath<QuestSO>(path);
        }

        private static void SetupPersistentCall(SerializedProperty callsList, MonoBehaviour target, string methodName, QuestSO argument)
        {
            callsList.ClearArray();
            callsList.InsertArrayElementAtIndex(0);
            SerializedProperty call = callsList.GetArrayElementAtIndex(0);
            
            call.FindPropertyRelative("m_Target").objectReferenceValue = target;
            call.FindPropertyRelative("m_MethodName").stringValue = methodName;
            call.FindPropertyRelative("m_Mode").enumValueIndex = 2; // Object argument mode
            
            SerializedProperty args = call.FindPropertyRelative("m_Arguments");
            args.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = argument;
            args.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = "QuestSO, Assembly-CSharp";
            
            call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // EditorAndRuntime
        }
    }
}
#endif
