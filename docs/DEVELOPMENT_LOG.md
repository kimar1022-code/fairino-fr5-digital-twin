# 개발 일지 (DEVELOPMENT_LOG)

이 문서는 Fairino FR5 Unity Digital Twin 프로젝트의 주요 개발 단계와 기술적 결정사항을 기록합니다.

## 📅 개발 단계

### Phase 1: 환경 설정 및 임포트

#### Unity 버전 결정
- 초기: Unity 6000.4.3f1
- 최종: **Unity 6000.0.64f1** (URDF Importer 호환성 문제로 다운그레이드)
- `manifest.json`에서 `adaptiveperformance`, `vectorgraphics` 패키지 제거

#### URDF 임포트
- Fairino 공식 URDF 사용 (`fr5_description`)
- STL 파일을 OBJ로 변환 (Unity URDF Importer 호환성)
- 자동으로 ArticulationBody 계층 구조 생성

### Phase 2: 좌표 시스템

#### CoordinateConverter
URDF Importer의 표준 좌표 매핑:
```
Robot X ← Unity Z
Robot Y ← -Unity X  
Robot Z ← Unity Y
```
- 고정 XYZ 오일러 회전 변환
- mm/deg ↔ m/rad 단위 자동 변환

### Phase 3: 인터페이스 설계

#### IRobotController
Sim과 Real의 통합 인터페이스:
```csharp
interface IRobotController
{
    void Connect();
    void SetJointTarget(int i, float angleDeg);
    void StartCartesianJog(int axis, int dir);
    CartesianPose GetCurrentTCPPose();
    // ...
}
```

#### RobotManager
- Sim/Real/Mirror 모드 전환
- PrimaryReader 패턴: 모드에 따라 적절한 컨트롤러에서 데이터 읽음
- IsBusy 상태 추적 (실로봇 MoveJ 실행 중)

### Phase 4: 시뮬레이션

#### SimulatedRobotController
- ArticulationBody 사용
- Stiffness=10000, Damping=1000, Force Limit=1000
- xDrive.target으로 조인트 제어

#### InverseKinematicsSolver
- DLS (Damped Least Squares) Jacobian 방식
- damping=0.1, maxIterations=8
- Transform 기반 FK (속도 우선)

### Phase 5: 실로봇 통신

#### Fairino SDK 통합
- DLL: `libfairino.dll`, `CookComputing.XmlRpcV2.dll`
- 소스: `using System.Windows.Forms;` 제거 (Unity 미지원)
- Scripting Define: `FAIRINO_SDK`
- API Compatibility: `.NET Framework`

#### 네트워크 설정
- 로봇 IP: 192.168.58.2 (XML-RPC 포트 20003)
- PC IP: 192.168.58.100
- 직접 LAN 케이블 권장

### Phase 6: UI 시스템

#### 자동 UI 생성
- Canvas + Panel 자동 생성
- 탭 시스템: JOINT CTRL / CARTESIAN / GRIPPER / SETTINGS
- 슬라이더 + 직접 입력 + 버튼 (±1°, ±5°)
- 길게 누르기 지원 (0.3s 초기, 0.1s 반복)

#### Pose Slot
- 3개 슬롯
- Save / Load 버튼
- 슬롯 상태 표시 ("비어있음" / "저장됨")

## 🐛 주요 디버깅 사례

### 1. rc=14 "joint command point error"
**증상**: MoveJ 호출 시 에러 코드 14

**원인 분석**:
- 로봇 티치펜던트 설정 확인: Tool=1, Wobj=1
- 코드는 tool=0, user=0 전송
- → Tool/Wobj 불일치

**해결**:
```csharp
// Connect 시 자동 감지
robot.GetActualTCPNum(ref currentToolNum);
robot.GetActualWObjNum(ref currentWobjNum);
```

### 2. MoveJ DescPose Zero 에러
**증상**: DescPose=(0,0,0,0,0,0) 전달 시 IK 실패

**원인**: 일부 SDK 오버로드는 DescPose가 필수, 0이면 역산 불가

