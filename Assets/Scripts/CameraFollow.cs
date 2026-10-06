using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    //adiciona o componente Transform de um "target", que no caso é o Player
    public Transform target;

    //cria a variavel Vector3 do offset da camera
    public Vector3 offset = new Vector3(0f, 9f, -6f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //transform.position da camera vira target.position + o offset pré-definido
        transform.position = target.position + offset;
    }
}
