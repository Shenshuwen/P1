using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using P1.Common;

using LLama;
using LLama.Common;
using LLama.Sampling;

namespace P1.ViewModels
{
    public partial class ProcessPageViewModel : ViewModelBase, IDisposable
    {
        // ================= 聊天消息模型 =================
        public partial class ChatMessage : ObservableObject
        {
            [ObservableProperty]
            private bool _isUser;
            [ObservableProperty]
            private string _text  = string.Empty;
        }
        private readonly LabStatusService _labStatus;
        // ================= LLamaSharp 类 =================
        private LLamaWeights? _weights;
        private LLamaContext? _context;
        private InteractiveExecutor? _executor;
        private ChatSession? _session;
        private InferenceParams? _inferenceParams;

        public ProcessPageViewModel()
        {
            _labStatus = LabStatusService.Instance;
        }

        // ================= 输入框 =================
        [ObservableProperty]
        private string inputText = string.Empty;

        [ObservableProperty]
        private string aiOnClose = "未连接";
        // ================= RS485语音助手 =================
        private readonly SpeechHelper _speechHelper = new();

        // ================= 消息集合 =================
        public ObservableCollection<ChatMessage> Messages { get; } = new();

        private static string BuildExceptionMessage(Exception ex)
        {
            if (ex is TypeInitializationException tie && tie.InnerException != null)
            {
                return $"{ex.Message} | Inner: {tie.InnerException.Message}";
            }

            return ex.Message;
        }

        private string BuildLabContextData()
        {
            if (_labStatus.History.Count == 0)
            {
                return "当前未接收到实验室实时数据（设备可能离线或未连接 WiFi 模块）。";
            }

            return _labStatus.GetSummary();
        }

