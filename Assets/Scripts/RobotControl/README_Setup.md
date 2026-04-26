# 🤖 Fairino FR5 Unity 통합 제어 시스템 (최종판 + IK)

**시뮬레이션 + 실로봇 완전 동일 동작** — 이제 Unity 시뮬에서도 **DLS IK 기반의 정확한 Cartesian JOG** 가능!

---

## 📁 파일 구성 (10개 스크립트 + 2개 문서)

| 파일 | 역할 | 분류 |
|------|------|------|
| `IRobotController.cs` | 공통 인터페이스 | Core |
| `JointConfig.cs` | 조인트 설정 + FR5 프리셋 | Core |
| `CoordinateConverter.cs` | Unity ↔ 로봇 좌표 변환 | Core |
| `InverseKinematicsSolver.cs` | ⭐ DLS IK 솔버 (시뮬 Cartesian용) | Core |
| `SimulatedRobotController.cs` | 시뮬레이션 구현 | Controller |
| `FairinoRobotController.cs` | Fairino SDK 래퍼 | Controller |
| `RobotManager.cs` | Sim/Real/Mirror 라우터 | Controller |
| `GripperController.cs` | 그리퍼 시각 제어 | Controller |
| `CoordinateCalibrator.cs` | 좌표 검증 도구 | Tool |
| `RobotControlUI.cs` | 런타임 UI 자동 생성 | UI |
| `COORDINATE_SYSTEM.md` | 좌표계 상세 문서 | Doc |

## 🏗 아키텍처

```
                  ┌─────────────────────┐
                  │  RobotControlUI     │  조인트/Cartesian 탭 + 홈포즈 + 모드/스피드
                  └──────────┬──────────┘
                             │ IRobotController
                             ▼
                  ┌─────────────────────┐
                  │  RobotManager       │  Sim/Real/Mirror 모드 라우팅
                  └─────┬─────────┬─────┘
                        │         │
               ┌────────▼──┐  ┌──▼──────────────┐
               │Simulated  │  │Fairino           │
               │Robot      │  │Robot             │
               │Controller │  │Controller        │
               │(Articula- │  │(SDK + Thread)    │
               │tionBody)  │  │                  │
               └───────────┘  └──────────────────┘
                     │               │
                     └───────┬───────┘
                             ▼
                  ┌─────────────────────┐
                  │  GripperController  │  (시뮬용 시각 제어)
                  └─────────────────────┘
                  ┌─────────────────────┐
                  │ CoordinateCalibrator│  (검증 도구)
                  └─────────────────────┘
```

---

## 🚀 Unity 세팅 전체 순서

### 📦 1단계: 파일 복사

`Assets/Scripts/RobotControl/` 폴더 만들고 **9개 .cs 파일** 전부 복사.  
`COORDINATE_SYSTEM.md`는 별도 문서 폴더에 보관.

### 🔌 2단계: Fairino SDK 준비 (실로봇 연결 필요 시)

1. **DLL 복사** → `Assets/Plugins/`
   - `libfairino.dll`
   - `CookComputing.XmlRpcV2.dll`
2. **Fairino 소스 복사** → `Assets/Scripts/Fairino/`
   - `FRRobot.cs`, `FRLog.cs`, `FRUdpClient.cs`, `FrameHandle.cs`
   - `RPCHandle.cs`, `RobotTypes.cs`, `StatusTCPClient.cs`, `TCPClient.cs`
3. ⚠️ `RPCHandle.cs` 맨 위의 `using System.Windows.Forms;` **삭제**
4. `Project Settings → Player → API Compatibility Level: .NET Framework`
5. `Project Settings → Player → Scripting Define Symbols: FAIRINO_SDK` 추가

### 🎨 3단계: Unity 기본 설정

1. `Project Settings → Player → Active Input Handling: Both` (재시작 필요)
2. URDF Importer 설치 (Package Manager → 'URDF Importer')
3. FR5 URDF 파일 임포트 → 씬에 드래그

### 🏗 4단계: 씬 계층 구성

