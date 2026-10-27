using System.Collections.Generic;
using System.Text;
using UnityEngine;
public class LSistema
{
    class Regla
    {
        public char simbolo;
        public string resultado;
        public float peso;
    }

    public string nombre;
    public string axioma;
    public float angulo;
    public int iteraciones;
    List<Regla> reglas = new List<Regla>();

    public LSistema(string nombre, string axioma, float angulo, int iteraciones)
    {
        this.nombre = nombre;
        this.axioma = axioma;
        this.angulo = angulo;
        this.iteraciones = iteraciones;
    }

    public void agregarRegla(char simbolo, string resultado, float peso = 1f)
    {
        reglas.Add(new Regla { simbolo = simbolo, resultado = resultado, peso = peso });
    }

    // Aqui se genera la cadena final del L-System
    public string generar()
    {
        string actual = axioma;
        for (int i = 0; i < iteraciones; i++)
        {
            StringBuilder siguiente = new StringBuilder();
            foreach (char c in actual)
                siguiente.Append(reemplazo(c));
            actual = siguiente.ToString();
        }
        return actual;
    }

    // Pues se elige una regla de reemplazo aleatoria para el símbolo dado
    string reemplazo(char c)
    {
        List<Regla> opciones = new List<Regla>();
        foreach (Regla r in reglas)
            if (r.simbolo == c) opciones.Add(r);
        if (opciones.Count == 0) return c.ToString();

        float total = 0f;
        foreach (Regla r in opciones) total += r.peso;
        float azar = Random.value * total;
        foreach (Regla r in opciones)
        {
            azar -= r.peso;
            if (azar <= 0f) return r.resultado;
        }
        return opciones[opciones.Count - 1].resultado;
    }

    // Recuerda esto Bastian....reglas! (nota debo eliminar este comentario despues)
    // Simbolos: F rama, L hoja, + - girar, & ^ inclinar, \ / rotar, [ ] guardar y volver, ! más delgado

    // Roble que tiene tronco que se divide en 3 ramas una y otra vez
    public static LSistema roble()
    {
        LSistema s = new LSistema("Roble", "FFFA", 25f, 4);
        s.agregarRegla('A', "[&&FL!A]/////[&&FL!A]///////[&&FL!A]");
        return s;
    }

    // Pino el que tiene unas ramas en pisos, las de abajo más largas
    public static LSistema pino()
    {
        LSistema s = new LSistema("Pino", "FFA", 28f, 6);
        s.agregarRegla('A', "F[&&&B]////[&&&B]////[&&&B]////[&&&B]!A");
        s.agregarRegla('A', "F[&&&B]/////[&&&B]/////[&&&B]!A");
        s.agregarRegla('B', "F[-L][+L]B");
        s.agregarRegla('B', "FLB", 0.6f);
        return s;
    }

    // Arbusto tiene ramas que salen desde el suelo
    public static LSistema arbusto()
    {
        LSistema s = new LSistema("Arbusto", "A", 40f, 4);
        s.agregarRegla('A', "[&FL!A]/////[&FL!A]///////[&FL!A]");
        return s;
    }

    // Cristal de la mina, esta varia las puntas desde el mismo punto
    public static LSistema cristal()
    {
        LSistema s = new LSistema("Cristal", "A", 30f, 2);
        s.agregarRegla('A', "[FL]////[&FL!A]////[&&FL]////[&FL!A]");
        s.agregarRegla('A', "[FL]/////[&FL!A]/////[&FL!A]", 0.7f);
        return s;
    }
}
