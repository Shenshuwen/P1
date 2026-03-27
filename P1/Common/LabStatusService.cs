using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// 读取实验室状态的服务类（共享数据流）
namespace P1.Common
{
    // 历史记录数据结构
    public record HistoryRecord(
        DateTime hisTime,
        double hisWind,
        double hisPressure,
        double hisTemperature,
        double hisHumidity
    );

    public class LabStatusService
    {
        // 单例
        public static LabStatusService Instance { get; } = new LabStatusService();

        public LabStatusService() { }

        // ===== 实时数据 =====

        public double Wind { get; set; }
        public double Stress { get; set; }
        public double Tem1 { get; set; }
        public double Wep1 { get; set; }


        // ===== 历史数据集合 =====
        public ObservableCollection<HistoryRecord> History { get; } = new();

        // ===== 添加历史记录 =====
        public void AddHistory()
        {
            History.Insert(0, new HistoryRecord(
                DateTime.Now,
                Wind,
                Stress,
                Tem1,
                Wep1
            ));
            if (History.Count > 200)
                History.RemoveAt(History.Count - 1);
        }
        // ===== AI读取实验室状态 =====
        public string GetSummary()
        {
            return $"当前实验室温度 {Tem1:F1} ℃，湿度 {Wep1:F1} %，风速 {Wind:F2} m/s，压力{Stress:F2}Pa";
        }
    }
}