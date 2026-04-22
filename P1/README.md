# P1 智慧实验室平台

## 项目简介
`P1` 是基于 `Avalonia + .NET 8 + CommunityToolkit.Mvvm` 的桌面端实验室监测应用，当前聚焦于：
- 通过 TCP 与实验室模块通信，轮询采集风速/压力/温湿度
- 实时看板展示与历史记录展示
- 接入本地 AI（`LLamaSharp` + `GGUF` 模型）进行问答

当前工程：`net8.0`、C# `12.0`。

---

## 本次文档更新内容（按当前代码）

本 README 已根据工程文件实际实现进行重写，主要修正与补充如下：

1. **采集架构说明修正**
   - 当前并非 TCP 服务端模式，而是 `HomePageViewModel` 通过 `TcpModuleClient` 作为**客户端轮询**模块。

2. **新增配置能力说明**
   - 补充 `ModuleConnectionOptions` 单例配置项（模块 IP/端口、WiFi、网关、DNS、远端地址端口）。
   - 补充设置页参数保存、推荐参数填充、AT 指令生成流程。

3. **新增网络抽象说明**
   - 补充 `ITcpModuleClient` 与 `TcpModuleClient` 的职责分离，便于后续测试替换与通信实现扩展。

4. **AI 链路说明补充**
   - 明确 `ProcessPageViewModel` 内通过 `LLamaSharp` 直接加载本地 `GGUF` 模型。
   - 支持通过 `[TOOL:READ_ENV]` 触发实验室状态读取，再次补充上下文回答。

5. **“已实现功能 / 未完成功能”重新梳理**
   - 明确已实现能力边界，保留当前未完成项（如语音识别输入、历史导出逻辑）。

---

## 技术栈与依赖

### 基础环境
- .NET SDK `8.0+`
- Avalonia `11.3.0`
- CommunityToolkit.Mvvm `8.2.1`

### 主要包（`P1.csproj`）
- `Avalonia`
- `Avalonia.Desktop`
- `Avalonia.Themes.Fluent`
- `Avalonia.Fonts.Inter`
- `CommunityToolkit.Mvvm`
- `LLamaSharp`
- `System.Speech`
- `TouchSocket`（当前核心通信代码主要使用 `TcpClient`）

---

## 当前已实现功能

### 1) 主界面导航
- `MainWindowViewModel` 已实现首页、进程页、历史页、设置页切换。
- 支持侧边栏展开/收起。

### 2) 设备 TCP 轮询采集
- `HomePageViewModel` 启动后持续轮询：
  - 风速指令
  - 压力指令
  - 温湿度连接指令
  - 温湿度读取指令
- 指令编码与 CRC：`CommunBll`
- 响应解析：`Decode`
- 通信实现：`ITcpModuleClient` / `TcpModuleClient`

### 3) 实时状态共享与历史记录
- `LabStatusService` 单例维护：
  - 当前风速、压力、温度、湿度
  - `ObservableCollection<HistoryRecord>` 历史数据（最多 200 条）
- 历史页绑定 `History` 并展示表格。

### 4) AI 问答与语音播报
- `ProcessPageViewModel.OpenAi()` 直接加载本地 `GGUF` 模型并建立 `ChatSession`。
- `SendMessage()` 支持流式输出回复。
- 当模型输出 `[TOOL:READ_ENV]` 时，自动读取 `LabStatusService.GetSummary()` 并补充系统消息继续回答。

### 5) 设置页参数管理与 AT 指令生成
- `SettingPageViewModel` 已支持：
  - 模块 IP/端口、WiFi、网关、子网、DNS、远端地址端口编辑
  - Ping + TCP 端口连接测试
  - 参数保存到 `ModuleConnectionOptions.Instance`
  - 一键填充推荐参数
  - 生成 `TAS-WIFI-260-AT` 指令脚本

---

## 当前未完成 / 待完善

1. `ProcessPageViewModel.ReadSpeech()` 目前为空实现（语音识别输入未接入）。
2. `HistoryPageViewModel.ExportHistoryData()` 尚未实现导出逻辑。
3. 首页部分卡片仍为静态展示值（未全部接入实时数据源）。
4. 历史记录在一次轮询中可能写入多条“阶段性快照”，后续可优化为“完整采样点”写入。

