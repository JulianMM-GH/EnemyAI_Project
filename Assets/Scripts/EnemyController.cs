using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

public enum EnemyState
{
    Patrolling,
    Following,
    Attacking
}

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [SerializeField] private Transform[] patrolPoints;

    [SerializeField] private TextMeshProUGUI debugText;

    [Header("In-Game Debug Settings")]
    [SerializeField] private bool showDebugText = false;

    [SerializeField] private bool showVisionGizmos = false;

    public int arcSegments = 30;

    public LayerMask obstacleMask;

    private MeshFilter visionMeshFilter;
    private Mesh visionMesh;

    public Material visionMaterial;
    public Material visionFollowingMaterial;

    [Header("Enemy Vision")]
    [SerializeField] private float detectionRange = 10f;
    [Range(0, 360)] public float viewAngle = 90f;

    [Header("Settings")]
    [SerializeField] private float patrolSpeed = 3.5f;
    [SerializeField] private float chaseSpeed = 6.0f;
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float stopAtDistance = 0.5f;
    [SerializeField] private float losePlayerTime = 3f;
    [SerializeField] private float attackRange = 1.2f;

    private NavMeshAgent agent;
    private Animator animator;
    private EnemyState state = EnemyState.Patrolling;
    private int currentPatrolIndex;
    private bool isWaiting;
    private float timeSinceLostPlayer;
    private bool isAttacking;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        agent.speed = patrolSpeed;

        GoToNextPatrolPoint();

        visionMeshFilter = GetComponent<MeshFilter>();
        visionMesh = new Mesh();
        visionMesh.name = "Vision Cone Mesh";
        visionMeshFilter.mesh = visionMesh;

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (visionMaterial != null)
        {
            meshRenderer.material = visionMaterial;
        }
        else
        {
            Debug.LogWarning("Please assign a Vision Material");
        }
    }

    private void Update()
    {
        var distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (state)
        {
            case EnemyState.Patrolling:
                Patrol();
                if (distanceToPlayer <= detectionRange && CanSeePlayer())
                {
                    state = EnemyState.Following;
                    agent.speed = chaseSpeed;
                }

                break;

            case EnemyState.Following:
                FollowPlayer();
                
                if (distanceToPlayer <= attackRange)
                {
                    state = EnemyState.Attacking;
                    StartAttack();
                }

                if (!CanSeePlayer())
                {
                    timeSinceLostPlayer += Time.deltaTime;
                    if (timeSinceLostPlayer >= losePlayerTime)
                    {
                        state = EnemyState.Patrolling;
                        agent.speed = patrolSpeed;
                        GoToClosestPatrolPoint();
                    }
                }
                else
                {
                    timeSinceLostPlayer = 0f;
                }
                break;

            case EnemyState.Attacking:
                Attack();
                if (!isAttacking && distanceToPlayer > attackRange)
                {
                    state = EnemyState.Following;
                    agent.speed = chaseSpeed;
                    agent.isStopped = false;
                }
                break;

        }
        UpdateAnimations();

        UpdateDebugUI(distanceToPlayer);

        UpdateVisionMesh();
    }

    private void FollowPlayer()
    {
        agent.SetDestination(player.position);
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

    // Used by animation event
    private void OnAttackAnimationEnd()
    {
        isAttacking = false;
    }

    private void Patrol()
    {
        if (isWaiting) return;

        if (!agent.pathPending && agent.remainingDistance <= stopAtDistance)
        {
            StartCoroutine(WaitAtPatrolPoint());
        }
    }

    private IEnumerator WaitAtPatrolPoint()
    {
        isWaiting = true;
        agent.isStopped = true;

        yield return new WaitForSeconds(patrolWaitTime);

        agent.isStopped = false;
        GoToNextPatrolPoint();
        isWaiting = false;
    }

    private void GoToNextPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;

        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void GoToClosestPatrolPoint()
    {
        if (patrolPoints.Length == 0) return;
        var closestIndex = 0;
        var closestDistance = float.MaxValue;

        for (var i = 0; i < patrolPoints.Length; i++)
        {
            var distance = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        currentPatrolIndex = closestIndex;
        agent.SetDestination(patrolPoints[currentPatrolIndex].position);
    }

    private bool CanSeePlayer()
    {
        return IsFacingPlayer() && HasClearPathToPlayer();
    }

    private bool IsFacingPlayer()
    {
        var dirToPlayer = (player.position - transform.position).normalized;
        var angle = Vector3.Angle(transform.forward, dirToPlayer); 
        return angle <= viewAngle / 2f;
    }

    private bool HasClearPathToPlayer()
    {
        var dirToPlayer = player.position - transform.position;
        if (Physics.Raycast(transform.position, dirToPlayer.normalized, out RaycastHit hit, dirToPlayer.magnitude))
        {
            return hit.transform == player;
        }

        return true;
    }

    private void UpdateAnimations()
    {
        bool isMoving = agent.velocity.sqrMagnitude > 0.01f;

        animator.SetBool("isWalking", isMoving && state == EnemyState.Patrolling);
        animator.SetBool("isRunning", isMoving && state == EnemyState.Following);
    }

    private void UpdateDebugUI(float distanceToPlayer)
    {
        // If there isn't a text box to put the info into, return
        if (debugText == null) return;

        if (!showDebugText)
        {
            debugText.gameObject.SetActive(false);
        }
        else
        {
            debugText.gameObject.SetActive(true);
        }

            // Instantiates a clean, efficient text string worker
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

        sb.AppendLine($"<b>[ENEMY DEBUG]</b>");
        sb.AppendLine($"State: <color=yellow>{state}</color>");
        sb.AppendLine($"Distance to Player: {distanceToPlayer:F2}m");
        sb.AppendLine($"Can See Player: {(CanSeePlayer() ? "<color=green>YES</color>" : "<color=red>NO</color>")}");

        // Displays what point the AI is currently moving towards and if it is currently waiting at a point
        if (state == EnemyState.Patrolling)
        {
            sb.AppendLine($"Patrol Index: {currentPatrolIndex}");
            sb.AppendLine($"Is Waiting at Point: {isWaiting}");
        }

        // Displays when the AI is chasing the player
        // When line of sight with the player is lost, the timer that counts until the AI reverts to patrolling can now be observed 
        else if (state == EnemyState.Following)
        {
            sb.AppendLine($"Time Since Lost Player: {timeSinceLostPlayer:F1}s / {losePlayerTime}s");
        }

        // Displays if the AI is currently attacking
        else if (state == EnemyState.Attacking)
        {
            sb.AppendLine($"Is Currently Attacking: {isAttacking}");
        }

        debugText.text = sb.ToString();
    }

    // Vision cone that shows where Enemy is looking
    private void UpdateVisionMesh()
    {
        if (visionMeshFilter == null || visionMesh == null) return;

        // Automatically swap the material depending on whether the enemy is chasing the player
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            if (state == EnemyState.Following || state == EnemyState.Attacking) // Following / Attacking states
            {
                if (visionFollowingMaterial != null && meshRenderer.sharedMaterial != visionFollowingMaterial)
                {
                    meshRenderer.material = visionFollowingMaterial;
                }
            }
            else // Patrolling state
            {
                if (visionMaterial != null && meshRenderer.sharedMaterial != visionMaterial)
                {
                    meshRenderer.material = visionMaterial;
                }
            }
        }

        if (!showVisionGizmos)
        {
            visionMesh.Clear();
            return;
        }

        int vertexCount = arcSegments + 2;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[arcSegments * 3];

        // Origin tracking in local space
        vertices[0] = new Vector3(0, 0.05f, 0);

        float startAngle = -viewAngle / 2f;
        float endAngle = viewAngle / 2f;

        for (int i = 0; i <= arcSegments; i++)
        {
            float t = (float)i / arcSegments;
            float segmentAngle = Mathf.Lerp(startAngle, endAngle, t);

            // 1. Calculate the local angle vector
            float rad = segmentAngle * Mathf.Deg2Rad;
            float x = Mathf.Sin(rad);
            float z = Mathf.Cos(rad);

            // 2. Convert to global direction for the raycast
            Vector3 globalDir = transform.TransformDirection(new Vector3(x, 0, z));
            Vector3 rayOrigin = transform.position + Vector3.up * 0.05f;

            // 3. Establish a default target point in World Space (assuming no wall hit)
            Vector3 targetWorldPoint = rayOrigin + globalDir * detectionRange;

            // 4. Perform the raycast
            if (Physics.Raycast(rayOrigin, globalDir, out RaycastHit hit, detectionRange, obstacleMask))
            {
                // Hit detected! Pull the point back by a tiny fraction (0.02f) to prevent Z-fighting bleed
                targetWorldPoint = hit.point - (globalDir * 0.02f);
            }

            // 5. Convert the world space hit point SAFELY back into local space for the vertex array
            Vector3 localTargetPoint = transform.InverseTransformPoint(targetWorldPoint);

            // Assign the converted coordinates to your vertex structure
            vertices[i + 1] = new Vector3(localTargetPoint.x, vertices[0].y, localTargetPoint.z);

            // 6. Define triangles
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