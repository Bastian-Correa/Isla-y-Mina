using System.Collections.Generic;
using UnityEngine;
public class Mina : MonoBehaviour
{
    [Header("General")]
    public int semilla = 2024;

    [Header("Mapa")]
    public int celdas = 110;
    public float tamanoCelda = 1f;
    public float alturaParedes = 5f;

    [Header("Galerías (BSP)")]
    public int divisiones = 4;
    public int tamanoMinimo = 14;

    [Header("Objetos")]
    public bool cantidadAleatoria = false;
    public int cantidadCristales = 25;
    public int cantidadVagonetas = 3;
    public int maxLuces = 24;

    bool[,] roca;
    bool[,] pasillo;
    bool[,] enSala;
    int[,] distancia;
    Vector2Int entrada;
    List<GeneradorMina.Sala> salas = new List<GeneradorMina.Sala>();
    List<GeneradorMina.Tramo> tramos = new List<GeneradorMina.Tramo>();

    Texture2D mapa;
    GameObject contenedor;
    CamaraOrbital camara;
    Material materialBase;
    Mesh cubo;
    float tamano;
    List<Object> creados = new List<Object>();

    List<CombineInstance> piezasMadera = new List<CombineInstance>();
    List<CombineInstance> piezasMetal = new List<CombineInstance>();
    List<CombineInstance> piezasFuego = new List<CombineInstance>();
    List<CombineInstance> piezasCarga = new List<CombineInstance>();

    float msBsp;
    float largoPasillos, recorridoMaximo;
    int antorchas, luces, cristales;
    int cristalesPedidos, vagonetasPedidas;

    Light sol;
    float intensidadSol;
    Color ambienteAnterior, fondoAnterior;
    int lucesAnterior;

    bool mostrarMenu = true;
    bool regenerarPendiente = false;
    string textoSemilla;
    Rect ventana = new Rect(10, 10, 340, 350);

    void Start()
    {
        camara = Camera.main.GetComponent<CamaraOrbital>();
        GameObject temporal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        materialBase = temporal.GetComponent<MeshRenderer>().sharedMaterial;
        cubo = temporal.GetComponent<MeshFilter>().sharedMesh;
        Destroy(temporal);

        textoSemilla = semilla.ToString();
        generar();
        camara.encuadrar(tamano);
    }

    // Al entrar a la mina se oscurece la escena
    void OnEnable()
    {
        foreach (Light luz in FindObjectsByType<Light>(FindObjectsSortMode.None))
            if (luz.type == LightType.Directional) sol = luz;
        if (sol != null)
        {
            intensidadSol = sol.intensity;
            sol.intensity = 0.15f;
        }
        ambienteAnterior = RenderSettings.ambientLight;
        RenderSettings.ambientLight = new Color(0.16f, 0.15f, 0.19f);
        lucesAnterior = QualitySettings.pixelLightCount;
        QualitySettings.pixelLightCount = maxLuces;
        if (Camera.main != null)
        {
            fondoAnterior = Camera.main.backgroundColor;
            Camera.main.backgroundColor = new Color(0.04f, 0.04f, 0.06f);
        }

        if (contenedor != null)
        {
            contenedor.SetActive(true);
            camara.encuadrar(tamano);
        }
    }

    // Al salir se devuelve la luz de la isla
    void OnDisable()
    {
        if (sol != null) sol.intensity = intensidadSol;
        RenderSettings.ambientLight = ambienteAnterior;
        QualitySettings.pixelLightCount = lucesAnterior;
        if (Camera.main != null) Camera.main.backgroundColor = fondoAnterior;
        if (contenedor != null) contenedor.SetActive(false);
    }

