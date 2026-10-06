using UnityEngine;

public class PlayerAutoAttack : MonoBehaviour
{
    //guarda o tmepo pro proximo ataque
    private float nextAttackTime;


    //serializeField permite que eu consiga selecionar um valor no inspector apesar de ser private
    [SerializeField] private Transform attackOrigin;
    
    //referencio PlayerStats como playerStats
    private PlayerStats playerStats;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    // Update is called once per frame
    void Update()
    {

        //se o attackSpeed for menor ou igual a 0 o ataque nao ocorre
        if (playerStats.attackSpeed <= 0f)
        {
            return;

        }

        //se o nextAttackTime for maior que o tempo decorrido da sessão, ele esepra até o prazo
        if (nextAttackTime > Time.time)
        {

            return;
        }

        //determina que o prazo do proximo ataque é o tempo decorrido + 1 / attackspeed
            nextAttackTime = Time.time + (1.0f / playerStats.attackSpeed);
            TryAttack();
    }
    
    //função de ataque
    void TryAttack()
        {
            Debug.Log("AutoAttack!");
        }





}

