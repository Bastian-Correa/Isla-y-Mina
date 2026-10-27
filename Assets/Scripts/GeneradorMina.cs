using System.Collections.Generic;
using UnityEngine;

public static class GeneradorMina
{
    public class Sala
    {
        public int x, y, ancho, alto;
        public Vector2Int centro() { return new Vector2Int(x + ancho / 2, y + alto / 2); }
    }

    // Tramo recto de pasillo, se usa para poner los rieles
    public class Tramo
    {
        public Vector2Int desde, hasta;
    }

    // Aqui esta el nodo del árbol BSP, tiene dos hijos o una sala
    class Zona
    {
        public int x, y, ancho, alto;
        public Zona hijoA, hijoB;
        public Sala sala;
    }

    public const int anchoPasillo = 3;

    // Arma la mina y devuelve qué celdas son roca
    public static bool[,] generar(int n, int tamanoMinimo, int divisiones, Vector2Int entrada,
                                  List<Sala> salas, List<Tramo> tramos, bool[,] pasillo)
    {
        bool[,] roca = new bool[n, n];
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
                roca[x, y] = true;

        // Se deja un borde de roca y espacio abajo para la entrada
        Zona raiz = new Zona { x = 2, y = 8, ancho = n - 4, alto = n - 10 };
        dividir(raiz, tamanoMinimo, divisiones);

        crearSalas(raiz, salas);
        foreach (Sala s in salas) abrir(roca, s.x, s.y, s.ancho, s.alto);

        conectar(raiz, roca, pasillo, tramos);

        // Pasillo desde la entrada hasta la sala más cercana
        Sala cercana = salas[0];
        foreach (Sala s in salas)
            if (distancia(s.centro(), entrada) < distancia(cercana.centro(), entrada)) cercana = s;
        cavarPasillo(roca, pasillo, tramos, entrada, cercana.centro());
        abrir(roca, entrada.x - 2, entrada.y - 1, 5, 3);
        return roca;
    }

    // Corta la zona en dos por el lado más largo y repite con cada mitad
    static void dividir(Zona z, int minimo, int divisiones)
    {
        if (divisiones == 0) return;

        bool cortarAncho;
        if (z.ancho > z.alto * 1.25f) cortarAncho = true;
        else if (z.alto > z.ancho * 1.25f) cortarAncho = false;
        else cortarAncho = Random.value < 0.5f;

        int largo = cortarAncho ? z.ancho : z.alto;
        if (largo < minimo * 2) return;
        int corte = Random.Range(minimo, largo - minimo + 1);

        if (cortarAncho)
        {
            z.hijoA = new Zona { x = z.x, y = z.y, ancho = corte, alto = z.alto };
            z.hijoB = new Zona { x = z.x + corte, y = z.y, ancho = z.ancho - corte, alto = z.alto };
        }
        else
        {
            z.hijoA = new Zona { x = z.x, y = z.y, ancho = z.ancho, alto = corte };
            z.hijoB = new Zona { x = z.x, y = z.y + corte, ancho = z.ancho, alto = z.alto - corte };
        }
        dividir(z.hijoA, minimo, divisiones - 1);
        dividir(z.hijoB, minimo, divisiones - 1);
    }

    // Una sala en cada zona final, con roca alrededor
    static void crearSalas(Zona z, List<Sala> salas)
    {
        if (z.hijoA != null)
        {
            crearSalas(z.hijoA, salas);
            crearSalas(z.hijoB, salas);
            return;
        }
        int maxAncho = z.ancho - 2;
        int maxAlto = z.alto - 2;
        int ancho = Random.Range(Mathf.Max(4, (int)(maxAncho * 0.6f)), maxAncho + 1);
        int alto = Random.Range(Mathf.Max(4, (int)(maxAlto * 0.6f)), maxAlto + 1);
        z.sala = new Sala
        {
            x = z.x + 1 + Random.Range(0, maxAncho - ancho + 1),
            y = z.y + 1 + Random.Range(0, maxAlto - alto + 1),
            ancho = ancho,
            alto = alto,
        };
        salas.Add(z.sala);
    }

    // Une las salas más cercanas de cada mitad, desde abajo hacia arriba
    static void conectar(Zona z, bool[,] roca, bool[,] pasillo, List<Tramo> tramos)
    {
        if (z.hijoA == null) return;
        conectar(z.hijoA, roca, pasillo, tramos);
        conectar(z.hijoB, roca, pasillo, tramos);

        List<Sala> salasA = new List<Sala>();
        List<Sala> salasB = new List<Sala>();
        salasDe(z.hijoA, salasA);
        salasDe(z.hijoB, salasB);

        Sala mejorA = salasA[0], mejorB = salasB[0];
        foreach (Sala a in salasA)
            foreach (Sala b in salasB)
                if (distancia(a.centro(), b.centro()) < distancia(mejorA.centro(), mejorB.centro()))
                {
                    mejorA = a;
                    mejorB = b;
                }
        cavarPasillo(roca, pasillo, tramos, mejorA.centro(), mejorB.centro());
    }