    // Borra la mina anterior y crea una nueva
    void generar()
    {
        if (contenedor != null) Destroy(contenedor);
        foreach (Object o in creados) Destroy(o);
        creados.Clear();
        contenedor = new GameObject("Mina generada");
        tamano = celdas * tamanoCelda;
        luces = 0;

        // La misma semilla genera la misma mina
        Random.InitState(semilla);

        var reloj = System.Diagnostics.Stopwatch.StartNew();
        salas.Clear();
        tramos.Clear();
        pasillo = new bool[celdas, celdas];
        entrada = new Vector2Int(celdas / 2, 2);
        roca = GeneradorMina.generar(celdas, tamanoMinimo, divisiones, entrada, salas, tramos, pasillo);
        msBsp = (float)reloj.Elapsed.TotalMilliseconds;

        // La distancia de cada celda a la entrada sirve para poner cristales en las salas más alejadas
        distancia = GeneradorMina.medirDistancias(roca, entrada);
        enSala = new bool[celdas, celdas];
        foreach (GeneradorMina.Sala s in salas)
            for (int x = s.x; x < s.x + s.ancho; x++)
                for (int y = s.y; y < s.y + s.alto; y++)
                    enSala[x, y] = true;

        // La roca usa la misma malla que el terreno de la isla
        float[,] alturas = GeneradorMina.crearAlturas(roca);
        Mesh malla = GeneradorTerreno.crearMalla(alturas, tamano, alturaParedes, 0f);
        mapa = GeneradorMina.crearTextura(alturas, pasillo);
        creados.Add(malla);
        creados.Add(mapa);
        Material materialRoca = nuevoMaterial(Color.white);
        materialRoca.mainTexture = mapa;
        crearObjeto("Roca", malla, materialRoca);

        if (cantidadAleatoria)
        {
            cristalesPedidos = Random.Range(10, 61);
            vagonetasPedidas = Random.Range(0, 9);
        }
        else
        {
            cristalesPedidos = cantidadCristales;
            vagonetasPedidas = cantidadVagonetas;
        }
        piezasMadera.Clear();
        piezasMetal.Clear();
        piezasFuego.Clear();
        piezasCarga.Clear();
        ponerLamparasDeSala();
        ponerRielesYAntorchas();
        ponerVagonetas();
        ponerCristales();

        // Materiales para ponerlos en las mallas juntas
        juntar(piezasMadera, "Madera", new Color(0.36f, 0.24f, 0.13f));
        juntar(piezasMetal, "Metal", new Color(0.30f, 0.30f, 0.33f));
        juntar(piezasFuego, "Faroles", new Color(1f, 0.65f, 0.25f));
        juntar(piezasCarga, "Mineral", new Color(0.12f, 0.11f, 0.12f));

        largoPasillos = 0f;
        foreach (GeneradorMina.Tramo t in tramos) largoPasillos += GeneradorMina.distancia(t.desde, t.hasta) * tamanoCelda;
        int maximo = 0;
        foreach (int d in distancia) maximo = Mathf.Max(maximo, d);
        recorridoMaximo = maximo * tamanoCelda;
    }

    // Un farol en el centro de cada sala
    void ponerLamparasDeSala()
    {
        foreach (GeneradorMina.Sala s in salas)
        {
            Vector2Int c = s.centro();
            Vector3 p = puntoDelMundo(c.x, c.y) + new Vector3(1.2f, 0, 1.2f);
            agregarCubo(piezasMadera, p + Vector3.up * 1.2f, Quaternion.identity, new Vector3(0.2f, 2.4f, 0.2f));
            agregarCubo(piezasFuego, p + Vector3.up * 2.5f, Quaternion.identity, Vector3.one * 0.35f);
            crearLuz(p + Vector3.up * 2.6f, Mathf.Max(s.ancho, s.alto) * tamanoCelda * 0.9f);
        }
    }

    // Rieles en cada pasillo y una antorcha cada 6 celdas
    void ponerRielesYAntorchas()
    {
        antorchas = 0;
        foreach (GeneradorMina.Tramo t in tramos)
        {
            Vector3 a = puntoDelMundo(t.desde.x, t.desde.y);
            Vector3 b = puntoDelMundo(t.hasta.x, t.hasta.y);
            Vector3 dir = (b - a).normalized;
            Vector3 lado = Vector3.Cross(Vector3.up, dir);
            float largo = Vector3.Distance(a, b);
            Quaternion giro = Quaternion.LookRotation(dir);

            Vector3 medio = (a + b) / 2f + Vector3.up * 0.12f;
            agregarCubo(piezasMetal, medio + lado * 0.45f, giro, new Vector3(0.08f, 0.08f, largo));
            agregarCubo(piezasMetal, medio - lado * 0.45f, giro, new Vector3(0.08f, 0.08f, largo));
            for (float d = 0f; d <= largo; d += 1.5f)
                agregarCubo(piezasMadera, a + dir * d + Vector3.up * 0.04f, giro, new Vector3(1.3f, 0.08f, 0.25f));

            int pasos = GeneradorMina.distancia(t.desde, t.hasta);
            int dx = t.hasta.x > t.desde.x ? 1 : (t.hasta.x < t.desde.x ? -1 : 0);
            int dy = t.hasta.y > t.desde.y ? 1 : (t.hasta.y < t.desde.y ? -1 : 0);
            for (int i = 3; i < pasos - 1; i += 6)
            {
                int cx = t.desde.x + dx * i;
                int cy = t.desde.y + dy * i;
                if (enSala[cx, cy]) continue;

                Vector3 p = puntoDelMundo(cx, cy) + lado * (antorchas % 2 == 0 ? 1.2f : -1.2f);
                agregarCubo(piezasMadera, p + Vector3.up * 0.8f, giro, new Vector3(0.15f, 1.6f, 0.15f));
                agregarCubo(piezasFuego, p + Vector3.up * 1.7f, giro, Vector3.one * 0.3f);
                crearLuz(p + Vector3.up * 2f, 8f);
                antorchas++;
            }
        }
    }

