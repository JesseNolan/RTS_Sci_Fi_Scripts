using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Settings : MonoBehaviour
{
    private static Settings _instance;

    public static Settings Instance { get { return _instance; } }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    public int terrainDecimate = 8;

    public float rockThreshold = 0.3f;
    public int rockDensity = 25;
    public float rockSlopeThreshold = 20f;
    public float rockTerrainHeightCutoff = 10f;
    public float rockTerrainMinHeight = 10f;


    public float buildingRotationSpeed = 150f;

    public float gridHighlightDisplayDistance = 70f;


    // Resource Related Settings
    public int Resource_Rock_Cluster_Nodes = 10; // the amount of rocks in a cluster
    public float Resource_Rock_Abundance = 0.02f;   // the number and frequency of resource items
    public float Resource_Rock_Richness = 0.2f;  // the amount of rock resource able to be extracted
    public int Resource_Iron_Cluster_Nodes = 10; // the amount of rocks in a cluster
    public float Resource_Iron_Abundance = 0.02f;   // the number and frequency of resource items
    public float Resource_Iron_Richness = 0.2f;  // the amount of rock resource able to be extracted


    // Starting Resources
    public int Starting_Money;
    public int Starting_Rock;
    public int Starting_Meat;
    public int Starting_Vegetables;
    public int Starting_Iron;
    public int Starting_Copper;
    public int Starting_Gold;
    public int Starting_Platinum;
    public int Starting_Tin;
}