```
Scene
├─ RobotRoot (빈 GameObject)
│   ├─ SimulatedRobotController      ← 스크립트 부착
│   ├─ FairinoRobotController        ← 스크립트 부착
│   │
│   └─ FR5WM (URDF 임포트 결과)
│       └─ base_link (ArticulationBody, Immovable ✓)
│           └─ J1 (Revolute)
│               └─ J2 → J3 → J4 → J5 → J6
│                   └─ GripperMount (빈 GameObject)
│                       ├─ GripperController  ← 스크립트 부착
│                       ├─ palm_body
│                       ├─ Finger_L_Pivot → Finger_L
│                       ├─ Finger_R_Pivot → Finger_R
│                       └─ TCP_Point (빈 GameObject)  ← TCP 기준점
│
├─ RobotManager (빈 GameObject)
│   └─ RobotManager                  ← 스크립트 부착
│
├─ UIManager (빈 GameObject)
│   └─ RobotControlUI                ← 스크립트 부착
│
└─ CalibrationTool (빈 GameObject)
    └─ CoordinateCalibrator          ← 스크립트 부착 (실로봇 검증용)
```

### ⚙️ 5단계: 각 스크립트 Inspector 설정

#### RobotRoot → SimulatedRobotController
```
Drive Mode: ArticulationBody
Joints (Size: 6): URDF의 J1~J6 Transform 드래그, FR5 limit 입력
  [0] J1: Min=-170, Max=170, Home=0
  [1] J2: Min=-170, Max=80,  Home=-90
  [2] J3: Min=-150, Max=150, Home=0
  [3] J4: Min=-170, Max=170, Home=-90
  [4] J5: Min=-170, Max=170, Home=90
  [5] J6: Min=-360, Max=360, Home=0
Stiffness: 10000
Damping: 1000
Force Limit: 1000
Gripper: GripperMount 드래그
TCP Transform: TCP_Point 드래그  ← 좌표 표시용
```

#### RobotRoot → FairinoRobotController
```
Robot IP: 192.168.58.2
Command Rate: 20
MoveJ Vel: 20  |  Acc: 30  |  Ovl: 50
JOG Vel: 30  |  Acc: 30
Joints: SimulatedRobotController와 동일 limit 입력
Gripper Config: 실 그리퍼 모델에 맞춰
Gripper Type: 0 (평행) 또는 1 (회전)
```

#### RobotManager
```
Mode: SimOnly (개발 중) / RealOnly / Mirror
Auto Connect Real: ☐ (안전상 수동 권장)
Sim: RobotRoot 드래그
Real: RobotRoot 드래그
```

#### UIManager → RobotControlUI
```
Robot Manager: RobotManager 드래그
```

#### GripperMount → GripperController
```
Gripper Type: Parallel (손 그리퍼면 Hinge)
Finger L: Finger_L_Pivot
Finger R: Finger_R_Pivot
Finger L Close Direction: (1, 0, 0) 또는 모델에 맞춰
Finger R Close Direction: (-1, 0, 0)
Close Distance: 50 (GripperMount Scale 0.001 고려)
Open Percent: 100 (초기 열림)
```

#### CalibrationTool → CoordinateCalibrator
```
Sim Controller: RobotRoot 드래그
Expected Pose: (실로봇 연결 전 비워둠, 검증 시 티치펜던트 값 입력)
```

---

## 🎮 UI 기능 개요

실행하면 화면 좌상단에 UI 자동 생성:

```
┌─ 🤖 FAIRINO ROBOT CONTROL ─────┐
│                                 │
│ ■ CONNECTION                   │
│ IP: [192.168.58.2]             │
│ [CONNECT] [DISCONNECT]         │
│ Mode: [SIM][REAL][MIRROR]      │
│ Status: ...                    │
│                                 │
│ ■ OPERATION                    │
│ Op Mode: [AUTO][MANUAL]        │
│ Speed: [━━●━━━━━] 50%          │
│                                 │
│ ■ TCP POSE (mm / deg)          │
│ X: ... Y: ... Z: ...           │
│ Rx: ... Ry: ... Rz: ...        │
│                                 │
│ [JOINT CTRL] [CARTESIAN]       │  ← 탭 전환
│                                 │
│ ■ ARM JOINTS  (또는 Cartesian) │
│ J1 ━━━●━━━━━  0.0°             │
│ ...                             │
│                                 │
│ ■ GRIPPER                      │
│ Open ━━●━━━━━  50%             │
│                                 │
│ ■ HOME POSE (각 조인트 입력°)  │
│ J1:[  0.0] J2:[-90.0]          │
│ J3:[  0.0] J4:[-90.0]          │
│ J5:[ 90.0] J6:[  0.0]          │
│ [SET FROM CURRENT][APPLY][GO]  │
│                                 │
│ [ ■ EMERGENCY STOP ]           │
└─────────────────────────────────┘
```

---

## 🧪 실로봇 연결 전 필수 검증

