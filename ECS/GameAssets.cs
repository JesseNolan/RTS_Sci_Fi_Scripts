using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class GameAssets : MonoBehaviour
{
    [System.Serializable]
    public class BigObject
    {
        public Mesh[] mesh;
        public Material[] material;
        public ShadowCastingMode CastShadows;
    }

    public BigObject[] BigObjects;

}
