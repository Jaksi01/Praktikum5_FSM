using UnityEngine;
using UnityEngine.AI;

public class EnemyFSM : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Chase,
        Attack,
        Flee,
        Dead
    }

    [Header("Current State")]
    [SerializeField] private State currentState = State.Patrol;

    [Header("References")]
    [SerializeField] private EnemyPerception perception;
    [SerializeField] private EnemyHealth enemyHealth;
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private Transform player;
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private Transform safePoint;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float fleeSpeed = 4.5f;
    [SerializeField] private float patrolReachDistance = 0.5f;

    [Header("Combat")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float attackDamage = 10f;

    [Header("Flee")]
    [SerializeField] private float fleeHealthThreshold = 30f;

    [Header("Debug")]
    [SerializeField] private bool showPatrolDebug = true;

    private NavMeshAgent agent;
    private int patrolIndex;
    private float lastAttackTime = -999f;
    private bool isDead;
    private bool patrolDestinationSet;

    public State CurrentState => currentState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (perception == null)
            perception = GetComponent<EnemyPerception>();

        if (enemyHealth == null)
            enemyHealth = GetComponent<EnemyHealth>();

        if (player == null && perception != null)
            player = perception.Player;

        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<PlayerHealth>();
    }

    private void Start()
    {
        if (agent == null || perception == null ||
            enemyHealth == null || player == null)
        {
            Debug.LogError(
                "[EnemyFSM] Periksa NavMeshAgent, EnemyPerception, " +
                "EnemyHealth, dan referensi Player!",
                this
            );

            enabled = false;
            return;
        }

        if (playerHealth == null)
        {
            Debug.LogError(
                "[EnemyFSM] PlayerHealth tidak ditemukan pada Player!",
                this
            );

            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "[EnemyFSM] Enemy tidak berada di NavMesh!",
                this
            );

            enabled = false;
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            Debug.LogError(
                "[EnemyFSM] Patrol Points belum diisi!",
                this
            );

            enabled = false;
            return;
        }

        patrolIndex = 0;
        patrolDestinationSet = false;

        Debug.Log(
            "[EnemyFSM] FSM aktif. State awal: Patrol",
            this
        );

        EnterState(currentState);
    }

    private void Update()
    {
        if (isDead)
            return;

        if (enemyHealth.IsDead)
        {
            ChangeState(State.Dead);
            return;
        }

        if (enemyHealth.ShouldFlee &&
            currentState != State.Flee)
        {
            ChangeState(State.Flee);
            return;
        }

        switch (currentState)
        {
            case State.Patrol:
                UpdatePatrol();
                break;

            case State.Chase:
                UpdateChase();
                break;

            case State.Attack:
                UpdateAttack();
                break;

            case State.Flee:
                UpdateFlee();
                break;

            case State.Dead:
                UpdateDead();
                break;
        }
    }

    private void UpdatePatrol()
    {
        if (perception.CanSeePlayer())
        {
            ChangeState(State.Chase);
            return;
        }

        if (patrolPoints == null || patrolPoints.Length == 0)
            return;

        if (patrolIndex >= patrolPoints.Length)
            patrolIndex = 0;

        Transform destination = patrolPoints[patrolIndex];

        if (destination == null)
        {
            Debug.LogWarning(
                $"[Patrol] Waypoint index {patrolIndex} kosong!",
                this
            );

            patrolDestinationSet = false;
            return;
        }

        agent.speed = patrolSpeed;
        agent.isStopped = false;

        if (!patrolDestinationSet)
        {
            bool accepted =
                agent.SetDestination(destination.position);

            patrolDestinationSet = accepted;

            if (showPatrolDebug)
            {
                Debug.Log(
                    $"[Patrol] Tujuan: {destination.name}, " +
                    $"SetDestination: {accepted}",
                    this
                );
            }

            if (!accepted)
                return;
        }

        if (agent.pathPending)
            return;

        if (agent.pathStatus == NavMeshPathStatus.PathInvalid)
        {
            if (showPatrolDebug)
            {
                Debug.LogWarning(
                    $"[Patrol] Path tidak valid ke {destination.name}",
                    this
                );
            }

            patrolDestinationSet = false;
            return;
        }

        if (agent.pathStatus == NavMeshPathStatus.PathPartial)
        {
            if (showPatrolDebug)
            {
                Debug.LogWarning(
                    $"[Patrol] Path hanya sebagian menuju {destination.name}",
                    this
                );
            }

            return;
        }

        if (agent.hasPath &&
            agent.remainingDistance <=
            Mathf.Max(
                patrolReachDistance,
                agent.stoppingDistance
            ))
        {
            if (showPatrolDebug)
            {
                Debug.Log(
                    $"[Patrol] Sampai di {destination.name}",
                    this
                );
            }

            patrolIndex =
                (patrolIndex + 1) % patrolPoints.Length;

            patrolDestinationSet = false;
        }
    }

    private void UpdateChase()
    {
        if (!perception.CanSeePlayer())
        {
            ChangeState(State.Patrol);
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance <= attackRange)
        {
            ChangeState(State.Attack);
            return;
        }

        agent.speed = chaseSpeed;
        agent.isStopped = false;
        agent.SetDestination(player.position);
    }

    private void UpdateAttack()
    {
        if (!perception.CanSeePlayer())
        {
            ChangeState(State.Patrol);
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        if (distance > attackRange * 1.2f)
        {
            ChangeState(State.Chase);
            return;
        }

        agent.isStopped = true;
        FacePlayer();

        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;

            Debug.Log(
                $"[EnemyFSM] Enemy menyerang Player sebesar " +
                $"{attackDamage} damage.",
                this
            );

            // Mengurangi HP Player.
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(attackDamage);
            }
            else
            {
                Debug.LogWarning(
                    "[EnemyFSM] PlayerHealth belum terhubung!",
                    this
                );
            }
        }
    }

    private void UpdateFlee()
    {
        if (safePoint == null)
        {
            Debug.LogWarning(
                "[EnemyFSM] Safe Point belum diisi!",
                this
            );

            return;
        }

        agent.speed = fleeSpeed;
        agent.isStopped = false;
        agent.SetDestination(safePoint.position);

        if (!agent.pathPending &&
            agent.remainingDistance <= patrolReachDistance)
        {
            agent.isStopped = true;
        }
    }

    private void UpdateDead()
    {
        // Enemy tidak melakukan tindakan saat mati.
    }

    private void ChangeState(State newState)
    {
        if (currentState == newState)
            return;

        ExitState(currentState);
        currentState = newState;

        Debug.Log(
            $"[EnemyFSM] State berubah menjadi: {currentState}",
            this
        );

        EnterState(currentState);
    }

    private void EnterState(State state)
    {
        // Log tambahan untuk mengetahui state yang dimasuki.
        Debug.Log(
            $"[EnemyFSM] MASUK STATE: {state}",
            this
        );

        switch (state)
        {
            case State.Patrol:
                agent.isStopped = false;
                patrolDestinationSet = false;
                break;

            case State.Chase:
                agent.isStopped = false;
                break;

            case State.Attack:
                agent.isStopped = true;
                break;

            case State.Flee:
                agent.isStopped = false;
                break;

            case State.Dead:
                isDead = true;
                agent.isStopped = true;
                agent.ResetPath();
                agent.enabled = false;
                break;
        }
    }

    private void ExitState(State state)
    {
        if (state == State.Attack &&
            agent != null &&
            agent.enabled)
        {
            agent.isStopped = false;
        }
    }

    private void FacePlayer()
    {
        if (player == null)
            return;

        Vector3 direction =
            player.position - transform.position;

        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(direction);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 8f
            );
        }
    }
}