    // Vagonetas con mineral sobre los rieles
    void ponerVagonetas()
    {
        for (int i = 0; i < vagonetasPedidas; i++)
        {
            GeneradorMina.Tramo t = tramos[Random.Range(0, tramos.Count)];
            Vector3 a = puntoDelMundo(t.desde.x, t.desde.y);
            Vector3 b = puntoDelMundo(t.hasta.x, t.hasta.y);
            Vector3 p = Vector3.Lerp(a, b, Random.Range(0.2f, 0.8f));
            Quaternion giro = Quaternion.LookRotation((b - a).normalized);
            agregarCubo(piezasMetal, p + Vector3.up * 0.55f, giro, new Vector3(1.1f, 0.7f, 1.6f));
            agregarCubo(piezasCarga, p + Vector3.up * 0.95f, giro, new Vector3(0.9f, 0.25f, 1.4f));
        }
    }

    // Cristales en las salas lejanas, junto a las paredes
    void ponerCristales()
    {
        LSistema especie = LSistema.cristal();
        List<Mesh> variantes = new List<Mesh>();
        for (int v = 0; v < 4; v++)
        {
            Mesh m = Tortuga.construirArbol(especie.generar(), especie.angulo, 1.6f, 0.2f, 0.35f);
            creados.Add(m);
            variantes.Add(m);
        }
        Color[] colorBase = { new Color(0.25f, 0.75f, 0.90f), new Color(0.55f, 0.30f, 0.85f), new Color(0.30f, 0.85f, 0.45f) };
        Color[] colorPunta = { new Color(0.70f, 1f, 1f), new Color(0.85f, 0.70f, 1f), new Color(0.75f, 1f, 0.80f) };
        Material[] bases = new Material[3];
        Material[] puntas = new Material[3];
        for (int c = 0; c < 3; c++)
        {
            bases[c] = nuevoMaterial(colorBase[c]);
            puntas[c] = nuevoMaterial(colorPunta[c]);
        }

        int maxima = 0;
        foreach (int d in distancia) maxima = Mathf.Max(maxima, d);

        cristales = 0;
        for (int intento = 0; intento < cristalesPedidos * 80 && cristales < cristalesPedidos; intento++)
        {
            int x = Random.Range(1, celdas - 1);
            int y = Random.Range(1, celdas - 1);
            if (!enSala[x, y] || pasillo[x, y]) continue;
            if (distancia[x, y] < maxima * 0.5f) continue;
            if (GeneradorMina.vecinosDeRoca(roca, x, y) < 3) continue;

            int c = Random.Range(0, 3);
            GameObject cristal = crearObjeto("Cristal", variantes[Random.Range(0, variantes.Count)], bases[c]);
            cristal.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { bases[c], puntas[c] };
            cristal.transform.position = puntoDelMundo(x, y);
            cristal.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            cristal.transform.localScale = Vector3.one * Random.Range(0.8f, 1.4f);
            cristales++;
        }
    }

