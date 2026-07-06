# VNGame View Architecture

目前 UI 已從 runtime 自動生成改為 View/Prefab 架構。

## Generate Scene

在 Unity 編譯完成後，點選：

`VNGame > Build View Scene`

它會產生：

- `Assets/Scenes/VNGameScene.unity`
- `Assets/VNGame/Prefabs/SaveSlotView.prefab`

開啟 `VNGameScene` 後，Hierarchy 會看到：

```text
VNGameRoot
- Canvas
  - MainMenuView
  - NovelView
  - SaveLoadView
  - SettingsView
  - BacklogView
  - CreditsView
- EventSystem
- Main Camera
```

## Sprite Replacement

所有主要 UI 元件都使用 Unity UI `Image`：

- 主選單按鈕
- Novel 背景
- 左/中/右立繪
- 對話框
- Save/Load 視窗
- Save slot prefab

因此可以直接在 Inspector 對 `Image.sprite` 拖入正式素材。現在仍是 placeholder 色塊，但已經是可替換 sprite 的 Unity 物件。

## Novel Controls

- 右鍵 / Esc：只關閉目前 overlay；遊戲進行中不會直接退回主選單。
- Hide：隱藏或顯示對話框，不影響角色立繪和背景。
- 左/中/右三個立繪 Image 會使用 placeholder sprite，確認同一畫面最多三人同屏的版面。

## Placeholder Sprites

目前提供三張臨時長方形人物立繪：

- `Assets/VNGame/Art/Placeholders/placeholder_character_left.png`
- `Assets/VNGame/Art/Placeholders/placeholder_character_center.png`
- `Assets/VNGame/Art/Placeholders/placeholder_character_right.png`

範例劇本 `prologue.csv` 使用：

- `left_demo`
- `rei_smile` / `rei_happy` / `rei_serious`
- `right_demo`

## Script Layout

```text
Assets/VNGame/Scripts/Core
Assets/VNGame/Scripts/Novel
Assets/VNGame/Scripts/Save
Assets/VNGame/Scripts/UI
Assets/VNGame/Scripts/Assets
Assets/VNGame/Editor
```

`VNGameController` 負責流程與功能；各 `*View` 只負責 UI references 和 button events。
