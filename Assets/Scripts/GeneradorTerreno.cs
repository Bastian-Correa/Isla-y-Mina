using UnityEngine;

public static class GeneradorTerreno
{
    // Alturas donde cambia el tipo de terreno
    public const float bandaArena = 0.03f;
    public const float nivelBosque = 0.48f;
    public const float nivelRoca = 0.68f;
    public const float nivelNieve = 0.82f;

    // Ruido Perlin con varias octavas
    public static float[,] generarAlturas(int n, float escala, int octavas, float persistencia, float lacunaridad, bool formaDeIsla)
    {
        float[,] alturas = new float[n, n];

        // Desfase al azar para que cada semilla de una isla distinta
        Vector2[] desfases = new Vector2[octavas];
        for (int o = 0; o < octavas; o++)
            desfases[o] = new Vector2(Random.Range(-10000f, 10000f), Random.Range(-10000f, 10000f));

        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                float amplitud = 1f;
                float frecuencia = 1f;
                float valor = 0f;
                for (int o = 0; o < octavas; o++)
                {
                    float px = x / (float)n * escala * frecuencia + desfases[o].x;
                    float py = y / (float)n * escala * frecuencia + desfases[o].y;
                    valor += (Mathf.PerlinNoise(px, py) * 2f - 1f) * amplitud;
                    amplitud *= persistencia;
                    frecuencia *= lacunaridad;
                }
                alturas[x, y] = valor;
            }
        }

        normalizar(alturas);
        if (formaDeIsla)
        {
            aplicarFormaDeIsla(alturas);
            normalizar(alturas);
        }
        return alturas;
    }

    // Deja las alturas entre 0 y 1
    static void normalizar(float[,] alturas)
    {
        float min = float.MaxValue, max = float.MinValue;
        foreach (float h in alturas)
        {
            if (h < min) min = h;
            if (h > max) max = h;
        }
        int n = alturas.GetLength(0);
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
                alturas[x, y] = (alturas[x, y] - min) / (max - min);
    }

    // Baja los bordes para que quede una isla
    static void aplicarFormaDeIsla(float[,] alturas)
    {
        int n = alturas.GetLength(0);
        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                float dx = x / (float)(n - 1) * 2f - 1f;
                float dy = y / (float)(n - 1) * 2f - 1f;
                float distancia = Mathf.Min(1f, Mathf.Sqrt(dx * dx + dy * dy));
                alturas[x, y] *= 1f - Mathf.Pow(distancia, 2.2f);
            }
        }
    }

    // Color según la altura
    public static Color colorPorAltura(float h, float nivelDelMar)
    {
        if (h < nivelDelMar * 0.6f) return new Color(0.10f, 0.22f, 0.45f);
        if (h < nivelDelMar) return new Color(0.20f, 0.42f, 0.68f);
        if (h < nivelDelMar + bandaArena) return new Color(0.87f, 0.80f, 0.56f);
        if (h < nivelBosque) return new Color(0.42f, 0.66f, 0.27f);
        if (h < nivelRoca) return new Color(0.24f, 0.47f, 0.20f);
        if (h < nivelNieve) return new Color(0.48f, 0.44f, 0.40f);
        return new Color(0.95f, 0.96f, 0.98f);
    }

    // Crea la malla 3D con las alturas
    public static Mesh crearMalla(float[,] alturas, float tamano, float alturaMaxima, float nivelDelMar)
    {
        int n = alturas.GetLength(0);
        float celda = tamano / (n - 1);
        Vector3[] vertices = new Vector3[n * n];
        Vector2[] uvs = new Vector2[n * n];
        int[] triangulos = new int[(n - 1) * (n - 1) * 6];

        for (int x = 0; x < n; x++)
        {
            for (int y = 0; y < n; y++)
            {
                int i = x * n + y;
                // El mar queda plano
                float h = Mathf.Max(alturas[x, y], nivelDelMar);
                vertices[i] = new Vector3(x * celda - tamano / 2f, h * alturaMaxima, y * celda - tamano / 2f);
                uvs[i] = new Vector2(x / (float)(n - 1), y / (float)(n - 1));
            }
        }

        int t = 0;
        for (int x = 0; x < n - 1; x++)
        {
            for (int y = 0; y < n - 1; y++)
            {
                int a = x * n + y;
                int b = (x + 1) * n + y;
                int c = x * n + y + 1;
                int d = (x + 1) * n + y + 1;
                triangulos[t++] = a; triangulos[t++] = c; triangulos[t++] = b;
                triangulos[t++] = b; triangulos[t++] = c; triangulos[t++] = d;
            }
        }

        Mesh malla = new Mesh();
        malla.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        malla.vertices = vertices;
        malla.uv = uvs;
        malla.triangles = triangulos;
        malla.RecalculateNormals();
        return malla;
    }

    // Pinta la textura según la altura
    public static Texture2D crearTextura(float[,] alturas, float nivelDelMar, int detalle = 4)
    {
        int n = alturas.GetLength(0);
        int lado = (n - 1) * detalle + 1;
        Texture2D textura = new Texture2D(lado, lado);
        textura.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < lado; x++)
            for (int y = 0; y < lado; y++)
                textura.SetPixel(x, y, colorPorAltura(alturaSuave(alturas, x / (float)detalle, y / (float)detalle), nivelDelMar));
        textura.Apply();
        return textura;
    }

    // Mezcla las 4 celdas vecinas para que la costa salga suave
    public static float alturaSuave(float[,] alturas, float fx, float fy)
    {
        int n = alturas.GetLength(0);
        int x = Mathf.Min((int)fx, n - 2);
        int y = Mathf.Min((int)fy, n - 2);
        float tx = fx - x;
        float ty = fy - y;
        float abajo = Mathf.Lerp(alturas[x, y], alturas[x + 1, y], tx);
        float arriba = Mathf.Lerp(alturas[x, y + 1], alturas[x + 1, y + 1], tx);
        return Mathf.Lerp(abajo, arriba, ty);
    }
}
