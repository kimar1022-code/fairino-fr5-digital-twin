# 🎯 Unity ↔ Fairino FR5 좌표계 정리

## ⚠️ 가장 중요한 메시지

**실로봇 연결 전에 반드시 좌표 검증을 하세요.** 축 매핑이 틀리면 로봇이 의도와 다른 방향으로 움직여 **충돌/사고 위험**이 있습니다.

---

## 📐 좌표계 비교

### Fairino FR5 (Base Frame)
```
     Z+ (위)
     |
     |
     +------ Y+ (왼쪽)
    /
   X+ (앞, 로봇 정면)
```
- **Right-handed**
- 단위: **mm**
- 회전: **Rx/Ry/Rz** (Fixed XYZ Euler, -180° ~ +180°)

### Unity World
```
     Y+ (위)
     |
     |
     +------ X+ (오른쪽)
    /
   Z+ (앞)
```
- **Left-handed**
- 단위: **m (meter)**
- 회전: **Euler ZXY 순서**

### 축 매핑 (URDF Importer 기준)

| Fairino | Unity |
|---------|-------|
| X (앞) | Z |
| Y (왼) | -X |
| Z (위) | Y |

---

## ✅ 올바르게 작동하는 부분

**조인트 각도는 자동으로 맞습니다.** URDF Importer가 알아서 변환해주기 때문에:
- 실로봇 J1 = 30° → Unity J1 ArticulationBody 각도도 30°
- **조인트 슬라이더로만 제어하면 좌표계 걱정 없음**

## ⚠️ 변환 필요한 부분

다음 기능들은 `CoordinateConverter.cs`가 자동 변환:
- TCP 포즈 표시 (Unity 위치 → 로봇 mm/RPY)
- Cartesian JOG (실로봇은 SDK가 처리, 시뮬은 근사)
- MoveL / MoveCart (추후 기능)

---

## 🧪 좌표 검증 절차 (필수!)

### 1단계: 씬에 CoordinateCalibrator 추가

1. 빈 GameObject 생성 → 이름 `CalibrationTool`
2. `CoordinateCalibrator` 스크립트 부착
3. Inspector에서 **Sim Controller**에 RobotRoot 드래그

### 2단계: 기준 포즈 기록

실로봇 티치펜던트 사용:
1. 로봇을 **홈 자세**로 이동 (예: J1=0, J2=-90, J3=0, J4=-90, J5=90, J6=0)
2. 티치펜던트 "데카르트 공간 이동" 화면에서 TCP 포즈 **정확히 메모**:
   - X, Y, Z (mm)
   - Rx, Ry, Rz (°)

### 3단계: Unity 검증

1. Unity에서 동일한 조인트 각도로 로봇 이동 (홈 포즈 버튼)
2. CalibrationTool의 Inspector에서:
   - **Expected Pose**에 메모한 티치펜던트 값 입력
3. Play → Inspector 확인:
   - **Position Diff Mm**: (0, 0, 0)에 가까워야 함
   - **Rotation Diff Deg**: (0, 0, 0)에 가까워야 함
   - **Calibration Status**: ✅ OK 이어야 함

### 4단계: 결과에 따른 조치

| 결과 | 조치 |
|------|------|
| ✅ 모두 OK | 좌표 매핑 정확 → 실로봇 연결 안전 |
| ⚠ 위치 OK, 회전 불일치 | `CoordinateConverter.UnityRotationToRobotRPY` 조정 필요 |
| ⚠ 회전 OK, 위치 불일치 | TCP Transform 위치 확인, 또는 RobotRoot 위치가 (0,0,0)인지 |
| ❌ 모두 불일치 | URDF 임포트 시 축 옵션이 달랐을 수 있음 - README의 "축 매핑 재조정" 참조 |

---

## 🔧 축 매핑 재조정 (필요 시)

`CoordinateConverter.cs`의 `UnityPositionToRobot` 함수:

```csharp
public static Vector3 UnityPositionToRobot(Vector3 unityWorld)
{
    return new Vector3(
        unityWorld.z * 1000f,      // Robot X ← Unity Z
        -unityWorld.x * 1000f,     // Robot Y ← -Unity X
        unityWorld.y * 1000f       // Robot Z ← Unity Y
    );
}
```

만약 검증 시 X/Y가 뒤바뀐 것 같으면:
```csharp
// 예: X ↔ Y 스왑, 부호 조정 등
return new Vector3(
    -unityWorld.x * 1000f,     // 부호 반전
    unityWorld.z * 1000f,      // X ↔ Y 스왑
    unityWorld.y * 1000f
);
```

