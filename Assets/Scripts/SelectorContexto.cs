using UnityEngine;
public class SelectorContexto : MonoBehaviour
{
    Isla isla;
    Mina mina;

    // Parte mostrando la isla
    void Awake()
    {
        isla = GetComponent<Isla>();
        mina = GetComponent<Mina>();
        isla.enabled = true;
        mina.enabled = false;
    }

    // Activa un script y desactiva el otro
    void OnGUI()
    {
        int actual = mina.enabled ? 1 : 0;
        int elegido = GUI.Toolbar(new Rect(Screen.width / 2f - 100, 10, 200, 28), actual, new string[] { "Isla", "Mina" });
        if (elegido != actual)
        {
            isla.enabled = elegido == 0;
            mina.enabled = elegido == 1;
        }
    }
}
