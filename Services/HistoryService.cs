using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using SunloginManager.Models;

namespace SunloginManager.Services
{
    public class HistoryService
    {
        private readonly string _historyFilePath;
        private readonly List<ConnectionHistory> _histories;

        public HistoryService(string dataDirectory)
        {
            _historyFilePath = Path.Combine(dataDirectory, "history.json");
            _histories = LoadHistories();
        }

        public void RecordStart(int connectionId, string name, string code, DateTime startTime)
        {
            var entry = new ConnectionHistory
            {
                Id = _histories.Count > 0 ? _histories.Max(h => h.Id) + 1 : 1,
                ConnectionId = connectionId,
                ConnectionName = name,
                IdentificationCode = code,
                StartTime = startTime,
                Status = ConnectionStatus.Success
            };
            _histories.Add(entry);
            SaveHistories();
        }

        public void RecordEnd(int connectionId, DateTime endTime)
        {
            var entry = _histories.LastOrDefault(h => h.ConnectionId == connectionId && h.EndTime == null);
            if (entry != null)
            {
                entry.EndTime = endTime;
                entry.Duration = endTime - entry.StartTime;
                SaveHistories();
            }
        }

        public void RecordFailed(int connectionId, string name, string code, string error, ConnectionStatus status)
        {
            var entry = new ConnectionHistory
            {
                Id = _histories.Count > 0 ? _histories.Max(h => h.Id) + 1 : 1,
                ConnectionId = connectionId,
                ConnectionName = name,
                IdentificationCode = code,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now,
                Duration = TimeSpan.Zero,
                Status = status,
                ErrorMessage = error
            };
            _histories.Add(entry);
            SaveHistories();
        }

        public List<ConnectionHistory> GetAll() => _histories.OrderByDescending(h => h.StartTime).ToList();

        public List<ConnectionHistory> GetByConnection(int connectionId) =>
            _histories.Where(h => h.ConnectionId == connectionId).OrderByDescending(h => h.StartTime).ToList();

        public List<ConnectionHistory> GetRecent(int days)
        {
            var cutoff = DateTime.Now.AddDays(-days);
            return _histories.Where(h => h.StartTime >= cutoff).OrderByDescending(h => h.StartTime).ToList();
        }

        public int GetConnectCount(int connectionId) => _histories.Count(h => h.ConnectionId == connectionId && h.Status == ConnectionStatus.Success);

        public TimeSpan? GetTotalDuration(int connectionId)
        {
            var entries = _histories.Where(h => h.ConnectionId == connectionId && h.Duration.HasValue).ToList();
            if (entries.Count == 0) return null;
            long totalTicks = 0;
            foreach (var e in entries) totalTicks += e.Duration.Value.Ticks;
            return new TimeSpan(totalTicks);
        }

        public ConnectionStats GetStats()
        {
            var all = _histories;
            var success = all.Where(h => h.Status == ConnectionStatus.Success).ToList();
            var failed = all.Where(h => h.Status != ConnectionStatus.Success).ToList();
            var withDuration = success.Where(h => h.Duration.HasValue).ToList();

            var top5 = success
                .GroupBy(h => new { h.ConnectionId, h.ConnectionName })
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new TopConnection { ConnectionId = g.Key.ConnectionId, Name = g.Key.ConnectionName, Count = g.Count() })
                .ToList();

            var last7 = all.Where(h => h.StartTime >= DateTime.Now.AddDays(-7)).ToList();
            var last30 = all.Where(h => h.StartTime >= DateTime.Now.AddDays(-30)).ToList();

            long avgTicks = 0;
            if (withDuration.Count > 0)
            {
                long totalTicks = 0;
                foreach (var e in withDuration) totalTicks += e.Duration.Value.Ticks;
                avgTicks = totalTicks / withDuration.Count;
            }

            return new ConnectionStats
            {
                TotalConnections = all.Count,
                SuccessCount = success.Count,
                FailedCount = failed.Count,
                AverageDuration = withDuration.Count > 0 ? new TimeSpan(avgTicks) : null,
                TopConnections = top5,
                Last7DaysCount = last7.Count,
                Last30DaysCount = last30.Count,
                Last7DaysSuccess = last7.Count(h => h.Status == ConnectionStatus.Success),
                Last30DaysSuccess = last30.Count(h => h.Status == ConnectionStatus.Success)
            };
        }

        private List<ConnectionHistory> LoadHistories()
        {
            try
            {
                if (!File.Exists(_historyFilePath)) return new List<ConnectionHistory>();
                var json = File.ReadAllText(_historyFilePath);
                var list = JsonSerializer.Deserialize<List<ConnectionHistory>>(json);
                return list ?? new List<ConnectionHistory>();
            }
            catch { return new List<ConnectionHistory>(); }
        }

        private void SaveHistories()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(_historyFilePath, JsonSerializer.Serialize(_histories, options));
            }
            catch (Exception ex) { LogService.LogError($"保存历史记录失败: {ex.Message}", ex); }
        }
    }

    public class ConnectionStats
    {
        public int TotalConnections { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
        public TimeSpan? AverageDuration { get; set; }
        public List<TopConnection> TopConnections { get; set; } = new();
        public int Last7DaysCount { get; set; }
        public int Last30DaysCount { get; set; }
        public int Last7DaysSuccess { get; set; }
        public int Last30DaysSuccess { get; set; }
    }

    public class TopConnection
    {
        public int ConnectionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
