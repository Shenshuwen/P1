# P1 智慧实验室平台

## 项目简介
`P1` 是基于 `Avalonia + .NET 8 + CommunityToolkit.Mvvm` 的桌面端实验室监测应用，当前聚焦于：
- 通过 TCP 与实验室模块通信，轮询采集风速/压力/温湿度
- 实时看板展示与历史记录展示
- 接入本地 AI（OpenAI 兼容接口）进行问答，并支持语音播报

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

4. **AI 与语音链路说明补充**
   - 明确 `ProcessPageViewModel` -> `SpeechHelper` -> 本地 AI 服务的调用路径。
   - 说明 AI 回复事件回传 UI 与 `System.Speech` 语音播报。

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
- `ProcessPageViewModel.OpenAi()` 可测试并建立 AI 可用状态。
- `SendMessage()` 支持基于关键词自动拼接实验室实时上下文。
- `SpeechHelper` 调用本地 OpenAI 兼容接口：
  - `GET /v1/models`（连通性）
  - `POST /v1/chat/completions`（对话）
- AI 回复可自动语音播报。

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
| `Common/SpeechHelper.cs` | AI HTTP 调用与 TTS 语音播报 |

---

## 快速启动

1. 准备 .NET 8 SDK。
2. 确保传感器模块可达（默认 `192.168.3.100:10193`，可在设置页修改）。
3. 若需 AI 对话，启动本地 OpenAI 兼容服务（默认 `http://localhost:1234`）。
4. 执行：
   - `dotnet restore`
   - `dotnet build`
   - `dotnet run --project P1`

---

## 备注

当前版本重心是“采集 + 展示 + AI 联动”的主流程跑通。后续建议优先补齐导出与语音输入，并进一步完善历史记录策略与线程安全边界。
