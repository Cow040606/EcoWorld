using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class vFootstepContainer : MonoBehaviour
{
    static vFootstepContainer instance;

    public static Transform root
    {
        get
        {
            if (!instance)
            {
                instance = new GameObject("Footstep Container", typeof(vFootstepContainer)).GetComponent<vFootstepContainer>();
            }
            return instance.transform;
        }
    }
}
