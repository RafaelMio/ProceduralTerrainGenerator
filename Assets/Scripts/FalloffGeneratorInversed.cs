using UnityEngine;

public static class FalloffGeneratorInversed
{
    public static float[,] GenerateFalloffMap(int size)
    {
        float[,] map = new float[size, size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float i = x / (float)size * 2 - 1;
                float j = y / (float)size * 2 - 1;

                float value = Mathf.Sqrt(i * i + j * j);

                float falloff = Mathf.Clamp01(value);
                falloff = Evaluate(falloff);      

                map[x, y] = falloff;
            }
        }

        return map;
    }
    static float Evaluate(float value)
    {
        float a = 3f;
        float b = 2.2f;
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}