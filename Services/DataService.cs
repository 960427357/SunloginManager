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

        public DataService()
        {
            _dataDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
            _settingsFilePath = Path.Combine(_dataDirectory, "settings.json");
            _connectionsFilePath = Path.Combine(_dataDirectory, "connections.json");

            // 确保数据目录存在
            if (!Directory.Exists(_dataDirectory))
            {
                Directory.CreateDirectory(_dataDirectory);
            }
            
            // 修复现有数据中的ID问题
            FixConnectionIds();
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
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(connections, options);
                File.WriteAllText(_connectionsFilePath, json);
                LogService.LogInfo($"已保存 {connections.Count} 个远程连接");
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
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var connections = JsonSerializer.Deserialize<System.Collections.Generic.List<RemoteConnection>>(json, options);
                LogService.LogInfo($"已加载 {connections?.Count ?? 0} 个远程连接");
                return connections ?? new System.Collections.Generic.List<RemoteConnection>();
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
                
                // 如果连接ID为0，则分配一个新的唯一ID
                if (connection.Id == 0)
                {
                    int maxId = 0;
                    foreach (var conn in connections)
                    {
                        if (conn.Id > maxId)
                            maxId = conn.Id;
                    }
                    connection.Id = maxId + 1;
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
    }

    /// <summary>
    /// 应用程序设置类
    /// </summary>
    public class AppSettings
    {
        public string SunloginPath { get; set; } = string.Empty;
    }
}