---

## 关键文件与职责

| 文件 | 作用 |
|---|---|
| `Program.cs` | 应用入口，启动 Avalonia 桌面生命周期 |
| `App.axaml.cs` | 创建主窗口并注入 `MainWindowViewModel` |
| `ViewModels/MainWindowViewModel.cs` | 页面导航与侧边栏状态 |
| `ViewModels/HomePageViewModel.cs` | TCP 客户端轮询采集与实时属性更新 |
| `ViewModels/ProcessPageViewModel.cs` | AI 建连、消息发送、上下文拼接 |
| `ViewModels/HistoryPageViewModel.cs` | 历史记录绑定与导出入口 |
| `ViewModels/SettingPageViewModel.cs` | 连接参数管理、连通性测试、AT 指令生成 |
| `Common/ModuleConnectionOptions.cs` | 模块连接参数单例存储 |
| `Common/ITcpModuleClient.cs` / `Common/TcpModuleClient.cs` | TCP 通信抽象与实现 |
| `Common/CommunBll.cs` | 十六进制指令处理与 CRC16 |
| `Common/Decode.cs` | 传感器响应解析 |
| `Common/LabStatusService.cs` | 实时状态与历史记录共享 |
| `Common/SpeechHelper.cs` | 预留 AI HTTP 调用与 TTS 语音播报能力 |

---

## 快速启动

1. 准备 .NET 8 SDK。
2. 确保传感器模块可达（默认 `192.168.3.100:10193`，可在设置页修改）。
3. 若需 AI 对话，确保本地 `GGUF` 模型路径可用（当前写死在 `ProcessPageViewModel.OpenAi()`）。
4. 执行：
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run --project P1`

---

## 备注

当前版本重心是“采集 + 展示 + AI 联动”的主流程跑通。后续建议优先补齐导出与语音输入，并进一步完善历史记录策略与线程安全边界。

---

## 本次修复记录（本地大模型）

- 修复 `ProcessPageViewModel` 编译错误：
  - `ChatMessage` 增加 `partial`，匹配 `ObservableProperty` 源生成器要求。
  - 修正不存在变量：`triggerMessage` / `uiMessage` / `systemUpdate`。
  - 修正 `InferenceParams` 配置方式，改为 `SamplingPipeline = new DefaultSamplingPipeline { Temperature = 0.7f }`。
  - 修正实验室状态读取字段，改为使用 `LabStatusService.GetSummary()`。
- 已通过 `dotnet build`（`net8.0`）编译验证。

---

## 追加记录（2026-04-22 16:35）

### 本次代码修改

1. 按硬件拓扑将 `Common/SpeechHelper.cs` 从串口(`SerialPort`)改为 TCP 发送：
   - 通过 `ModuleConnectionOptions.Instance` 读取模块 IP/端口。
   - 发送内容保持语音模块命令格式：`#[v8]{text}`（TCP 下发送 `\r\n` 结尾）。
2. 增加语音发送容错：
   - 首次发送失败后自动重连并重发一次。
3. 增加资源释放链路：
   - `ProcessPageViewModel` 实现 `IDisposable`，释放语音连接与模型资源。
   - `MainWindowViewModel` 实现 `IDisposable`，统一调用页面释放。
   - `MainWindow` 在关闭时触发 `Dispose()`。

### 问题回答

1. **“模块是 Modbus RTU，但当前代码没体现 Modbus RTU，是否正确？”**
   - 对“语音播报”这条链路来说，当前写法是合理的：你给语音模块下发的是其私有文本命令（如 `#[v8]...`），不是传感器寄存器读写，因此不需要按 Modbus RTU 组帧。
   - 只有当你的语音模块协议文档明确要求“必须按 Modbus RTU 帧（地址/功能码/CRC）”时，才需要实现 Modbus RTU 封包。

2. **“目前能否完成功能？”**
   - 目前代码已具备“AI 文本 -> TCP -> RS485-WiFi 模块 -> 语音模块播报”的软件链路能力。
   - 真实可用还依赖三项现场条件：
     - WiFi 模块 TCP 模式与 IP/端口配置正确；
     - 模块确实把 TCP 透传到 RS485；
     - 语音模块命令格式与波特率/串口参数匹配。
