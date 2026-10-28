using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
  
 
    
    // referencia componente CharacterController
    private CharacterController controller;
    private PlayerStats playerStats;

    //referencia componente animator
    private Animator animator;

    // Velocidade angular da virada, em graus por segundo.
    [SerializeField] private float turnSpeed = 540f;
    [SerializeField] private float referenceMoveSpeed = 7.5f;
    [SerializeField] private float runAnimationSpeed = 1.2f;

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

        // Vector3 indica a direção do movimento, e normalized mantém a mesma velocidade nas diagonais.
        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;
        
        //defino que movementAmount = magnitude da variavel direction
        float movementAmount = direction.magnitude;

        //seto o float de animator"speed" com o valor de movementAmount
        if (animator != null)
        {
            animator.SetFloat("Speed", movementAmount);
            animator.speed = movementAmount > 0f
                ? runAnimationSpeed * playerStats.moveSpeed / Mathf.Max(referenceMoveSpeed, 0.01f)
                : 1f;
        }

        if (movementAmount > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime);
        }

        // Cálculo de movimento
        controller.Move(direction * Time.deltaTime * playerStats.moveSpeed);
    }
}
