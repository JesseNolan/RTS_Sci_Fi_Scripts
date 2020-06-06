using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NoiseData")]
public class NoiseData : ScriptableObject {

    public int _seed;
    public int _octaves;
    public float _persistance;
    public float _lacunarity;
    public float _scale;
    public Vector2 _offset;
    public float _heightScale;
    public float _heightOffset;


    public NoiseData(int seed, int octaves, float persistance, float lacunarity, float scale, Vector2 offset, float heightScale, float heightOffset)
    {
        _seed = seed;
        _octaves = octaves;
        _persistance = persistance;
        _lacunarity = lacunarity;
        _scale = scale;
        _offset = offset;
        _heightOffset = heightOffset;
        _heightScale = heightScale;
    }

}
