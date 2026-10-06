using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    //adiciona o componente Transform de um "target", que no caso é o Player
    public Transform target;

    //cria a referencia pra camera
    private Camera cameraComponent;

    //define os valores de Maior e menor angulo de visão
    public float maxFov = 70f; 
    public float minFov = 25f;
    
    //sensibilidade do zoom
    public float zoomSpeed = 20f;

    //cria a variavel Vector3 do offset da camera
    public Vector3 offset = new Vector3(0f, 9f, -6f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //inicializa o cameraComponent, apontando pra camera uma vez que o script é iniciado
        cameraComponent = GetComponent<Camera>();
    }

    void Update()
    {
        //Lê todo frame o input da ScrollWheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        //faz o calculo do novo fov (primeiro multiplicação)
        float newFov = cameraComponent.fieldOfView - scroll * zoomSpeed;

        //limita a contacom Mathf.Clamp(valor, valorMin, valorMax)
        cameraComponent.fieldOfView = Mathf.Clamp(newFov, minFov, maxFov);
    }


    // Update is called once per frame
    void LateUpdate()
    {
        //transform.position da camera vira target.position + o offset pré-definido
        transform.position = target.position + offset;
    }
}
