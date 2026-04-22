//using System;
//using System.Linq;
//using System.Net.Http;
//using System.Text;
//using System.Text.Json;
//using System.Speech.Synthesis;
//using System.Threading.Tasks;

//namespace P1.Common
//{
//    //与LM studio建立联系对话的服务类
//    public class SpeechHelper
//    {
//        private static readonly string ApiBaseUrl = "http://localhost:1234";
//        private static readonly HttpClient _httpClient = new();
//        private SpeechSynthesizer? _synthesizer;

//        // 关键：AI 回复事件
//        public event Action<string>? OnAiReply;

//        public async Task<bool> TestConnectionAsync()
//        {
//            try
//            {
//                var res = await _httpClient.GetAsync($"{ApiBaseUrl}/v1/models");
//                return res.IsSuccessStatusCode;
//            }
//            catch
//            {
//                return false;
//            }
//        }

//        public SpeechHelper()
//        {
//            InitializeSpeech();
//        }

//        private void InitializeSpeech()
//        {
//            _synthesizer = new SpeechSynthesizer();
//            var chinese = _synthesizer.GetInstalledVoices()
//                .FirstOrDefault(v => v.VoiceInfo.Culture.Name.StartsWith("zh"));

//            if (chinese != null)
//                _synthesizer.SelectVoice(chinese.VoiceInfo.Name);

//            _synthesizer.Rate = 3;
//            _synthesizer.Volume = 100;
//        }

//        public async Task SendUserMessageAsync(string userMessage)
//        {
//            string reply = await GetAIResponseAsync(userMessage);

//            // 通知 UI
//            OnAiReply?.Invoke(reply);

//            // 语音播报
//            _synthesizer?.SpeakAsync(reply);
//        }

//        private async Task<string> GetAIResponseAsync(string userMessage)
//        {
//            var body = new
//            {
//                messages = new[] { new { role = "user", content = userMessage } },
//                temperature = 0.7,
//                max_tokens = 500
//            };

//            var json = JsonSerializer.Serialize(body);
//            var content = new StringContent(json, Encoding.UTF8, "application/json");

//            var response = await _httpClient.PostAsync(
//                $"{ApiBaseUrl}/v1/chat/completions", content);

//            if (!response.IsSuccessStatusCode)
//                return "AI 服务异常";

//            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
//            return doc.RootElement
//                .GetProperty("choices")[0]
//                .GetProperty("message")
//                .GetProperty("content")
//                .GetString() ?? "空回复";
//        }
//    }

//}

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace P1.Common 
{
    public class SpeechHelper : IDisposable 
    {
        private readonly ModuleConnectionOptions _options = ModuleConnectionOptions.Instance;
        private TcpClient? _tcpClient;
        private NetworkStream? _stream;
        private readonly object _syncRoot = new();

        public SpeechHelper() 
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private void EnsureConnected() 
        {
            if (_tcpClient?.Connected == true && _stream != null)
            {
                return;
            }

            DisposeConnection();

            try
            {
                var client = new TcpClient();
                using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
                client.ConnectAsync(_options.ModuleIp, _options.ModulePort, cts.Token).GetAwaiter().GetResult();

                _tcpClient = client;
                _stream = _tcpClient.GetStream();
                _stream.WriteTimeout = 1000;

                Console.WriteLine($"[{_options.ModuleIp}:{_options.ModulePort}] 语音链路已连接");
            }
            catch (Exception ex)
            {
                DisposeConnection();
                Console.WriteLine($"[{_options.ModuleIp}:{_options.ModulePort}] 语音链路连接失败: {ex.Message}");
                throw;
            }
        }

        public void Speak(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            lock (_syncRoot)
            {
                try
                {
                    EnsureConnected();
                    SendText(text);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{_options.ModuleIp}:{_options.ModulePort}] 语音发送失败，尝试重连: {ex.Message}");
                    try
                    {
                        DisposeConnection();
                        EnsureConnected();
                        SendText(text);
                    }
                    catch (Exception retryEx)
                    {
                        Console.WriteLine($"[{_options.ModuleIp}:{_options.ModulePort}] 重连后发送仍失败: {retryEx.Message}");
                    }
                }
            }
        }

        private void SendText(string text)
        {
            if (_stream == null)
            {
                throw new InvalidOperationException("语音链路未建立");
            }

            var gb2312 = Encoding.GetEncoding("GB2312");
            string command = $"#[v8]{text}";
            byte[] payload = gb2312.GetBytes(command);
            _stream.Write(payload, 0, payload.Length);
            _stream.Flush();
        }

        public void Dispose() 
        {
            lock (_syncRoot)
            {
                DisposeConnection();
            }
        }

        private void DisposeConnection()
        {
            try
            {
                _stream?.Dispose();
                _tcpClient?.Dispose();
            }
            finally
            {
                _stream = null;
                _tcpClient = null;
            }
        }
    }
}