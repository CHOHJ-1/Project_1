# 국립해양박물관 — 2번 유물 조회 플랫폼

24인치 가로형 터치 전시용 Unity 프로젝트입니다. 왼쪽 PNG 아이콘을 터치하면 오른쪽에 같은 이름의 설명 PNG가 표시됩니다. USB 동기화 후에는 USB를 분리해도 PC 저장본으로 전시합니다.

## 열기

1. Unity Hub에서 이 저장소 폴더를 프로젝트로 추가합니다.
2. 기준 Editor는 **Unity 6000.0.62f1 (Unity 6 LTS)** 입니다. 현재 납품 장비의 OS는 미확정이며 빌드 기본값은 **Windows x64, 1920×1080**입니다. 최종 Editor 패치와 해상도는 장비 확인 후 고정하세요.
3. `Assets/Museum/Scenes/MainScene.unity`를 엽니다.
4. Player Settings → Active Input Handling은 **Input Manager (Old)** 또는 **Both**로 설정합니다. 이 프로젝트는 표준 uGUI + StandaloneInputModule을 사용합니다.
5. Scene/Game 뷰에는 편집용 샘플 이미지가 보입니다. 이 샘플은 실제 유물이 아닌 임시 도형이며, Play 시 제거됩니다. 실제 전시 데이터는 USB 또는 기존 PC 저장본에서 읽습니다.
6. Play하면 기본 Editor Source Folder인 `USB-Example/MuseumContent`에서 샘플 6개를 읽습니다. 다른 테스트 폴더는 `ContentManager > UsbContentSync > Editor Source Folder`에서 바꾸세요. 실제 USB를 Editor에서 테스트할 때는 이 값을 비웁니다. 실제 빌드에서는 Editor 전용 경로를 사용하지 않습니다.

## Hierarchy에서 수정할 위치

| 대상 | 위치 / 설정 |
|---|---|
| 좌우 화면 비율 | `Canvas/LeftPanel`, `Canvas/RightPanel`의 RectTransform 앵커 |
| 아이콘 열 개수 | `LeftPanel/ArtifactScrollView/Viewport/Content` → EditableGrid → Fixed Column Count |
| 아이콘 크기·간격 | 같은 Grid의 Cell Size, Spacing, Padding |
| 아이콘 외형·선택 색 | `Assets/Museum/Prefabs/ArtifactItem.prefab`의 이미지 및 ArtifactItem → Normal Color / Selected Color |
| 설명 이미지 위치 | `RightPanel/DescriptionImage` (이미지 비율은 코드에서 유지) |
| 확대 화면 | `Canvas/ZoomOverlay`를 에디터에서 활성화하여 편집; 시작 시 자동 숨김 |
| 확대 배율 | Canvas → ImageZoom → Maximum Scale |
| 스크롤 감도·관성 | ArtifactScrollView → EditableScrollRect |
| 터치 드래그 판정 | EventSystem → TouchEventSystem → Drag Threshold |
| USB 확인 주기 | ContentManager → UsbContentSync → Poll Seconds |
| 화면 대응 해상도 | Canvas → EditableScaler → Reference Resolution |
| 안내 문구 외형 | Canvas/Status의 Text 설정 (운영 안내는 현재 영어) |

전체 UI는 씬에 직렬화되어 있습니다. 실행 시 화면 틀을 생성하는 코드가 없습니다. 유물 개수에 따라 아이콘 Prefab의 인스턴스만 생성합니다. `PreviewArtifact_*`는 편집용 예시로, Play 시 삭제됩니다. Grid는 Fixed Column Count를 사용하고, 열 수를 늘릴 때는 패널 폭과 Cell Size도 함께 조정하세요.

`Editable*` 컴포넌트는 Unity 기본 UI 클래스를 그대로 상속한 얇은 클래스입니다. 일반 uGUI 속성을 Inspector에 노출하고, 씬 참조 GUID를 프로젝트 안에서 고정하기 위한 용도입니다. 별도 UI 프레임워크는 사용하지 않습니다.

## USB 준비 및 콘텐츠 관리

`USB-Example/MuseumContent` 폴더 전체를 USB 최상위에 복사합니다.

