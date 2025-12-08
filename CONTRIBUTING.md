# 贡献指南

感谢您对 SunloginManager 项目的关注！我们欢迎任何形式的贡献，包括但不限于：

- 🐛 报告错误
- 💡 提出新功能建议
- 📝 改进文档
- 🔧 提交代码修复
- ✨ 开发新功能

## 开发环境设置

1. **克隆仓库**
   ```bash
   git clone https://github.com/songtay/SunloginManager.git
   cd SunloginManager
   ```

2. **安装依赖**
   - 确保您已安装 .NET 8.0 SDK
   - 使用 Visual Studio 2022 或 Visual Studio Code 作为开发环境

3. **构建项目**
   ```bash
   dotnet build
   ```

4. **运行应用**
   ```bash
   dotnet run
   ```

## 代码风格和规范

- 遵循 C# 编码规范
- 使用有意义的变量和方法名
- 为公共方法添加 XML 文档注释
- 保持代码简洁和可读性
- 避免过长的行（建议不超过120个字符）

## 提交 Pull Request 的步骤

1. **Fork 仓库**
   - 在 GitHub 上点击 "Fork" 按钮

2. **创建功能分支**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **进行更改**
   - 实现您的功能或修复
   - 添加必要的测试
   - 确保代码通过所有测试

4. **提交更改**
   ```bash
   git add .
   git commit -m "描述您的更改"
   ```

5. **推送分支**
   ```bash
   git push origin feature/your-feature-name
   ```

6. **创建 Pull Request**
   - 在 GitHub 上访问您的 fork
   - 点击 "New Pull Request"
   - 提供清晰的描述说明您的更改

## 提交消息规范

请使用清晰、描述性的提交消息：

- 使用现在时态："Add feature" 而不是 "Added feature"
- 首行简短描述（50个字符以内）
- 如有必要，添加更详细的描述
- 使用相关的标签：[feat], [fix], [docs], [style], [refactor], [test], [chore]

示例：
```
[feat] Add system tray icon customization

- Add color selection for tray icon
- Add option to use custom icon file
- Update settings dialog with new options
```

## 报告问题

使用 GitHub Issues 报告问题时，请提供：

- 详细的描述
- 重现步骤
- 预期行为和实际行为
- 环境信息（操作系统、.NET版本等）
- 相关的截图或日志

## 功能请求

提出新功能建议时，请：

- 描述功能的用途和价值
- 说明预期的用户界面或交互方式
- 考虑可能的实现方案

## 文档贡献

文档改进也是重要的贡献形式：

- 修正拼写错误
- 改进现有文档的清晰度
- 添加使用示例
- 翻译文档

## 行为准则

请尊重所有项目参与者，保持友好和专业的交流环境。我们致力于为每个人提供无骚扰的体验，无论性别、性别认同和表达、性取向、残疾、外貌、体型、种族、年龄、宗教或国籍。

## 发布流程

项目维护者负责：

- 审查和合并 Pull Request
- 更新版本号
- 创建发布标签
- 编写发布说明

## 获取帮助

如果您有任何问题或需要帮助：

- 查看 [Issues](https://github.com/songtay/SunloginManager/issues) 页面
- 在相关 Issue 中提问
- 通过 [GitHub](https://github.com/songtay) 联系项目维护者

再次感谢您的贡献！🎉