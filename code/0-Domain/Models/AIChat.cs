using Newtonsoft.Json;

namespace DocMind
{
    public class ChatRequest 
    {
        public string model { get; set; }
        public ChatMessage[] messages { get; set; }
        public double temperature { get; set; }
        public int max_tokens { get; set; }

        public bool stream { get; set; }
    }

    public class ChatMessage
    {
        public required string role { get; set; }
        public required string content { get; set; }
    }


    public class DeepSeekResponse
    {
        public Choice[] choices { get; set; }
    }

    public class Choice
    {
        public ContentMessage message { get; set; }

        [JsonProperty("delta")]
        public DeepSeekStreamDelta Delta { get; set; }
    }

    public class ContentMessage
    {
        public string content { get; set; }
    }

    public class DeepSeekStreamDelta
    {
        [JsonProperty("content")]
        public string Content { get; set; }
    }
}
