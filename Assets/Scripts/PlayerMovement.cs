using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
  
 
    
    // referencia componente CharacterController
    private CharacterController controller;
    private PlayerStats playerStats;

    //referencia componente animator
    private Animator animator;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerStats = GetComponent<PlayerStats>();
        animator = GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //Variavel que armazena o valor lido do movimento do eixo horizontal
        float horizontal = Input.GetAxisRaw("Horizontal");
        
        //Variavel que armazena o valor lido do movimento do eixo vertical
        float vertical = Input.GetAxisRaw("Vertical");

        // (Vector3 é um tipo de dado (x,y,z)). Variavel que armazena o calculo da direção do movimento do jogador e (.normalized) mantem a diagonal na mesma velocidade
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        
        //defino que movementAmount = magnitude da variavel direction
        float movementAmount = direction.magnitude;

        //seto o float de animator"speed" com o valor de movementAmount
        animator.SetFloat("Speed", movementAmount);

        // Cálculo de movimento
        controller.Move(direction * Time.deltaTime * playerStats.moveSpeed);
    }
}
