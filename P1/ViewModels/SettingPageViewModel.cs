using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using P1.Common;

namespace P1.ViewModels
{
    public partial class SettingPageViewModel : ViewModelBase
    {
        private readonly ModuleConnectionOptions _options = ModuleConnectionOptions.Instance;

        [ObservableProperty]
        public string _ipAddress = "192.168.3.100"; // 默认模块IP

        [ObservableProperty]
        public int _port = 10193; // 默认端口

        [ObservableProperty]
        public string _moduleStatus = "未测试";

        [ObservableProperty]
        public string _setupGuide =
            "模块已通过串口配置为 STA + TCP Server。\n" +
            "上位机仅作为 TCP Client 连接模块，读取传感器数据。";

        public SettingPageViewModel()
        {
            IpAddress = _options.ModuleIp;
            Port = _options.ModulePort;
        }

        [RelayCommand]
        private async Task Ipsetting()
        {
            if (string.IsNullOrWhiteSpace(IpAddress))
            {
                ModuleStatus = "请输入模块 IP";
                return;
            }

            try
            {
                bool isConnected = await TestIPConnection(IpAddress);
                bool isPortOpen = await TestPortConnection(IpAddress, Port);

                ModuleStatus = isConnected && isPortOpen
                    ? $"连接成功：{IpAddress}:{Port}"
                    : $"连接失败：{IpAddress}:{Port}（若模块尚未切换到 Server 模式，此结果为预期）";

            }
            catch (Exception ex)
            {
                ModuleStatus = $"连接错误: {ex.Message}";
            }
        }

        [RelayCommand]
        private void SaveSetting()
        {
            _options.ModuleIp = IpAddress;
            _options.ModulePort = Port;
            ModuleStatus = "参数已保存。上位机将作为 TCP Client 按该地址连接模块";
        }

        // 测试IP连接（Ping）
        private async Task<bool> TestIPConnection(string ipAddress)
        {
            try
            {
                using (Ping ping = new Ping())
                {
                    PingReply reply = await ping.SendPingAsync(ipAddress, 3000); // 3秒超时
                    return reply.Status == IPStatus.Success;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        // 测试端口连接（TCP）
        private async Task<bool> TestPortConnection(string ipAddress, int port)
        {
            try
            {
                using (TcpClient tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(ipAddress, port);
                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
