using System.Collections.Generic;
using UnityEngine;
public class Isla : MonoBehaviour
{
    [Header("General")]
    public int semilla = 12345;

    [Header("Terreno (ruido Perlin)")]
    public int resolucion = 129;      
    public float tamano = 200f;       
    public float alturaMaxima = 40f;
    public float escala = 3.5f;       
    public int octavas = 5;
    public float persistencia = 0.5f;
    public float lacunaridad = 2f;
    public bool formaDeIsla = true;
    public float nivelDelMar = 0.32f;

    [Header("Árboles (L-Systems)")]
    public bool cantidadAleatoria = false;
    public int cantidadArboles = 250;
    public int variantesPorEspecie = 4;

    float[,] alturas;
    Texture2D mapa;
    GameObject contenedor;
    CamaraOrbital camara;
    Material materialBase;
    List<Object> creados = new List<Object>();

    float msTerreno, msArboles;
    int[] arbolesPorEspecie = new int[3];
    int arbolesPedidos;
    float porcentajeAgua, porcentajeTierra, porcentajeMontana;

    bool mostrarMenu = true;
    bool regenerarPendiente = false;
    string textoSemilla;
    Rect ventana = new Rect(10, 10, 340, 450);

    // Inicia la escena y genera la isla
    void Start()
    {
        prepararEscena();
        textoSemilla = semilla.ToString();
        generar();
        camara.encuadrar(tamano);
    }

    // Activa y desactiva este codigo pues
    void OnEnable()
    {
        if (contenedor == null) return;
        contenedor.SetActive(true);
        camara.encuadrar(tamano);
    }

    void OnDisable()
    {
        if (contenedor != null) contenedor.SetActive(false);
    }