```text
USB 드라이브/
└─ MuseumContent/
   ├─ content-source.txt     # 내용: museum-platform-2
   ├─ Icons/
   │  ├─ 001.png
   │  └─ 002.png
   └─ Descriptions/
      ├─ 001.png
      └─ 002.png
```

- 파일명은 소문자 영문, 숫자, `-`, `_`만 사용하고 확장자는 `.png`로 합니다. 아이콘과 설명 파일명은 정확히 같아야 합니다.
- 파일명 순서로 표시합니다. `001`, `002`, `010`처럼 자릿수를 맞추세요.
- 추가: 양쪽 폴더에 같은 이름의 PNG를 추가합니다.
- 교체: 같은 이름의 PNG를 덮어씁니다.
- 삭제: 양쪽 폴더에서 해당 파일을 삭제합니다. 다음 정상 동기화에서 PC 목록에도 반영됩니다.
- 모두 삭제: 실수로 비운 USB가 전시를 지우지 않도록 기본 차단합니다. 정말 모두 삭제하려면 두 폴더를 비우고 `MuseumContent/allow-empty.txt` 빈 파일을 만드세요. 이후 일반 운영에서는 이 파일을 제거하세요.
- 한 쌍이 누락되거나 PNG가 잘못되면 전체 업데이트를 거부하고 기존 전시를 유지합니다.
- 기본 3초 간격의 두 검사에서 내용이 같으면 동기화합니다. 전체 복사가 끝난 USB를 연결하는 방식이 가장 안전합니다. 연결 상태에서 여러 파일을 복사할 때 복사 도중 완전한 목록이 잠시 형성되면 중간 목록도 반영될 수 있습니다.
- 관리자 안내에 `Update complete. USB may be removed.` 또는 `Content up to date.`가 나오면 분리할 수 있습니다. USB 자체의 쓰기 작업은 OS에서 완료·안전 제거하세요.
- 콘텐츠 USB는 한 번에 하나만 연결합니다. 여러 소스가 발견되면 동기화를 차단합니다.
- Windows에서는 이동식 드라이브 및 고정 디스크로 인식되는 USB SSD의 최상위 전용 폴더를 찾습니다. 내부 디스크 최상위에 같은 전용 폴더를 만들지 마세요. Linux 개발용으로 `/media`, `/run/media`, `/mnt` 아래도 검색합니다.

현재 방식은 연결된 전용 USB를 신뢰하는 **로컬 수동 운영**입니다. 관리자 로그인, USB 인증, 원격 관리, 3번 영상 플랫폼은 포함하지 않습니다.

## 이미지 권장 규격과 제한

- 아이콘은 투명 배경 PNG, 약 256×256 또는 512×512를 권장합니다.
- 설명은 글과 유물 이미지를 포함한 완성 PNG입니다. 오른쪽 영역에 원본 비율을 유지하며 맞춥니다. 긴 세로 이미지는 글씨가 작아지므로 실제 패널 비율에 맞춰 제작하세요.
- 설명 영역을 터치하면 전체 확대 화면이 열립니다. 두 손가락으로 확대·축소, 한 손가락으로 이동, CLOSE로 닫기입니다. 마우스 클릭·휠·드래그와 Escape도 지원합니다.
- 파일당 최대 32 MiB, 전체 256 MiB, 최대 200유물입니다.
- PNG는 한 변 최대 8192px, 한 장 최대 16,777,216픽셀입니다.
- 모든 아이콘의 총 픽셀 수는 기본 16,000,000픽셀 이하입니다. Inspector에서 변경할 수 있지만 장비 메모리에 맞춰 검증해야 합니다.

## 저장·복구

PC 저장 위치는 `Application.persistentDataPath/MuseumPlatform2`입니다. Windows 빌드 기본 위치는 `%USERPROFILE%/AppData/LocalLow/Museum/Museum Platform 2/MuseumPlatform2`입니다. Editor의 경로는 Player 설정에 따라 다를 수 있습니다.

