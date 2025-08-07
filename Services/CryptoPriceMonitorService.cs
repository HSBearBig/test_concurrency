using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using CryptoPriceMonitor.Models;

namespace CryptoPriceMonitor.Services;

public class CryptoPriceMonitorService : IDisposable
{
    private readonly CoinGeckoApiClient _apiClient;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _monitoringTasks;
    private readonly SemaphoreSlim _semaphore;
    private readonly NotificationService _notificationService;
    private bool _isMonitoring;

    public ObservableCollection<CryptoCurrency> Cryptocurrencies { get; }
    public event EventHandler<PriceUpdateEventArgs>? PriceUpdated;
    public event EventHandler<string>? StatusChanged;

    public CryptoPriceMonitorService()
    {
        _apiClient = new CoinGeckoApiClient();
        _notificationService = new NotificationService();
        _monitoringTasks = new ConcurrentDictionary<string, CancellationTokenSource>();
        _semaphore = new SemaphoreSlim(5); // 限制同時最多5個API請求
        Cryptocurrencies = new ObservableCollection<CryptoCurrency>();
        
        InitializeCryptocurrencies();
    }

    private void InitializeCryptocurrencies()
    {
        var cryptoIds = new[]
        {
            "bitcoin", "ethereum", "binancecoin", "cardano", "solana",
            "polkadot", "dogecoin", "matic-network", "chainlink", "litecoin"
        };

        foreach (var cryptoId in cryptoIds)
        {
            var crypto = new CryptoCurrency
            {
                Symbol = cryptoId.ToUpper(),
                Name = GetCryptoName(cryptoId),
                Status = "準備中"
            };
            
            Cryptocurrencies.Add(crypto);
        }
    }

    public async Task StartMonitoringAsync()
    {
        if (_isMonitoring) return;
        
        _isMonitoring = true;
        StatusChanged?.Invoke(this, "開始監控...");

        var monitoringTasks = new List<Task>();

        foreach (var crypto in Cryptocurrencies)
        {
            var cryptoId = crypto.Symbol.ToLower();
            var cancellationTokenSource = new CancellationTokenSource();
            
            if (_monitoringTasks.TryAdd(cryptoId, cancellationTokenSource))
            {
                var task = MonitorCryptoPriceAsync(cryptoId, cancellationTokenSource.Token);
                monitoringTasks.Add(task);
            }
        }

        StatusChanged?.Invoke(this, $"正在監控 {monitoringTasks.Count} 種加密貨幣");
        
        await Task.WhenAll(monitoringTasks);
    }

    private async Task MonitorCryptoPriceAsync(string cryptoId, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken);
                
                try
                {
                    var prices = await _apiClient.GetCryptoPricesAsync(new[] { cryptoId });
                    
                    if (prices.Count > 0)
                    {
                        var updatedCrypto = prices[0];
                        var existingCrypto = Cryptocurrencies.FirstOrDefault(c => 
                            c.Symbol.Equals(updatedCrypto.Symbol, StringComparison.OrdinalIgnoreCase));
                        
                        if (existingCrypto != null)
                        {
                            var previousPrice = existingCrypto.Price;
                            
                            // 更新數據
                            existingCrypto.Price = updatedCrypto.Price;
                            existingCrypto.Change24h = updatedCrypto.Change24h;
                            existingCrypto.MarketCap = updatedCrypto.MarketCap;
                            existingCrypto.LastUpdate = updatedCrypto.LastUpdate;
                            existingCrypto.Status = updatedCrypto.Status;
                            
                            // 檢查價格警報和通知
                            _notificationService.CheckPriceAlerts(
                                existingCrypto.Symbol,
                                existingCrypto.Price,
                                previousPrice,
                                existingCrypto.Change24h);
                            
                            // 觸發價格更新事件
                            PriceUpdated?.Invoke(this, new PriceUpdateEventArgs
                            {
                                Symbol = existingCrypto.Symbol,
                                PreviousPrice = previousPrice,
                                CurrentPrice = existingCrypto.Price,
                                Change24h = existingCrypto.Change24h
                            });
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
                
                // 每10秒更新一次
                await Task.Delay(10000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // 正常取消操作
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"監控 {cryptoId} 時發生錯誤: {ex.Message}");
                
                var crypto = Cryptocurrencies.FirstOrDefault(c => 
                    c.Symbol.Equals(cryptoId, StringComparison.OrdinalIgnoreCase));
                if (crypto != null)
                {
                    crypto.Status = "錯誤";
                }
                
                await Task.Delay(5000, cancellationToken); // 錯誤時等待5秒再重試
            }
        }
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;
        
        _isMonitoring = false;
        StatusChanged?.Invoke(this, "停止監控中...");

        foreach (var kvp in _monitoringTasks)
        {
            kvp.Value.Cancel();
        }
        
        _monitoringTasks.Clear();
        
        foreach (var crypto in Cryptocurrencies)
        {
            crypto.Status = "已停止";
        }
        
        StatusChanged?.Invoke(this, "監控已停止");
    }

    public int GetActiveConnections()
    {
        return _monitoringTasks.Count(kvp => !kvp.Value.Token.IsCancellationRequested);
    }

    private static string GetCryptoName(string cryptoId)
    {
        return cryptoId switch
        {
            "bitcoin" => "Bitcoin",
            "ethereum" => "Ethereum",
            "binancecoin" => "BNB",
            "cardano" => "Cardano",
            "solana" => "Solana",
            "polkadot" => "Polkadot",
            "dogecoin" => "Dogecoin",
            "matic-network" => "Polygon",
            "chainlink" => "Chainlink",
            "litecoin" => "Litecoin",
            _ => cryptoId.Replace("-", " ").ToTitleCase()
        };
    }

    public void SetPriceAlert(string symbol, decimal threshold)
    {
        _notificationService.SetPriceAlert(symbol, threshold);
    }

    public void Dispose()
    {
        StopMonitoring();
        _apiClient?.Dispose();
        _semaphore?.Dispose();
        _notificationService?.Dispose();
        
        foreach (var kvp in _monitoringTasks.Values)
        {
            kvp.Dispose();
        }
    }
}

public class PriceUpdateEventArgs : EventArgs
{
    public string Symbol { get; set; } = string.Empty;
    public decimal PreviousPrice { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal Change24h { get; set; }
}