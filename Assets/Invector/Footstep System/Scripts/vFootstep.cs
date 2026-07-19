using System.Collections.Generic;
using UnityEngine;

namespace Invector.FootstepSystem
{
    public class vFootstep : MonoBehaviour
    {
        public AnimationType animationType = AnimationType.Humanoid;

        [SerializeField, Range(0, 1f)] protected float _volume = 1f;

        public enum TriggerType
        {
            OnTriggerEnter,
            AnimationEvent
        }

        [Tooltip("OnTriggerEnter: Triggers the FootStep Sound when the sphere collider enter the mesh\nAnimation Event: call the method PlayFootStep on each AnimationClip")]
        public TriggerType triggerType = TriggerType.OnTriggerEnter;

        [Tooltip("Bipeds use 2 FootstepTriggers, with non Biped you can create as many as you need")]
        public bool IsBiped = true;

        public float Volume { get { return _volume; } set { _volume = value; } }

        protected int surfaceIndex = 0;
        protected Terrain terrain;
        protected TerrainCollider terrainCollider;
        protected TerrainData terrainData;
        protected Vector3 terrainPos;

        public vFootstepTrigger leftFootTrigger;
        public vFootstepTrigger rightFootTrigger;
        [HideInInspector]
        public Transform currentStep;
        [HideInInspector]
        public List<vFootstepTrigger> footStepTriggers;

        public bool debugTextureName;

        [Tooltip("This surface will play on any terrain or mesh as the primary footstep")]
        public vAudioSurface defaultSurface;
        [Tooltip("Create new CustomSurfaces for specific textures in the menu Invector > Footstep > New AudioSurface")]
#if UNITY_2020_2_OR_NEWER
        [NonReorderable]
#endif
        public List<vAudioSurface> customSurfaces;

        protected FootstepObject currentFootStep;

        protected virtual void Start()
        {
            InitFootStep();
        }

