using UnityEngine;

// Este scrip se encarga de la cámara que gira alrededor del mapa, para que sepan señores y señoras
public class CamaraOrbital : MonoBehaviour
{
    public Vector3 objetivo;
    public float distancia = 300f;
    public float giro = 45f;
    public float inclinacion = 45f;
    public bool vistaCenital = false;
    public bool bloquearRueda = false;

    // Centra la cámara según el tamaño del mapa
    public void encuadrar(float tamano)
    {
        objetivo = Vector3.zero;
        distancia = tamano * 1.2f;
    }

    // Rotar con clic derecho, zoom con la rueda y mover con WASD
    void LateUpdate()
    {
        if (Input.GetMouseButton(1))
        {
            giro += Input.GetAxis("Mouse X") * 4f;
            inclinacion = Mathf.Clamp(inclinacion - Input.GetAxis("Mouse Y") * 4f, 5f, 89f);
        }
        if (!bloquearRueda)
            distancia = Mathf.Clamp(distancia * (1f - Input.mouseScrollDelta.y * 0.1f), 5f, 1000f);

        Vector3 adelante = Quaternion.Euler(0, giro, 0) * Vector3.forward;
        Vector3 derecha = Quaternion.Euler(0, giro, 0) * Vector3.right;
        objetivo += (derecha * Input.GetAxisRaw("Horizontal") + adelante * Input.GetAxisRaw("Vertical")) * distancia * 0.6f * Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.T)) vistaCenital = !vistaCenital;
        Quaternion rotacion = vistaCenital ? Quaternion.Euler(89.9f, 0, 0) : Quaternion.Euler(inclinacion, giro, 0);
        transform.position = objetivo - rotacion * Vector3.forward * distancia;
        transform.rotation = rotacion;
    }
}
