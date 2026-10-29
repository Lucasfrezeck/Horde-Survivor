using UnityEngine;

public class PlayerLookAtMouse : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale <= 0f) return;
        // pega a posição do cursor e calcula uma linha invisível que vai da câmera até o mundo
        Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);

        //cria um plano infinito na altura 0
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);


        //se o raio cruza o chao, distance guarda o quanto o raio percorreu
        if (groundPlane.Raycast(mouseRay, out float distance))
        {

            
            Vector3 mousePoint = mouseRay.GetPoint(distance);

            Vector3 direction = mousePoint - transform.position;
            direction.y = 0f;

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }
}
