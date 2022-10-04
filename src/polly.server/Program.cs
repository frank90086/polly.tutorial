using System.Net;
using System.Net.Http.Json;
using Polly;
using Polly.Server;

var urls = new[]
           {
               "https://run.mocky.io/v3/bcd7ed05-df2b-4aa8-9ccf-ea3b28a7ca9c",
               "https://run.mocky.io/v3/7dbc2e97-ff5f-45b5-b687-478792149602",
               "https://run.mocky.io/v3/2872316f-f596-4cde-8d3b-4864e0cbf89e",
               "https://run.mocky.io/v3/9cd85d8a-1edb-4e69-a2da-bb8667e4e0d8"
           };

await Policy.Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => r 
                                                    is { StatusCode: >= HttpStatusCode.InternalServerError } 
                                                       or { StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.NotFound })
            .WaitAndRetryAsync(5, retryTimes => TimeSpan.FromSeconds(Math.Pow(2, retryTimes)) + TimeSpan.FromMilliseconds(new Random().Next(0, 1000)),
                               onRetryAsync: async (response, retrySpan, retryCount, _) =>
                                             {
                                                 Console.WriteLine($"------第 {retryCount} 呼叫------");
                                                 Console.WriteLine($"------等待 {retrySpan}------");
                                                 Console.WriteLine($"錯誤碼: {response.Result.StatusCode}");

                                                 var result = await response.Result.Content.ReadFromJsonAsync<MockyResponse>();

                                                 Console.WriteLine($"發生錯誤: {result}");
                                                 Console.WriteLine("");
                                             })
            .ExecuteAsync(async ct => await MockyClient.Call(urls[new Random().Next(0, 4)], ct), CancellationToken.None);

public record MockyResponse(string Message, int Code, MockyResponseContent Content);

public record MockyResponseContent(long Id, string Username, string Email);