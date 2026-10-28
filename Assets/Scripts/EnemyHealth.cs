
using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour
{
    public int maxHealth = 30;
    private int currentHealth;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => Mathf.Max(0, maxHealth);
    public event Action<int, int> HealthChanged;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = MaxHealth;
        EnemyHealthBar healthBar = GetComponent<EnemyHealthBar>();
        if (healthBar == null)
        {
            healthBar = gameObject.AddComponent<EnemyHealthBar>();
        }

        healthBar.Initialize(this);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    // Update is called once per frame
    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
        Debug.Log(gameObject.name + "Recebeu " + damage + "de dano. Vida atual: " + currentHealth);

        if (currentHealth == 0)
        {
            Destroy(gameObject);

        }
    }
}