새 콘텐츠를 별도 세대 폴더에 복사 → PNG 디코딩 검사 → USB 내용 재검사 → `active.txt` 원자 교체 순으로 반영합니다. 기존 데이터는 마지막 전환 전까지 삭제하지 않습니다. 파일 검사·복사는 작업 스레드에서, Unity 이미지 디코딩은 메인 스레드에서 프레임별로 진행합니다. 마지막 전체 해시 재검사와 화면 갱신 시에는 파일 수·장비 속도에 따른 짧은 지연이 있을 수 있습니다.

현재·직전 세대를 유지합니다. 수동 복구 시 앱과 콘텐츠 USB를 분리·종료하고 저장 폴더를 백업한 뒤 `previous.txt`의 세대 이름을 `active.txt`에 복사하고 앱을 다시 실행하세요. 복구 후 같은 USB를 연결하면 USB 내용이 다시 반영됩니다. USB 분리나 인식 실패 자체는 삭제로 처리하지 않습니다.

## 빌드

Unity Hub에서 해당 Editor의 **Windows Build Support (Mono)** 모듈을 설치합니다. Unity 메뉴 `Museum > Build Windows x64`로 `Builds/Windows/MuseumPlatform2.exe`를 만듭니다. 프로젝트를 열 때 현재 Build Target도 Windows로 전환하세요.

라이선스가 활성화된 Editor에서 배치 빌드하는 예시:

```text
Unity -batchmode -quit -projectPath <프로젝트 절대경로> -buildTarget Win64 -executeMethod Museum.Editor.MuseumBuild.Windows -logFile <로그 경로>
```

실행파일만 옮기지 말고 생성된 빌드 폴더 전체를 납품합니다. 운영 PC의 자동 실행, 절전 해제, Windows 업데이트 및 키오스크 정책은 현장 설정 항목입니다.

## 검증

Unity 없이 USB 저장 핵심 로직을 검증할 수 있습니다. .NET 8 SDK와 Python 3가 필요하며 외부 NuGet 패키지는 사용하지 않습니다.

```sh
python3 Tools/run_core_tests.py
# 별도 SDK 경로를 사용할 때
python3 Tools/run_core_tests.py --dotnet-root /path/to/dotnet
```

일반 개발 PC에서는 `dotnet run --project Tests/ContentStore.Tests -- <저장소 절대경로>`도 가능합니다.

자동 검사는 추가·삭제·교체·재시작·중간 분리·파일 변경·불완전한 쌍·헤더 손상·크기 제한·명시적 전체 삭제·백업 보존을 검증합니다. 실제 PNG 디코딩 콜백 실패도 모의합니다. 실제 Unity 디코더나 터치 하드웨어를 검사하는 테스트는 아닙니다.

**현재 검증 상태:** 클라우드에서 저장 로직 18개 검사 통과. 씬·Prefab의 YAML 및 342개 연결 참조 검사도 통과했습니다. 현재 클라우드의 Unity 패키지 서버 접근은 HTTP 403으로 차단되어 있으며, `packages.unity.com`, `download.packages.unity.com` 허용을 환경 설정 초안에 저장했습니다. 초안 저장은 현재 네트워크 변경이나 게시를 의미하지 않습니다. Unity Editor 설치·라이선스가 없어 Editor import, Play Mode, Windows 빌드, 실제 터치는 아직 검증하지 않았습니다. 납품 가능한 검증 완료본으로 간주하면 안 됩니다.

납품 전 필수 확인:

1. Unity import 후 Console 컴파일·참조 오류가 없는지 확인합니다.
2. Play에서 테스트 경로로 샘플 콘텐츠를 읽고 좌측 선택·스크롤, 우측 표시를 확인합니다.
3. 실제 모니터에서 드래그가 선택으로 오인되지 않는지, 빠른 연속 터치와 두 손가락 확대·축소가 정상인지 확인합니다.
4. USB의 추가·교체·삭제 및 분리 후 유지·앱 재시작을 확인합니다.
5. 손상 PNG, 파일 쌍 누락, 복사 중 분리, 빈 폴더, 여러 USB 연결 시 기존 콘텐츠를 유지하는지 확인합니다.
6. 납품 해상도·배율, 최대 콘텐츠, 장시간 반복 조작과 전원 재시작을 검증합니다.
