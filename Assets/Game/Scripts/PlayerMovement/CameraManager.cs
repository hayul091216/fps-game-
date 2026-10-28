using System.Drawing;
using UnityEngine;

// 3인칭 카메라를 관리하는 스크립트
// 구조: CameraManager(이 오브젝트) -> cameraPivot(상하 회전) -> Main Camera(앞뒤 거리, 충돌)
public class CameraManager : MonoBehaviour
{
    //inputmanager불러오기
    InputManager inputManager;
    public Transform playerTransform;   // 카메라가 따라갈 플레이어
    public Transform cameraTransform;   // 실제 Main Camera의 Transform
    [Header("Cmera movement")]
    public Transform cameraPivot;       // 상하 회전의 축 역할 (Main Camera의 부모)

    private Vector3 cameraFollowVelocity = Vector3.zero;   // SmoothDamp가 내부적으로 사용하는 속도 값
    public float cameraFollowSpeed = 0.3f;                // 플레이어를 따라가는 부드러움 (작을수록 빨리 따라감)
    public float lookAngle;                               // 좌우 회전 각도 (Y축)
    public float pivotAngle;                              // 상하 회전 각도 (X축)
    public float minPivotAngle = -30f;                    // 위아래 시점 제한 (최소)
    public float maxPivotAngle = 30f;                     // 위아래 시점 제한 (최대)
    public float camLookSpeed = 2f;                       // 좌우 마우스 감도

    public float camPivotSpeed = 2f;                      // 상하 마우스 감도

    [Header("Cmera Collisior")]
    public LayerMask collisionLayer;                      // 카메라가 충돌로 인식할 레이어 (벽, 바닥 등)
    public float defaultPosition;                         // 카메라의 기본 z 거리 (플레이어 뒤쪽이라 음수)
    public float cameraCollisionOffset = 0.2f;            // 벽에서 카메라를 띄울 여유 거리
    public float minCollisionOffset = 0.2f;               // 카메라가 피벗에 너무 가까워지지 않게 하는 최소 거리
    public float cameraCollisionRadius = 0.2f;            // 충돌 검사용 구(SphereCast)의 반지름
    private Vector3 CameraVectorPosition;                 // 카메라 localPosition 갱신용 (z값만 사용)


    private PlayerMovement playerMovement;







    void Awake()
    {
        // 마우스 커서를 화면 중앙에 고정하고 숨김
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // 씬에서 필요한 스크립트와 오브젝트를 찾아 연결
        inputManager = FindAnyObjectByType<InputManager>();
        playerMovement = FindAnyObjectByType<PlayerMovement>();
        playerTransform = FindAnyObjectByType<PlayerManager>().transform;

        cameraTransform = Camera.main.transform;
        // 시작할 때의 카메라 z 위치를 기본 거리로 저장
        defaultPosition = cameraTransform.localPosition.z;



    }

    // 외부(보통 PlayerManager의 LateUpdate)에서 매 프레임 호출하는 함수
    public void HandleAllCameraMovement()
    {
        FollowTarget();     // 1. 플레이어 위치 따라가기
        RotateCamera();     // 2. 마우스 입력으로 회전
        CameraCollision();  // 3. 벽에 가려지지 않게 거리 조절
    }
    void FollowTarget()
    {
        // 현재 위치에서 플레이어 위치까지 부드럽게 이동
        Vector3 targetPosition = Vector3.SmoothDamp(transform.position, playerTransform.position, ref cameraFollowVelocity, cameraFollowSpeed);

        transform.position = targetPosition;
    }

    void RotateCamera()
    {
        Vector3 rotation;
        Quaternion targetRotation;
        // 마우스 입력만큼 회전 각도를 누적
        lookAngle = lookAngle + (inputManager.cameraInputX * camLookSpeed);
        pivotAngle = pivotAngle + (inputManager.cameraInputY * camPivotSpeed);
        // 상하 각도가 제한 범위를 넘지 않게 고정
        pivotAngle = Mathf.Clamp(pivotAngle, minPivotAngle, maxPivotAngle);

        // 좌우 회전: 이 오브젝트(CameraManager) 전체를 Y축으로 회전
        rotation = Vector3.zero;
        rotation.y = lookAngle;
        targetRotation = Quaternion.Euler(rotation);
        transform.rotation = targetRotation;

        // 상하 회전: cameraPivot만 X축으로 회전
        rotation = Vector3.zero;
        rotation.x = pivotAngle;
        targetRotation = Quaternion.Euler(rotation);
        cameraPivot.localRotation = targetRotation;

        // 플레이어가 멈춰 있을 때만 캐릭터가 카메라가 보는 방향(좌우)을 바라보게 함
        if (playerMovement.isMoving == false && playerMovement.isSprinting == false)
        {
            playerTransform.rotation = Quaternion.Euler(0, lookAngle, 0);
        }
    }

    void CameraCollision()
    {
        float targetPosition = defaultPosition;   // 기본적으로는 원래 거리를 목표로 함
        RaycastHit hit;
        // 피벗에서 카메라 쪽으로 향하는 방향
        Vector3 direction = cameraTransform.position - cameraPivot.position;
        direction.Normalize();

        // 피벗에서 카메라 방향으로 구를 쏴서 중간에 벽이 있는지 검사
        if (Physics.SphereCast(cameraPivot.transform.position, cameraCollisionRadius, direction, out hit, Mathf.Abs(targetPosition), collisionLayer))
        {
            // 벽에 부딪혔다면 벽 바로 앞(offset만큼 여유)까지 카메라를 당김
            float distance = Vector3.Distance(cameraPivot.position, hit.point);
            targetPosition = -(distance - cameraCollisionOffset);
        }
        // 카메라가 피벗에 너무 붙으면 최소 거리만큼 더 뒤로 보냄
        if (Mathf.Abs(targetPosition) < minCollisionOffset)
        {
            targetPosition = targetPosition - minCollisionOffset;
        }

        // 현재 z 위치에서 목표 위치로 부드럽게 이동 (0.2 = 보간 속도)
        CameraVectorPosition.z = Mathf.Lerp(cameraTransform.localPosition.z, targetPosition, 0.2f);
        cameraTransform.localPosition = CameraVectorPosition;
    }
}