// First attempt at a follower horror enemy
// Google AI made the modifications to existing enemycontroller, results are something to learn from, but not worthwhile to modify and refine
// Need to reevaluate approach


using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public enum StalkerState
{
    Following,
    Hiding,
    Attacking
}

public class StalkerEnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Camera playerCamera;

    [SerializeField] private Transform[] hidingSpots;

    [SerializeField] private TextMeshProUGUI debugText;

    [Header("In-Game Debug Settings")]
    [SerializeField] private bool showDebugText = true;
    [SerializeField] private bool showVisionGizmos = true;

    public int arcSegments = 30;

    public LayerMask obstacleMask;

    private MeshFilter visionMeshFilter;
    private Mesh visionMesh;

    public Material visionMaterial;
    public Material visionFollowingMaterial;

    [Header("Enemy Vision")]
    [SerializeField] private float detectionRange = 15f;
    [Range(0, 360)] public float viewAngle = 90f;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float stopAtDistance = 0.5f;

    [Header("Stalker Settings")]
    [SerializeField] private float normalSpeed = 3.5f;
    [SerializeField] private float panicSpeed = 7f;

    [SerializeField] private float stalkBehindDistance = 2f; // How far behind the player it tries to stay

    [SerializeField] private float hideTime = 8f; // How long to hide before pursuing the player again

    [Range(0, 180)][SerializeField] private float playerFieldOfView = 60f; // Player's visual cone width


    private NavMeshAgent agent;
    private Animator animator;
    private StalkerState state = StalkerState.Following;
    private bool isAttacking;
    private float hideTimer;
    private Transform targetHidingSpot;
    private Collider enemyCollider;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        enemyCollider = GetComponent<Collider>();
    }

    private void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main;
        }

        visionMeshFilter = GetComponent<MeshFilter>();
        visionMesh = new Mesh { name = "Vision Cone Mesh" };
        visionMeshFilter.mesh = visionMesh;

        agent.speed = normalSpeed;
        UpdateMaterial();
    }

    private void Update()
    {
        var distanceToPlayer = Vector3.Distance(transform.position, player.position);

        // Core Stalker Mechanic: Immediately check if caught
        if (state != StalkerState.Hiding && IsPlayerLookingAtMe())
        {
            TriggerRetreat();
            return;
        }

        switch (state)
        {
            case StalkerState.Following:
                FollowPlayerBehind();

                if (distanceToPlayer <= attackRange)
                {
                    state = StalkerState.Attacking;
                    StartAttack();
                }
                break;

            case StalkerState.Hiding:
                ExecuteHidingLogic();
                break;

            case StalkerState.Attacking:
                Attack();
                if (!isAttacking && distanceToPlayer > attackRange)
                {
                    state = StalkerState.Following;
                    agent.isStopped = false;
                }
                break;
        }

        UpdateAnimations();
        UpdateDebugUI(distanceToPlayer);
        UpdateVisionMesh();
    }

    private void FollowPlayerBehind()
    {
        agent.isStopped = false;
        agent.updateRotation = true; // Normal tracking rotation
        agent.speed = normalSpeed;

        Vector3 targetBehindPosition = player.position - (player.forward * stalkBehindDistance);

        if (NavMesh.SamplePosition(targetBehindPosition, out NavMeshHit navHit, stalkBehindDistance * 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(navHit.position);
        }
        else
        {
            agent.SetDestination(player.position);
        }
    }

    private void TriggerRetreat()
    {
        state = StalkerState.Hiding;
        hideTimer = 0f;

        agent.speed = panicSpeed;
        agent.updateRotation = false; // Disable standard pathfinding auto-rotation so we can force backwards styling

        targetHidingSpot = FindBestHidingSpot();
        if (targetHidingSpot != null)
        {
            agent.isStopped = false;
            agent.SetDestination(targetHidingSpot.position);
        }
        UpdateMaterial();
    }

    private void ExecuteHidingLogic()
    {
        // Force the enemy to face the player directly while stepping backward
        Vector3 faceDirection = (player.position - transform.position).normalized;
        faceDirection.y = 0f;
        if (faceDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(faceDirection);
        }

        if (!agent.pathPending && agent.remainingDistance <= stopAtDistance)
        {
            // Once safely arrived at the destination node, normalize parameters
            agent.updateRotation = true;
            agent.speed = normalSpeed;

            hideTimer += Time.deltaTime;
            if (hideTimer >= hideTime)
            {
                state = StalkerState.Following;
                UpdateMaterial();
            }
        }
    }

    private Transform FindBestHidingSpot()
    {
        if (hidingSpots.Length == 0) return null;

        Transform bestSpot = null;
        float closestDistance = float.MaxValue;

        foreach (var spot in hidingSpots)
        {
            float dist = Vector3.Distance(transform.position, spot.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                bestSpot = spot;
            }
        }
        return bestSpot;
    }

    private bool IsPlayerLookingAtMe()
    {
        Vector3 dirToEnemy = (transform.position - player.position).normalized;
        Vector3 playerForward = playerCamera != null ? playerCamera.transform.forward : player.forward;

        float angle = Vector3.Angle(playerForward, dirToEnemy);

        if (angle <= playerFieldOfView)
        {
            Vector3 rayOrigin = playerCamera != null ? playerCamera.transform.position : player.position + Vector3.up * 1f;
            Vector3 targetCenter = enemyCollider != null ? enemyCollider.bounds.center : transform.position + Vector3.up * 1f;
            Vector3 rayDir = targetCenter - rayOrigin;

            float maxRayDistance = Vector3.Distance(rayOrigin, targetCenter) + 1f;

            if (Physics.Raycast(rayOrigin, rayDir.normalized, out RaycastHit hit, maxRayDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private void StartAttack()
    {
        agent.isStopped = true;
        agent.updateRotation = true;
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

    private void UpdateAnimations()
    {
        // Restore standard global animation speed
        animator.speed = 1f;

        float currentVelocityMagnitude = agent.velocity.sqrMagnitude;
        bool isMoving = currentVelocityMagnitude > 0.01f;

        animator.SetBool("isWalking", isMoving);

        if (isMoving)
        {
            if (state == StalkerState.Hiding)
            {
                // Passing a negative value to a Blend Tree or Multiplier parameter keeps the motor state moving smoothly backward
                animator.SetFloat("VerticalSpeed", -1f);
            }
            else
            {
                animator.SetFloat("VerticalSpeed", 1f);
            }
        }
        else
        {
            animator.SetFloat("VerticalSpeed", 0f);
        }
    }

    private void UpdateMaterial()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null) return;

        if (state == StalkerState.Following || state == StalkerState.Attacking)
        {
            if (visionFollowingMaterial != null) meshRenderer.material = visionFollowingMaterial;
        }
        else
        {
            if (visionMaterial != null) meshRenderer.material = visionMaterial;
        }
    }

    private void UpdateDebugUI(float distanceToPlayer)
    {
        if (debugText == null) return;
        debugText.gameObject.SetActive(showDebugText);
        if (!showDebugText) return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"<b>[STALKER DEBUG]</b>");
        sb.AppendLine($"State: <color=red>{state}</color>");
        sb.AppendLine($"Distance: {distanceToPlayer:F2}m");
        sb.AppendLine($"Spotted by Player: {(IsPlayerLookingAtMe() ? "<color=red>YES!</color>" : "<color=green>NO</color>")}");

        if (state == StalkerState.Hiding)
        {
            sb.AppendLine($"Retreating to: {targetHidingSpot?.name}");
            sb.AppendLine($"Hide Timer: {hideTimer:F1}s / {hideTime}s");
        }

        debugText.text = sb.ToString();
    }

    private void UpdateVisionMesh()
    {
        if (visionMeshFilter == null || visionMesh == null) return;

        if (!showVisionGizmos)
        {
            visionMesh.Clear();
            return;
        }

        int vertexCount = arcSegments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[arcSegments * 3];

        vertices[0] = new Vector3(0, 0.05f, 0);
        float startAngle = -viewAngle / 2f;
        float endAngle = viewAngle / 2f;

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = (float)i / arcSegments;
            float segmentAngle = Mathf.Lerp(startAngle, endAngle, t);

            float rad = segmentAngle * Mathf.Deg2Rad;
            float x = Mathf.Sin(rad);
            float z = Mathf.Cos(rad);

            Vector3 globalDir = transform.TransformDirection(new Vector3(x, 0, z));
            float currentDistance = detectionRange;
            Vector3 rayOrigin = transform.position + Vector3.up * 0.05f;

            if (Physics.Raycast(rayOrigin, globalDir, out RaycastHit hit, detectionRange, obstacleMask))
            {
                currentDistance = hit.distance;
            }

            vertices[i + 1] = new Vector3(x * currentDistance, vertices[0].y, z * currentDistance);

            if (i < arcSegments)
            {
                int triangleIndexOffset = i * 3;
                triangles[triangleIndexOffset] = 0;
                triangles[triangleIndexOffset + 1] = i + 1;
                triangles[triangleIndexOffset + 2] = i + 2;
            }
        }

        visionMesh.Clear();
        visionMesh.vertices = vertices;
        visionMesh.triangles = triangles;
        visionMesh.RecalculateBounds();
        visionMesh.RecalculateNormals();
    }
}