    // Luz de farol, con un maximo 
    void crearLuz(Vector3 posicion, float alcance)
    {
        if (luces >= maxLuces) return;
        Light luz = new GameObject("Luz").AddComponent<Light>();
        luz.transform.SetParent(contenedor.transform);
        luz.transform.position = posicion;
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.62f, 0.28f);
        luz.range = alcance;
        luz.intensity = 1.8f;
        luces++;
    }

    void agregarCubo(List<CombineInstance> lista, Vector3 posicion, Quaternion giro, Vector3 escala)
    {
        lista.Add(new CombineInstance { mesh = cubo, transform = Matrix4x4.TRS(posicion, giro, escala) });
    }

    void juntar(List<CombineInstance> piezas, string nombre, Color color)
    {
        Mesh malla = new Mesh();
        malla.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        malla.CombineMeshes(piezas.ToArray());
        creados.Add(malla);
        crearObjeto(nombre, malla, nuevoMaterial(color));
    }

    Vector3 puntoDelMundo(int x, int y)
    {
        float celda = tamano / (celdas - 1);
        return new Vector3(x * celda - tamano / 2f, 0f, y * celda - tamano / 2f);
    }

    Material nuevoMaterial(Color color)
    {
        Material m = new Material(materialBase);
        m.color = color;
        creados.Add(m);
        return m;
    }

    GameObject crearObjeto(string nombre, Mesh malla, Material material)
    {
        GameObject obj = new GameObject(nombre);
        obj.transform.SetParent(contenedor.transform);
        obj.AddComponent<MeshFilter>().sharedMesh = malla;
        obj.AddComponent<MeshRenderer>().sharedMaterial = material;
        return obj;
    }

    // Controla las teclas y la regeneración
    void Update()
    {
        if (regenerarPendiente && !Input.GetMouseButton(0))
        {
            regenerarPendiente = false;
            generar();
        }

        if (GUIUtility.keyboardControl != 0) return;
        if (Input.GetKeyDown(KeyCode.N)) semillaAleatoria();
        if (Input.GetKeyDown(KeyCode.H)) mostrarMenu = !mostrarMenu;
    }

    void semillaAleatoria()
    {
        semilla = new System.Random().Next(1000000);
        textoSemilla = semilla.ToString();
        generar();
    }

    void OnGUI()
    {
        camara.bloquearRueda = mostrarMenu && ventana.Contains(Event.current.mousePosition);
        if (!mostrarMenu) return;
        ventana = GUILayout.Window(1, ventana, dibujarMenu, "Generador de minas");
        dibujarPanel();
    }

    void dibujarMenu(int id)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Semilla", GUILayout.Width(55));
        textoSemilla = GUILayout.TextField(textoSemilla, GUILayout.Width(90));
        if (GUILayout.Button("Aplicar") && int.TryParse(textoSemilla, out int s))
        {
            semilla = s;
            GUIUtility.keyboardControl = 0;
            generar();
        }
        if (GUILayout.Button("Aleatoria")) semillaAleatoria();
        GUILayout.EndHorizontal();

        GUILayout.Label("Galerías (BSP)");
        divisiones = deslizadorEntero("Divisiones", divisiones, 1, 6);
        tamanoMinimo = deslizadorEntero("Tamaño mínimo", tamanoMinimo, 8, 30);

        GUILayout.Label("Objetos");
        bool aleatoria = GUILayout.Toolbar(cantidadAleatoria ? 1 : 0, new string[] { "Cantidad elegida", "Cantidad al azar" }) == 1;
        if (aleatoria != cantidadAleatoria)
        {
            cantidadAleatoria = aleatoria;
            regenerarPendiente = true;
        }
        if (cantidadAleatoria) GUILayout.Label("Cada semilla elige sus cristales y vagonetas.");
        else
        {
            cantidadCristales = deslizadorEntero("Cristales", cantidadCristales, 0, 60);
            cantidadVagonetas = deslizadorEntero("Vagonetas", cantidadVagonetas, 0, 8);
        }

        GUILayout.Space(6);
        if (GUILayout.Button("Encuadrar")) camara.encuadrar(tamano);
        GUILayout.Space(6);
        GUILayout.Label("Controles");
        GUILayout.Label("• Clic derecho + arrastrar: rotar la cámara");
        GUILayout.Label("• Rueda del mouse: zoom");
        GUILayout.Label("• WASD o flechas: mover");
        GUILayout.Label("• T: vista desde arriba");
        GUILayout.Label("• N: semilla aleatoria");
        GUILayout.Label("• H: ocultar o mostrar el menú");
        GUI.DragWindow();
    }

    int deslizadorEntero(string nombre, int valor, int min, int max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(nombre, GUILayout.Width(110));
        int nuevo = Mathf.RoundToInt(GUILayout.HorizontalSlider(valor, min, max, GUILayout.Width(150)));
        GUILayout.Label(nuevo.ToString(), GUILayout.Width(40));
        GUILayout.EndHorizontal();
        if (nuevo != valor) regenerarPendiente = true;
        return nuevo;
    }

    void dibujarPanel()
    {
        float ancho = 260f;
        GUILayout.BeginArea(new Rect(Screen.width - ancho - 10, 10, ancho, 450), GUI.skin.box);
        Rect cuadro = GUILayoutUtility.GetRect(ancho - 10, ancho - 10);
        GUI.DrawTexture(cuadro, mapa);
        GUILayout.Label("Semilla: " + semilla);
        GUILayout.Label("Salas: " + salas.Count + " · Pasillos: " + largoPasillos.ToString("0") + " m");
        GUILayout.Label("Recorrido más largo: " + recorridoMaximo.ToString("0") + " m");
        GUILayout.Label("Antorchas " + antorchas + " · Luces " + luces);
        string modo = cantidadAleatoria ? " (al azar)" : " (elegida)";
        GUILayout.Label("Cristales " + cristales + " · Vagonetas " + vagonetasPedidas + modo);
        GUILayout.Label("Tiempo BSP: " + msBsp.ToString("0.0") + " ms");
        GUILayout.EndArea();
    }
}
