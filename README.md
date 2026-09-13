# HD2 Helper — FileLoadout v9

[ChubbyMaru/HD2-Helper](https://github.com/ChubbyMaru/HD2-Helper)를 기반으로 수정한 비공식 포크입니다. 아래 원작 설명은 출처와 기존 기능 안내를 위해 보존했습니다.

## 다운로드 및 업데이트

[이 포크의 Releases](https://github.com/cocos285/HD2-Helper/releases)에서 `HD2-Helper-v9-Overwrite-Update.zip`을 받으세요.
기존 HD2 Helper를 종료한 뒤, 실행 파일이 있는 폴더에 ZIP 내용을 덮어쓰고 다시 실행합니다. 이 ZIP은 업데이트용이므로 기존 images 폴더 등 원본 파일이 필요합니다.

## 추가 및 수정 기능

- testament_new.sav를 읽기 전용으로 1초마다 확인하고, 동일한 내용이 두 번 확인되면 선택한 스트라타잼을 슬롯 1~4 단축키에 연결합니다.
- 기본 식별 코드 67종과 사용자 직접 등록·수정·삭제를 지원합니다.
- 사용자 코드표는 `%LOCALAPPDATA%/HD2-Helper/user_stratagem_codes.json`에, 프리셋·키 설정은 `%APPDATA%/HD2 Helper`에 보관합니다.
- 자동 연결 중 슬롯 5~10은 사용하지 않습니다. 자동 연결을 끄면 수동 프리셋을 사용합니다.
- 한글 쌍받침 입력 시 Shift로 조합이 끊어지는 문제와 빈 슬롯 호출 후 입력 잠금 문제를 수정했습니다.
- 포격 FRV(asdsdsw), 이글 가스 공중타격(wdad)을 반영했습니다.
- v9에서는 장비 코드 앞의 가변 값을 고정값으로 검사해 정상 저장 파일을 거부하던 문제를 수정했습니다.
- 원본 자동 업데이트 확인은 비활성화했습니다.

파일 자동 연결에는 화면 인식을 쓰지 않습니다. 기존 자동선택 단축키(F1)는 화면 인식과 키보드 조작으로 프리셋 장비를 선택하는 별도 기능입니다.

## 검증과 한계

이전 저장 파일 28개와 v9 오류 재현 파일을 검사했습니다. 파일/사용자 등록 검사 및 한글 조합 검사를 수행했습니다. 저장 파일 구조는 관찰된 버전을 기준으로 하며 게임 업데이트에 따라 변경될 수 있습니다. 장비 식별 코드를 공유해도 미보유 장비가 해금되지는 않습니다.

## 빌드

Windows와 .NET 8 SDK에서 `HD2 Helper.csproj`를 Release로 게시합니다. 실행 폴더에는 database.json, stratagem_codes.json 및 images 폴더가 필요합니다.

---

## 원작 README

<img src="https://github.com/user-attachments/assets/3384471b-1ba8-4e44-99de-7250e3e6e3ec" />
<img src="https://github.com/user-attachments/assets/543e33a2-fead-42f9-9d34-a3abf014b862" />
<img src="https://github.com/user-attachments/assets/4e7cbd90-dd1e-4e79-8ce9-7b336592eb8c" />

## 헬다이버즈2 보조 기구
* **스트라타젬 단축키 자동 인식**<br>게임 설정 파일 `input_settings.config` 기반으로 작동하여 별도의 초기 설정이 필요 없습니다.

* **사후 데이터 관리**<br>업데이트가 중단되어도 `database.json` 수정을 통해 직접 추가할 수 있습니다.

* **패드 지원**<br>`XBOX` / `PS` 외에도 다양한 패드를 지원하여, 단축키 등록이 가능합니다.

* **스트라타젬 & 장비 자동 선택**<br>게임 내 장비구성 메뉴에서 단축키 한 번으로 설정된 항목들을 자동으로 선택합니다.<br>
`텍스트 언어 설정은 한국어만 지원하며, 폰트 변경 모드 사용 시 정상 작동하지 않을 수 있습니다.`<br>
`보유하지 않은 스트라타젬 및 장비는 우클릭으로 제외해야 정상 작동합니다.`

* **라디얼 오버레이**<br>게임 내 오버레이로 직관적인 스트라타젬 선택이 가능합니다.

* **슬롯별 단축키**<br>각 슬롯에 설정된 스트라타젬을 복잡한 커맨드 없이 단축키 하나로 사용 가능합니다.

* **한글 채팅**<br>별도 오버레이 없이 게임 내 채팅창에서 즉시 한글을 입력합니다.<br>
캐릭터 안 움직이거나 한글 입력 안될 경우 `ESC` / `발사` / `조준` 버튼으로 상태 초기화가 필요합니다.<br>
패드 연결 시에는 한글 채팅 비활성화 됩니다.

* [보조 기구 장착하기](https://github.com/ChubbyMaru/HD2-Helper/releases/latest)<br>
[![GitHub Downloads](https://img.shields.io/github/downloads/ChubbyMaru/HD2-Helper/total?style=flat-square)](#)

## 단축키 기본 설정값
* **자동선택 :** `F1`
* **오버레이 :** `마우스 휠 클릭`
* **증원 :** `마우스 버튼1(뒤로 가기)`
* **슬롯 :** `설정 필요`

## 프로그램 특수 조작
* **스트라타젬 & 장비 슬롯**<br>
우클릭 : `선택 해제`

* **스트라타젬 슬롯**<br>
드래그 : `슬롯 변경`

* **선택 목록**<br>
우클릭 : `제외` / `제외 해제`

* **프리셋**<br>
우클릭 : `삭제`<br>
더블클릭 : `이름 변경`<br>
드래그 : `순서 변경`<br>

* **프로그램 설정**<br>
단축키 입력 대기 중 우클릭 : `단축키 해제`<br>
입력 딜레이 좌클릭 : `+5ms`<br>
입력 딜레이 우클릭 : `-5ms`

## 업데이트 중단 안내
* **버전 2.0.5 이후로 유지 보수 정도만 진행합니다.**