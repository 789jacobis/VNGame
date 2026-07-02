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
