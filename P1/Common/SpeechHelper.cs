using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace P1.Common
{
    //与LM studio建立联系对话的服务类
    public class SpeechHelper
    {
        private static readonly string ApiBaseUrl = "http://localhost:1234";
        private static readonly HttpClient _httpClient = new();
        private SpeechSynthesizer? _synthesizer;

        // 关键：AI 回复事件
        public event Action<string>? OnAiReply;

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var res = await _httpClient.GetAsync($"{ApiBaseUrl}/v1/models");
                return res.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public SpeechHelper()
        {
            InitializeSpeech();
        }

        private void InitializeSpeech()
        {
            _synthesizer = new SpeechSynthesizer();
            var chinese = _synthesizer.GetInstalledVoices()
                .FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("zh"));

            if (chinese != null)
                _synthesizer.SelectVoice(chinese.VoiceInfo.Name);

            _synthesizer.Rate = 3;
            _synthesizer.Volume = 100;
        }

        public async Task SendUserMessageAsync(string userMessage)
        {
            string reply = await GetAIResponseAsync(userMessage);

            // 通知 UI
            OnAiReply?.Invoke(reply);

            // 语音播报
            _synthesizer?.SpeakAsync(reply);
        }

        private async Task<string> GetAIResponseAsync(string userMessage)
        {
            var body = new
            {
                messages = new[] { new { role = "user", content = userMessage } },
                temperature = 0.7,
                max_tokens = 500
            };

            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                $"{ApiBaseUrl}/v1/chat/completions", content);

            if (!response.IsSuccessStatusCode)
                return "AI 服务异常";

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "空回复";
        }
    }

}