# 向日葵远程连接管理器 (SunloginManager)

![GitHub release (latest by date)](https://img.shields.io/github/v/release/songtay/SunloginManager)
![GitHub license](https://img.shields.io/github/license/songtay/SunloginManager)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Platform](https://img.shields.io/badge/Platform-Windows-lightgrey)

一个功能强大、界面美观的向日葵远程连接管理工具，采用现代化的 Apple 设计风格，提供安全的连接管理、分组功能和加密存储。

## ✨ 核心特性

### 🎨 现代化界面
- **Apple 风格设计**：圆角卡片、柔和阴影、流畅动画
- **直观的用户体验**：清晰的布局、一致的交互
- **响应式设计**：悬停效果、选中状态、视觉反馈

### 🔐 安全保护
- **连接码加密存储**：使用 Windows DPAPI 加密敏感信息
- **密码输入保护**：PasswordBox 隐藏输入内容
- **列表掩码显示**：连接码显示为 `********`
- **临时查看功能**：眼睛按钮按住显示，松开隐藏
- **用户级加密**：每个用户的加密密钥不同

### 📁 分组管理
- **连接分组**：按项目、客户或用途分类管理
- **颜色标识**：为每个分组设置不同颜色
- **分组筛选**：快速切换查看不同分组
- **批量管理**：支持分组的增删改查
- **默认分组**：系统自动创建，不可删除

### 🚀 快速连接
- **一键连接**：选择连接后点击按钮即可
- **自动填充**：自动输入识别码和连接码
- **键盘模拟**：使用 Tab 键切换输入框
- **智能等待**：自动检测窗口状态

### 📝 日志记录
- **详细日志**：记录所有操作和错误
- **日志查看器**：内置日志查看工具
- **按日期分类**：每天生成独立日志文件
- **问题排查**：便于定位和解决问题

### ⚙️ 灵活配置
- **自定义路径**：配置向日葵客户端路径
- **数据持久化**：JSON 格式存储，易于备份
- **自动迁移**：旧数据自动升级为新格式

## 📸 界面预览

### 主界面
- 清晰的连接列表
- 分组筛选下拉框
- 快速操作按钮
- 连接详情面板

### 对话框
- 添加/编辑连接对话框
- 分组管理对话框
- 设置对话框
- 日志查看器

## 🔧 安装使用

### 系统要求
- Windows 10/11
- .NET 8.0 Runtime
- 向日葵远程控制客户端

### 从源代码构建

1. **克隆仓库**
   ```bash
   git clone https://github.com/songtay/SunloginManager.git
   cd SunloginManager
   ```

2. **安装依赖**
   ```bash
   dotnet restore
   ```

3. **构建项目**
   ```bash
   dotnet build
   ```

4. **运行应用**
   ```bash
   dotnet run
   ```

### 发布版本

从 [Releases](https://github.com/songtay/SunloginManager/releases) 页面下载最新版本的可执行文件。

## 📖 使用指南

### 添加连接

1. 点击主界面的"添加连接"按钮
2. 填写以下信息：
   - **连接名称**：便于识别的名称
   - **识别码**：向日葵设备识别码
   - **连接码**：远程连接密码（加密存储）
   - **所属分组**：选择或创建分组
   - **备注**：可选的说明信息
3. 点击"添加"保存

### 管理分组

1. 点击"管理分组"按钮
2. 可以进行以下操作：
   - **添加分组**：创建新分组，设置名称、描述和颜色
   - **编辑分组**：修改分组信息
   - **删除分组**：删除不需要的分组（连接会移到默认分组）
3. 分组会在主界面的筛选下拉框中显示

### 连接远程设备

1. 在列表中选择要连接的设备
2. 点击"快速连接"按钮
3. 应用会自动：
   - 启动向日葵客户端
   - 输入识别码
   - 切换到连接码输入框
   - 输入连接码
   - 建立连接

### 查看连接码

**在列表中**：
- 连接码显示为 `********`，保护隐私

**在编辑对话框中**：
1. 打开"编辑连接"对话框
2. 找到连接码输入框
3. 按住右侧的眼睛图标按钮
4. 连接码以明文显示
5. 松开按钮，恢复为圆点显示

### 查看日志

1. 点击"查看日志"按钮
2. 选择日期查看对应的日志
3. 日志包含：
   - 操作记录
   - 错误信息
   - 系统事件

## 🏗️ 项目架构

### 技术栈
- **框架**：.NET 8.0
- **UI**：WPF (Windows Presentation Foundation)
- **加密**：Windows DPAPI
- **序列化**：System.Text.Json
- **架构**：MVVM + 服务层

### 目录结构
```
SunloginManager/
├── Models/                      # 数据模型
│   ├── RemoteConnection.cs      # 远程连接模型
│   └── ConnectionGroup.cs       # 连接分组模型
├── Services/                    # 服务层
│   ├── DataService.cs           # 数据服务
│   ├── LogService.cs            # 日志服务
│   ├── SunloginService.cs       # 向日葵服务
│   └── EncryptionService.cs     # 加密服务
├── Helpers/                     # 辅助类
│   ├── WindowsApiHelper.cs      # Windows API 封装
│   ├── KeyboardInputHelper.cs   # 键盘输入辅助
│   └── WindowManagerHelper.cs   # 窗口管理辅助
├── Constants/                   # 常量定义
│   ├── KeyboardConstants.cs     # 键盘常量
│   ├── WindowConstants.cs       # 窗口常量
│   └── TimingConstants.cs       # 时间常量
├── Dialogs/                     # 对话框
│   ├── AddConnectionDialog.xaml         # 添加连接
│   ├── EditConnectionDialog.xaml        # 编辑连接
│   ├── ManageGroupsDialog.xaml          # 管理分组
│   ├── EditGroupDialog.xaml             # 编辑分组
│   ├── SettingsDialog.xaml              # 设置
│   └── LogViewerDialog.xaml             # 日志查看器
├── Converters.cs                # 值转换器
├── MainWindow.xaml              # 主窗口
└── App.xaml                     # 应用入口
```

### 核心服务

#### EncryptionService（加密服务）
- 使用 Windows DPAPI 加密连接码
- 用户级加密，每个用户密钥不同
- 自动加密/解密，对用户透明
- 支持旧数据自动迁移

#### DataService（数据服务）
- JSON 格式存储数据
- 支持连接和分组的 CRUD 操作
- 自动修复数据问题
- 数据迁移和升级

#### SunloginService（向日葵服务）
- 启动向日葵客户端
- 自动输入识别码和连接码
- 窗口检测和管理
- 键盘事件模拟

#### LogService（日志服务）
- 按日期分类存储
- 支持不同日志级别
- 异常详细记录
- 便于问题排查

## 🔒 安全特性

### 加密存储
- **算法**：Windows DPAPI (Data Protection API)
- **范围**：CurrentUser（用户级）
- **格式**：Base64 编码
- **特点**：
  - 每个用户的加密密钥不同
  - 无法跨机器解密
  - 系统级安全保护

### 数据格式
```json
{
  "id": 1,
  "name": "测试连接",
  "identificationCode": "123456789",
  "encryptedConnectionCode": "AQAAANCMnd8BFdERjHoAwE...",
  "groupId": 1,
  "remarks": "测试备注"
}
```

### 显示保护
- **列表显示**：`********`（固定8个星号）
- **输入显示**：`●●●●●●●●`（PasswordBox 圆点）
- **临时查看**：按住眼睛按钮显示明文
- **自动隐藏**：松开或移开鼠标自动隐藏

## 📂 数据存储

### 存储位置
```
%AppData%\SunloginManager\
├── Data\
│   ├── connections.json    # 连接数据（加密）
│   ├── groups.json          # 分组数据
│   └── settings.json        # 应用设置
└── Logs\
    ├── app_20250101.log     # 日志文件
    ├── app_20250102.log
    └── ...
```

### 备份建议
- 定期备份 `Data` 文件夹
- 加密数据只能在同一用户下恢复
- 建议导出明文备份（未来功能）

## 🎯 未来计划

### 功能增强
- [ ] 搜索功能：快速查找连接
- [ ] 批量操作：批量删除、移动分组
- [ ] 导入/导出：支持数据导入导出
- [ ] 快捷键：常用操作的快捷键
- [ ] 双击连接：双击列表项直接连接
- [ ] 连接历史：记录连接历史
- [ ] 连接统计：统计连接次数和时长

### 安全增强
- [ ] 主密码：应用启动时需要输入主密码
- [ ] 自动锁定：一段时间无操作自动锁定
- [ ] 密码强度：检查连接码强度
- [ ] 二次确认：删除操作二次确认

### 用户体验
- [ ] 主题切换：支持亮色/暗色主题
- [ ] 自定义颜色：自定义界面颜色
- [ ] 窗口记忆：记住窗口位置和大小
- [ ] 拖拽排序：支持拖拽调整顺序

## 🤝 贡献指南

欢迎贡献代码！请遵循以下步骤：

1. Fork 本仓库
2. 创建特性分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

### 代码规范
- 遵循 C# 编码规范
- 添加必要的注释
- 保持代码整洁
- 编写单元测试

## 📄 许可证

本项目采用 MIT 许可证 - 查看 [LICENSE](LICENSE) 文件了解详情。

## 👨‍💻 作者

**songtay** - [GitHub](https://github.com/songtay)

## 🙏 致谢

- 感谢向日葵远程控制软件
- 感谢所有贡献者和用户
- 感谢开源社区的支持

## 📝 更新日志

### v1.0.0 (2024-12-05)
- 🎉 初始版本发布
- ✅ 基本的连接管理功能
- 📋 系统托盘集成
- 📝 日志记录功能
- ⚙️ 设置管理功能

## 💬 反馈与支持

如果您遇到任何问题或有功能建议：
- 提交 [Issue](https://github.com/songtay/SunloginManager/issues)
- 发送邮件联系作者
- 在 GitHub 上 Star 和 Fork

## ⚠️ 免责声明

本应用程序仅用于管理向日葵远程连接，需要您已安装向日葵客户端。请确保：
- 您有合法的向日葵使用许可
- 遵守向日葵的使用条款
- 不用于非法用途
- 妥善保管连接信息

---

**Made with ❤️ by songtay**
