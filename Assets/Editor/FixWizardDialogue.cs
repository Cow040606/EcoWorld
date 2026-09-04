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

        private const string DIALOGUE_JSON = "{\"Options\": [{\"Connections\": [{\"__type\": \"EditableSpeechConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 2}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 23.0000763, \"yPos\": 234.999527}, \"ID\": 1, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Youknow who I am?\", \"parentUIDs\": [0], \"SpeechUID\": -1}, {\"Connections\": [{\"__type\": \"EditableSpeechConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 4}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": -276.3338, \"yPos\": 428.3327}, \"ID\": 3, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Tellme what I must do.\", \"parentUIDs\": [2], \"SpeechUID\": -1}, {\"Connections\": [{\"__type\": \"EditableSpeechConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 6}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": -260.333679, \"yPos\": 570.9995}, \"ID\": 5, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Iwill get them.\", \"parentUIDs\": [4], \"SpeechUID\": -1}, {\"Connections\": [{\"__type\": \"EditableSpeechConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 9}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 120.332962, \"yPos\": 754.332031}, \"ID\": 8, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Ihave the 20 Ores.\", \"parentUIDs\": [0], \"SpeechUID\": -1}, {\"Connections\": [{\"__type\": \"EditableSpeechConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 18}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 743.667, \"yPos\": 487.667358}, \"ID\": 17, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"I will defeat the Orc Hammer.\", \"parentUIDs\": [16], \"SpeechUID\": -1}, {\"Connections\": [{\"__type\": \"EditableSpeechConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 21}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 404.999451, \"yPos\": 504.9984}, \"ID\": 20, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"TheOrc Hammer is dead\", \"parentUIDs\": [0], \"SpeechUID\": -1}, {\"Connections\": [{\"__type\": \"EditableSpeechConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 23}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": -190.333664, \"yPos\": 115.666473}, \"ID\": 22, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"iam still working\", \"parentUIDs\": [0], \"SpeechUID\": -1}, {\"Connections\": [{\"__type\": \"EditableSpeechConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 27}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 963.6664, \"yPos\": 438.333069}, \"ID\": 26, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Thatbeast is too strong for me.\", \"parentUIDs\": [16], \"SpeechUID\": -1}, {\"Connections\": [{\"__type\": \"EditableSpeechConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 29}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 286.9994, \"yPos\": 124.333221}, \"ID\": 28, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"imstill hunting the beast\", \"parentUIDs\": [0], \"SpeechUID\": -1}], \"Parameters\": [{\"__type\": \"EditableBoolParameter:#DialogueEditor\", \"ParameterName\": \"talk2_DangLam\", \"BoolValue\": false}, {\"__type\": \"EditableBoolParameter:#DialogueEditor\", \"ParameterName\": \"daoquang_DangLam\", \"BoolValue\": false}, {\"__type\": \"EditableBoolParameter:#DialogueEditor\", \"ParameterName\": \"daoquang_DaXong\", \"BoolValue\": false}, {\"__type\": \"EditableBoolParameter:#DialogueEditor\", \"ParameterName\": \"bossfinal_DangLam\", \"BoolValue\": false}, {\"__type\": \"EditableBoolParameter:#DialogueEditor\", \"ParameterName\": \"bossfinal_DaXong\", \"BoolValue\": false}, {\"__type\": \"EditableBoolParameter:#DialogueEditor\", \"ParameterName\": \"Daoquangchuanhan\", \"BoolValue\": false}], \"SpeechNodes\": [{\"Connections\": [{\"__type\": \"EditableOptionConnection:#DialogueEditor\", \"Conditions\": [{\"__type\": \"EditableBoolCondition:#DialogueEditor\", \"ParameterName\": \"Daoquangchuanhan\", \"CheckType\": 0, \"RequiredValue\": true}], \"NodeUID\": 1}, {\"__type\": \"EditableOptionConnection:#DialogueEditor\", \"Conditions\": [{\"__type\": \"EditableBoolCondition:#DialogueEditor\", \"ParameterName\": \"daoquang_DangLam\", \"CheckType\": 0, \"RequiredValue\": true}], \"NodeUID\": 22}, {\"__type\": \"EditableOptionConnection:#DialogueEditor\", \"Conditions\": [{\"__type\": \"EditableBoolCondition:#DialogueEditor\", \"ParameterName\": \"daoquang_DaXong\", \"CheckType\": 0, \"RequiredValue\": true}], \"NodeUID\": 8}, {\"__type\": \"EditableOptionConnection:#DialogueEditor\", \"Conditions\": [{\"__type\": \"EditableBoolCondition:#DialogueEditor\", \"ParameterName\": \"bossfinal_DaXong\", \"CheckType\": 0, \"RequiredValue\": true}], \"NodeUID\": 20}, {\"__type\": \"EditableOptionConnection:#DialogueEditor\", \"Conditions\": [{\"__type\": \"EditableBoolCondition:#DialogueEditor\", \"ParameterName\": \"bossfinal_DangLam\", \"CheckType\": 0, \"RequiredValue\": true}], \"NodeUID\": 28}], \"EditorInfo\": {\"isRoot\": true, \"xPos\": -567.9999, \"yPos\": 242.666626}, \"ID\": 0, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Ihave been waiting for you... The winds whispered of your arrival\", \"parentUIDs\": [], \"AdvanceDialogueAutomatically\": false, \"AudioGUID\": null, \"AutoAdvanceShouldDisplayOption\": false, \"IconGUID\": null, \"Name\": \"THEMASTER \", \"OptionUIDs\": null, \"SpeechUID\": 0, \"TimeUntilAdvance\": 0, \"Volume\": 0}, {\"Connections\": [{\"__type\": \"EditableOptionConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 3}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 13.6663361, \"yPos\": 365.9995}, \"ID\": 2, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Youare the successor. The one destined to bear the ultimate power. But power mustbe earned, not given.\", \"parentUIDs\": [1], \"AdvanceDialogueAutomatically\": false, \"AudioGUID\": null, \"AutoAdvanceShouldDisplayOption\": false, \"IconGUID\": null, \"Name\": \"THEMASTER \", \"OptionUIDs\": null, \"SpeechUID\": 0, \"TimeUntilAdvance\": 0, \"Volume\": 0}, {\"Connections\": [{\"__type\": \"EditableOptionConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 5}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 45.6667023, \"yPos\": 499.9994}, \"ID\": 4, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"First,we temper your physical vessel. Bring me 20 Ores from the deep earth\", \"parentUIDs\": [3], \"AdvanceDialogueAutomatically\": false, \"AudioGUID\": null, \"AutoAdvanceShouldDisplayOption\": false, \"IconGUID\": null, \"Name\": \"THEMASTER \", \"OptionUIDs\": null, \"SpeechUID\": 0, \"TimeUntilAdvance\": 0, \"Volume\": 0}, {\"Connections\": [], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 16.3330841, \"yPos\": 614.667236}, \"ID\": 6, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Go.The earth does not yield its secrets to the weak.\", \"parentUIDs\": [5], \"AdvanceDialogueAutomatically\": false, \"AudioGUID\": null, \"AutoAdvanceShouldDisplayOption\": false, \"IconGUID\": null, \"Name\": \"THEMASTER \", \"OptionUIDs\": null, \"SpeechUID\": 0, \"TimeUntilAdvance\": 0, \"Volume\": 0}, {\"Connections\": [{\"__type\": \"EditableSpeechConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 16}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": -263.666931, \"yPos\": 841.9989}, \"ID\": 9, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"A solid foundation. Your body is ready. You are finally ready for the true test.\", \"parentUIDs\": [8], \"AdvanceDialogueAutomatically\": false, \"AudioGUID\": null, \"AutoAdvanceShouldDisplayOption\": false, \"IconGUID\": null, \"Name\": \"THEMASTER \", \"OptionUIDs\": null, \"SpeechUID\": 0, \"TimeUntilAdvance\": 0, \"Volume\": 0}, {\"Connections\": [{\"__type\": \"EditableOptionConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 17}, {\"__type\": \"EditableOptionConnection:#DialogueEditor\", \"Conditions\": [], \"NodeUID\": 26}], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 767.000732, \"yPos\": 329.333557}, \"ID\": 16, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"The final trial. Slay the world's strongest beast: The Orc Hammer. Only then will your true power awaken.\", \"parentUIDs\": [9], \"AdvanceDialogueAutomatically\": false, \"AudioGUID\": null, \"AutoAdvanceShouldDisplayOption\": false, \"IconGUID\": null, \"Name\": \"THEMASTER \", \"OptionUIDs\": null, \"SpeechUID\": 0, \"TimeUntilAdvance\": 0, \"Volume\": 0}, {\"Connections\": [], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 790.333252, \"yPos\": 602.0006}, \"ID\": 18, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Maythe stars guide your blade. Return only in victory\", \"parentUIDs\": [17], \"AdvanceDialogueAutomatically\": false, \"AudioGUID\": null, \"AutoAdvanceShouldDisplayOption\": false, \"IconGUID\": null, \"Name\": \"THEMASTER \", \"OptionUIDs\": null, \"SpeechUID\": 0, \"TimeUntilAdvance\": 0, \"Volume\": 0}, {\"Connections\": [], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 468.998779, \"yPos\": 648.66626}, \"ID\": 21, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Theprophecy is fulfilled. Arise, true Hero! The ultimate power is now yours.\", \"parentUIDs\": [20], \"AdvanceDialogueAutomatically\": false, \"AudioGUID\": null, \"AutoAdvanceShouldDisplayOption\": false, \"IconGUID\": null, \"Name\": \"THEMASTER \", \"OptionUIDs\": null, \"SpeechUID\": 0, \"TimeUntilAdvance\": 0, \"Volume\": 0}, {\"Connections\": [], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 64.3332062, \"yPos\": -5.33370972}, \"ID\": 23, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Go.The earth does not yield its secrets to the weak\", \"parentUIDs\": [22], \"AdvanceDialogueAutomatically\": false, \"AudioGUID\": null, \"AutoAdvanceShouldDisplayOption\": false, \"IconGUID\": null, \"Name\": \"THEMASTER \", \"OptionUIDs\": null, \"SpeechUID\": 0, \"TimeUntilAdvance\": 0, \"Volume\": 0}, {\"Connections\": [], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 1091.0011, \"yPos\": 560.666138}, \"ID\": 27, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Fearis the enemy of fate. You will never become the true Hero\", \"parentUIDs\": [26], \"AdvanceDialogueAutomatically\": false, \"AudioGUID\": null, \"AutoAdvanceShouldDisplayOption\": false, \"IconGUID\": null, \"Name\": \"THEMASTER \", \"OptionUIDs\": null, \"SpeechUID\": 0, \"TimeUntilAdvance\": 0, \"Volume\": 0}, {\"Connections\": [], \"EditorInfo\": {\"isRoot\": false, \"xPos\": 546.3323, \"yPos\": 92.66647}, \"ID\": 29, \"ParamActions\": [], \"TMPFontGUID\": null, \"Text\": \"Maythe stars guide your blade. Return only in victory\", \"parentUIDs\": [28], \"AdvanceDialogueAutomatically\": false, \"AudioGUID\": null, \"AutoAdvanceShouldDisplayOption\": false, \"IconGUID\": null, \"Name\": \"THEMASTER \", \"OptionUIDs\": null, \"SpeechUID\": 0, \"TimeUntilAdvance\": 0, \"Volume\": 0}]}";

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
                // 1. Load Quest Assets
                QuestSO daoquang = LoadQuestAsset("4451efa8b92cfc04ab7aff92a3a59bc9");
                QuestSO bossfinal = LoadQuestAsset("02b9e432cd5119d44a604af5dbcc1fc0");
                QuestSO returnKing = LoadQuestAsset("d070433017888474ba4b740e11e96d70"); // NhiemVu_Moi 1: find King

                if (daoquang == null || bossfinal == null)
                {
                    return;
                }

                // 2. Fix the Prefab Asset
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                if (prefab == null)
                {
                    return;
                }

                NPC_DialogueTrigger trigger = prefab.GetComponentInChildren<NPC_DialogueTrigger>();
                if (trigger == null)
                {
                    return;
                }

                NPC_QuestBridge bridge = prefab.GetComponentInChildren<NPC_QuestBridge>();
                if (bridge == null)
                {
                    return;
                }

                NPCConversation prefabConv = prefab.GetComponentInChildren<NPCConversation>();
                if (prefabConv != null)
                {
                    SerializedObject soConv = new SerializedObject(prefabConv);
                    soConv.Update();
                    soConv.FindProperty("json").stringValue = DIALOGUE_JSON;
                    soConv.FindProperty("saveVersion").intValue = 110;
                    soConv.ApplyModifiedProperties();
                }

                // Update NPC ID
                SerializedObject soTrigger = new SerializedObject(trigger);
                soTrigger.Update();
                soTrigger.FindProperty("npcID").intValue = 11;
                soTrigger.ApplyModifiedProperties();

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
                    else if (holder.NodeID == 11) // Removed craft2 (clear call)
                    {
                        calls.ClearArray();
                    }
                    else if (holder.NodeID == 13) // Removed craft2 (clear call)
                    {
                        calls.ClearArray();
                    }
                    else if (holder.NodeID == 17) // Give bossfinal (Orc Hammer)
                    {
                        SetupPersistentCall(calls, bridge, "GiaoViecChoPlayer", bossfinal);
                    }
                    else if (holder.NodeID == 20) // Reclaim bossfinal AND give return to King quest
                    {
                        SetupPersistentCall(calls, bridge, "ThuHoiViecTuPlayer", bossfinal);
                        if (returnKing != null)
                        {
                            AddPersistentCall(calls, bridge, "GiaoViecChoPlayer", returnKing);
                        }
                    }
                    
                    soHolder.ApplyModifiedProperties();
                }

                // Save Prefab changes
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();

                // 3. Fix active Scene instances
                NPC_DialogueTrigger[] sceneTriggers = GameObject.FindObjectsByType<NPC_DialogueTrigger>(FindObjectsSortMode.None);
                int sceneFixCount = 0;
                
                foreach (var sceneTrig in sceneTriggers)
                {
                    if (sceneTrig.gameObject.name.Contains("Character_Male_Wizard_01"))
                    {
                        // Revert overrides on components if part of prefab instance
                        if (PrefabUtility.IsPartOfPrefabInstance(sceneTrig))
                        {
                            PrefabUtility.RevertObjectOverride(sceneTrig, InteractionMode.AutomatedAction);
                        }
                        
                        NPC_QuestBridge sceneBridge = sceneTrig.GetComponent<NPC_QuestBridge>();
                        if (sceneBridge != null && PrefabUtility.IsPartOfPrefabInstance(sceneBridge))
                        {
                            PrefabUtility.RevertObjectOverride(sceneBridge, InteractionMode.AutomatedAction);
                        }
                        
                        NPCConversation sceneConv = sceneTrig.GetComponent<NPCConversation>();
                        if (sceneConv != null)
                        {
                            if (PrefabUtility.IsPartOfPrefabInstance(sceneConv))
                            {
                                PrefabUtility.RevertObjectOverride(sceneConv, InteractionMode.AutomatedAction);
                            }
                            SerializedObject soSceneConv = new SerializedObject(sceneConv);
                            soSceneConv.Update();
                            soSceneConv.FindProperty("json").stringValue = DIALOGUE_JSON;
                            soSceneConv.FindProperty("saveVersion").intValue = 110;
                            soSceneConv.ApplyModifiedProperties();
                        }
                        
                        NodeEventHolder[] sceneHolders = sceneTrig.GetComponentsInChildren<NodeEventHolder>(true);
                        foreach (var sh in sceneHolders)
                        {
                            if (PrefabUtility.IsPartOfPrefabInstance(sh))
                            {
                                PrefabUtility.RevertObjectOverride(sh, InteractionMode.AutomatedAction);
                            }
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
                }
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

                private static void AddPersistentCall(SerializedProperty callsList, MonoBehaviour target, string methodName, QuestSO argument)
        {
            int index = callsList.arraySize;
            callsList.InsertArrayElementAtIndex(index);
            SerializedProperty call = callsList.GetArrayElementAtIndex(index);
            
            call.FindPropertyRelative("m_Target").objectReferenceValue = target;
            call.FindPropertyRelative("m_MethodName").stringValue = methodName;
            call.FindPropertyRelative("m_Mode").enumValueIndex = 2; // Object argument mode
            
            SerializedProperty args = call.FindPropertyRelative("m_Arguments");
            args.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = argument;
            args.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = "QuestSO, Assembly-CSharp";
            
            call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // EditorAndRuntime
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
