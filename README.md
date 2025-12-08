# SunloginManager

![GitHub release (latest by date)](https://img.shields.io/github/v/release/songtay/SunloginManager)
![GitHub license](https://img.shields.io/github/license/songtay/SunloginManager)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)

一个基于 WPF 的向日葵远程连接管理工具，提供便捷的远程连接管理、日志记录和系统托盘集成功能。

## 功能特点

- 🖥️ **直观的用户界面**：现代化的WPF界面，提供流畅的用户体验
- 🔗 **连接管理**：添加、编辑、删除和管理多个向日葵远程连接
- 📋 **快速连接**：一键连接到已保存的远程设备
- 🔔 **系统托盘集成**：最小化到系统托盘，方便快速访问
- 📝 **日志记录**：详细的操作日志，便于问题排查
- ⚙️ **设置管理**：自定义向日葵客户端路径和其他应用设置
- ℹ️ **关于页面**：展示应用信息和作者信息

## 截图

![主界面](screenshots/main-interface.png)
*主界面 - 连接列表和管理功能*

![关于页面](screenshots/about-page.png)
*关于页面 - 应用信息和作者链接*

## 安装

### 从源代码构建

1. 克隆仓库：
   ```bash
   git clone https://github.com/songtay/SunloginManager.git
   cd SunloginManager
   ```

2. 确保您已安装 .NET 8.0 SDK

3. 构建项目：
   ```bash
   dotnet build
   ```

4. 运行应用：
   ```bash
   dotnet run
   ```

### 发布版本

您可以在 [Releases](https://github.com/songtay/SunloginManager/releases) 页面下载最新的发布版本。

## 使用方法

1. **添加连接**：
   - 点击"添加连接"按钮
   - 填写连接名称、识别码和连接码
   - 可选添加备注信息
   - 点击"确定"保存

2. **连接远程设备**：
   - 从列表中选择要连接的设备
   - 点击"连接"按钮或使用详情面板中的连接按钮
   - 应用程序将自动启动向日葵客户端并建立连接

3. **管理连接**：
   - 使用"编辑"按钮修改现有连接信息
   - 使用"删除"按钮移除不需要的连接
   - 通过搜索框快速查找特定连接

4. **系统托盘**：
   - 关闭主窗口时，应用程序会最小化到系统托盘
   - 双击托盘图标或右键菜单可以重新打开主窗口
   - 通过托盘菜单可以快速退出应用程序

## 技术栈

- **框架**：.NET 8.0
- **UI框架**：WPF (Windows Presentation Foundation)
- **序列化**：System.Text.Json
- **架构模式**：MVVM (Model-View-ViewModel)

## 项目结构

```
SunloginManager/
├── Models/                 # 数据模型
│   └── RemoteConnection.cs  # 远程连接模型
├── Services/               # 服务层
│   ├── DataService.cs      # 数据服务
│   ├── LogService.cs      # 日志服务
│   └── SunloginService.cs # 向日葵服务
├── Views/                  # 视图
│   ├── AboutWindow.xaml    # 关于窗口
│   └── AboutWindow.xaml.cs
├── Dialogs/                # 对话框
│   ├── AddConnectionDialog.xaml    # 添加连接对话框
│   ├── EditConnectionDialog.xaml   # 编辑连接对话框
│   ├── SettingsDialog.xaml         # 设置对话框
│   └── LogViewerDialog.xaml        # 日志查看对话框
├── Converters.cs          # 值转换器
├── App.xaml               # 应用程序入口
├── MainWindow.xaml        # 主窗口
└── IconGenerator.cs       # 图标生成器
```

## 配置

应用程序配置文件存储在用户的AppData目录中：
- 连接数据：`%AppData%\SunloginManager\Data\connections.json`
- 应用设置：`%AppData%\SunloginManager\settings.json`
- 日志文件：`%AppData%\SunloginManager\Logs\`

## 贡献

欢迎贡献代码！请遵循以下步骤：

1. Fork 本仓库
2. 创建您的特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交您的更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 打开一个 Pull Request

## 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 作者

**songtay** - [GitHub](https://github.com/songtay)

## 致谢

- 感谢向日葵远程控制软件提供的API和服务
- 感谢所有贡献者和用户的支持

## 更新日志

### v1.0.0 (2025-12-05)
- 初始版本发布
- 基本的连接管理功能
- 系统托盘集成
- 日志记录功能
- 设置管理功能

## 反馈与支持

如果您遇到任何问题或有功能建议，请：
- 在 [Issues](https://github.com/songtay/SunloginManager/issues) 页面提交问题
- 或通过 [GitHub](https://github.com/songtay) 联系作者

---

**注意**：本应用程序仅用于管理向日葵远程连接，需要您已安装向日葵客户端。请确保您有合法的向日葵使用许可。