위치가 맞아졌다면 `UnityRotationToRobotRPY`의 축 교체 부분도 동일한 방식으로 수정.

---

## 🤖 실로봇 운용 체크리스트

실로봇 연결 전 최종 확인:

- [ ] CoordinateCalibrator로 좌표 검증 완료 (✅ OK 상태)
- [ ] 홈 포즈 저장됨 (안전한 자세)
- [ ] Global Speed가 **10~20%로 낮게 설정**
- [ ] 로봇 주변 안전 영역 확보
- [ ] 비상정지 스위치가 손 닿는 거리에
- [ ] 첫 조작은 **JOINT CTRL 탭**으로 (CARTESIAN 탭은 검증 후)
- [ ] Mirror 모드로 먼저 Sim만 움직이는 것 확인 후 Real 연결

---

## 💡 Cartesian JOG에 대한 주의

시뮬레이션의 Cartesian JOG는 자체 DLS IK 솔버(`InverseKinematicsSolver`)로 동작합니다. 실로봇은 Fairino SDK가 펌웨어에서 IK를 처리하므로, 두 경로는 **서로 다른 운동학 구현**입니다.

- **시뮬 모드(SimOnly)**: Unity DLS IK가 처리 — X/Y/Z 선형 JOG 동작 확인됨
- **실로봇 모드(RealOnly)**: Fairino SDK가 정확한 IK 처리 ✅
- **Mirror 모드**: `RobotManager.StartCartesianJog`가 early return으로 **실로봇만 JOG**하고, 시뮬은 `Update()`에서 실로봇 각도를 그대로 복사 → 완전 일치

### 검증 현황 (SimOnly 한정)

Unity 6000.4.3f1, `JointConfig.rotationAxis = {x:0, y:-1, z:0}` 설정 기준입니다.

| 항목 | 상태 |
|---|---|
| X / Y / Z 선형 JOG | 동작 확인 |
| Rx / Ry / Rz 회전 JOG | 동작 및 **방향 일치** 확인 |

#### 회전 방향에 추가 부호 반전이 필요 없는 이유

Unity는 left-handed, FR5는 right-handed이므로 회전 방향을 뒤집어야 할 것처럼 보이지만, **축 매핑 행렬 자체가 이미 방향 반전 사상**입니다.

```
Robot X → Unity  Z  = ( 0, 0, 1)          | 0  -1   0 |
Robot Y → Unity -X  = (-1, 0, 0)   det M = | 0   0   1 | = -1
Robot Z → Unity  Y  = ( 0, 1, 0)          | 1   0   0 |
```

행렬식이 −1이면 그 사상이 방향을 뒤집으므로, 좌표계 손잡이 차이와 서로 상쇄됩니다. 따라서 `SimulatedRobotController.JogLoop`의 회전 분기는 축 교체만 하면 되고, 별도 부호 반전을 넣으면 오히려 방향이 뒤집힙니다.

`CoordinateConverter.UnityRotationToRobotRPY`가 `-x,-y,-z` 반전을 하는 것은 쿼터니언 성분 재배치(`qz, -qx, qy, qw`)라는 다른 방식을 쓰기 때문이며, 이 경로와 요구사항이 다릅니다. **두 경로를 같은 규칙으로 다루면 안 됩니다.**

#### `rotationAxis` 값

Unity Revolute 관절은 앵커 프레임의 X축을 회전축으로 삼습니다. 6축 모두 `ArticulationBody.anchorRotation`이 Z축 −90°이므로 회전축은 다음과 같습니다.

```
R(-90°, Z) · (1, 0, 0) = (0, -1, 0)
```

이 값으로 설정했을 때 선형·회전 JOG가 모두 정상 동작합니다. 값이 틀리면 IK 내부 FK가 6축을 같은 축으로 돌리게 되어 Jacobian의 열이 거의 평행해지고, 팔이 부채 접히듯 안으로 말립니다.

> 씬 파일은 이 저장소에 포함되지 않으므로 위 값은 프로젝트에서 직접 설정해야 합니다.

> **참고**: 위 항목은 SimOnly 모드에만 해당합니다. Mirror/Real 모드는 Unity IK 경로를 타지 않습니다.

---

## 📚 참고: Fairino DescPose 구조

```
DescPose {
    tran.x, tran.y, tran.z   // mm, base frame 기준
    rpy.rx, rpy.ry, rpy.rz   // degree, Fixed XYZ Euler
}
```

- `MoveL(joint, pose, ...)`에 이 포즈 전달하면 SDK가 IK로 이동
- `GetActualTCPPose(flag, ref pose)`로 현재 포즈 읽기
- UI에서 Cartesian 값 직접 입력해서 이동시키려면 이 구조체 활용
