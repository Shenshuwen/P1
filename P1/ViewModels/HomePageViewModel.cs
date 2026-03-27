using System;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using P1.Common;

namespace P1.ViewModels
{
    public partial class HomePageViewModel : ViewModelBase
    {
        private const int ReadTimeoutMs = 5000;
        private const int PollIntervalMs = 3000;

        private readonly ITcpModuleClient _moduleClient;
        private readonly ModuleConnectionOptions _moduleOptions = ModuleConnectionOptions.Instance;

        public string commandwind { get; set; } = "0x02 0x03 0x00 0x63 0x00 0x01";
        public string commandstress { get; set; } = "0x01 0x03 0x00 0x02 0x00 0x02";
        public string commandTemWep { get; set; } = "0x03 0x10 0x0F 0xA0 0x00 0x06 0x0C 0x00 0x0A 0x7B 0x46 0x39 0x39 0x52 0x44 0x44 0x7D 0x0D 0x0A 0xE1 0x2E";
        public string GetTemWep { get; set; } = "0x03 0x03 0x10 0x04 0x00 0x2D 0xC1 0x34";

        [ObservableProperty]
        private string _time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        private Timer _uiTimer; // 每秒更新时间

        //定义viewmodel和view之间的桥梁
        [ObservableProperty]
        private float _wind;

        [ObservableProperty]
        private float _stress;

        [ObservableProperty]
        private float _tem;

        [ObservableProperty]
        private float _wep;

        public HomePageViewModel() : this(new TcpModuleClient())
        {
        }

        public HomePageViewModel(ITcpModuleClient moduleClient)
        {
            _moduleClient = moduleClient;

            if (Design.IsDesignMode)
            {
                Wind = 1.23f;
                Stress = 1013.6f;
                Tem = 24.8f;
                Wep = 56.2f;
                return;
            }

            _ = StartTcpClientLoopAsync();
            StartTimers();
        }

        private void StartTimers()
        {
            // UI 时间更新计时器：每秒更新时间（使用 Dispatcher 切回 UI 线程）
            _uiTimer = new Timer(1000);
            _uiTimer.Elapsed += (s, e) =>
            {
                // 始终在 UI 线程修改绑定属性
                Dispatcher.UIThread.Post(() =>
                {
                    Time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                });
            };
            _uiTimer.AutoReset = true;
            _uiTimer.Start();
        }

        private async Task StartTcpClientLoopAsync()
        {
            while (true)
            {
                try
                {
                    if (!_moduleClient.IsConnected)
                    {
                        await _moduleClient.ConnectAsync(_moduleOptions.ModuleIp, _moduleOptions.ModulePort);
                        Log($"已连接模块 TCP Server: {_moduleOptions.ModuleIp}:{_moduleOptions.ModulePort}");
                    }

                    await SendWindCommand();
                    await SendStressCommand();
                    await SendConnectTemWepCommand();
                    await SendGetTemWepCommand();
                }
                catch (OperationCanceledException)
                {
                    Log("采集读取超时，等待下一轮轮询");
                }
                catch (Exception ex)
                {
                    Log($"客户端轮询异常: {ex.Message}");
                    await _moduleClient.DisconnectAsync();
                }

                await Task.Delay(PollIntervalMs);
            }
        }

        private async Task SendWindCommand()
        {
            var bufferwind = CommunBll.ModBusStringToCRC16(commandwind);
            await ReadClientResponse(bufferwind, "wind");
        }

        private async Task SendStressCommand()
        {
            var bufferstress = CommunBll.ModBusStringToCRC16(commandstress);
            await ReadClientResponse(bufferstress, "stress");
        }

        private async Task SendConnectTemWepCommand()
        {
            var buffer = CommunBll.HexStringToBytes(commandTemWep);
            await ReadClientResponse(buffer, "lianjie");
        }

        private async Task SendGetTemWepCommand()
        {
            var buffer = CommunBll.HexStringToBytes(GetTemWep);
            await ReadClientResponse(buffer, "temwep");
        }

        private async Task ReadClientResponse(byte[] command, string type)
        {
            byte[] response;
            try
            {
                Log($"发送[{type}] 指令: {ToHexString(command)}");
                response = await _moduleClient.SendAndReceiveAsync(command, 1024, ReadTimeoutMs, System.Threading.CancellationToken.None);
                Log($"接收[{type}] 响应: {ToHexString(response)}");
            }
            catch (OperationCanceledException)
            {
                Log($"读取 {type} 响应超时（{ReadTimeoutMs}ms）");
                return;
            }
            catch (Exception ex)
            {
                Log($"读取 {type} 响应异常: {ex.Message}");
                return;
            }

            if (response.Length <= 0)
                return;

            ParseResponse(response, type);
        }
        // 注入 LabStatusService 服务（告诉中间层实验室的数据）
        private readonly LabStatusService _labStatus = LabStatusService.Instance;

        Decode decode = new Decode();

        private void ParseResponse(byte[] data, string type)
        {
            if (data.Length < 5) return;

            switch (type)
            {
                case "wind":
                   var wind = decode.ParseAndCalculateWind(data);
                    Log($"解析风速: {wind:F2} m/s");
                    _labStatus.Wind = wind;// 业务层存储（用于ai对话）
                    Wind = wind; // UI 显示

                    _labStatus.GetSummary();
                    _labStatus.AddHistory();
                    break;

                case "stress":
                   var stress = decode.ParseAndCalculateStress(data);
                    Log($"解析压力: {stress:F2} Pa");
                    _labStatus.Stress = stress;
                    Stress = stress;

                    _labStatus.GetSummary();
                    _labStatus.AddHistory();
                    break;
                case "lianjie":
                    //处理连接后的返回值
                    Log("温湿度连接指令响应已接收");
                    break;
                case "temwep":
                    var (temperature, humidity) = decode.ParseTemperatureHumidity(data);
                    Tem = (float)temperature;  // 温度
                    Wep = (float)humidity;     // 湿度
                    Log($"解析温度: {Tem:F2} °C");
                    Log($"解析湿度: {Wep:F2} %RH");
                    _labStatus.Tem1 = Tem;
                    _labStatus.Wep1 = Wep;

                    _labStatus.GetSummary();
                    _labStatus.AddHistory();
                    break;

                default:
                    Log("未处理的类型: " + type);
                    break;
            }
        }

        private static void Log(string message)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] [HomePage] {message}");
        }

        private static string ToHexString(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return "<empty>";
            }

            return string.Join(' ', data.Select(b => b.ToString("X2")));
        }
    }
}