**해결**: DescPose 인자 없는 오버로드 사용 (SDK가 자동 FK)
```csharp
robot.MoveJ(jp, tool, wobj, vel, acc, ovl, blendT);  // DescPose 없음
```

### 3. 조인트 간 간섭 (가장 큰 버그!)
**증상**: 
- J1 슬라이더 움직임
- 실로봇에서 **J5도 같이 회전**
- "가끔은 되고 가끔은 안 됨"

**원인 분석**:
1. SetJointTarget(0, ...) 호출 시
2. `targetJointAngles[0]`만 새 값으로 변경
3. `targetJointAngles[1]~[5]`는 **오래된 값** 유지
4. SendMoveJ가 6축 전부 전송 → 다른 관절도 명령받아 이동

**해결**:
```csharp
public void SetJointTarget(int i, float angleDeg)
{
    // 다른 관절들을 현재 실측값으로 동기화
    for (int k = 0; k < 6; k++)
        if (k != i) targetJointAngles[k] = currentJointAngles[k];
    
    targetJointAngles[i] = clamped;
    jointDirty = true;
}
```

### 4. 카티시안 JOG 시뮬-실로봇 불일치
**증상**: 
- Real은 X축으로 직선 이동 (정확)
- Sim은 엉뚱한 회전, 모든 관절이 틀어짐

**원인 분석**:
- Real: SDK 정확한 IK 사용
- Sim: Unity DLS IK 사용 → URDF 모델과 실로봇 운동학 불일치
- 같은 카티시안 명령에 다른 결과

**해결: Mirror 동기화**
```csharp
void Update()
{
    if (mode != Mode.Mirror) return;
    if (real == null || !real.IsConnected) return;

    for (int i = 0; i < real.JointCount; i++)
        sim.SetJointTarget(i, real.GetJointAngle(i));
}
```
- Mirror 모드에서 Sim의 IK 안 씀
- Real이 SDK IK로 움직이고
- Sim은 Real 각도를 매 프레임 따라감
- → 완벽한 시각적 동기화

## 🎯 핵심 설계 원칙

### 1. Sim은 Real의 그림자
Mirror 모드에서 Sim은 **수동적**:
- 자체 IK 계산 안 함
- Real의 결과를 단순 재생
- → 정확성 보장

### 2. 인터페이스 통합
`IRobotController` 인터페이스로:
- Sim과 Real을 같은 방식으로 사용
- 모드 전환 자유로움
- 코드 재사용성 ↑

### 3. 안전 우선
- Op Mode AUTO 강제 (실로봇 동작 시)
- Speed 제한 (기본 20%)
- IsBusy 체크로 중복 명령 방지
- Stop 버튼 우선 처리

## 📊 성능 지표

### 실로봇 통신
- XML-RPC 지연: ~10-20ms
- MoveJ 응답: ~100ms
- 조인트 각도 읽기: 매 프레임 가능

### 시뮬레이션
- 프레임률: 60 FPS 유지
- IK 계산: ~1-2ms (DLS, 8 iterations)
- ArticulationBody 안정성: Stiffness 10000 이상에서 안정

### Mirror 동기화
- Update 주기: 60 FPS (16.67ms)
- Real 각도 ↔ Sim 차이: <1° (정상 범위)

## 🔮 향후 개선 가능 사항

### 1. J5 시각 일치
- 현재: URDF와 실로봇 기준 차이로 시각적 불일치
- 해결책 후보:
  - J5 Transform Rotation 수동 보정
  - URDF 자체 수정
  - Visual 메시 회전

### 2. 추가 기능
- 궤적 녹화/재생
- 충돌 검출
- 작업 좌표계 시각화
- 다중 워크포인트 관리

### 3. UX 개선
- 에러 메시지 한글화
- 단축키 지원
- 다국어 지원

## 📚 참고 자료

- Unity URDF Importer: https://github.com/Unity-Technologies/URDF-Importer
- Fairino SDK: https://www.fairino.com
- DLS IK 알고리즘: Buss & Kim, "Selectively Damped Least Squares" (2005)

---

**최종 업데이트**: 2026-04-22  
**작업 시간**: 약 2주 (단계적 개발)
