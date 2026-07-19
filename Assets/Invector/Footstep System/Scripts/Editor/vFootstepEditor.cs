using UnityEngine;
using UnityEditor;

namespace Invector.FootstepSystem
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(vFootstep), true)]
    public class vFootstepEditor : Editor
    {
        GUISkin skin;
        bool openWindow;
        private Texture2D m_Logo = null;

        void OnEnable()
        {
            m_Logo = (Texture2D)Resources.Load("footStepIcon", typeof(Texture2D));
            CheckColliders();
        }

        public override void OnInspectorGUI()
        {
            if (!skin) skin = Resources.Load("FootstepEditorSkin") as GUISkin;

            if (serializedObject == null) return;

            GUILayout.BeginVertical("INVECTOR FOOTSTEP SYSTEM", skin.window);
            GUILayout.Label(m_Logo, GUILayout.MaxHeight(25));

            openWindow = GUILayout.Toggle(openWindow, openWindow ? "Close" : "Open", EditorStyles.toolbarButton);
            if (openWindow)
            {
                GUILayout.BeginVertical(skin.box);

                GUILayout.Label(" FOOTSTEP SETTINGS", skin.label);

                //EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("animationType"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("triggerType"));
                var _isBiped = serializedObject.FindProperty("IsBiped");
                var isHumanoid = serializedObject.FindProperty("animationType").enumValueIndex == (int)AnimationType.Humanoid;
                if (!isHumanoid)
                {
                    EditorGUILayout.PropertyField(_isBiped);
                }
                serializedObject.FindProperty("debugTextureName").boolValue = EditorGUILayout.Toggle("Debug Texture Name", serializedObject.FindProperty("debugTextureName").boolValue);
                //EditorGUILayout.PropertyField(serializedObject.FindProperty("_volume"));

                GUILayout.EndVertical();

                if (isHumanoid)
                {
                    GUILayout.BeginVertical(skin.box);

                    GUILayout.Label(" FOOTSTEP TRIGGERS", skin.label);

                    GUILayout.BeginHorizontal("box");

                    if (CheckColliders())
                    {
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("leftFootTrigger"), new GUIContent("", null, "leftFootTrigger"));
                        EditorGUILayout.Separator();
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("rightFootTrigger"), new GUIContent("", null, "rightFootTrigger"));
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("Can't create FootStepTriggers, make sure your model is set to Humanoid or change the AnimationType to Generic if you want to use a Generic Model", MessageType.Warning);
                        CheckColliders();
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.EndVertical();
                }
                else
                {
                    if (_isBiped.boolValue)
                    {
                        GUILayout.BeginVertical(skin.box);

                        GUILayout.Label(" FOOTSTEP TRIGGERS", skin.label);

                        if (GUILayout.Button("Create New Trigger", EditorStyles.miniButton))
                        {
                            var go = new GameObject("New Footstep Trigger", typeof(vFootstepTrigger), typeof(SphereCollider));
                            go.GetComponent<SphereCollider>().radius = 0.05f;
                            go.transform.position = (target as vFootstep).transform.position;
                            go.layer = LayerMask.NameToLayer("Ignore Raycast");
                            go.transform.parent = (target as vFootstep).transform;
                        }

                        GUILayout.BeginHorizontal("box");

                        EditorGUILayout.PropertyField(serializedObject.FindProperty("leftFootTrigger"), new GUIContent("", null, "leftFootTrigger"));
                        EditorGUILayout.Separator();
                        EditorGUILayout.PropertyField(serializedObject.FindProperty("rightFootTrigger"), new GUIContent("", null, "rightFootTrigger"));

                        GUILayout.EndHorizontal();
                        GUILayout.EndVertical();
                    }
                    else
                    {
                        DrawFootStepList();
                    }
                }

                //GUILayout.Space(6);

                GUILayout.BeginVertical(skin.box);

                GUILayout.Label(" FOOTSTEP SURFACE", skin.label);

                EditorGUILayout.PropertyField(serializedObject.FindProperty("defaultSurface"));

                EditorGUILayout.PropertyField(serializedObject.FindProperty("customSurfaces"));

                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            if (GUI.changed)
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        bool CheckColliders()
        {
            if (AssetDatabase.Contains(target))
                return true;

            var transform = (serializedObject.targetObject as vFootstep).transform;
            if (transform == null) return false;
            var animator = transform.GetComponent<Animator>();
            if (animator == null) return false;
            var leftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            vFootstepTrigger leftFoot_trigger = null;
            if (leftFoot != null)
                leftFoot_trigger = leftFoot.GetComponentInChildren<vFootstepTrigger>();

            if (leftFoot_trigger == null && leftFoot != null)
            {
                var lFoot = new GameObject("leftFoot_trigger");
                var collider = lFoot.AddComponent<SphereCollider>();
                collider.radius = 0.1f;
                leftFoot_trigger = lFoot.AddComponent<vFootstepTrigger>();
                leftFoot_trigger.transform.position = new Vector3(leftFoot.position.x, transform.position.y, leftFoot.position.z);
                leftFoot_trigger.transform.rotation = transform.rotation;
                leftFoot_trigger.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                leftFoot_trigger.transform.parent = leftFoot;
                serializedObject.FindProperty("leftFootTrigger").objectReferenceValue = leftFoot_trigger;
                serializedObject.ApplyModifiedProperties();
            }
            serializedObject.FindProperty("leftFootTrigger").objectReferenceValue = leftFoot_trigger;

            if (leftFoot_trigger != null && leftFoot_trigger.GetComponent<Collider>() == null)
            {
                var collider = leftFoot_trigger.gameObject.AddComponent<SphereCollider>();
                collider.radius = 0.1f;
            }

            var rightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            vFootstepTrigger rightFoot_trigger = null;
            if (rightFoot != null)
                rightFoot_trigger = rightFoot.GetComponentInChildren<vFootstepTrigger>();

            if (rightFoot_trigger == null && rightFoot != null)
            {
                var rFoot = new GameObject("rightFoot_trigger");
                var collider = rFoot.AddComponent<SphereCollider>();
                collider.radius = 0.1f;
                rightFoot_trigger = rFoot.gameObject.AddComponent<vFootstepTrigger>();
                rightFoot_trigger.transform.position = new Vector3(rightFoot.position.x, transform.position.y, rightFoot.position.z);
                rightFoot_trigger.transform.rotation = transform.rotation;
                rightFoot_trigger.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                rightFoot_trigger.transform.parent = rightFoot;
                serializedObject.FindProperty("rightFootTrigger").objectReferenceValue = rightFoot_trigger;
                serializedObject.ApplyModifiedProperties();
            }
            serializedObject.FindProperty("rightFootTrigger").objectReferenceValue = rightFoot_trigger;
            if (rightFoot_trigger != null && rightFoot_trigger.GetComponent<Collider>() == null)
            {
                var collider = rightFoot_trigger.gameObject.AddComponent<SphereCollider>();
                collider.radius = 0.1f;
            }

            if (serializedObject.FindProperty("rightFootTrigger").objectReferenceValue != null && serializedObject.FindProperty("leftFootTrigger").objectReferenceValue != null) return true;
            return false;
        }

        void DrawFootStepList()
        {
            var footStepList = serializedObject.FindProperty("footStepTriggers");
            if (footStepList != null)
            {
                GUILayout.BeginVertical(skin.box);

                GUILayout.Label(" FOOTSTEP TRIGGERS", skin.label);

                GUILayout.BeginHorizontal();

                if (GUILayout.Button("Create New Trigger", EditorStyles.miniButton))
                {
                    footStepList.arraySize++;
                    var go = new GameObject("Trigger-" + footStepList.arraySize.ToString("00"), typeof(vFootstepTrigger), typeof(SphereCollider));
                    go.GetComponent<SphereCollider>().radius = 0.05f;
                    go.transform.position = (target as vFootstep).transform.position;
                    go.layer = LayerMask.NameToLayer("Ignore Raycast");
                    go.transform.parent = (target as vFootstep).transform;

                    footStepList.GetArrayElementAtIndex(footStepList.arraySize - 1).objectReferenceValue = go.GetComponent<vFootstepTrigger>();
                }
                GUILayout.EndHorizontal();
                GUILayout.BeginVertical();
                for (int i = 0; i < footStepList.arraySize; i++)
                {
                    if (!DrawFootStepElement(footStepList, footStepList.GetArrayElementAtIndex(i), i)) break;
                }
                GUILayout.EndVertical();
                GUILayout.EndVertical();
            }
        }

        bool DrawFootStepElement(SerializedProperty list, SerializedProperty footStepElement, int index)
        {
            GUILayout.BeginHorizontal("box");
            EditorGUILayout.PropertyField(footStepElement, new GUIContent(""));
            if (GUILayout.Button("-", EditorStyles.miniButtonMid, GUILayout.MaxWidth(12)))
            {
                if ((footStepElement.objectReferenceValue as vFootstepTrigger) != null)
                {
                    DestroyImmediate((footStepElement.objectReferenceValue as vFootstepTrigger).gameObject);
                    list.DeleteArrayElementAtIndex(index);
                }

                list.DeleteArrayElementAtIndex(index);
                GUILayout.EndHorizontal();
                return false;
            }
            GUILayout.EndHorizontal();
            return true;
        }

        void DrawSingleSurface(SerializedProperty surface, bool showListNames)
        {
            EditorGUILayout.PropertyField(surface.FindPropertyRelative("source"), false);
            EditorGUILayout.PropertyField(surface.FindPropertyRelative("name"), new GUIContent("Surface Name"), false);

            if (showListNames)
                DrawSimpleList(surface.FindPropertyRelative("TextureOrMaterialNames"), false);

            DrawSimpleList(surface.FindPropertyRelative("audioClips"), true);
        }

        void DrawTextureNames(SerializedProperty textureNames)
        {
            for (int i = 0; i < textureNames.arraySize; i++)
                EditorGUILayout.PropertyField(textureNames.GetArrayElementAtIndex(i), true);
        }

        void DrawSimpleList(SerializedProperty list, bool useDragBox)
        {
            EditorGUILayout.PropertyField(list);

            if (list.isExpanded)
            {
                if (useDragBox)
                    DrawDragBox(list);
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Add"))
                {
                    list.arraySize++;
                }
                if (GUILayout.Button("Clear"))
                {
                    list.arraySize = 0;
                }
                GUILayout.EndHorizontal();
                EditorGUILayout.Space();

                for (int i = 0; i < list.arraySize; i++)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("-"))
                    {
                        RemoveElementAtIndex(list, i);
                    }

                    if (i < list.arraySize && i >= 0)
                        EditorGUILayout.PropertyField(list.GetArrayElementAtIndex(i), new GUIContent("", null, ""));

                    GUILayout.EndHorizontal();
                }
            }
        }

        private void RemoveElementAtIndex(SerializedProperty array, int index)
        {
            if (index != array.arraySize - 1)
            {
                array.GetArrayElementAtIndex(index).objectReferenceValue = array.GetArrayElementAtIndex(array.arraySize - 1).objectReferenceValue;
            }
            array.arraySize--;
        }

        void DrawDragBox(SerializedProperty list)
        {
            GUI.skin.box.alignment = TextAnchor.MiddleCenter;
            GUI.skin.box.normal.textColor = Color.white;
            GUILayout.Box("Drag your audio clips here!", "box", GUILayout.MinHeight(50), GUILayout.ExpandWidth(true));
            var dragAreaGroup = GUILayoutUtility.GetLastRect();

            switch (Event.current.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dragAreaGroup.Contains(Event.current.mousePosition))
                        break;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (Event.current.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (var dragged in DragAndDrop.objectReferences)
                        {
                            var clip = dragged as AudioClip;
                            if (clip == null)
                                continue;
                            list.arraySize++;
                            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = clip;
                        }
                    }
                    serializedObject.ApplyModifiedProperties();
                    Event.current.Use();
                    break;
            }
        }

        public override bool UseDefaultMargins()
        {
            return false;
        }
    }
}