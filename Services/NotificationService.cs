using System.Media;
using System.Windows;

namespace CryptoPriceMonitor.Services;

public class NotificationService
{
    private readonly Dictionary<string, decimal> _priceThresholds;
    private readonly SoundPlayer? _alertSound;

    public NotificationService()
    {
        _priceThresholds = new Dictionary<string, decimal>();
        
        try
        {
            // 可以添加音效文件，這裡使用系統提示音
            _alertSound = new SoundPlayer();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"音效初始化失敗: {ex.Message}");
        }
    }

    public void SetPriceAlert(string symbol, decimal threshold)
    {
        _priceThresholds[symbol] = threshold;
    }

    public void CheckPriceAlerts(string symbol, decimal currentPrice, decimal previousPrice, decimal change24h)
    {
        try
        {
            // 檢查價格閾值警報
            if (_priceThresholds.ContainsKey(symbol))
            {
                var threshold = _priceThresholds[symbol];
                if (currentPrice >= threshold && previousPrice < threshold)
                {
                    ShowNotification($"{symbol} 價格警報", 
                                   $"{symbol} 已達到目標價格 ${currentPrice:C}！", 
                                   NotificationType.PriceAlert);
                }
            }

            // 檢查大幅價格變動（超過5%）
            if (Math.Abs(change24h) > 5)
            {
                var direction = change24h > 0 ? "上漲" : "下跌";
                var icon = change24h > 0 ? "🚀" : "📉";
                
                ShowNotification($"{symbol} 大幅{direction}", 
                               $"{symbol} 在24小時內{direction}了 {Math.Abs(change24h):F2}% {icon}", 
                               NotificationType.SignificantChange);
            }

            // 檢查瞬間價格變化（與上次更新相比）
            var instantChange = previousPrice > 0 ? ((currentPrice - previousPrice) / previousPrice) * 100 : 0;
            if (Math.Abs(instantChange) > 2) // 瞬間變化超過2%
            {
                var direction = instantChange > 0 ? "急漲" : "急跌";
                ShowNotification($"{symbol} 價格{direction}", 
                               $"{symbol}: ${previousPrice:C} → ${currentPrice:C} ({instantChange:+0.00;-0.00}%)", 
                               NotificationType.InstantChange);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"價格警報檢查錯誤: {ex.Message}");
        }
    }

    private void ShowNotification(string title, string message, NotificationType type)
    {
        try
        {
            // Windows 10/11 Toast 通知（簡化版）
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                try
                {
                    // 播放提示音
                    if (type == NotificationType.PriceAlert || type == NotificationType.SignificantChange)
                    {
                        SystemSounds.Exclamation.Play();
                    }
                    else
                    {
                        SystemSounds.Beep.Play();
                    }

                    // 在控制台顯示通知（實際應用中可以使用 Windows Toast 或第三方通知庫）
                    var timestamp = DateTime.Now.ToString("HH:mm:ss");
                    Console.WriteLine($"[{timestamp}] 🔔 {title}: {message}");
                    
                    // 也可以在這裡添加彈出式通知窗口
                    if (type == NotificationType.PriceAlert)
                    {
                        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"顯示通知時發生錯誤: {ex.Message}");
                }
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"通知服務錯誤: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _alertSound?.Dispose();
    }
}

public enum NotificationType
{
    PriceAlert,
    SignificantChange,
    InstantChange,
    SystemAlert
}