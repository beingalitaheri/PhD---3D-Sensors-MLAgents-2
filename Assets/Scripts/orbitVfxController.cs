using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class orbitVfxController : MonoBehaviour
{
    public VisualEffect vfx;
    public Transform target;

    void Update()
    {
        if (target != null)
        {
            vfx.SetVector3("Position", target.position);
        }
        else
        {
            vfx.SetFloat("Intensity", 0);
        }
    }
}
