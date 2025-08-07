using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CryptoPriceMonitor.Models;

public class CryptoCurrency : INotifyPropertyChanged
{
    private string _symbol = string.Empty;
    private string _name = string.Empty;
    private decimal _price;
    private decimal _change24h;
    private decimal _marketCap;
    private DateTime _lastUpdate;
    private string _status = "離線";

    public string Symbol
    {
        get => _symbol;
        set => SetProperty(ref _symbol, value);
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public decimal Change24h
    {
        get => _change24h;
        set
        {
            SetProperty(ref _change24h, value);
            OnPropertyChanged(nameof(IsPositiveChange));
            OnPropertyChanged(nameof(IsNegativeChange));
        }
    }

    public decimal MarketCap
    {
        get => _marketCap;
        set => SetProperty(ref _marketCap, value);
    }

    public DateTime LastUpdate
    {
        get => _lastUpdate;
        set => SetProperty(ref _lastUpdate, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool IsPositiveChange => Change24h > 0;
    public bool IsNegativeChange => Change24h < 0;

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}