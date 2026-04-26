# 설치 및 설정 가이드

이 문서는 Unity 프로젝트를 처음 클론한 후 빌드하기까지의 설정 과정을 안내합니다.

## ✅ 사전 요구사항

### 필수
- **Unity Hub**
- **Unity 6000.0.64f1** (정확한 버전 권장)
- **Visual Studio 2022** 또는 **JetBrains Rider** (C# IDE)
- **Git** (버전 관리)

### 하드웨어
- Windows 10/11 PC
- Fairino FR5 협동로봇 (실로봇 제어 시)
- 이더넷 케이블

## 🛠️ 설치 단계

### 1. 저장소 클론
```bash
git clone https://github.com/[your-username]/fairino-fr5-unity-twin.git
cd fairino-fr5-unity-twin
```

### 2. Unity 프로젝트 열기
- Unity Hub → Open → 클론한 폴더 선택
- 처음 열 때 패키지 자동 다운로드 (몇 분 소요)

### 3. Unity 설정 확인

#### Edit → Project Settings → Player

**Configuration**:
- API Compatibility Level: **`.NET Framework`**
- Scripting Backend: **Mono**

**Scripting Define Symbols**:
```
FAIRINO_SDK
```

#### Edit → Project Settings → Graphics
- Scriptable Render Pipeline Settings: **URP Asset 사용**

### 4. Fairino SDK 설치

#### DLL 파일 (이미 포함되어 있어야 함)
```
Assets/Plugins/
├── libfairino.dll
└── CookComputing.XmlRpcV2.dll
```

DLL 파일이 없다면 [Fairino 공식 사이트](https://www.fairino.com)에서 다운로드.

#### SDK 소스 (이미 포함됨)
```
Assets/Scripts/Fairino/
├── FRRobot.cs
├── FRLog.cs
└── ...
```

### 5. URDF 임포트 (이미 임포트되어 있다면 스킵)

#### URDF Importer 패키지 추가
- Window → Package Manager → + → "Add package from git URL..."
- URL: `https://github.com/Unity-Technologies/URDF-Importer.git?path=/com.unity.robotics.urdf-importer`

#### URDF 파일 임포트
1. `Assets/URDF/FR5/fr5.urdf` 우클릭
2. "Import Robot from URDF"
3. 옵션:
   - Mesh Decomposer: VHACD
   - Convex: 체크
   - Use Gravity: 미체크 (시뮬용)

## 🌐 네트워크 설정

### Windows 네트워크 어댑터 설정

1. **제어판 → 네트워크 → 어댑터 설정 변경**
2. 사용할 이더넷 어댑터 우클릭 → 속성
3. **TCP/IPv4** 더블클릭
4. **수동 설정**:
   ```
   IP 주소:        192.168.58.100
   서브넷 마스크:   255.255.255.0
   기본 게이트웨이:  (비워둠)
   ```
5. 확인

### 연결 테스트
명령 프롬프트:
```bash
ping 192.168.58.2
```

**성공 시 출력**:
```
192.168.58.2의 응답: 바이트=32 시간<1ms TTL=255
```

## 🤖 로봇 준비

### 티치펜던트 설정

1. **모드를 Auto로 전환** (수동 모드에서는 외부 제어 불가)
2. **System State**: Enabled
3. **Tool**: 사용할 Tool 좌표계 (예: toolcoord1)
4. **Wobj**: 사용할 작업 좌표계 (예: 작업 좌표계1)

### 안전 점검
- 비상정지 버튼 위치 확인
- 작업 영역 내 장애물 제거
- 처음에는 Speed 20-30%로 시작

## ▶️ 실행 방법

### Unity Play 시작
1. Unity Editor에서 **`SampleScene`** 또는 메인 씬 열기
2. **Play 버튼** 클릭
3. UI가 자동으로 표시됨

### Mirror 모드 실행
1. **Mode**: MIRROR 선택
2. **CONNECT** 클릭
3. Status: `Connected | Auto | Tool=1 Wobj=1` 확인
4. **Speed**: 20% 이상
5. **GO HOME** → 시뮬과 실로봇 동시 이동

## 🔧 문제 해결

### "FAIRINO_SDK 정의되지 않음" 에러
- Project Settings → Player → Scripting Define Symbols
- `FAIRINO_SDK` 추가 후 Apply

### "libfairino.dll을 찾을 수 없음"
- `Assets/Plugins/` 폴더에 DLL 있는지 확인
- Inspector에서 Plugin Settings 확인:
  - Standalone: ✓
  - x86_64: ✓

### "using System.Windows.Forms" 컴파일 에러
- `Assets/Scripts/Fairino/RPCHandle.cs`, `FRRobot.cs`
- 해당 줄 주석 처리 또는 삭제

### URDF Importer가 안 보임
- Package Manager에서 재설치
- Unity 6000.0.64f1 호환 버전 확인

### 네트워크 연결 안 됨
- PC IP가 192.168.58.100인지 확인
- 방화벽이 Unity 차단하지 않는지 확인
- 케이블 직접 연결 권장 (스위치/허브 거치지 말 것)

### "rc=14 joint command point error"
- 티치펜던트의 Tool/Wobj 번호 확인
- 코드는 자동으로 감지하지만, 수동 설정 가능:
  ```csharp
  fairinoController.SetToolNum(1);
  fairinoController.SetWobjNum(1);
  ```

## 📝 다음 단계

설치 완료 후:
1. **README.md** 읽기 (전체 기능 이해)
2. **DEVELOPMENT_LOG.md** 읽기 (개발 히스토리)
3. SIM 모드에서 먼저 테스트
4. MIRROR 모드로 실로봇 제어

---

문제 있으면 Issues 탭에 등록해주세요!
