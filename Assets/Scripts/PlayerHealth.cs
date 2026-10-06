using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    //referencio PlayerStats em playerStats
    private PlayerStats playerStats;

    //crio uma variavel pra vida atual do player
    private int currentHealth;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        currentHealth = playerStats.maxHealth;

        Debug.Log("Você tem " + currentHealth + " de vida.");
        
    }

    // função TakeDamage que recebe um inteiro damage
    public void TakeDamage(int damage)
    {
        currentHealth -= damage;


        //if apenas para impedir que a vida fique negativa
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        Debug.Log("Você recebeu " + damage + " de dano. " + currentHealth + "pontos de vida restantes" );


        //if pra printar que vc morreu
        if (currentHealth == 0)
        {
            Debug.Log("Você está morto.");
        }
    }
}
