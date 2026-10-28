using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;

// 플레이어의 이동, 회전, 점프, 낙하/착지, 체력(데미지)을 담당하는 스크립트 (Photon 멀티플레이 대응)
public class PlayerMovement : MonoBehaviour
{
    [Header("Player Health")]
    const float maxHealth = 150f;       // 최대 체력
    public float currentHealth;         // 현재 체력
    public Slider healthbarSlider;      // 체력바 UI
    public GameObject playerUI;         // 플레이어 UI 오브젝트 (내 캐릭터에서만 사용)

    [Header("Ref & Physics")]
    InputManager inputManager;                          // 키 입력 값을 가져오는 스크립트
    Vector3 moveDirection;                              // 최종 이동 방향(속도 포함)
    Transform cameragameObject;                         // 메인 카메라 (이동/회전 방향의 기준)
    Rigidbody playerRigidbody;                          // 물리 이동에 사용하는 Rigidbody
    PlayerManager playerManager;                        // 상호작용(애니메이션 재생 중) 상태 등을 관리
    PlayerControllerManager playerControllerManager;    // 플레이어 생성/사망을 관리하는 매니저
    AnimatorManager animatorManager;                    // 애니메이션 제어 스크립트
    [Header("Falling and Landing")]
    public float inAirTimer;            // 공중에 떠 있는 시간
    public float leapingVelocity;       // 공중에서 앞으로 밀어주는 힘
    public float fallingVelocity;       // 떨어질 때 아래로 가하는 힘
    public float rayCastHeightOffset = 0.5f;    // 바닥 검사를 시작할 높이 보정값 (발 위쪽에서 시작)
    public LayerMask groundLayer;               // 바닥으로 인식할 레이어

    [Header("Movement flags")]
    public bool isMoving;       // 걷는 중인지
    public bool isSprinting;    // 달리는 중인지
    public bool isGrounded;     // 땅에 닿아 있는지
    public bool isJumping;      // 점프 중인지

    [Header("Movement values")]
    public float movementSpeed = 2f;    // 걷기 속도
    public float rotationSpeed = 13f;   // 캐릭터 회전 속도
    public float sprintingSpeed = 7f;   // 달리기 속도

    [Header("Jump Var")]
    public float jumpHeight = 4f;           // 점프 높이
    public float gravityIntensity = -15f;   // 점프 속도 계산에 쓰는 중력 값

    PhotonView view;    // 이 캐릭터가 내 것인지 판별하는 Photon 컴포넌트



    void Awake()
    {
        // 체력 초기화
        currentHealth = maxHealth;
        // 같은 오브젝트의 컴포넌트들을 가져옴
        inputManager = GetComponent<InputManager>();
        playerRigidbody = GetComponent<Rigidbody>();
        playerManager = GetComponent<PlayerManager>();
        animatorManager = GetComponent<AnimatorManager>();
        cameragameObject = Camera.main.transform;
        view = GetComponent<PhotonView>();

        // 생성될 때 전달받은 PhotonView ID로 PlayerControllerManager를 찾음
        playerControllerManager = PhotonView.Find((int)view.InstantiationData[0]).GetComponent<PlayerControllerManager>();

        // 체력바 슬라이더 초기 설정
        healthbarSlider.minValue = 0f;
        healthbarSlider.maxValue = maxHealth;
        healthbarSlider.value = currentHealth;
    }
    void Start()
    {
        // 내 캐릭터가 아니면 Rigidbody와 UI를 제거 (다른 플레이어의 물리/UI는 내가 처리하지 않음)
        if (!view.IsMine)
        {
            Destroy(playerRigidbody);
            Destroy(playerUI);
        }
    }


    // 외부(PlayerManager)에서 호출하는 이동 처리 진입점
    public void HandleAllMovement()
    {
        HandleFallingAndLanding();      // 낙하/착지는 항상 처리
        if (playerManager.isInteracting)    // 애니메이션 재생 중이면 이동/회전은 건너뜀
            return;
        HandleMovement();
        HandleRotation();
    }
    void HandleMovement()
    {
        // 점프 중에는 공중 방향 전환을 하지 않음
        if (isJumping)
            return;

        // 카메라가 바라보는 방향(수평)을 기준으로 이동 방향 계산
        moveDirection = new Vector3(cameragameObject.forward.x, 0f, cameragameObject.forward.z) * inputManager.verticalInput;
        moveDirection = moveDirection + cameragameObject.right * inputManager.horizontalInput;
        moveDirection.Normalize();      // 대각선 이동이 더 빨라지지 않게 길이를 1로

        moveDirection.y = 0;

        if (isSprinting)
        {
            moveDirection = moveDirection * sprintingSpeed;     // 달리기 속도 적용
        }
        else
        {
            // 입력이 어느 정도 있으면 걷기 속도 적용
            if (inputManager.movementAmount >= 0.5f)
            {
                moveDirection = moveDirection * movementSpeed;
                isMoving = true;
            }
            // 입력이 없으면 멈춘 상태로 처리
            if (inputManager.movementAmount <= 0f)
            {
                isMoving = false;
            }
        }

        Vector3 movementVelocity = moveDirection;

        // y속도는 기존 값을 유지 (중력/점프가 덮어써지지 않게)
        movementVelocity.y = playerRigidbody.linearVelocity.y;

        playerRigidbody.linearVelocity = movementVelocity;

    }

