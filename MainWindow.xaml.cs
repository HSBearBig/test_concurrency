using System.Windows;
using System.Windows.Threading;
using CryptoPriceMonitor.Services;

namespace CryptoPriceMonitor;

public partial class MainWindow : Window
{
    private readonly CryptoPriceMonitorService _monitorService;
    private readonly DispatcherTimer _uiUpdateTimer;

    public MainWindow()
    {
        InitializeComponent();
        
        _monitorService = new CryptoPriceMonitorService();
        
        // 設置數據綁定
        CryptoDataGrid.ItemsSource = _monitorService.Cryptocurrencies;
        
        // 訂閱事件
        _monitorService.PriceUpdated += OnPriceUpdated;
        _monitorService.StatusChanged += OnStatusChanged;
        
        // 創建UI更新計時器
        _uiUpdateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _uiUpdateTimer.Tick += OnUiUpdateTimer_Tick;
        _uiUpdateTimer.Start();
        
        // 窗口關閉事件
        Closing += MainWindow_Closing;
        
        StatusTextBlock.Text = "準備就緒 - 點擊開始監控按鈕開始";
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            
            StatusTextBlock.Text = "正在啟動監控服務...";
            
            // 在背景線程中啟動監控
            await Task.Run(async () =>
            {
                await _monitorService.StartMonitoringAsync();
            });
        }
        catch (Exception ex)
        {
            Dispatcher.Invoke(() =>
            {
                MessageBox.Show($"啟動監控時發生錯誤: {ex.Message}", "錯誤", 
                               MessageBoxButton.OK, MessageBoxImage.Error);
                
                StartButton.IsEnabled = true;
                StopButton.IsEnabled = false;
                StatusTextBlock.Text = "啟動失敗";
            });
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _monitorService.StopMonitoring();
            
            StartButton.IsEnabled = true;
            StopButton.IsEnabled = false;
            
            StatusTextBlock.Text = "監控已停止";
            ActiveConnectionsText.Text = "0";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"停止監控時發生錯誤: {ex.Message}", "錯誤", 
                           MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnPriceUpdated(object? sender, PriceUpdateEventArgs e)
    {
        // 確保UI更新在主線程執行
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                // 觸發視覺效果（可以在這裡添加價格變化動畫）
                AnimatePriceChange(e);
                
                // 更新狀態信息
                var now = DateTime.Now;
                StatusTextBlock.Text = $"最後更新: {now:HH:mm:ss} - {e.Symbol} 價格更新";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UI更新錯誤: {ex.Message}");
            }
        });
    }

    private void OnStatusChanged(object? sender, string status)
    {
        Dispatcher.BeginInvoke(() =>
        {
            StatusTextBlock.Text = status;
        });
    }

    private void OnUiUpdateTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            // 更新活躍連線數
            var activeConnections = _monitorService.GetActiveConnections();
            ActiveConnectionsText.Text = activeConnections.ToString();
            
            // 如果沒有活躍連線但按鈕狀態不正確，進行修正
            if (activeConnections == 0 && StopButton.IsEnabled)
            {
                // 可能監控已自動停止，更新UI狀態
                var hasOnlineStatus = _monitorService.Cryptocurrencies.Any(c => c.Status == "線上");
                if (!hasOnlineStatus)
                {
                    StartButton.IsEnabled = true;
                    StopButton.IsEnabled = false;
                    StatusTextBlock.Text = "監控已停止";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"UI計時器更新錯誤: {ex.Message}");
        }
    }

    private void AnimatePriceChange(PriceUpdateEventArgs e)
    {
        try
        {
            // 簡單的視覺反饋 - 可以擴展為更複雜的動畫效果
            var crypto = _monitorService.Cryptocurrencies.FirstOrDefault(c => c.Symbol == e.Symbol);
            if (crypto != null)
            {
                // 在這裡可以添加更複雜的動畫效果
                // 例如：閃爍效果、顏色變化等
                
                Console.WriteLine($"{e.Symbol}: ${e.PreviousPrice:F2} -> ${e.CurrentPrice:F2} " +
                                $"({(e.CurrentPrice > e.PreviousPrice ? "↑" : e.CurrentPrice < e.PreviousPrice ? "↓" : "→")})");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"動畫效果錯誤: {ex.Message}");
        }
    }

    private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try
        {
            // 停止計時器
            _uiUpdateTimer?.Stop();
            
            // 停止監控服務
            _monitorService?.StopMonitoring();
            _monitorService?.Dispose();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"關閉窗口時發生錯誤: {ex.Message}");
        }
    }
}