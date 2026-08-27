#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using DialogueEditor;

namespace EcoWorld.Editor
{
    [InitializeOnLoad]
    public class FixKingDialogue
    {
        private const string PREFAB_PATH = "Assets/tantest/pre/SM_Chr_King_01.prefab";
        private const string DIALOGUE_JSON = @"{""Options"":[{""Connections"":[{""__type"":""EditableSpeechConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":2}],""EditorInfo"":{""isRoot"":false,""xPos"":-464.333466,""yPos"":302.333252},""ID"":1,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""I have arrived, Your Majesty."",""parentUIDs"":[0],""SpeechUID"":-1},{""Connections"":[{""__type"":""EditableSpeechConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":18}],""EditorInfo"":{""isRoot"":false,""xPos"":-531,""yPos"":518.333},""ID"":4,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""I will do it, Your Majesty."",""parentUIDs"":[2],""SpeechUID"":-1},{""Connections"":[{""__type"":""EditableSpeechConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":24}],""EditorInfo"":{""isRoot"":false,""xPos"":126.333221,""yPos"":112.333084},""ID"":6,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""I have reached Level 20, Your Majesty."",""parentUIDs"":[0],""SpeechUID"":-1},{""Connections"":[{""__type"":""EditableSpeechConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":25}],""EditorInfo"":{""isRoot"":false,""xPos"":80.99983,""yPos"":469.665161},""ID"":9,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""I will bring his head."",""parentUIDs"":[8],""SpeechUID"":-1},{""Connections"":[{""__type"":""EditableSpeechConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":26}],""EditorInfo"":{""isRoot"":false,""xPos"":370.333221,""yPos"":432.999023},""ID"":10,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""It's too dangerous."",""parentUIDs"":[8],""SpeechUID"":-1},{""Connections"":[{""__type"":""EditableSpeechConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":29}],""EditorInfo"":{""isRoot"":false,""xPos"":148.3331,""yPos"":-50.3349762},""ID"":12,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""The Knight Hero is defeated, Your Majesty."",""parentUIDs"":[0],""SpeechUID"":-1},{""Connections"":[{""__type"":""EditableSpeechConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":20}],""EditorInfo"":{""isRoot"":false,""xPos"":-737.3334,""yPos"":432.999756},""ID"":19,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""Maybe later"",""parentUIDs"":[2],""SpeechUID"":-1},{""Connections"":[{""__type"":""EditableSpeechConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":5}],""EditorInfo"":{""isRoot"":false,""xPos"":-141.333252,""yPos"":277.666626},""ID"":22,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""I am still working on my strength, Your Majesty."",""parentUIDs"":[0],""SpeechUID"":-1},{""Connections"":[{""__type"":""EditableSpeechConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":28}],""EditorInfo"":{""isRoot"":false,""xPos"":-256.0001,""yPos"":24.9999237},""ID"":27,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""I am hunting the traitor, Your Majesty."",""parentUIDs"":[0],""SpeechUID"":-1},{""Connections"":[],""EditorInfo"":{""isRoot"":false,""xPos"":395.999481,""yPos"":73.66661},""ID"":30,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""I will go, Your Majesty."",""parentUIDs"":[15],""SpeechUID"":-1}],""Parameters"":[{""__type"":""EditableBoolParameter:#DialogueEditor"",""ParameterName"":""find king_DangLam"",""BoolValue"":false},{""__type"":""EditableBoolParameter:#DialogueEditor"",""ParameterName"":""LV20_DangLam"",""BoolValue"":false},{""__type"":""EditableBoolParameter:#DialogueEditor"",""ParameterName"":""LV20_DaXong"",""BoolValue"":false},{""__type"":""EditableBoolParameter:#DialogueEditor"",""ParameterName"":""craft_DaXong"",""BoolValue"":false},{""__type"":""EditableBoolParameter:#DialogueEditor"",""ParameterName"":""boss2_DangLam"",""BoolValue"":false},{""__type"":""EditableBoolParameter:#DialogueEditor"",""ParameterName"":""boss2_DaXong"",""BoolValue"":false}],""SpeechNodes"":[{""Connections"":[{""__type"":""EditableOptionConnection:#DialogueEditor"",""Conditions"":[{""__type"":""EditableBoolCondition:#DialogueEditor"",""ParameterName"":""find king_DangLam"",""CheckType"":0,""RequiredValue"":true}],""NodeUID"":1},{""__type"":""EditableOptionConnection:#DialogueEditor"",""Conditions"":[{""__type"":""EditableBoolCondition:#DialogueEditor"",""ParameterName"":""LV20_DangLam"",""CheckType"":0,""RequiredValue"":true}],""NodeUID"":22},{""__type"":""EditableOptionConnection:#DialogueEditor"",""Conditions"":[{""__type"":""EditableBoolCondition:#DialogueEditor"",""ParameterName"":""LV20_DaXong"",""CheckType"":0,""RequiredValue"":true}],""NodeUID"":6},{""__type"":""EditableOptionConnection:#DialogueEditor"",""Conditions"":[{""__type"":""EditableBoolCondition:#DialogueEditor"",""ParameterName"":""boss2_DangLam"",""CheckType"":0,""RequiredValue"":true}],""NodeUID"":27},{""__type"":""EditableOptionConnection:#DialogueEditor"",""Conditions"":[{""__type"":""EditableBoolCondition:#DialogueEditor"",""ParameterName"":""boss2_DaXong"",""CheckType"":0,""RequiredValue"":true}],""NodeUID"":12}],""EditorInfo"":{""isRoot"":true,""xPos"":-295.333282,""yPos"":164.666672},""ID"":0,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""Welcome, new knight. No time to celebrate. Our kingdom is in peril, and I need you."",""parentUIDs"":[],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""KING"",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[{""__type"":""EditableOptionConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":4},{""__type"":""EditableOptionConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":19}],""EditorInfo"":{""isRoot"":false,""xPos"":-481.666718,""yPos"":389.999329},""ID"":2,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""You have arrived? You are the successor of the Hero. Now go and increase your strength, and go defeat the Knight Hero. The Knight Hero is the country's hero, but due to his greed for vanity, he betrayed us and must be defeated."",""parentUIDs"":[1],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""KING"",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[],""EditorInfo"":{""isRoot"":false,""xPos"":-156.333374,""yPos"":378.666077},""ID"":5,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""Come back when you are ready."",""parentUIDs"":[22],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""KING"",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[{""__type"":""EditableOptionConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":9},{""__type"":""EditableOptionConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":10}],""EditorInfo"":{""isRoot"":false,""xPos"":235.666718,""yPos"":309.332581},""ID"":8,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""Now, execute 'The HERO'. He is a traitor blinded by glory."",""parentUIDs"":[24],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""KING"",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[],""EditorInfo"":{""isRoot"":false,""xPos"":119.666107,""yPos"":784.6659},""ID"":13,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""Excellent work! The kingdom owes you."",""parentUIDs"":[],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""KING"",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[{""__type"":""EditableOptionConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":30}],""EditorInfo"":{""isRoot"":false,""xPos"":397.666229,""yPos"":-69.33455},""ID"":15,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""One last thing. Seek the Old Wizard to awaken your true power. Go now."",""parentUIDs"":[29],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""KING"",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[],""EditorInfo"":{""isRoot"":false,""xPos"":-606.666748,""yPos"":601.9994},""ID"":18,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""Go, and do not fail me."",""parentUIDs"":[4],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""KING"",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[],""EditorInfo"":{""isRoot"":false,""xPos"":-879.3334,""yPos"":529.999756},""ID"":20,""ParamActions"":[],""TMPFontGUID"":null,""Text"":null,""parentUIDs"":[19],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""Do not make me wait "",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[{""__type"":""EditableSpeechConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":8}],""EditorInfo"":{""isRoot"":false,""xPos"":231.999969,""yPos"":191.999908},""ID"":24,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""Impressive. Now go and defeat the Knight Hero."",""parentUIDs"":[6],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":"""",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[],""EditorInfo"":{""isRoot"":false,""xPos"":-159.3335,""yPos"":525.333},""ID"":25,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""Very good"",""parentUIDs"":[9],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""KING"",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[],""EditorInfo"":{""isRoot"":false,""xPos"":479.999939,""yPos"":311.33313},""ID"":26,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""It must be done"",""parentUIDs"":[10],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""KING "",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[],""EditorInfo"":{""isRoot"":false,""xPos"":-112.66687,""yPos"":-103.999954},""ID"":28,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""Find him and end this threat."",""parentUIDs"":[27],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""KING"",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0},{""Connections"":[{""__type"":""EditableSpeechConnection:#DialogueEditor"",""Conditions"":[],""NodeUID"":15}],""EditorInfo"":{""isRoot"":false,""xPos"":256.6664,""yPos"":-198.66658},""ID"":29,""ParamActions"":[],""TMPFontGUID"":null,""Text"":""Excellent work! The kingdom owes you."",""parentUIDs"":[12],""AdvanceDialogueAutomatically"":false,""AudioGUID"":null,""AutoAdvanceShouldDisplayOption"":false,""IconGUID"":null,""Name"":""KING"",""OptionUIDs"":null,""SpeechUID"":0,""TimeUntilAdvance"":0,""Volume"":0}]}";

        static FixKingDialogue()
        {
            // Auto run on load/compilation
            EditorApplication.delayCall += () => {
                FixNow();
            };
        }

        [MenuItem("Tools/Fix King Dialogue")]
        public static void FixNow()
        {
            try
            {
                Debug.Log("<color=cyan>[FixKingDialogue]</color> Starting dialogue fix process...");

                // 1. Load Quest Assets
                QuestSO findKing = LoadQuestAsset("0131099c81e23f641a21d869bd57e951");
                QuestSO lv20 = LoadQuestAsset("9d93510490629b94ba55bdf717f643d3");
                QuestSO boss2 = LoadQuestAsset("f700ec3eccecc5d40a966752e4324dc8");
                QuestSO talk2 = LoadQuestAsset("f31086f453cd46441b4f2776acb1831e");

                if (findKing == null || lv20 == null || boss2 == null || talk2 == null)
                {
                    Debug.LogError("<color=red>[FixKingDialogue]</color> One or more QuestSO assets could not be loaded!");
                    return;
                }

                // 2. Fix the Prefab Asset
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PATH);
                if (prefab == null)
                {
                    Debug.LogError("<color=red>[FixKingDialogue]</color> Prefab not found at path: " + PREFAB_PATH);
                    return;
                }

                NPCConversation prefabConv = prefab.GetComponentInChildren<NPCConversation>();
                if (prefabConv == null)
                {
                    Debug.LogError("<color=red>[FixKingDialogue]</color> NPCConversation component not found in prefab!");
                    return;
                }

                NPC_QuestBridge bridge = prefab.GetComponentInChildren<NPC_QuestBridge>();
                if (bridge == null)
                {
                    Debug.LogError("<color=red>[FixKingDialogue]</color> NPC_QuestBridge component not found in prefab!");
                    return;
                }

                // Update prefab values using SerializedObject to ensure persistence
                SerializedObject soConv = new SerializedObject(prefabConv);
                soConv.Update();
                soConv.FindProperty("json").stringValue = DIALOGUE_JSON;
                soConv.FindProperty("saveVersion").intValue = 110;
                soConv.ApplyModifiedProperties();

                // Fix persistent calls for prefab NodeEventHolders
                NodeEventHolder[] holders = prefab.GetComponentsInChildren<NodeEventHolder>(true);
                foreach (var holder in holders)
                {
                    SerializedObject soHolder = new SerializedObject(holder);
                    soHolder.Update();
                    
                    SerializedProperty calls = soHolder.FindProperty("Event.m_PersistentCalls.m_Calls");
                    
                    if (holder.NodeID == 1) // Reclaim find king
                    {
                        SetupPersistentCall(calls, bridge, "ThuHoiViecTuPlayer", findKing);
                    }
                    else if (holder.NodeID == 2) // Give LV20 quest ONLY
                    {
                        SetupPersistentCall(calls, bridge, "GiaoViecChoPlayer", lv20);
                    }
                    else if (holder.NodeID == 9) // Give boss2 quest ONLY
                    {
                        SetupPersistentCall(calls, bridge, "GiaoViecChoPlayer", boss2);
                    }
                    else if (holder.NodeID == 4) // Clear Option 4 events
                    {
                        calls.ClearArray();
                    }
                    else if (holder.NodeID == 12) // Reclaim boss2
                    {
                        SetupPersistentCall(calls, bridge, "ThuHoiViecTuPlayer", boss2);
                    }
                    else if (holder.NodeID == 6) // Reclaim LV20
                    {
                        SetupPersistentCall(calls, bridge, "ThuHoiViecTuPlayer", lv20);
                    }
                    else if (holder.NodeID == 30) // Give talk2 (Find The Master)
                    {
                        SetupPersistentCall(calls, bridge, "GiaoViecChoPlayer", talk2);
                    }
                    
                    soHolder.ApplyModifiedProperties();
                }

                // Save Prefab changes
                EditorUtility.SetDirty(prefab);
                AssetDatabase.SaveAssets();
                Debug.Log("<color=green>[FixKingDialogue]</color> Prefab asset updated successfully.");

                // 3. Fix active Scene instances
                NPCConversation[] sceneConvs = GameObject.FindObjectsByType<NPCConversation>(FindObjectsSortMode.None);
                int sceneFixCount = 0;
                
                foreach (var sceneConv in sceneConvs)
                {
                    // Check if it belongs to the target NPC name/prefab instance
                    if (sceneConv.gameObject.name.Contains("SM_Chr_King_01"))
                    {
                        // Revert overrides on the NPCConversation component if part of prefab instance
                        if (PrefabUtility.IsPartOfPrefabInstance(sceneConv))
                        {
                            PrefabUtility.RevertObjectOverride(sceneConv, InteractionMode.AutomatedAction);
                        }
                        
                        // Revert overrides on all NodeEventHolder components on the instance
                        NodeEventHolder[] sceneHolders = sceneConv.GetComponentsInChildren<NodeEventHolder>(true);
                        foreach (var sh in sceneHolders)
                        {
                            if (PrefabUtility.IsPartOfPrefabInstance(sh))
                            {
                                PrefabUtility.RevertObjectOverride(sh, InteractionMode.AutomatedAction);
                            }
                        }

                        // Just in case, force values on the scene instance directly too
                        SerializedObject soScene = new SerializedObject(sceneConv);
                        soScene.Update();
                        soScene.FindProperty("json").stringValue = DIALOGUE_JSON;
                        soScene.FindProperty("saveVersion").intValue = 110;
                        soScene.ApplyModifiedProperties();
                        
                        EditorUtility.SetDirty(sceneConv.gameObject);
                        sceneFixCount++;
                    }
                }

                if (sceneFixCount > 0)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                    Debug.Log($"<color=green>[FixKingDialogue]</color> Reverted overrides and updated {sceneFixCount} scene instances.");
                }
                else
                {
                    Debug.LogWarning("<color=orange>[FixKingDialogue]</color> No King instances found in the active scene. (Make sure you have map1 active!)");
                }

                Debug.Log("<color=green>[FixKingDialogue]</color> All fixes applied successfully! Please open/reopen the Dialogue Editor window now.");
            }
            catch (Exception ex)
            {
                Debug.LogError("<color=red>[FixKingDialogue]</color> Exception during dialogue fix: " + ex.ToString());
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

        private static void SetupPersistentCalls(SerializedProperty callsList, MonoBehaviour target, string methodName, QuestSO[] arguments)
        {
            callsList.ClearArray();
            for (int i = 0; i < arguments.Length; i++)
            {
                callsList.InsertArrayElementAtIndex(i);
                SerializedProperty call = callsList.GetArrayElementAtIndex(i);
                
                call.FindPropertyRelative("m_Target").objectReferenceValue = target;
                call.FindPropertyRelative("m_MethodName").stringValue = methodName;
                call.FindPropertyRelative("m_Mode").enumValueIndex = 2; // Object argument mode
                
                SerializedProperty args = call.FindPropertyRelative("m_Arguments");
                args.FindPropertyRelative("m_ObjectArgument").objectReferenceValue = arguments[i];
                args.FindPropertyRelative("m_ObjectArgumentAssemblyTypeName").stringValue = "QuestSO, Assembly-CSharp";
                
                call.FindPropertyRelative("m_CallState").enumValueIndex = 2; // EditorAndRuntime
            }
        }
    }
}
#endif