    // Prepara la cámara y la iluminación
    void prepararEscena()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
        }
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.55f, 0.72f, 0.90f);
        cam.farClipPlane = 3000f;
        camara = cam.GetComponent<CamaraOrbital>();
        if (camara == null) camara = cam.gameObject.AddComponent<CamaraOrbital>();

        if (FindFirstObjectByType<Light>() == null)
        {
            Light sol = new GameObject("Sol").AddComponent<Light>();
            sol.type = LightType.Directional;
            sol.shadows = LightShadows.Soft;
            sol.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.47f, 0.52f);
        GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        materialBase = cubo.GetComponent<MeshRenderer>().sharedMaterial;
        Destroy(cubo);
    }

    // Borra la isla anterior y crea una nueva
    void generar()
    {
        if (contenedor != null) Destroy(contenedor);
        foreach (Object o in creados) Destroy(o);
        creados.Clear();
        contenedor = new GameObject("Isla generada");

        // La misma semilla genera la misma isla
        Random.InitState(semilla);

        var reloj = System.Diagnostics.Stopwatch.StartNew();
        alturas = GeneradorTerreno.generarAlturas(resolucion, escala, octavas, persistencia, lacunaridad, formaDeIsla);
        msTerreno = (float)reloj.Elapsed.TotalMilliseconds;

        Mesh malla = GeneradorTerreno.crearMalla(alturas, tamano, alturaMaxima, nivelDelMar);
        mapa = GeneradorTerreno.crearTextura(alturas, nivelDelMar);
        creados.Add(malla);
        creados.Add(mapa);
        Material materialTerreno = nuevoMaterial(Color.white);
        materialTerreno.mainTexture = mapa;
        crearObjeto("Terreno", malla, materialTerreno);

        crearAgua();
        calcularPorcentajes();

        reloj.Restart();
        ponerArboles();
        msArboles = (float)reloj.Elapsed.TotalMilliseconds;
    }

    // Añade el agua al mapa
    void crearAgua()
    {
        GameObject agua = GameObject.CreatePrimitive(PrimitiveType.Plane);
        agua.name = "Agua";
        agua.transform.SetParent(contenedor.transform);
        agua.transform.position = new Vector3(0, nivelDelMar * alturaMaxima - 0.3f, 0);
        agua.transform.localScale = Vector3.one * (tamano * 1.6f / 10f);  // el plano del agua mide 10 x 10, por si me da la gana cambiarlo despues
        Destroy(agua.GetComponent<Collider>());
        agua.GetComponent<MeshRenderer>().sharedMaterial = nuevoMaterial(new Color(0.10f, 0.22f, 0.45f));
        agua.GetComponent<MeshRenderer>().receiveShadows = false; 
    }

    // Calcula los porcentajes del terreno
    void calcularPorcentajes()
    {
        int agua = 0, tierra = 0, montana = 0;
        foreach (float h in alturas)
        {
            if (h < nivelDelMar) agua++;
            else if (h < GeneradorTerreno.nivelRoca) tierra++;
            else montana++;
        }
        float total = alturas.Length;
        porcentajeAgua = 100f * agua / total;
        porcentajeTierra = 100f * tierra / total;
        porcentajeMontana = 100f * montana / total;
    }

    // Coloca los árboles
    void ponerArboles()
    {
        LSistema[] especies = { LSistema.roble(), LSistema.pino(), LSistema.arbusto() };
        // En metros
        float[] alturaEspecie = { 3.5f, 4.5f, 1.25f };
        float[] radioTronco = { 0.15f, 0.175f, 0.04f };
        float[] tamanoHoja = { 0.55f, 0.65f, 0.25f };
        Color[] colorHojas =
        {
            new Color(0.22f, 0.55f, 0.20f),
            new Color(0.10f, 0.36f, 0.22f),
            new Color(0.45f, 0.62f, 0.22f),
        };
        Material corteza = nuevoMaterial(new Color(0.36f, 0.25f, 0.16f));

        // Variantes de cada especie que se reutilizan en todos los árboles
        List<Mesh>[] variantes = new List<Mesh>[especies.Length];
        Material[] hojas = new Material[especies.Length];
        for (int e = 0; e < especies.Length; e++)
        {
            hojas[e] = nuevoMaterial(colorHojas[e]);
            variantes[e] = new List<Mesh>();
            for (int v = 0; v < variantesPorEspecie; v++)
            {
                string cadena = especies[e].generar();
                Mesh arbol = Tortuga.construirArbol(cadena, especies[e].angulo, alturaEspecie[e], radioTronco[e], tamanoHoja[e]);
                creados.Add(arbol);
                variantes[e].Add(arbol);
            }
        }

        // Segunda capa donde es alta hay bosque denso y donde es baja hay claros
        float desfaseX = Random.Range(0f, 1000f);
        float desfaseY = Random.Range(0f, 1000f);
        arbolesPorEspecie = new int[especies.Length];
        int puestos = 0;

        if (cantidadAleatoria) arbolesPedidos = Random.Range(50, 401);
        else arbolesPedidos = cantidadArboles;

        for (int intento = 0; intento < arbolesPedidos * 30 && puestos < arbolesPedidos; intento++)
        {
            int x = Random.Range(2, resolucion - 2);
            int y = Random.Range(2, resolucion - 2);
            float h = alturas[x, y];

            if (h < nivelDelMar + GeneradorTerreno.bandaArena) continue; 
            bool pradera = h < GeneradorTerreno.nivelBosque;
            bool bosque = h >= GeneradorTerreno.nivelBosque && h < GeneradorTerreno.nivelRoca;
            if (!pradera && !bosque) continue;

            float ruido = Mathf.PerlinNoise(x / (float)resolucion * 4f + desfaseX, y / (float)resolucion * 4f + desfaseY);
            if (Random.value > ruido * ruido) continue;

            int e;
            if (bosque) e = Random.value < 0.7f ? 1 : 0;
            else e = Random.value < 0.5f ? 2 : 0;

            Mesh malla = variantes[e][Random.Range(0, variantes[e].Count)];
            GameObject arbol = crearObjeto(especies[e].nombre, malla, corteza);
            arbol.GetComponent<MeshRenderer>().sharedMaterials = new Material[] { corteza, hojas[e] };
            arbol.transform.position = puntoDelMundo(x, y) + Vector3.down * 0.2f;
            arbol.transform.rotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);
            arbol.transform.localScale = Vector3.one * Random.Range(0.75f, 1.25f);
            arbolesPorEspecie[e]++;
            puestos++;
        }
    }

    Vector3 puntoDelMundo(int x, int y)
    {
        float celda = tamano / (resolucion - 1);
        return new Vector3(x * celda - tamano / 2f, alturas[x, y] * alturaMaxima, y * celda - tamano / 2f);
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
        ventana = GUILayout.Window(0, ventana, dibujarMenu, "Generador de islas");
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

        GUILayout.Label("Terreno");
        escala = deslizador("Escala", escala, 0.5f, 10f);
        octavas = deslizadorEntero("Octavas", octavas, 1, 8);
        persistencia = deslizador("Persistencia", persistencia, 0.1f, 0.9f);
        lacunaridad = deslizador("Lacunaridad", lacunaridad, 1.5f, 3.5f);
        alturaMaxima = deslizador("Altura", alturaMaxima, 5f, 80f);
        nivelDelMar = deslizador("Nivel del mar", nivelDelMar, 0.05f, 0.6f);

        GUILayout.Label("Árboles");
        bool aleatoria = GUILayout.Toolbar(cantidadAleatoria ? 1 : 0, new string[] { "Cantidad elegida", "Cantidad al azar" }) == 1;
        if (aleatoria != cantidadAleatoria)
        {
            cantidadAleatoria = aleatoria;
            regenerarPendiente = true;
        }
        if (cantidadAleatoria) GUILayout.Label("Cada semilla elige entre 50 y 400 árboles.");
        else cantidadArboles = deslizadorEntero("Cantidad", cantidadArboles, 0, 600);

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
    float deslizador(string nombre, float valor, float min, float max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(nombre, GUILayout.Width(100));
        float nuevo = GUILayout.HorizontalSlider(valor, min, max, GUILayout.Width(160));
        GUILayout.Label(nuevo.ToString("0.00"), GUILayout.Width(40));
        GUILayout.EndHorizontal();
        if (nuevo != valor) regenerarPendiente = true;
        return nuevo;
    }

    int deslizadorEntero(string nombre, int valor, int min, int max)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(nombre, GUILayout.Width(100));
        int nuevo = Mathf.RoundToInt(GUILayout.HorizontalSlider(valor, min, max, GUILayout.Width(160)));
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
        GUILayout.Label("Agua: " + porcentajeAgua.ToString("0.0") + " %");
        GUILayout.Label("Pasto y bosque: " + porcentajeTierra.ToString("0.0") + " %");
        GUILayout.Label("Montaña: " + porcentajeMontana.ToString("0.0") + " %");
        int total = arbolesPorEspecie[0] + arbolesPorEspecie[1] + arbolesPorEspecie[2];
        string modo = cantidadAleatoria ? " (al azar)" : " (elegida)";
        GUILayout.Label("Árboles: " + total + modo);
        if (total < arbolesPedidos) GUILayout.Label("No cupieron todos: se pidieron " + arbolesPedidos);
        GUILayout.Label("Robles " + arbolesPorEspecie[0] + " · Pinos " + arbolesPorEspecie[1] + " · Arbustos " + arbolesPorEspecie[2]);
        GUILayout.Label("Tiempo terreno: " + msTerreno.ToString("0.0") + " ms");
        GUILayout.Label("Tiempo árboles: " + msArboles.ToString("0.0") + " ms");
        GUILayout.EndArea();
    }
}