    void HandleRotation()
    {
        // 점프 중에는 회전하지 않음
        if (isJumping)
            return;
        Vector3 targetDirection = Vector3.zero;

        // 카메라 기준 입력 방향 계산
        targetDirection = cameragameObject.forward * inputManager.verticalInput;
        targetDirection = targetDirection + cameragameObject.right * inputManager.horizontalInput;
        targetDirection.y = 0;      // 위아래 기울어짐 제거
        targetDirection.Normalize();

        // 입력이 없으면 회전하지 않음 (LookRotation에 zero 벡터를 넣지 않기 위해)
        if (targetDirection == Vector3.zero)
            return;

        // 목표 방향으로 부드럽게 회전
        Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
        Quaternion playerRotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        transform.rotation = playerRotation;
    }
    void HandleFallingAndLanding()
    {
        RaycastHit hit;
        Vector3 rayCastOrigin = transform.position;
        Vector3 targetPosition;

        // 발 위쪽에서 아래로 바닥 검사를 시작
        rayCastOrigin.y = rayCastOrigin.y + rayCastHeightOffset;
        targetPosition = transform.position;

        // 공중에 있고 점프 중이 아니면 = 떨어지는 중
        if (!isGrounded && !isJumping)
        {
            if (!playerManager.isInteracting)
            {
                animatorManager.PlayTargetAnim("Falling", true);    // 낙하 애니메이션
            }
            inAirTimer = inAirTimer + Time.deltaTime;   // 공중 시간 누적
            playerRigidbody.AddForce(transform.forward * leapingVelocity);                  // 앞으로 살짝 밀어줌
            playerRigidbody.AddForce(-Vector3.up * fallingVelocity * inAirTimer);           // 시간이 지날수록 더 빨리 떨어짐

        }
        // 아래로 구를 쏴서 바닥 검사
        // (참고: 5번째 인자 자리는 maxDistance라서, 여기 들어간 groundLayer가 레이어 마스크로 적용되지 않을 수 있음)
        if (Physics.SphereCast(rayCastOrigin, 0.2f, -Vector3.up, out hit, groundLayer))
        {
            // 공중에 있다가 바닥에 닿는 순간이면 착지 애니메이션
            if (!isGrounded && !playerManager.isInteracting)
            {
                animatorManager.PlayTargetAnim("Landing", true);
            }
            Vector3 rayCastHitPoint = hit.point;
            targetPosition.y = rayCastHitPoint.y;   // 바닥 높이를 목표 높이로
            inAirTimer = 0;                         // 공중 시간 초기화
            isGrounded = true;

        }
        else
        {
            isGrounded = false;
        }
        // 땅 위에 있을 때 캐릭터 높이를 바닥에 맞춤
        if (isGrounded && !isJumping)
        {
            if (playerManager.isInteracting || inputManager.movementAmount > 0)
            {
                // 움직이는 중이면 부드럽게 보정
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime / 0.1f);
            }
            else
            {
                // 멈춰 있으면 바로 바닥에 고정
                transform.position = targetPosition;
            }
        }
    }
    // 점프 처리 (InputManager에서 점프 입력 시 호출)
    public void HandleJumping()
    {
        if (isGrounded)
        {
            animatorManager.animator.SetBool("isJumping", true);
            animatorManager.PlayTargetAnim("Jump", false);

            // 원하는 점프 높이에 필요한 초기 속도 계산 (v = √(-2 * g * h))
            float jumpingVelocity = Mathf.Sqrt(-2 * gravityIntensity * jumpHeight);
            Vector3 playerVelocity = moveDirection;     // 점프 직전의 이동 방향을 유지
            playerVelocity.y = jumpingVelocity;
            playerRigidbody.linearVelocity = playerVelocity;

            isJumping = false;
        }
    }
    // 외부에서 점프 상태를 바꿀 때 사용
    public void SetJumping(bool isJumping)
    {
        this.isJumping = isJumping;
    }

    // 다른 스크립트(총알 등)가 호출: 모든 클라이언트에 데미지 RPC를 보냄
    public void ApplyDamage(float damageValue)
    {
        view.RPC("RPC_TakeDamage", RpcTarget.All, damageValue);
    }

    [PunRPC]

    void RPC_TakeDamage(float damage)
    {
        // 내 캐릭터에서만 체력을 깎음
        if (!view.IsMine)
            return;


        currentHealth -= damage;
        healthbarSlider.value = currentHealth;      // 체력바 갱신
        if (currentHealth <= 0)
        {
            Die();
        }

        Debug.Log("Damage Taken" + damage);
        Debug.Log("current Health" + currentHealth);
    }

    // 사망 처리는 PlayerControllerManager에 맡김
    private void Die()
    {
        playerControllerManager.Die();
    }
}


