using System.Net.Http.Json;

namespace Polly.Server;

public static class MockyClient
{
    public static async Task<HttpResponseMessage> Call(string url, CancellationToken cancellationToken = default)
    {
        using var client = new HttpClient();
        
        Console.WriteLine("開始請求");

        var response = await client.GetAsync(url, cancellationToken);

        if (response.IsSuccessStatusCode)
            Console.WriteLine(await response.Content.ReadFromJsonAsync<MockyResponse>(cancellationToken: cancellationToken));

        Console.WriteLine("結束請求");

        return response;
    }
}