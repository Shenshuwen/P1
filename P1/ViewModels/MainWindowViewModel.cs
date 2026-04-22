using System;
using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using P1.Models;
using P1.Views;

namespace P1.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase, IDisposable
    {
        private const string buttonActiveClass = "active";

        //隐藏页面的bool值
        [ObservableProperty]
        public bool _sideMenuExpanded = true;

        [ObservableProperty]
        //界面选项的交互提示
        [NotifyPropertyChangedFor(nameof(HomePageIsActive))]
        [NotifyPropertyChangedFor(nameof(ProcessPageIsActive))]
        [NotifyPropertyChangedFor(nameof(HistoryPageIsActive))]
        private ViewModelBase _currentPage;

        public bool HomePageIsActive => CurrentPage == _homePage;
        public bool ProcessPageIsActive => CurrentPage == _processPage;
        public bool HistoryPageIsActive => CurrentPage == _historyPage;

        //各个页面的绑定
        private readonly HomePageViewModel _homePage = new();
        private readonly ProcessPageViewModel _processPage = new();
        private readonly SettingPageViewModel _settingPage = new();
        //private readonly HistortyPageViewModel _historyPage = new();
        private readonly HistoryPageViewModel _historyPage = new();


        public MainWindowViewModel()
        {
            CurrentPage = _homePage;
        }

        [RelayCommand]
        private void SideMenuResize()
        {

            SideMenuExpanded = !SideMenuExpanded;
        }
        [RelayCommand]
        public void GoToHome()
        {
            CurrentPage = _homePage;
        }
        [RelayCommand]
        public void GoToProcess()
        {
            CurrentPage = _processPage;
        }
        [RelayCommand]
        public void GoToSetting()
        {
            CurrentPage = _settingPage;
        }

        [RelayCommand]
        public void GoToHistory()
        {
            CurrentPage = _historyPage;
        }

        public void Dispose()
        {
            _processPage.Dispose();
            GC.SuppressFinalize(this);
        }

    }
}