### 1. SimOnly 모드에서 기본 동작 확인
- [ ] 조인트 슬라이더로 로봇 회전
- [ ] 그리퍼 슬라이더로 개폐
- [ ] 홈 포즈 설정 및 이동

### 2. 좌표 검증 (CoordinateCalibrator)
- [ ] 실로봇을 티치펜던트로 홈 자세 이동
- [ ] 티치펜던트의 TCP 포즈 값 메모 (X/Y/Z/Rx/Ry/Rz)
- [ ] Expected Pose에 입력
- [ ] Unity에서 동일 각도로 이동
- [ ] Diff가 허용치 이내인지 확인
- [ ] ✅ OK 뜰 때까지 `CoordinateConverter.cs` 조정 (자세한 건 `COORDINATE_SYSTEM.md`)

### 3. 실로봇 연결 순서
1. **Mode: REAL** 선택
2. **Global Speed: 10~15%** 로 낮게 설정
3. IP 입력 → CONNECT
4. 처음에는 **JOINT CTRL 탭**만 사용 (CARTESIAN 검증 후)
5. 문제 없으면 Mirror 모드로 디지털 트윈 운영

---

## ⚠️ 안전 수칙

1. **첫 테스트는 무조건 낮은 스피드** (10~15%)
2. **홈 포즈는 안전한 자세로 저장** (충돌 없는 위치)
3. **비상정지 버튼은 항상 손 닿는 거리**
4. **Cartesian JOG는 좌표 검증 완료 후에만**
5. **Auto 모드는 프로그램 실행 시에만** (수동 조작할 땐 Manual)
6. **Mirror 모드에서는 시뮬과 실로봇 limit이 일치**해야 함

---

## 🐛 트러블슈팅 요약

| 증상 | 원인/해결 |
|------|----------|
| 슬라이더 드래그 안 됨 | Active Input Handling을 Both로 |
| 로봇이 바닥으로 꺼짐 | 루트 ArticulationBody의 Immovable 체크 |
| "Press arrow keys" 메시지 | URDF Importer의 Controller/JointControl/FKRobot 스크립트 제거 |
| 관절이 안 움직임 | Joints 배열의 Min/Max가 0이면 설정, Stiffness가 0이면 10000으로 |
| 그리퍼가 비틀림 | Finger_L_Pivot / Finger_R_Pivot으로 감싸기 |
| 핑크(마젠타) 렌더 | URP 머티리얼 변환: Edit → Rendering → Materials |
| 좌표 불일치 | CoordinateCalibrator로 검증 후 CoordinateConverter 조정 |

---

## 🔧 주요 SDK 함수 매핑

| UI 동작 | Fairino SDK 호출 |
|---------|------------------|
| CONNECT | `RPC(ip)` + `RobotEnable(1)` + `Mode(...)` + `SetSpeed(...)` + `ActGripper(idx, 1)` |
| 조인트 슬라이더 | `MoveJ(jointPos, ...)` with `blendT=50ms` |
| Cartesian JOG | `StartJOG(refType=2, nb, dir, ...)` / `StopJOG(3)` |
| Joint JOG | `StartJOG(refType=0, nb, dir, ...)` / `StopJOG(1)` |
| Op Mode | `Mode(0=Auto, 1=Manual)` |
| Global Speed | `SetSpeed(percent)` |
| GO HOME | `MoveJ(homeJoints, ...)` |
| Gripper | `MoveGripper(idx, pos, vel, force, ...)` |
| TCP Pose | `GetActualTCPPose(0, ref pose)` |
| Emergency | `StopMotion()` + `ImmStopJOG()` |

---

## 📚 참고 파일

- **COORDINATE_SYSTEM.md**: 좌표계 변환 상세, 축 매핑 조정 방법
- Fairino SDK 문서: `fairino-csharp-sdk-main/src/FRRobot/FRRobot.cs` 내 주석
- URDF Importer: Unity Robotics Hub 공식 문서

---

## 🔮 확장 아이디어

- **포즈 프리셋**: 여러 홈 포즈 저장/로드 (JSON)
- **궤적 재생**: 조인트 시퀀스 녹화 및 재생
- **MoveL 지원**: 데카르트 직선 이동 (목표 XYZ + RPY 입력 UI)
- **IK 통합**: Unity에서도 정확한 Cartesian 시뮬 (Unity Robotics Hub IK 패키지)
- **충돌 감지**: ArticulationBody Collider로 시뮬 충돌 미리 검증
- **I/O 제어**: DigitalOutput/AnalogOutput UI 추가 (SDK의 SetDO/SetAO)