        public virtual void InitFootStep()
        {
            var colls = GetComponentsInChildren<Collider>();
            if (animationType == AnimationType.Humanoid || IsBiped)
            {
                if (IsBiped)
                {
                    footStepTriggers.Clear();
                }
                if (leftFootTrigger == null && rightFootTrigger == null)
                {
                    Debug.Log("Missing FootStep Sphere Trigger, please unfold the FootStep Component to create the triggers.");
                    return;
                }
                else
                {
                    leftFootTrigger.trigger.isTrigger = true;
                    rightFootTrigger.trigger.isTrigger = true;
                    Physics.IgnoreCollision(leftFootTrigger.trigger, rightFootTrigger.trigger);
                    for (int i = 0; i < colls.Length; i++)
                    {
                        var coll = colls[i];
                        if (coll.enabled && coll.gameObject != leftFootTrigger.gameObject)
                        {
                            Physics.IgnoreCollision(leftFootTrigger.trigger, coll);
                        }

                        if (coll.enabled && coll.gameObject != rightFootTrigger.gameObject)
                        {
                            Physics.IgnoreCollision(rightFootTrigger.trigger, coll);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < colls.Length; i++)
                {
                    var coll = colls[i];
                    for (int a = 0; a < footStepTriggers.Count; a++)
                    {
                        var trigger = footStepTriggers[a];
                        trigger.trigger.isTrigger = true;
                        if (coll.enabled && coll.gameObject != trigger.gameObject)
                        {
                            Physics.IgnoreCollision(trigger.trigger, coll);
                        }
                    }
                }
            }
        }

        protected virtual void UpdateTerrainInfo(Terrain newTerrain)
        {
            if (terrain == null || terrain != newTerrain)
            {
                terrain = newTerrain;
                if (terrain != null)
                {
                    terrainData = terrain.terrainData;
                    terrainPos = terrain.transform.position;
                    terrainCollider = terrain.GetComponent<TerrainCollider>();
                }
            }
        }

        protected virtual float[] GetTextureMix(FootstepObject footStepObj)
        {
            // returns an array containing the relative mix of textures
            // on the main terrain at this world position.

            // The number of values in the array will equal the number
            // of textures added to the terrain.

            UpdateTerrainInfo(footStepObj.terrain);

            // calculate which splat map cell the worldPos falls within (ignoring y)
            var worldPos = footStepObj.sender.position;
            int mapX = (int)(((worldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
            int mapZ = (int)(((worldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);

            // get the splat data for this cell as a 1x1xN 3d array (where N = number of textures)
            if (!terrainCollider.bounds.Contains(worldPos))
            {
                return new float[0];
            }

            float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

            // extract the 3D array data to a 1D array:
            float[] cellMix = new float[splatmapData.GetUpperBound(2) + 1];

            for (int n = 0; n < cellMix.Length; n++)
            {
                cellMix[n] = splatmapData[0, 0, n];
            }
            return cellMix;
        }

        protected virtual int GetMainTexture(FootstepObject footStepObj)
        {
            // returns the zero-based index of the most dominant texture
            // on the main terrain at this world position.
            float[] mix = GetTextureMix(footStepObj);

            if (mix == null)
            {
                return -1;
            }

            float maxMix = 0;
            int maxIndex = 0;

            // loop through each mix value and find the maximum
            for (int n = 0; n < mix.Length; n++)
            {
                if (mix[n] > maxMix)
                {
                    maxIndex = n;
                    maxMix = mix[n];
                }
            }
            return maxIndex;
        }

        protected virtual void OnDestroy()
        {
            if (leftFootTrigger != null)
            {
                Destroy(leftFootTrigger.gameObject);
            }

            if (rightFootTrigger != null)
            {
                Destroy(rightFootTrigger.gameObject);
            }

            if (footStepTriggers != null && footStepTriggers.Count > 0)
            {
                foreach (var comp in footStepTriggers)
                {
                    Destroy(comp.gameObject);
                }
            }
        }

        /// <summary>
        /// Step on Terrain
        /// </summary>
        /// <param name="footStepObject"></param>
        public void StepOnTerrain(FootstepObject footStepObject)
        {
            if (currentStep != null && currentStep == footStepObject.sender)
            {
                return;
            }

            currentStep = footStepObject.sender;
            surfaceIndex = GetMainTexture(footStepObject);

            if (surfaceIndex != -1)
            {
                var name = (terrainData != null && terrainData.terrainLayers.Length > 0) ? (terrainData.terrainLayers[surfaceIndex]).diffuseTexture.name : "";

                footStepObject.name = name;
                currentFootStep = footStepObject;

                if (triggerType == TriggerType.OnTriggerEnter)
                {
                    PlayFootStepEffect();
                }
                if (debugTextureName)
                {
                    Debug.Log(terrain.name + " " + name);
                }
            }
        }

        /// <summary>
        /// Step on Mesh
        /// </summary>
        /// <param name="footStepObject"></param>
        public void StepOnMesh(FootstepObject footStepObject)
        {
            if (currentStep != null && currentStep == footStepObject.sender)
            {
                return;
            }

            currentStep = footStepObject.sender;
            currentFootStep = footStepObject;

            if (triggerType == TriggerType.OnTriggerEnter)
            {
                PlayFootStepEffect();
            }

            if (debugTextureName)
            {
                Debug.Log(footStepObject.name);
            }
        }

        /// <summary>
        /// Play foot Step effect
        /// </summary>
        public void PlayFootStepEffect()
        {
            if (currentFootStep != null)
            {
                currentFootStep.volume = Volume;
                SpawnSurfaceEffect(currentFootStep);
            }
        }

        /// <summary>
        /// Play foot step effect from animation event
        /// </summary>
        /// <param name="evt"></param>
        public void PlayFootStep(AnimationEvent evt)
        {
            if (evt.animatorClipInfo.weight > 0.5)
            {
                if (!string.IsNullOrEmpty(evt.stringParameter))
                {
                    var trigger = footStepTriggers.Find(f => f.gameObject.name == evt.stringParameter);
                    if (trigger != null)
                    {
                        currentFootStep = trigger.footstepObj;
                        currentFootStep.sender = trigger.transform;
                    }
                }

                PlayFootStepEffect();
            }
        }

        /// <summary>
        /// Play left foot step effect from animation event
        /// </summary>
        /// <param name="evt"></param>
        public void PlayFootStepLeft(AnimationEvent evt)
        {
            if (evt.animatorClipInfo.weight > 0.5)
            {
                currentFootStep.sender = leftFootTrigger.transform;
                PlayFootStepEffect();
            }
        }

        /// <summary>
        /// Play right foot step effect from animation event
        /// </summary>
        /// <param name="evt"></param>
        public void PlayFootStepRight(AnimationEvent evt)
        {
            if (evt.animatorClipInfo.weight > 0.15)
            {
                currentFootStep.sender = rightFootTrigger.transform;
                PlayFootStepEffect();
            }
        }

        /// <summary>
        /// Play a foot step effect passing the <seealso cref="FootstepObject"/> to determine what surface is stepping
        /// </summary>
        /// <param name="footStepObject">Foot Step object with surface information</param>
        /// <param name="spawnParticle">Spawn Particle ?</param>
        /// <param name="spawnStepMark">Spwan Step Mark ?</param>
        /// <param name="volume">Audio effect volume</param>
        public virtual void SpawnSurfaceEffect(FootstepObject footStepObject)
        {
            if (footStepObject != null)
            {
                for (int i = 0; i < customSurfaces.Count; i++)
                {
                    if (customSurfaces[i] != null && ContainsTexture(footStepObject.name, customSurfaces[i]))
                    {
                        customSurfaces[i].SpawnSurfaceEffect(footStepObject);
                        return;
                    }
                }
            }

            if (defaultSurface != null)
            {
                defaultSurface.SpawnSurfaceEffect(footStepObject);
            }
        }

        /// <summary>
        /// Ccheck if AudioSurface Contains texture in TextureName List
        /// </summary>
        /// <param name="name"></param>
        /// <param name="surface"></param>
        /// <returns></returns>
        protected virtual bool ContainsTexture(string name, vAudioSurface surface)
        {
            for (int i = 0; i < surface.TextureOrMaterialNames.Count; i++)
                if (name.Contains(surface.TextureOrMaterialNames[i]))
                    return true;

            return false;
        }

    }

    public enum AnimationType
    {
        Humanoid, Generic
    }
}