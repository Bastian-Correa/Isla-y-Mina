using System.Collections.Generic;
using UnityEngine;

public static class Tortuga
{
    struct Estado
    {
        public Vector3 posicion;
        public Quaternion rotacion;
        public float largo;
        public float grosor;
    }

    struct Rama
    {
        public Vector3 inicio;
        public Vector3 fin;
        public Quaternion rotacion;
        public float grosor;
    }

    struct Hoja
    {
        public Vector3 posicion;
        public Quaternion rotacion;
    }

    static Mesh cilindro;
    static Mesh cubo;

    // Recorre la cadena y arma la malla del árbol
    public static Mesh construirArbol(string cadena, float angulo, float altura, float radioTronco, float tamanoHoja)
    {
        if (cilindro == null)
        {
            cilindro = mallaDePrimitiva(PrimitiveType.Cylinder);
            cubo = mallaDePrimitiva(PrimitiveType.Cube);
        }

        List<Rama> ramas = new List<Rama>();
        List<Hoja> hojas = new List<Hoja>();
        Stack<Estado> pila = new Stack<Estado>();

        Vector3 posicion = Vector3.zero;
        Quaternion rotacion = Quaternion.identity;
        float largo = 1f;
        float grosor = 1f;

        foreach (char c in cadena)
        {
            switch (c)
            {
                case 'F':
                    rotacion = rotacion.normalized;
                    Vector3 fin = posicion + rotacion * Vector3.up * largo;
                    ramas.Add(new Rama { inicio = posicion, fin = fin, rotacion = rotacion, grosor = grosor });
                    posicion = fin;
                    break;
                case 'L':
                    hojas.Add(new Hoja { posicion = posicion, rotacion = rotacion.normalized });
                    break;
                case '+': rotacion *= Quaternion.AngleAxis(angulo, Vector3.forward); break;
                case '-': rotacion *= Quaternion.AngleAxis(-angulo, Vector3.forward); break;
                case '&': rotacion *= Quaternion.AngleAxis(angulo, Vector3.right); break;
                case '^': rotacion *= Quaternion.AngleAxis(-angulo, Vector3.right); break;
                case '\\': rotacion *= Quaternion.AngleAxis(angulo, Vector3.up); break;
                case '/': rotacion *= Quaternion.AngleAxis(-angulo, Vector3.up); break;
                case '!': grosor *= 0.72f; break;
                case '[':
                    pila.Push(new Estado { posicion = posicion, rotacion = rotacion, largo = largo, grosor = grosor });
                    largo *= 0.85f;
                    grosor *= 0.85f;
                    break;
                case ']':
                    Estado guardado = pila.Pop();
                    posicion = guardado.posicion;
                    rotacion = guardado.rotacion;
                    largo = guardado.largo;
                    grosor = guardado.grosor;
                    break;
            }
        }

        // Escala el árbol para que mida la altura pedida
        float maxY = 0.01f;
        foreach (Rama r in ramas) maxY = Mathf.Max(maxY, r.fin.y);
        float escala = altura / maxY;

        // Ajustar las ramas y hojas a la escala
        List<CombineInstance> partesRamas = new List<CombineInstance>();
        foreach (Rama r in ramas)
        {
            Vector3 centro = (r.inicio + r.fin) / 2f * escala;
            float largoReal = Vector3.Distance(r.inicio, r.fin) * escala;
            float radio = radioTronco * r.grosor;
            Matrix4x4 m = Matrix4x4.TRS(centro, r.rotacion, new Vector3(radio * 2f, largoReal / 2f, radio * 2f));
            partesRamas.Add(new CombineInstance { mesh = cilindro, transform = m });
        }

        List<CombineInstance> partesHojas = new List<CombineInstance>();
        foreach (Hoja h in hojas)
        {
            Matrix4x4 m = Matrix4x4.TRS(h.posicion * escala, h.rotacion, Vector3.one * tamanoHoja);
            partesHojas.Add(new CombineInstance { mesh = cubo, transform = m });
        }

        // Ramas y hojas quedan en mallas separadas 
        Mesh mallaRamas = juntar(partesRamas);
        Mesh mallaHojas = juntar(partesHojas);
        Mesh arbol = new Mesh();
        arbol.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        arbol.CombineMeshes(new CombineInstance[]
        {
            new CombineInstance { mesh = mallaRamas, transform = Matrix4x4.identity },
            new CombineInstance { mesh = mallaHojas, transform = Matrix4x4.identity },
        }, false);
        Object.Destroy(mallaRamas);
        Object.Destroy(mallaHojas);
        return arbol;
    }

    // Junta muchas piezas en una sola malla
    static Mesh juntar(List<CombineInstance> partes)
    {
        Mesh malla = new Mesh();
        malla.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        malla.CombineMeshes(partes.ToArray());
        return malla;
    }

    // Saca la malla
    static Mesh mallaDePrimitiva(PrimitiveType tipo)
    {
        GameObject temporal = GameObject.CreatePrimitive(tipo);
        Mesh malla = temporal.GetComponent<MeshFilter>().sharedMesh;
        Object.Destroy(temporal);
        return malla;
    }
}
