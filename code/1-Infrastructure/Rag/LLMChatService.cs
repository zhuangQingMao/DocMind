using Newtonsoft.Json;
using System.IO;
using System.Net.Http;
using System.Text;

namespace DocMind
{
    public interface ILLMChatService
    {
        Task<string> ChatAsync(string userMessage, string systemMessage);
        IAsyncEnumerable<string> StreamChatAsync(string userMessage, string systemMessage);
    }

    public class LLMChatService : ILLMChatService
    {
        private readonly double Temperature = 0.7;
        private readonly int Max_tokens = 2048;
        private readonly HttpClient _httpClient;

        public LLMChatService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {LlmApi.ApiKey}");
        }

        private HttpRequestMessage GetRequest(string userMessage, string systemMessage, bool isStream = false)
        {
            var requestBody = new ChatRequest
            {
                model = "deepseek-chat",
                messages =
               [
                   new()
                    {
                        role = "system",
                        content = systemMessage
                    },
                    new()
                    {
                        role = "user",
                        content = userMessage
                    }
               ],
                temperature = Temperature,
                max_tokens = Max_tokens,
                stream = isStream
            };

            var jsonBody = JsonConvert.SerializeObject(requestBody);

            var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, LlmApi.ApiEndpoint)
            {
                Content = content
            };

            return request;
        }

        public async Task<string> ChatAsync(string userMessage, string systemMessage)
        {
            using var request = GetRequest(userMessage, systemMessage, false);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, default);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();

            var responseObj = JsonConvert.DeserializeObject<DeepSeekResponse>(responseContent);

            return responseObj?.choices?[0]?.message?.content ?? "未获取到响应内容";
        }

        public async IAsyncEnumerable<string> StreamChatAsync(string userMessage, string systemMessage)
        {
            using var request = GetRequest(userMessage, systemMessage, true);

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, default);

            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream, Encoding.UTF8);

            var fullResponse = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.StartsWith("data: "))
                {
                    var data = line["data: ".Length..].Trim();

                    if (data == "[DONE]")
                        break;

                    var streamResponse = JsonConvert.DeserializeObject<DeepSeekResponse>(data);
                    if (streamResponse?.choices != null && streamResponse.choices.Count() > 0)
                    {
                        var content = streamResponse.choices[0].Delta?.Content;
                        if (!string.IsNullOrEmpty(content))
                        {
                            fullResponse.Append(content);
                            yield return content;//
                        }
                    }
                }
            }
        }
    }
}
