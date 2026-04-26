using UnityEngine;

namespace RobotControl
{
    /// <summary>
    /// 특정 조인트의 "0도 기준"을 수동으로 보정하는 스크립트.
    /// Unity 시뮬 모델과 실로봇의 기본 자세가 다를 때 사용.
    /// 
    /// 사용법:
    /// 1. 보정이 필요한 조인트 오브젝트(예: J5)에 이 컴포넌트 추가
    /// 2. Inspector에서 Zero Pose Rotation에 원하는 기준 회전 입력
    ///    (실로봇 홈 자세와 같은 모양이 되는 값)
    /// 3. Play 시작 시 자동 적용 → 이 회전이 새로운 "0도"가 됨
    /// 4. Play 중에 값 바꿔도 "Apply Now" 버튼으로 즉시 재적용 가능
    /// 
    /// SimulatedRobotController가 tfInitialRots를 Awake에서 저장하므로,
    /// 이 스크립트의 Awake가 먼저 실행되도록 설정 (DefaultExecutionOrder=-200)
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public class JointZeroCalibrator : MonoBehaviour
    {
        [Tooltip("실로봇 홈 자세와 일치하도록 이 조인트를 추가 회전시킬 Euler Angles (degree).\n" +
                 "예: 시뮬 J5는 일자인데 실로봇은 직각이면 (-90, 0, 0) 시도")]
        public Vector3 zeroPoseRotation = new Vector3(-90f, 0f, 0f);

        [Tooltip("이 회전을 기존 localRotation에 곱해서 적용 (기존 방향 유지 + 추가 회전)")]
        public bool multiplyWithExisting = true;

        Quaternion originalRotation;   // 최초 localRotation 저장

        void Awake()
        {
            originalRotation = transform.localRotation;
            ApplyRotation();
            Debug.Log($"[JointZeroCalibrator] {name} Awake 적용: rotation={transform.localRotation.eulerAngles}, offset={zeroPoseRotation}");
        }

        /// <summary>원래 회전에 zeroPoseRotation을 적용</summary>
        [ContextMenu("Apply Now")]
        public void ApplyRotation()
        {
            Quaternion offsetRot = Quaternion.Euler(zeroPoseRotation);
            if (multiplyWithExisting)
                transform.localRotation = originalRotation * offsetRot;
            else
                transform.localRotation = offsetRot;
        }

        /// <summary>원래 상태로 복구 (디버깅용)</summary>
        [ContextMenu("Reset to Original")]
        public void ResetRotation()
        {
            transform.localRotation = originalRotation;
        }
    }
}

