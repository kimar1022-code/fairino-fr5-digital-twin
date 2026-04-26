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

**시뮬레이션의 Cartesian JOG는 근사적**입니다. Unity에는 기본 IK 솔버가 없어서 정확한 데카르트 제어는 실로봇 연결 시에만 작동합니다.

- **시뮬 모드(SimOnly)**: Cartesian JOG 버튼 = 시각적 참고용 (관련 조인트만 움직임)
- **실로봇 모드(RealOnly)**: Cartesian JOG 버튼 = SDK가 정확한 IK 처리 ✅
- **Mirror 모드**: 시뮬이 근사로 움직이고, 실로봇이 정확히 움직임 → 살짝 다르게 보일 수 있음

정확한 Cartesian 시뮬이 필요하면 Unity Robotics Hub의 **Inverse Kinematics 패키지** 통합을 고려하세요.

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
