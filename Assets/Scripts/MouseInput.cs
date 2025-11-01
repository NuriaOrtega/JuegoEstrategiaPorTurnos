using UnityEngine;

public class RaycastDesdeCamara : MonoBehaviour
{
    void Update()
    {
        // Detectar clic con el botón izquierdo del mouse
        if (Input.GetMouseButtonDown(0))
        {
            // Crear un rayo desde la cámara hacia donde está el puntero del mouse
            Ray rayo = Camera.main.ScreenPointToRay(Input.mousePosition);

            // Guardar información del objeto impactado
            RaycastHit hit;

            // Lanza el rayo
            if (Physics.Raycast(rayo, out hit))
            {
                Debug.Log("El rayo impactó con: " + hit.collider.name);

                // Puedes hacer algo con el objeto impactado
                // Ejemplo: cambiar color
                Renderer rend = hit.collider.GetComponent<Renderer>();
                if (rend != null)
                {
                    rend.material.color = Color.red;
                }

                // También puedes visualizar el rayo en la escena (solo modo Editor)
                Debug.DrawLine(rayo.origin, hit.point, Color.green, 1f);
            }
            else
            {
                Debug.Log("El rayo no impactó con nada.");
            }
        }
    }
}
