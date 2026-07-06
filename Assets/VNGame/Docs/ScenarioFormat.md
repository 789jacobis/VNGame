# VNGame Scenario Format

劇本建議先用 Excel 或 Google Sheets 維護，再匯出 UTF-8 CSV 到：

`Assets/StreamingAssets/VNGame/Scenarios/{scenarioId}.csv`

目前範例是：

`Assets/StreamingAssets/VNGame/Scenarios/prologue.csv`

## Columns

| 欄位 | 用途 |
| --- | --- |
| id | 對話列唯一 ID。存檔會記住目前 line id。 |
| speaker | 說話者名稱。 |
| text | 台詞內容。CSV 中可用引號包住中文、逗號、換行。 |
| bg | 背景 key。對應 `Assets/VNGame/Art/Backgrounds` 底下的路徑，不含副檔名。 |
| leftChar | 左側立繪 key。對應 `Assets/VNGame/Art/Characters` 底下的路徑，不含副檔名。 |
| centerChar | 中央立繪 key。對應 `Assets/VNGame/Art/Characters` 底下的路徑，不含副檔名。 |
| rightChar | 右側立繪 key。對應 `Assets/VNGame/Art/Characters` 底下的路徑，不含副檔名。 |
| bgm | BGM key，預留。 |
| sfx | 音效 key，預留。 |
| command | 指令欄，預留給 choice/jump/set 等劇情指令。 |
| next | 下一列 id。空白時會照表格順序往下。 |

## Visual Assets

角色立繪放在：

`Assets/VNGame/Art/Characters/{CharacterOrGroup}/{pose}.png`

劇本中填入相對於 `Characters` 的路徑，不含副檔名。例如：

| 檔案 | 劇本 key |
| --- | --- |
| `Assets/VNGame/Art/Characters/Rei/happy.png` | `Rei/happy` |
| `Assets/VNGame/Art/Characters/Rei/serious.png` | `Rei/serious` |
| `Assets/VNGame/Art/Characters/Demo/center.png` | `Demo/center` |

目前測試用三個暫用立繪位於：

- `Assets/VNGame/Art/Characters/Demo/left.png`
- `Assets/VNGame/Art/Characters/Demo/center.png`
- `Assets/VNGame/Art/Characters/Demo/right.png`

在 Unity 裡執行 `VNGame > Build View Scene` 時，SceneBuilder 會掃描 `Characters` 和 `Backgrounds` 資料夾，自動把 Sprite 建成 key-to-sprite 綁定。正式立繪只要依照資料夾規則放好，劇本填對 key，不需要手動在 Inspector 拖一大串圖。

## Implemented Controls

- 左鍵 / Space / Enter：推進對話；文字顯示中會先補完。
- 右鍵 / Esc：關閉目前頁面，或從遊戲返回主選單。
- Auto：自動播放。
- Skip：略過。設定中可切換是否允許略過未讀。
- Log：顯示最近台詞。
- Q.Save：遊戲中直接存入下一個快速存檔格，Q001-Q012 用完後循環覆蓋。
- Q.Load：遊戲中直接讀取最新的快速存檔。
- Save：一般存檔，玩家自選 slot 001-012。
- Load：一般讀檔頁，頁面上可切換 Load / QLoad；QLoad 可手動選任一快速存檔。

## Save Data

存檔位於：

`Application.persistentDataPath/VNGame/Saves`

一般存檔使用 slot 001-012；快速存檔使用 Q001-Q012，但檔名內部會用不同 slot id 避免互相覆蓋。

每個 slot 會有：

- `slot_001.json`：GameState、日期時間、縮圖檔名。
- `slot_001.png`：按下 Save 當下的遊戲畫面截圖。
- `quick_index.json`：記錄下一次 QSave 要寫入哪一格，以及最新 QLoad 目標。

`GameState` 已預留 variables、characters、questStates、inventory、money，之後可接地圖、任務、金錢與角色數值。
