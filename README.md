# FrameAnalyzer

FrameAnalyzer는 Windows Forms 기반의 OpenCV 이미지 처리 노드 그래프 편집기입니다.
이미지 입력, 전처리, 검출, 미리보기 노드를 캔버스에 배치하고 포트끼리 연결해서 처리 흐름을 구성할 수 있습니다.

## 주요 기능

- 노드 기반 이미지 처리 그래프
- 드래그로 노드 이동 및 크기 조절
- 포트 연결을 통한 처리 파이프라인 구성
- 다크/라이트 테마 전환
- 노드별 이미지 미리보기와 처리 시간 표시
- 그래프 저장 및 불러오기
- OpenCV 기반 이미지 처리 노드 제공

## 지원 노드

### OpenCV 기본 노드

- Image Input
- External Mat Input
- GrayScale
- Blur
- Heatmap
- Threshold
- Edge Detect
- Histogram
- Brightness / Contrast
- Morphology
- Blob Detect
- Contour Detect
- Resize
- Channel Split
- ROI Color Sampler
- Image Viewer

### OpenCV Detection 노드

- Background Subtraction

## 개발 환경

- Windows
- .NET 8
- Windows Forms
- OpenCvSharp 4

## 실행 방법

저장소를 받은 뒤 솔루션을 빌드하고 실행합니다.

```powershell
dotnet restore
dotnet build FrameAnalyzer.slnx
dotnet run --project FrameAnalyzer/FrameAnalyzer.csproj
```

Visual Studio에서는 `FrameAnalyzer.slnx`를 열고 `FrameAnalyzer` 프로젝트를 시작 프로젝트로 실행하면 됩니다.

## 기본 사용법

1. 앱을 실행하면 기본으로 `Image Input`과 `Image Viewer` 노드가 배치됩니다.
2. `Image Input` 노드에서 이미지를 선택합니다.
3. 캔버스에서 마우스 오른쪽 버튼을 눌러 필요한 OpenCV 노드를 추가합니다.
4. 출력 포트에서 입력 포트로 드래그해 노드를 연결합니다.
5. 상단 `Run` 버튼으로 그래프를 실행합니다.
6. `Preview`와 `Time` 체크박스로 미리보기와 처리 시간 표시를 켜거나 끌 수 있습니다.

## 그래프 저장과 불러오기

캔버스 우클릭 메뉴에서 `Save Graph`와 `Load Graph`를 사용할 수 있습니다.
저장된 그래프는 노드 위치, 포트, 연결 정보를 보관하며, 불러올 때 등록된 노드 팩토리를 통해 실제 컨트롤과 처리기를 다시 생성합니다.

## 프로젝트 구조

```text
FrameAnalyzer/
  App/                              앱 시작 화면과 WinForms 디자이너 파일
  NodeEditor/
    Contracts/                      노드 런타임 인터페이스
    Core/                           노드, 포트, 연결, 그래프 실행 데이터
    UI/                             노드 캔버스와 테마
  Nodes/OpenCv/
    Common/                         OpenCV 노드 공통 미리보기 컨트롤
    Basic/                          OpenCV 기본 처리 노드와 UI
    Detection/                      OpenCV 검출 처리 노드와 UI
    OpenCvNodeRegistrar.cs          기본 OpenCV 노드 등록
    OpenCvDetectionNodeRegistrar.cs 검출 계열 노드 등록
  Controls/                         공통 입력 컨트롤
```

## Git 관리

`.vs`, `bin`, `obj`, `*.user` 파일은 로컬 개발 환경과 빌드 산출물이므로 Git에서 제외합니다.

## 라이선스

이 저장소의 라이선스는 `LICENSE` 파일을 확인하세요.
