#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace Invector.FootstepSystem
{
    public partial class vMenuComponent
    {
        [MenuItem("Invector/Footstep System/Add Footstep Component")]
        static void FootStepMenu()
        {
            if (Selection.activeGameObject)
            {
                Selection.activeGameObject.AddComponent<vFootstep>();
            }
            else
            {
                Debug.Log("Please select a GameObject to add the component.");
            }
        }

        [MenuItem("Invector/Footstep System/Create New AudioSurface")]
        static void NewAudioSurface()
        {
            CreateAsset<vAudioSurface>();
        }

        public static void CreateAsset<T>() where T : ScriptableObject
        {
            var asset = ScriptableObject.CreateInstance<T>();
            ProjectWindowUtil.CreateAsset(asset, "New " + typeof(T).Name + ".asset");
        }
    }
}
#endif