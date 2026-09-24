using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class EnemyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform[] patrolPoints;

    [Header("Settings")]
    [SerializeField] private float patrolWaitTime = 2f;
    [SerializeField] private float stopAtDistance = 0.5f;

    private NavMeshAgent agent;
    private Animator animator;
    private int currentPatrolIndex;
    private bool isWaiting;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        GoToNextPatrolPoint();
    }

    private void Update()
    {
        Patrol();
        UpdateAnimations();
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

    private void UpdateAnimations()
    {
        var isWalking = agent.velocity.sqrMagnitude > 0.01f;
        animator.SetBool("isWalking", isWalking);
    }
}
