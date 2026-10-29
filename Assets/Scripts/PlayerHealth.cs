using UnityEngine;
using System;

public class PlayerHealth : MonoBehaviour
{
    private static readonly int HitParameter = Animator.StringToHash("Hit");
    private static readonly int HitVariantParameter = Animator.StringToHash("HitVariant");
    private static readonly int HitHeadState = Animator.StringToHash("Hit Reaction.Armature|Hit_Head");
    private static readonly int HitChestState = Animator.StringToHash("Hit Reaction.Armature|Hit_Chest");
    private const int HitReactionLayer = 2;

    // Referencia os atributos e as animações do jogador.
    private PlayerStats playerStats;
    private Animator animator;
    private int currentHealth;
    private bool isHitReactionPlaying;
    private bool hitAnimationStarted;
    private float hitReactionStartedAt;

    public int CurrentHealth => currentHealth;
    public int MaxHealth => playerStats != null ? Mathf.Max(0, playerStats.maxHealth) : 0;

    public event Action<int, int> HealthChanged;

    private void Start()
    {
        playerStats = GetComponent<PlayerStats>();
        animator = GetComponentInChildren<Animator>();
        if (playerStats == null)
        {
            Debug.LogError("PlayerHealth precisa de um componente PlayerStats no mesmo objeto.", this);
            enabled = false;
            return;
        }

        currentHealth = Mathf.Max(0, playerStats.maxHealth);
        if (animator != null && animator.layerCount > HitReactionLayer)
        {
            animator.SetLayerWeight(HitReactionLayer, 0f);
        }

        if (GetComponent<PlayerHealthHUD>() == null)
        {
            gameObject.AddComponent<PlayerHealthHUD>();
        }
        if (GetComponent<GameTimerHUD>() == null)
        {
            gameObject.AddComponent<GameTimerHUD>();
        }
        if (GetComponent<KillCounterHUD>() == null)
        {
            gameObject.AddComponent<KillCounterHUD>();
        }

        HealthChanged?.Invoke(currentHealth, MaxHealth);
        Debug.Log("Você tem " + currentHealth + " de vida.");
    }

    private void Update()
    {
        if (!isHitReactionPlaying)
        {
            return;
        }

        if (TryGetHitReactionProgress(out float normalizedTime))
        {
            hitAnimationStarted = true;
            if (normalizedTime >= 0.95f)
            {
                FinishHitReaction();
            }
        }
        else if (hitAnimationStarted || Time.time >= hitReactionStartedAt + 0.75f)
        {
            FinishHitReaction();
        }
    }

    public void TakeDamage(int damage)
    {
        if (damage <= 0 || currentHealth <= 0)
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);
        HealthChanged?.Invoke(currentHealth, MaxHealth);
        PlayHitReaction();

        Debug.Log("Você recebeu " + damage + " de dano. " + currentHealth + " pontos de vida restantes.");
        if (currentHealth == 0)
        {
            Debug.Log("Você está morto.");
        }
    }

    private void PlayHitReaction()
    {
        if (animator == null || animator.layerCount <= HitReactionLayer)
        {
            return;
        }

        animator.SetInteger(HitVariantParameter, UnityEngine.Random.Range(0, 2));
        animator.ResetTrigger(HitParameter);
        animator.SetTrigger(HitParameter);
        animator.SetLayerWeight(HitReactionLayer, 1f);

        isHitReactionPlaying = true;
        hitAnimationStarted = false;
        hitReactionStartedAt = Time.time;
    }

    private bool TryGetHitReactionProgress(out float normalizedTime)
    {
        normalizedTime = 0f;
        if (animator.IsInTransition(HitReactionLayer))
        {
            AnimatorStateInfo nextState = animator.GetNextAnimatorStateInfo(HitReactionLayer);
            if (IsHitReactionState(nextState.fullPathHash))
            {
                normalizedTime = nextState.normalizedTime;
                return true;
            }
        }

        AnimatorStateInfo currentState = animator.GetCurrentAnimatorStateInfo(HitReactionLayer);
        if (IsHitReactionState(currentState.fullPathHash))
        {
            normalizedTime = currentState.normalizedTime;
            return true;
        }

        return false;
    }

    private static bool IsHitReactionState(int stateHash)
    {
        return stateHash == HitHeadState || stateHash == HitChestState;
    }

    private void FinishHitReaction()
    {
        animator.SetLayerWeight(HitReactionLayer, 0f);
        isHitReactionPlaying = false;
        hitAnimationStarted = false;
    }
}
