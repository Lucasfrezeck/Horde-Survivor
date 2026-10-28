using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    //referencio PlayerStats em playerStats
    private PlayerStats playerStats;

    //crio uma variavel pra vida atual do player
    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => playerStats != null ? Mathf.Max(0, playerStats.maxHealth) : 0;

    public event Action<int, int> HealthChanged;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerHealth precisa de um componente PlayerStats no mesmo objeto.", this);
            enabled = false;
            return;
        }

        currentHealth = Mathf.Max(0, playerStats.maxHealth);
        if (GetComponent<PlayerHealthHUD>() == null)
        {
            gameObject.AddComponent<PlayerHealthHUD>();
        }
        if (GetComponent<GameTimerHUD>() == null)
        {
            gameObject.AddComponent<GameTimerHUD>();
        }

        HealthChanged?.Invoke(currentHealth, MaxHealth);

        Debug.Log("Voc� tem " + currentHealth + " de vida.");
        
    }

    // fun��o TakeDamage que recebe um inteiro damage
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth -= damage;


        //if apenas para impedir que a vida fique negativa
        if (currentHealth < 0)
        {
            currentHealth = 0;
        }

        HealthChanged?.Invoke(currentHealth, MaxHealth);

        Debug.Log("Voc� recebeu " + damage + " de dano. " + currentHealth + "pontos de vida restantes" );


        //if pra printar que vc morreu
        if (currentHealth == 0)
        {
            Debug.Log("Voc� est� morto.");
        }
    }
}
