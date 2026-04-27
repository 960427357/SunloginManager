using System;
using System.IO;
using System.Text.Json;
using SunloginManager.Models;

namespace SunloginManager.Services
{
    /// <summary>
    /// 数据服务类，用于处理应用程序的数据存储和读取
    /// </summary>
    public class DataService
    {
        private readonly string _dataDirectory;
        private readonly string _settingsFilePath;
        private readonly string _connectionsFilePath;
        private readonly string _groupsFilePath;

        public DataService()
        {
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _settingsFilePath = Path.Combine(_dataDirectory, "settings.json");
            _connectionsFilePath = Path.Combine(_dataDirectory, "connections.json");
            _groupsFilePath = Path.Combine(_dataDirectory, "groups.json");

            // 确保数据目录存在
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
            
            // 初始化默认分组
            InitializeDefaultGroup();
            
            // 修复现有数据中的ID问题
            FixConnectionIds();
            
            // 迁移旧数据：加密连接码
            MigrateConnectionCodes();
        }

        /// <summary>
        /// 保存向日葵路径
        /// </summary>
        /// <param name="path">向日葵路径</param>
        public void SaveSunloginPath(string path)
        {
            try
            {
                var settings = LoadSettings();
                settings.SunloginPath = path;
                SaveSettings(settings);
                LogService.LogInfo($"已保存向日葵路径: {path}");
            }
            catch (Exception ex)
            {
                LogService.LogError($"保存向日葵路径失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载向日葵路径
        /// </summary>
        /// <returns>向日葵路径</returns>
        public string LoadSunloginPath()
        {
            try
            {
                var settings = LoadSettings();
                LogService.LogInfo($"已加载向日葵路径: {settings.SunloginPath}");
                return settings.SunloginPath;
            }
            catch (Exception ex)
            {
                LogService.LogError($"加载向日葵路径失败: {ex.Message}", ex);
                return string.Empty;
            }
        }

        /// <summary>
        /// 修复现有数据中的ID问题
        /// </summary>
        private void FixConnectionIds()
        {
            try
            {
                if (!File.Exists(_connectionsFilePath))
                    return;
                    
                var connections = LoadConnections();
                bool needsFix = false;
                int nextId = 1;
                
                // 检查是否有ID为0的连接
                foreach (var conn in connections)
                {
                    if (conn.Id == 0)
                    {
                        needsFix = true;
                        break;
                    }
                    
                    if (conn.Id >= nextId)
                        nextId = conn.Id + 1;
                }
                
                // 如果需要修复，则为所有ID为0的连接分配唯一ID
                if (needsFix)
                {
                    foreach (var conn in connections)
                    {
                        if (conn.Id == 0)
                        {
                            conn.Id = nextId++;
                        }
                    }
                    
                    SaveConnections(connections);
                    LogService.LogInfo($"已修复 {connections.Count} 个连接的ID问题");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"修复连接ID失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 迁移旧数据：加密连接码
        /// </summary>
        private void MigrateConnectionCodes()
        {
            try
            {
                if (!File.Exists(_connectionsFilePath))
                    return;

                // 读取原始JSON文件
                string json = File.ReadAllText(_connectionsFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                // 使用动态类型读取，检查是否有未加密的连接码
                var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
                bool needsMigration = false;

                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    if (element.TryGetProperty("connectionCode", out var codeProperty))
                    {
                        string code = codeProperty.GetString() ?? string.Empty;
                        if (!string.IsNullOrEmpty(code) && !EncryptionService.IsEncrypted(code))
                        {
                            needsMigration = true;
                            break;
                        }
                    }
                }

                if (needsMigration)
                {
                    LogService.LogInfo("检测到未加密的连接码，开始迁移...");
                    
                    // 加载并重新保存，会自动加密
                    var connections = LoadConnections();
                    SaveConnections(connections);
                    
                    LogService.LogInfo($"已完成 {connections.Count} 个连接的加密迁移");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"迁移连接码加密失败: {ex.Message}", ex);
            }
        }

        #region 分组管理

        /// <summary>
        /// 初始化默认分组
        /// </summary>
        private void InitializeDefaultGroup()
        {
            try
            {
                var groups = LoadGroups();
                if (groups.Count == 0)
                {
                    var defaultGroup = new ConnectionGroup
                    {
                        Id = 1,
                        Name = "默认分组",
                        Description = "系统默认分组",
                        Color = "#007AFF",
                        SortOrder = 0,
                        IsDefault = true
                    };
                    groups.Add(defaultGroup);

                    var favoriteGroup = new ConnectionGroup
                    {
                        Id = 2,
                        Name = "收藏",
                        Description = "收藏的连接",
                        Color = "#FF9500",
                        SortOrder = 1,
                        IsFavoriteGroup = true
                    };
                    groups.Add(favoriteGroup);

                    SaveGroups(groups);
                    LogService.LogInfo("已创建默认分组和收藏分组");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"初始化默认分组失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取所有分组
        /// </summary>
        public System.Collections.Generic.List<ConnectionGroup> GetAllGroups()
        {
            return LoadGroups();
        }

        /// <summary>
        /// 保存分组
        /// </summary>
        public void SaveGroup(ConnectionGroup group)
        {
            try
            {
                var groups = LoadGroups();
                
                if (group.Id == 0)
                {
                    int maxId = 0;
                    foreach (var g in groups)
                    {
                        if (g.Id > maxId)
                            maxId = g.Id;
                    }
                    group.Id = maxId + 1;
                }
                
                groups.Add(group);
                SaveGroups(groups);
                LogService.LogInfo($"已保存分组: {group.Name} (ID: {group.Id})");
            }
            catch (Exception ex)
            {
                LogService.LogError($"保存分组失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新分组
        /// </summary>
        public void UpdateGroup(ConnectionGroup group)
        {
            try
            {
                var groups = LoadGroups();
                int index = groups.FindIndex(g => g.Id == group.Id);
                
                if (index >= 0)
                {
                    groups[index] = group;
                    SaveGroups(groups);
                    LogService.LogInfo($"已更新分组: {group.Name}");
                }
                else
                {
                    LogService.LogWarning($"未找到要更新的分组: {group.Id}");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"更新分组失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除分组
        /// </summary>
        public void DeleteGroup(int groupId)
        {
            try
            {
                var groups = LoadGroups();
                var group = groups.Find(g => g.Id == groupId);
                
                if (group != null && (group.IsDefault || group.IsFavoriteGroup))
                {
                    LogService.LogWarning(group.IsDefault ? "不能删除默认分组" : "不能删除收藏分组");
                    return;
                }
                
                int removedCount = groups.RemoveAll(g => g.Id == groupId);
                
                if (removedCount > 0)
                {
                    // 将该分组下的所有连接移到默认分组
                    var connections = LoadConnections();
                    foreach (var conn in connections)
                    {
                        if (conn.GroupId == groupId)
                        {
                            conn.GroupId = 1; // 移到默认分组
                        }
                    }
                    SaveConnections(connections);
                    
                    SaveGroups(groups);
                    LogService.LogInfo($"已删除分组: {groupId}");
                }
                else
                {
                    LogService.LogWarning($"未找到要删除的分组: {groupId}");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"删除分组失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 根据分组ID获取连接列表
        /// </summary>
        public System.Collections.Generic.List<RemoteConnection> GetConnectionsByGroup(int groupId)
        {
            try
            {
                var connections = LoadConnections();
                return connections.FindAll(c => c.GroupId == groupId);
            }
            catch (Exception ex)
            {
                LogService.LogError($"获取分组连接失败: {ex.Message}", ex);
                return new System.Collections.Generic.List<RemoteConnection>();
            }
        }

        /// <summary>
        /// 加载分组列表
        /// </summary>
        private System.Collections.Generic.List<ConnectionGroup> LoadGroups()
        {
            try
            {
                if (!File.Exists(_groupsFilePath))
                {
                    return new System.Collections.Generic.List<ConnectionGroup>();
                }

                string json = File.ReadAllText(_groupsFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var groups = JsonSerializer.Deserialize<System.Collections.Generic.List<ConnectionGroup>>(json, options);
                return groups ?? new System.Collections.Generic.List<ConnectionGroup>();
            }
            catch (Exception ex)
            {
                LogService.LogError($"加载分组列表失败: {ex.Message}", ex);
                return new System.Collections.Generic.List<ConnectionGroup>();
            }
        }

        /// <summary>
        /// 保存分组列表
        /// </summary>
        private void SaveGroups(System.Collections.Generic.List<ConnectionGroup> groups)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(groups, options);
                File.WriteAllText(_groupsFilePath, json);
                LogService.LogInfo($"已保存 {groups.Count} 个分组");
            }
            catch (Exception ex)
            {
                LogService.LogError($"保存分组列表失败: {ex.Message}", ex);
            }
        }

        #endregion

        /// <summary>
        /// 保存远程连接列表
        /// </summary>
        /// <param name="connections">远程连接列表</param>
        public void SaveConnections(System.Collections.Generic.List<RemoteConnection> connections)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
                };

                // 创建一个临时列表用于序列化，只保存加密的连接码
                var serializableConnections = new System.Collections.Generic.List<object>();
                foreach (var conn in connections)
                {
                    serializableConnections.Add(new
                    {
                        id = conn.Id,
                        name = conn.Name,
                        identificationCode = conn.IdentificationCode,
                        encryptedConnectionCode = conn.EncryptedConnectionCode,
                        verificationCode = conn.VerificationCode,
                        createdAt = conn.CreatedAt,
                        updatedAt = conn.UpdatedAt,
                        lastConnectedAt = conn.LastConnectedAt,
                        remarks = conn.Remarks,
                        isEnabled = conn.IsEnabled,
                        isFavorite = conn.IsFavorite,
                        groupId = conn.GroupId
                    });
                }

                string json = JsonSerializer.Serialize(serializableConnections, options);
                File.WriteAllText(_connectionsFilePath, json);
                LogService.LogInfo($"已保存 {connections.Count} 个远程连接（连接码已加密）");
            }
            catch (Exception ex)
            {
                LogService.LogError($"保存远程连接列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载远程连接列表
        /// </summary>
        /// <returns>远程连接列表</returns>
        public System.Collections.Generic.List<RemoteConnection> LoadConnections()
        {
            try
            {
                if (!File.Exists(_connectionsFilePath))
                {
                    LogService.LogInfo("远程连接文件不存在，返回空列表");
                    return new System.Collections.Generic.List<RemoteConnection>();
                }

                string json = File.ReadAllText(_connectionsFilePath);
                
                // 使用JsonDocument先解析，以便处理新旧格式
                var jsonDoc = System.Text.Json.JsonDocument.Parse(json);
                var connections = new System.Collections.Generic.List<RemoteConnection>();

                foreach (var element in jsonDoc.RootElement.EnumerateArray())
                {
                    var conn = new RemoteConnection();
                    
                    if (element.TryGetProperty("id", out var idProp))
                        conn.Id = idProp.GetInt32();
                    
                    if (element.TryGetProperty("name", out var nameProp))
                        conn.Name = nameProp.GetString() ?? string.Empty;
                    
                    if (element.TryGetProperty("identificationCode", out var idCodeProp))
                        conn.IdentificationCode = idCodeProp.GetString() ?? string.Empty;
                    
                    // 优先读取加密的连接码
                    if (element.TryGetProperty("encryptedConnectionCode", out var encCodeProp))
                    {
                        conn.EncryptedConnectionCode = encCodeProp.GetString() ?? string.Empty;
                    }
                    // 兼容旧格式：如果没有加密字段，读取明文字段
                    else if (element.TryGetProperty("connectionCode", out var codeProp))
                    {
                        string code = codeProp.GetString() ?? string.Empty;
                        // 如果是明文，通过属性设置会自动加密
                        conn.ConnectionCode = code;
                    }
                    
                    if (element.TryGetProperty("verificationCode", out var verCodeProp))
                        conn.VerificationCode = verCodeProp.GetString() ?? string.Empty;
                    
                    if (element.TryGetProperty("createdAt", out var createdProp))
                        conn.CreatedAt = createdProp.GetDateTime();
                    
                    if (element.TryGetProperty("updatedAt", out var updatedProp))
                        conn.UpdatedAt = updatedProp.GetDateTime();
                    
                    if (element.TryGetProperty("lastConnectedAt", out var lastConnProp))
                        conn.LastConnectedAt = lastConnProp.GetDateTime();
                    
                    if (element.TryGetProperty("remarks", out var remarksProp))
                        conn.Remarks = remarksProp.GetString() ?? string.Empty;
                    
                    if (element.TryGetProperty("isEnabled", out var enabledProp))
                        conn.IsEnabled = enabledProp.GetBoolean();

                    if (element.TryGetProperty("isFavorite", out var favProp))
                        conn.IsFavorite = favProp.GetBoolean();

                    if (element.TryGetProperty("groupId", out var groupIdProp))
                        conn.GroupId = groupIdProp.GetInt32();
                    
                    connections.Add(conn);
                }

                LogService.LogInfo($"已加载 {connections.Count} 个远程连接");
                return connections;
            }
            catch (Exception ex)
            {
                LogService.LogError($"加载远程连接列表失败: {ex.Message}", ex);
                return new System.Collections.Generic.List<RemoteConnection>();
            }
        }

        /// <summary>
        /// 获取所有远程连接
        /// </summary>
        /// <returns>远程连接列表</returns>
        public System.Collections.Generic.List<RemoteConnection> GetAllConnections()
        {
            return LoadConnections();
        }

        /// <summary>
        /// 保存单个远程连接
        /// </summary>
        /// <param name="connection">远程连接对象</param>
        public void SaveConnection(RemoteConnection connection)
        {
            try
            {
                var connections = LoadConnections();

                // 按识别码去重：如果识别码已存在，跳过保存
                if (!string.IsNullOrEmpty(connection.IdentificationCode))
                {
                    int existingIndex = connections.FindIndex(c => c.IdentificationCode == connection.IdentificationCode);
                    if (existingIndex >= 0)
                    {
                        // 如果是同一个连接（ID相同），则更新
                        if (connections[existingIndex].Id == connection.Id && connection.Id != 0)
                        {
                            connections[existingIndex] = connection;
                            SaveConnections(connections);
                            LogService.LogInfo($"已更新远程连接: {connection.Name} (ID: {connection.Id})");
                            return;
                        }
                        // 识别码重复但ID不同，跳过
                        LogService.LogWarning($"跳过重复识别码的连接: {connection.Name} (识别码: {connection.IdentificationCode})");
                        return;
                    }
                }

                if (connection.Id == 0)
                {
                    int maxId = connections.Count > 0 ? connections.Max(c => c.Id) : 0;
                    connection.Id = maxId + 1;
                }
                else
                {
                    int index = connections.FindIndex(c => c.Id == connection.Id);
                    if (index >= 0)
                    {
                        connections[index] = connection;
                        SaveConnections(connections);
                        LogService.LogInfo($"已更新远程连接: {connection.Name} (ID: {connection.Id})");
                        return;
                    }
                }

                connections.Add(connection);
                SaveConnections(connections);
                LogService.LogInfo($"已保存远程连接: {connection.Name} (ID: {connection.Id})");
            }
            catch (Exception ex)
            {
                LogService.LogError($"保存远程连接失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 更新远程连接
        /// </summary>
        /// <param name="connection">远程连接对象</param>
        public void UpdateConnection(RemoteConnection connection)
        {
            try
            {
                var connections = LoadConnections();
                int index = connections.FindIndex(c => c.Id.ToString() == connection.Id.ToString());
                
                if (index >= 0)
                {
                    connections[index] = connection;
                    SaveConnections(connections);
                    LogService.LogInfo($"已更新远程连接: {connection.Name}");
                }
                else
                {
                    LogService.LogWarning($"未找到要更新的远程连接: {connection.Id}");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"更新远程连接失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 删除远程连接
        /// </summary>
        /// <param name="id">连接ID</param>
        public void DeleteConnection(string id)
        {
            try
            {
                var connections = LoadConnections();
                int removedCount = connections.RemoveAll(c => c.Id.ToString() == id);
                
                if (removedCount > 0)
                {
                    SaveConnections(connections);
                    LogService.LogInfo($"已删除远程连接: {id}");
                }
                else
                {
                    LogService.LogWarning($"未找到要删除的远程连接: {id}");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError($"删除远程连接失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 加载应用程序设置
        /// </summary>
        /// <returns>应用程序设置</returns>
        private AppSettings LoadSettings()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    LogService.LogInfo("设置文件不存在，创建默认设置");
                    return new AppSettings();
                }

                string json = File.ReadAllText(_settingsFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var settings = JsonSerializer.Deserialize<AppSettings>(json, options);
                return settings ?? new AppSettings();
            }
            catch (Exception ex)
            {
                LogService.LogError($"加载设置失败: {ex.Message}", ex);
                return new AppSettings();
            }
        }

        /// <summary>
        /// 保存应用程序设置
        /// </summary>
        /// <param name="settings">应用程序设置</param>
        private void SaveSettings(AppSettings settings)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(_settingsFilePath, json);
                LogService.LogInfo("已保存应用程序设置");
            }
            catch (Exception ex)
            {
                LogService.LogError($"保存设置失败: {ex.Message}", ex);
            }
        }

        #region 快捷键设置

        /// <summary>
        /// 获取快捷键配置
        /// </summary>
        public ShortcutsSettings GetShortcutsSettings()
        {
            try
            {
                var settings = LoadSettings();
                return settings.Shortcuts ?? new ShortcutsSettings();
            }
            catch (Exception ex)
            {
                LogService.LogError($"获取快捷键设置失败: {ex.Message}", ex);
                return new ShortcutsSettings();
            }
        }

        /// <summary>
        /// 保存快捷键配置
        /// </summary>
        public void SaveShortcutsSettings(ShortcutsSettings shortcuts)
        {
            try
            {
                var settings = LoadSettings();
                settings.Shortcuts = shortcuts;
                SaveSettings(settings);
                LogService.LogInfo("已保存快捷键配置");
            }
            catch (Exception ex)
            {
                LogService.LogError($"保存快捷键设置失败: {ex.Message}", ex);
            }
        }

        #endregion

        #region 安全设置

        /// <summary>
        /// 是否已设置主密码
        /// </summary>
        public bool HasMasterPassword()
        {
            var settings = LoadSettings();
            return !string.IsNullOrEmpty(settings.MasterPasswordHash);
        }

        /// <summary>
        /// 设置主密码
        /// </summary>
        public void SetMasterPassword(string password)
        {
            var settings = LoadSettings();
            string salt = EncryptionService.GenerateSalt();
            settings.MasterPasswordHash = EncryptionService.HashPassword(password, salt);
            settings.PasswordSalt = salt;
            SaveSettings(settings);
            LogService.LogInfo("主密码已设置");
        }

        /// <summary>
        /// 验证主密码
        /// </summary>
        public bool VerifyMasterPassword(string password)
        {
            var settings = LoadSettings();
            if (string.IsNullOrEmpty(settings.MasterPasswordHash) || string.IsNullOrEmpty(settings.PasswordSalt))
                return false;
            return EncryptionService.VerifyPassword(password, settings.MasterPasswordHash, settings.PasswordSalt);
        }

        /// <summary>
        /// 修改主密码
        /// </summary>
        public bool ChangeMasterPassword(string currentPassword, string newPassword)
        {
            if (!VerifyMasterPassword(currentPassword))
                return false;
            SetMasterPassword(newPassword);
            LogService.LogInfo("主密码已更改");
            return true;
        }

        /// <summary>
        /// 移除主密码
        /// </summary>
        public void RemoveMasterPassword()
        {
            var settings = LoadSettings();
            settings.MasterPasswordHash = null;
            settings.PasswordSalt = null;
            SaveSettings(settings);
            LogService.LogInfo("主密码已移除");
        }

        /// <summary>
        /// 获取自动锁定时间（分钟，0 = 关闭）
        /// </summary>
        public int GetAutoLockMinutes()
        {
            var settings = LoadSettings();
            return settings.AutoLockMinutes;
        }

        /// <summary>
        /// 设置自动锁定时间（分钟，0 = 关闭）
        /// </summary>
        public void SetAutoLockMinutes(int minutes)
        {
            var settings = LoadSettings();
            settings.AutoLockMinutes = Math.Max(0, minutes);
            SaveSettings(settings);
            LogService.LogInfo($"自动锁定已设置为 {(minutes == 0 ? "关闭" : minutes + "分钟")}");
        }

        #endregion
    }

    /// <summary>
    /// 应用程序设置类
    /// </summary>
    public class AppSettings
    {
        public string SunloginPath { get; set; } = string.Empty;
        public ShortcutsSettings Shortcuts { get; set; } = new ShortcutsSettings();
        public string? MasterPasswordHash { get; set; }
        public string? PasswordSalt { get; set; }
        public int AutoLockMinutes { get; set; } = 0;
    }
}