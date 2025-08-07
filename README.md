# 加密貨幣價格監控系統

這是一個使用 C# WPF 開發的加密貨幣價格監控桌面應用程序，具有並發監控功能。

## 功能特性

### 🚀 並發特性
- **多線程監控**: 使用 `Task` 和 `CancellationToken` 實現並發監控多種加密貨幣
- **信號量控制**: 使用 `SemaphoreSlim` 限制同時 API 請求數量（最多5個）
- **線程安全**: 使用 `ConcurrentDictionary` 管理監控任務
- **UI 線程分離**: 後台線程處理數據，主線程更新 UI

### 💰 監控功能
- 實時監控 10 種主流加密貨幣價格
- 24 小時價格變化百分比顯示
- 市值信息顯示
- 每 10 秒自動更新價格數據
- 連線狀態實時監控

### 🔔 智能通知
- **價格閾值警報**: 可設定目標價格觸發通知
- **大幅變動提醒**: 24小時變化超過5%時提醒
- **瞬間價格變化**: 檢測短期內價格急劇變化（>2%）
- **音效提醒**: 不同類型通知搭配不同系統音效

### 🎨 用戶界面
- 現代化 WPF 界面設計
- 實時數據綁定和自動更新
- 價格漲跌顏色指示（綠色上漲，紅色下跌）
- 狀態欄顯示活躍連線數和最後更新時間

## 技術架構

### 並發設計模式
1. **生產者-消費者模式**: API 客戶端獲取數據，UI 消費並顯示
2. **觀察者模式**: 價格更新事件通知系統
3. **資源池模式**: 使用信號量控制 HTTP 連線數量

### 核心類別
- `CryptoPriceMonitorService`: 主要監控服務，管理並發任務
- `CoinGeckoApiClient`: API 客戶端，處理網路請求  
- `NotificationService`: 通知服務，處理警報邏輯
- `CryptoCurrency`: 數據模型，支持 MVVM 綁定

## 執行步驟

### 環境要求
- .NET 8.0 或更高版本
- Windows 操作系統（支援 WPF）

### 編譯執行
```bash
# 編譯項目
dotnet build

# 執行應用程式
dotnet run
```

### 使用說明
1. 啟動應用程式
2. 點擊「開始監控」按鈕
3. 觀察價格數據實時更新
4. 系統會自動檢測價格異常並發出通知
5. 點擊「停止監控」可停止所有監控任務

## 監控的加密貨幣
- Bitcoin (BTC)
- Ethereum (ETH)  
- Binance Coin (BNB)
- Cardano (ADA)
- Solana (SOL)
- Polkadot (DOT)
- Dogecoin (DOGE)
- Polygon (MATIC)
- Chainlink (LINK)
- Litecoin (LTC)

## API 來源
使用 [CoinGecko API](https://coingecko.com/api) 獲取實時加密貨幣價格數據。

---
*本項目展示了 C# 中的並發程式設計概念，包括多線程處理、資源管理和線程安全操作。*