        private static string NormalizeAiText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            var normalized = text
                // 清除常见的特定控制符 
                .Replace("<|assistant|>", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("<|user|>", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("<|system|>", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("<|im_start|>", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("<|im_end|>", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("[TOOL:READ_ENV]", string.Empty, StringComparison.OrdinalIgnoreCase);

            // 去掉无论出现在哪里的 User: / Assistant: / System:

            normalized = Regex.Replace(normalized, @"(?im)\b(assistant|user|system)\s*[:：]\s*", string.Empty);

            //清理首尾多余的空格和换行符
            return normalized.Trim(' ', '\r', '\n');
        }

        private static string CleanStreamForDisplay(string buffer)
        {

            string cleaned = NormalizeAiText(buffer);

            // 动态长遮罩：拦截以 '<' 或 '[' 开头且还没闭合的长标签
            int lastAngleBracket = cleaned.LastIndexOf('<');
            if (lastAngleBracket >= 0 && (cleaned.Length - lastAngleBracket) < 30)
            {
                cleaned = cleaned.Substring(0, lastAngleBracket);
            }

            int lastSquareBracket = cleaned.LastIndexOf('[');
            if (lastSquareBracket >= 0 && (cleaned.Length - lastSquareBracket) < 20)
            {
                cleaned = cleaned.Substring(0, lastSquareBracket);
            }

            int tailLength = Math.Min(15, cleaned.Length);
            string tail = cleaned.Substring(cleaned.Length - tailLength).ToLower();

            // 如果尾部含有这几个敏感字母，启动正则预判
            if (tail.Contains("u") || tail.Contains("a") || tail.Contains("s"))
            {
                // 匹配正在输入中的半截残词（比如匹配 "us", "assi", "system:"）
                // \s*$ 表示这些残词必须紧挨着当前输出的最末尾
                var match = Regex.Match(tail, @"(u|us|use|user|user:|a|as|ass|assi|assis|assist|assista|assistan|assistant|assistant:|s|sy|sys|syst|syste|system|system:)\s*$");
                if (match.Success)
                {
                    // 如果发现了残词，直接把这部分从用于显示的 UI 字符串中砍掉暂不显示
                    cleaned = cleaned.Substring(0, cleaned.Length - match.Length);
                }
            }

            return cleaned;
        }
        private static bool IsEnvQuery(string userText)
        {
            if (string.IsNullOrWhiteSpace(userText))
            {
                return false;
            }

            var text = userText.ToLowerInvariant();
            string[] keywords =
            [
                "温度", "湿度", "风速", "压力", "环境", "传感器", "实验室状态", "实时数据",
                "temperature", "humidity", "pressure", "wind", "sensor", "environment"
            ];

            foreach (var keyword in keywords)
            {
                if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // ================= 连接 AI =================
        [RelayCommand]
        public async Task OpenAi()
        {
            AiOnClose = "连接中...";
            Messages.Add(new ChatMessage
            {
                IsUser = false,
                Text = "正在连接 AI 服务，请稍候..."
            });

            try 
            {
                await Task.Run(() => {
                    string modelPath = @"D:\\Work\\Projects\\llama.cpp-master\\build\\build\\bin\\models\\qwen2.5-3b-instruct-q4_k_m.gguf";
                    if (!File.Exists(modelPath))
                        throw new FileNotFoundException($"模型文件不存在: {modelPath}");

                    var parameters = new ModelParams(modelPath)
                    {
                        ContextSize = 4096,
                        GpuLayerCount = 0
                    };
                    _weights = LLamaWeights.LoadFromFile(parameters);
                    _context = _weights.CreateContext(parameters);
                    _executor = new InteractiveExecutor(_context);

                    // 设置推理参数
                    var chatHistory = new ChatHistory();
                    string systemPrompt = @"你是实验室智能助手.
                    提供关于实验室环境和设备的实时信息。请基于用户的问题和实验室状态数据进行回答。
                    【查询模式】:如果用户的问题涉及实验室的温度、湿度等实验室环境内容，请使用[TOOL:READ_ENV]格式提供回答.
                    【普通模式】:如果用户的问题不涉及实验室环境内容，请直接回答问题，不要使用工具格式.";

                    chatHistory.AddMessage(AuthorRole.System,systemPrompt);
                    _session = new ChatSession(_executor, chatHistory);

                    _inferenceParams = new InferenceParams()
                    {
                        MaxTokens = 512,
                        AntiPrompts = [
                            "User:"],
                        SamplingPipeline = new DefaultSamplingPipeline
                        {
                            Temperature = 0.7f
                        }
                    };
                }); 
                AiOnClose = "已连接";
                Messages.Add(new ChatMessage
                {
                    IsUser = false,
                    Text = "✅ AI 服务连接成功！"
                });
            }
            catch (Exception ex)
            {
                AiOnClose = "未连接";
                Messages.Add(new ChatMessage
                {
                    IsUser = false,
                    Text = $"❌ 连接 AI 服务失败: {BuildExceptionMessage(ex)}"
                });
                return;
            }
        }

        // ================= 发送消息 =================
        [RelayCommand]
        public async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(InputText))
                return;

            if (_session == null || _inferenceParams == null)
            {
                await OpenAi();
                if (_session == null || _inferenceParams == null)
                {
                    Messages.Add(new ChatMessage { IsUser = false, Text = "⚠️ AI 尚未连接成功，请先确认模型路径与文件。" });
                    return;
                }
            }

            var userText = InputText;
            InputText = string.Empty;

            // 显示用户消息
            Messages.Add(new ChatMessage { IsUser = true, Text = userText });
            // 空ai消息框
            var aiMessage = new ChatMessage { IsUser = false, Text = "AI 正在思考..." };
            Messages.Add(aiMessage);
            // ai处理
            await ProcessAiResponseAsync(userText, aiMessage);
        }
        // 处理 AI 响应
        private async Task ProcessAiResponseAsync(string userText, ChatMessage aiMessage)
        {
            try
            {
                string aiResponseBuffer = "";
                bool needLabFlag = false;
                bool allowEnvTool = IsEnvQuery(userText);

                await foreach (var text in _session!.ChatAsync(new ChatHistory.Message(AuthorRole.User, userText), _inferenceParams!))
                {
                    aiResponseBuffer += text;
                    if (allowEnvTool && aiResponseBuffer.Contains("[TOOL:READ_ENV]", StringComparison.OrdinalIgnoreCase))
                    {
                        needLabFlag = true;
                        Dispatcher.UIThread.Post(() => aiMessage.Text = "读取实验室环境数据...");
                        break;
                    }
                    var displayText = CleanStreamForDisplay(aiResponseBuffer);
                    Dispatcher.UIThread.Post(() => aiMessage.Text = displayText);
                }

                if (needLabFlag)
                {
                    if (_labStatus.History.Count == 0)
                    {
                        Dispatcher.UIThread.Post(() => aiMessage.Text = "当前暂无实验室实时数据（设备未连接或离线），无法提供温湿度读数。");
                        _speechHelper.Speak("当前暂无实验室实时数据，无法提供温湿度读数。");
                        return;
                    }

                    string contextData = BuildLabContextData();
                    string userUpdate = $"这是实验室实时数据：{contextData}。请基于这些数据回答我上一条问题，不要输出 system/user/assistant 前缀，也不要输出工具标记。";

                    aiResponseBuffer = "";
                    Dispatcher.UIThread.Post(() => aiMessage.Text = "");

                    await foreach (var finalText in _session.ChatAsync(new ChatHistory.Message(AuthorRole.User, userUpdate), _inferenceParams))
                    {
                        aiResponseBuffer += finalText;
                        var displayText = NormalizeAiText(aiResponseBuffer);
                        Dispatcher.UIThread.Post(() => aiMessage.Text = displayText);
                    }
                }
                string finalCleanText = NormalizeAiText(aiResponseBuffer);
                if (!string.IsNullOrWhiteSpace(finalCleanText))
                {
                    finalCleanText = finalCleanText.Replace("*", "").Replace("#", "");
                    Task.Run(() => _speechHelper.Speak(finalCleanText));
                }
            }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() => aiMessage.Text = $"❌ AI 响应失败: {BuildExceptionMessage(ex)}");
                    _speechHelper.Speak("抱歉，处理您的请求时发生了错误。");
                 }
        }

        // ================= 麦克风按钮 =================
        [RelayCommand]
        public void ReadSpeech()
        {

            
        }

        public void Dispose()
        {
            _speechHelper.Dispose();
            _session = null;
            _executor = null;
            _context?.Dispose();
            _weights?.Dispose();
            _context = null;
            _weights = null;
        }
    }
}

