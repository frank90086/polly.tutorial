using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.CircuitBreaker;

class Program
{
    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> CreateCircuitBreakerPolicy()
    {
        return Policy.Handle<HttpRequestException>()
                     .OrResult<HttpResponseMessage>(r => r
                                                             is { StatusCode: >= HttpStatusCode.InternalServerError }
                                                                or { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.NotFound })
                     .CircuitBreakerAsync(2,
                                          TimeSpan.FromSeconds(10),
                                          onBreak: (response, state, retryTime, context) =>
                                                   {
                                                       var msg = response.Exception?.Message;

                                                       Console.WriteLine($"------發生錯誤------");
                                                       Console.WriteLine($"Breaker State: {_circuitBreakerPolicy.CircuitState}");
                                                   },
                                          onReset: (_) =>
                                                   {
                                                       Console.WriteLine("------重設斷路器------");
                                                       Console.WriteLine($"Breaker State: {_circuitBreakerPolicy.CircuitState}");
                                                   },
                                          onHalfOpen: () =>
                                                      {
                                                          Console.WriteLine("------斷路器半開------");
                                                          Console.WriteLine($"Breaker State: {_circuitBreakerPolicy.CircuitState}");
                                                      });
    }

    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> _circuitBreakerPolicy;

    public static async Task Main(string[] args)
    {
        _circuitBreakerPolicy = CreateCircuitBreakerPolicy();
        
        var services = new ServiceCollection();
        var tasks = new Task[1];
        var paths = new[]
                   {
                       "v3/bcd7ed05-df2b-4aa8-9ccf-ea3b28a7ca9c",
                       "v3/7dbc2e97-ff5f-45b5-b687-478792149602",
                       "v3/2872316f-f596-4cde-8d3b-4864e0cbf89e",
                       "v3/9cd85d8a-1edb-4e69-a2da-bb8667e4e0d8"
                   };
        
        services.AddHttpClient("mocky", client =>
                                        {
                                            client.BaseAddress = new Uri("https://run.mocky.io");
                                            client.DefaultRequestHeaders.Add("Accept", "application/json");
                                        })
                .AddPolicyHandler(_circuitBreakerPolicy);
        var serviceProvider = services.BuildServiceProvider();
        var clientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
        var client = clientFactory.CreateClient("mocky");

        do
        {
            var path = paths[new Random().Next(0, 4)];

            Console.WriteLine("");
            Console.WriteLine("------------");
            Console.WriteLine("開始請求");

            try
            {
                var response = await client.GetAsync(path);
                Console.WriteLine(await response.Content.ReadFromJsonAsync<MockyResponse>());
            }
            catch (BrokenCircuitException e)
            {
                Console.WriteLine("斷路器斷開中");
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("結束請求");
            Console.WriteLine("繼續/結束: y/任意鍵");
        } while (Console.ReadKey() is { Key: ConsoleKey.Y });

        Console.WriteLine("------End------");
    }
}

public record MockyResponse(string Message, int Code, MockyResponseContent Content);

public record MockyResponseContent(long Id, string Username, string Email);