    static void salasDe(Zona z, List<Sala> lista)
    {
        if (z.sala != null) lista.Add(z.sala);
        else
        {
            salasDe(z.hijoA, lista);
            salasDe(z.hijoB, lista);
        }
    }

    // Pasillo en forma de L
    static void cavarPasillo(bool[,] roca, bool[,] pasillo, List<Tramo> tramos, Vector2Int desde, Vector2Int hasta)
    {
        Vector2Int esquina;
        if (Random.value < 0.5f) esquina = new Vector2Int(hasta.x, desde.y);
        else esquina = new Vector2Int(desde.x, hasta.y);
        cavarTramo(roca, pasillo, tramos, desde, esquina);
        cavarTramo(roca, pasillo, tramos, esquina, hasta);
    }

    static void cavarTramo(bool[,] roca, bool[,] pasillo, List<Tramo> tramos, Vector2Int desde, Vector2Int hasta)
    {
        if (desde == hasta) return;
        tramos.Add(new Tramo { desde = desde, hasta = hasta });

        int dx = hasta.x > desde.x ? 1 : (hasta.x < desde.x ? -1 : 0);
        int dy = hasta.y > desde.y ? 1 : (hasta.y < desde.y ? -1 : 0);
        int r = anchoPasillo / 2;
        for (int i = 0; i <= distancia(desde, hasta); i++)
        {
            int cx = desde.x + dx * i;
            int cy = desde.y + dy * i;
            for (int ox = -r; ox <= r; ox++)
            {
                for (int oy = -r; oy <= r; oy++)
                {
                    roca[cx + ox, cy + oy] = false;
                    pasillo[cx + ox, cy + oy] = true;
                }
            }
        }
    }

    static void abrir(bool[,] roca, int x, int y, int ancho, int alto)
    {
        for (int i = x; i < x + ancho; i++)
            for (int j = y; j < y + alto; j++)
                roca[i, j] = false;
    }

    public static int distancia(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    // Se encarga de calcular la distancia en pasos desde la entrada a cada celda, ignorando la roca
    public static int[,] medirDistancias(bool[,] roca, Vector2Int entrada)
    {
        int n = roca.GetLength(0);
        int[,] pasos = new int[n, n];
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
                pasos[x, y] = -1;

        Vector2Int[] direcciones = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        Queue<Vector2Int> cola = new Queue<Vector2Int>();
        pasos[entrada.x, entrada.y] = 0;
        cola.Enqueue(entrada);
        while (cola.Count > 0)
        {
            Vector2Int actual = cola.Dequeue();
            foreach (Vector2Int d in direcciones)
            {
                Vector2Int v = actual + d;
                if (!roca[v.x, v.y] && pasos[v.x, v.y] == -1)
                {
                    pasos[v.x, v.y] = pasos[actual.x, actual.y] + 1;
                    cola.Enqueue(v);
                }
            }
        }
        return pasos;
    }

    // Cuántas de las 8 celdas vecinas son roca
    public static int vecinosDeRoca(bool[,] roca, int x, int y)
    {
        int cuenta = 0;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if ((dx != 0 || dy != 0) && roca[x + dx, y + dy]) cuenta++;
        return cuenta;
    }

    // Roca alta y suelo plano, así las paredes quedan empinadas
    public static float[,] crearAlturas(bool[,] roca)
    {
        int n = roca.GetLength(0);
        float[,] alturas = new float[n, n];
        float desfase = Random.Range(0f, 1000f);
        for (int x = 0; x < n; x++)
            for (int y = 0; y < n; y++)
                if (roca[x, y]) alturas[x, y] = 0.9f + 0.1f * Mathf.PerlinNoise(x * 0.15f + desfase, y * 0.15f + desfase);
        return alturas;
    }

    // Color del suelo, del pasillo y de las paredes
    public static Color colorPorAltura(float h, bool esPasillo)
    {
        if (h < 0.03f) return esPasillo ? new Color(0.46f, 0.38f, 0.29f) : new Color(0.38f, 0.31f, 0.25f);
        if (h < 0.85f) return Color.Lerp(new Color(0.50f, 0.46f, 0.42f), new Color(0.33f, 0.31f, 0.31f), h);
        return new Color(0.27f, 0.25f, 0.27f);
    }

    // Pinta la textura igual que en la isla
    public static Texture2D crearTextura(float[,] alturas, bool[,] pasillo, int detalle = 4)
    {
        int n = alturas.GetLength(0);
        int lado = (n - 1) * detalle + 1;
        Texture2D textura = new Texture2D(lado, lado);
        textura.wrapMode = TextureWrapMode.Clamp;
        for (int x = 0; x < lado; x++)
        {
            for (int y = 0; y < lado; y++)
            {
                float fx = x / (float)detalle;
                float fy = y / (float)detalle;
                float h = GeneradorTerreno.alturaSuave(alturas, fx, fy);
                bool esPasillo = pasillo[Mathf.RoundToInt(fx), Mathf.RoundToInt(fy)];
                float manchas = 0.85f + 0.3f * Mathf.PerlinNoise(fx * 0.3f, fy * 0.3f);
                textura.SetPixel(x, y, colorPorAltura(h, esPasillo) * manchas);
            }
        }
        textura.Apply();
        return textura;
    }
}
