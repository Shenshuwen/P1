using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using P1.Common;
using System.Speech.Recognition;
using System.Globalization;

namespace P1.ViewModels
{
    public partial class ProcessPageViewModel : ViewModelBase
    {
        // ================= 聊天消息模型 =================
        public class ChatMessage
        {
            public bool IsUser { get; set; }
            public string Text { get; set; } = string.Empty;
        }
        private readonly LabStatusService _labStatus;
        public ProcessPageViewModel()
        {
            _labStatus = LabStatusService.Instance;
        }

        // ================= 输入框 =================
        [ObservableProperty]
        private string inputText = string.Empty;

        [ObservableProperty]
        private string aiOnClose = "未连接";

        // ================= 消息集合 =================
        public ObservableCollection<ChatMessage> Messages { get; } = new();

        private SpeechHelper? _speechHelper;

        // ================= 连接 AI =================
        [RelayCommand]
        public async Task OpenAi()
        {
            _speechHelper = new SpeechHelper();

            _speechHelper.OnAiReply += reply =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Messages.Add(new ChatMessage
                    {
                        IsUser = false,
                        Text = reply
                    });
                });
            };

            bool ok = await _speechHelper.TestConnectionAsync();

            Messages.Add(new ChatMessage
            {
                IsUser = false,
                Text = ok ? "AI 已连接，可以开始对话" : "❌ 无法连接到 AI 服务"
            });

            AiOnClose = ok ? "已连接" : "未连接";
        }

        // ================= 发送消息 =================
        [RelayCommand]
        public async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(InputText))
                return;

            var userText = InputText;

            Messages.Add(new ChatMessage
            {
                IsUser = true,
                Text = userText
            });

            InputText = string.Empty;

            if (_speechHelper == null)
                return;

            bool needLabContext =
                userText.Contains("温度") ||
                userText.Contains("湿度") ||
                userText.Contains("风速") ||
                userText.Contains("实验室") ||
                userText.Contains("环境") ||
                userText.Contains("状态");

            string prompt;

            if (needLabContext)
            {
                string context = _labStatus.GetSummary();

                prompt =
                    $"你是实验室智能助手，请基于以下实时数据回答问题。\n\n" +
                    $"【实验室当前状态】\n{context}\n\n" +
                    $"【用户问题】\n{userText}";
            }
            else
            {
                prompt = userText;
            }

            await _speechHelper.SendUserMessageAsync(prompt);
        }

        // ================= 麦克风按钮 =================
        [RelayCommand]
        public void ReadSpeech()
        {

            
        }
    }
}

