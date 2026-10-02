using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public enum WAState
{
    Following,
    Attacking,
    Frozen
}

[RequireComponent(typeof(NavMeshAgent))]
public class WAController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private TextMeshProUGUI debugText;

    [Header("In-Game Debug Settings")]
    [SerializeField] private bool showDebugText = false;

    [Header("Settings")]
    [SerializeField] private float movementSpeed = 3.5f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("Weeping Angel Settings")]
    [SerializeField] private float freezeDelay = 0.5f; // Half-second movement window before freezing

    private NavMeshAgent agent;
    private Animator animator;
    private WAState state = WAState.Following;
    private bool isAttacking;
    private float freezeTimer; // Tracks how long the player has been looking

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Start()
    {
        agent.speed = movementSpeed;
    }

    private void Update()
    {
        var distanceToPlayer = Vector3.Distance(transform.position, player.position);
        bool playerIsLooking = IsPlayerLookingAtMe();

        // Weeping Angel freeze validation loop
        if (playerIsLooking)
        {
            if (state != WAState.Frozen)
            {
                freezeTimer += Time.deltaTime;

                // Only freeze once the half-second delay window has elapsed
                if (freezeTimer >= freezeDelay)
                {
                    state = WAState.Frozen;
                    FreezeEnemy();
                }
            }
        }
        else
        {
            // Reset the look-timer immediately when the player breaks line of sight
            freezeTimer = 0f;

            if (state == WAState.Frozen)
            {
                state = WAState.Following;
                UnfreezeEnemy();
            }
        }

        // Main behavioral state machine
        switch (state)
        {
            case WAState.Following:
                FollowPlayer();

                // CRITICAL FIX: Only attack if the player is NOT looking and the freeze timer hasn't started ticking
                if (distanceToPlayer <= attackRange && !playerIsLooking && freezeTimer == 0f)
                {
                    state = WAState.Attacking;
                    StartAttack();
                }
                break;

            case WAState.Frozen:
                // Locked perfectly in place. Absolutely no attacking allowed.
                break;

            case WAState.Attacking:
                Attack();

                if (!isAttacking && distanceToPlayer > attackRange)
                {
                    if (IsPlayerLookingAtMe())
                    {
                        freezeTimer = freezeDelay;
                        state = WAState.Frozen;
                        FreezeEnemy();
                    }
                    else
                    {
                        freezeTimer = 0f;
                        state = WAState.Following;
                        UnfreezeEnemy();
                    }
                }
                break;
        }

        UpdateAnimations();
        UpdateDebugUI(distanceToPlayer);
    }

    private void FollowPlayer()
    {
        agent.SetDestination(player.position);
    }

    private void FreezeEnemy()
    {
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (animator != null)
        {
            animator.speed = 0f;
        }
    }

    private void UnfreezeEnemy()
    {
        agent.isStopped = false;

        if (animator != null)
        {
            animator.speed = 1f;
        }
    }

    private void StartAttack()
    {
        agent.isStopped = true;
        isAttacking = true;
        animator.SetTrigger("Attack");
    }

    private void Attack()
    {
        agent.isStopped = true;
        var direction = (player.position - transform.position).normalized;
        direction.y = 0f;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void OnAttackAnimationEnd()
    {
        isAttacking = false;
    }

    private bool IsPlayerLookingAtMe()
    {
        if (playerCamera == null) return false;

        // 1. Calculate target center point (chest height)
        float heightOffset = 1f;
        if (agent != null) heightOffset = agent.height / 2f;
        Vector3 enemyCenterTarget = transform.position + Vector3.up * heightOffset;

        // 2. Convert to viewport coordinates
        Vector3 screenPoint = playerCamera.WorldToViewportPoint(enemyCenterTarget);

        // 3. FIX: Add peripheral padding (e.g., -0.1 to 1.1 instead of 0 to 1)
        // This catches them even if they are slightly off-screen or in your peripheral vision
        float padding = 0.1f;
        bool inViewport = screenPoint.z > 0 &&
                          screenPoint.x >= (0f - padding) && screenPoint.x <= (1f + padding) &&
                          screenPoint.y >= (0f - padding) && screenPoint.y <= (1f + padding);

        if (!inViewport) return false;

        // 4. Line of sight test
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = enemyCenterTarget - rayOrigin;

        if (Physics.Raycast(rayOrigin, rayDirection.normalized, out RaycastHit hit, rayDirection.magnitude, obstacleMask))
        {
            return false; // Hit a wall/obstacle before reaching the enemy
        }

        return true;
    }

    private void UpdateAnimations()
    {
        if (state == WAState.Frozen) return;

        bool isMoving = agent.velocity.sqrMagnitude > 0.01f;
        animator.SetBool("isWalking", false);
        animator.SetBool("isRunning", isMoving && state == WAState.Following);
    }

    private void UpdateDebugUI(float distanceToPlayer)
    {
        if (debugText == null) return;

        if (!showDebugText)
        {
            debugText.gameObject.SetActive(false);
            return;
        }

        debugText.gameObject.SetActive(true);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine($"<b>[WEEPING ANGEL DEBUG]</b>");
        sb.AppendLine($"State: <color=yellow>{state}</color>");
        sb.AppendLine($"Distance to Player: {distanceToPlayer:F2}m");

        bool looking = IsPlayerLookingAtMe();
        sb.AppendLine($"Spotted By Player: {(looking ? "<color=red>YES</color>" : "<color=green>NO</color>")}");

        if (looking && state != WAState.Frozen)
        {
            sb.AppendLine($"Freezing in: <color=orange>{(freezeDelay - freezeTimer):F2}s</color>");
        }

        debugText.text = sb.ToString();
    }
}
