using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using P1.Common;

namespace P1.ViewModels
{
    public partial class HistoryPageViewModel : ViewModelBase
    {
        // 引用共享数据服务
        private readonly LabStatusService _labStatus = LabStatusService.Instance;

        // 历史数据绑定
        public ObservableCollection<HistoryRecord> History => _labStatus.History;
        public HistoryPageViewModel()
        {

        }
        [RelayCommand]
        public void ExportHistoryData()
        {
            //Console.WriteLine("导出历史数据功能待实现");

        }
    }
}
