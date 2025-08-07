using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CryptoPriceMonitor.Models;

namespace CryptoPriceMonitor.Services;

public class CoinGeckoApiClient
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.coingecko.com/api/v3";

    public CoinGeckoApiClient()
    {
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<List<CryptoCurrency>> GetCryptoPricesAsync(string[] cryptoIds)
    {
        var cryptoList = new List<CryptoCurrency>();
        
        try
        {
            var ids = string.Join(",", cryptoIds);
            var url = $"{BaseUrl}/simple/price?ids={ids}&vs_currencies=usd&include_24hr_change=true&include_market_cap=true";
            
            var response = await _httpClient.GetStringAsync(url);
            var data = JsonConvert.DeserializeObject<JObject>(response);
            
            if (data != null)
            {
                foreach (var cryptoId in cryptoIds)
                {
                    if (data.ContainsKey(cryptoId))
                    {
                        var cryptoData = data[cryptoId];
                        if (cryptoData != null)
                        {
                            var crypto = new CryptoCurrency
                            {
                                Symbol = cryptoId.ToUpper(),
                                Name = GetCryptoName(cryptoId),
                                Price = cryptoData["usd"]?.Value<decimal>() ?? 0,
                                Change24h = cryptoData["usd_24h_change"]?.Value<decimal>() ?? 0,
                                MarketCap = cryptoData["usd_market_cap"]?.Value<decimal>() ?? 0,
                                LastUpdate = DateTime.Now,
                                Status = "線上"
                            };
                            
                            cryptoList.Add(crypto);
                        }
                    }
                }
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"網絡錯誤: {ex.Message}");
            foreach (var cryptoId in cryptoIds)
            {
                cryptoList.Add(new CryptoCurrency
                {
                    Symbol = cryptoId.ToUpper(),
                    Name = GetCryptoName(cryptoId),
                    Status = "連線錯誤"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API 錯誤: {ex.Message}");
        }
        
        return cryptoList;
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

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

public static class StringExtensions
{
    public static string ToTitleCase(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;
            
        var words = input.Split(' ');
        for (int i = 0; i < words.Length; i++)
        {
            if (words[i].Length > 0)
            {
                words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1).ToLower();
            }
        }
        return string.Join(" ", words);
    }
}