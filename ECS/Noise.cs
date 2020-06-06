using UnityEngine;
using System.Collections;

public class Noise {

    public static float[,] GenerateNoiseMap(int columns, int rows, NoiseData data)
    {
        int seed = data._seed;
        float scale = data._scale;
        int octaves = data._octaves;
        float persistance = data._persistance;
        float lacunarity = data._lacunarity;
        Vector2 offset = data._offset;
        float heightScale = data._heightScale;
        float heightOffset = data._heightOffset;

        float[,] noiseMap = new float[columns, rows];

        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        for(int p = 0; p < octaves; p++)
        {
            float offsetCol = prng.Next(-100000, 100000) + offset.x;
            float offsetRow = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[p] = new Vector2(offsetCol, offsetRow);
        }


        if (scale <= 0)
        {
            scale = 0.0001f;
        }


        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int i = 0; i < columns; i++)
        {
            for(int j = 0; j < rows; j++)
            {

                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;

                for(int k = 0; k < octaves; k++)
                {
                    float sampColumn = i / scale * frequency + octaveOffsets[k].x;
                    float sampRow = j / scale * frequency + octaveOffsets[k].y;

                    float perlinValue = Mathf.PerlinNoise(sampColumn, sampRow)*2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }
                if(noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                } else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[i, j] = noiseHeight;

            }
        }

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < rows; j++)
            {
                float result = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[i, j]) * heightScale + heightOffset;
                float val = Mathf.Clamp(result, 0.0f, 1.0f);

                noiseMap[i, j] = val;

            }
        }

                return noiseMap;
    }



}
