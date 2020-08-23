﻿using LiveCharts;
using LiveCharts.Wpf;
using MahApps.Metro.Controls.Dialogs;
using NsisoLauncher.Utils;
using NsisoLauncher.Views.Windows;
using NsisoLauncherCore.Net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NsisoLauncher.ViewModels.Pages
{
    public class DownloadPageViewModel : INotifyPropertyChanged
    {
        public Windows.MainWindowViewModel MainWindowVM { get; set; }


        public ObservableCollection<DownloadTask> Tasks { get; private set; }

        public double Percentage { get; set; }

        public string SpeedStr { get; set; }

        public int ProgressMaximum { get; set; }

        public int ProgressValue { get; set; }

        #region Char
        public SeriesCollection ChartSeries { get; set; }

        public ChartValues<double> SpeedValues { get; set; }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        #region Commands
        public ICommand CancelButtonCommand { get; set; }
        public ICommand PauseBeginButtonCommand { get; set; }
        public ICommand NewDownloadButtonCommand { get; set; }
        #endregion

        public DownloadPageViewModel()
        {
            InitChart();
            SpeedStr = "0Kb/s";
            ProgressMaximum = 1;
            ProgressValue = 0;
            Percentage = 0;
            if (App.NetHandler.Downloader != null)
            {
                Tasks = App.NetHandler.Downloader.ViewDownloadTasks;
                App.NetHandler.Downloader.DownloadProgressChanged += Downloader_DownloadProgressChanged;
                App.NetHandler.Downloader.DownloadSpeedChanged += Downloader_DownloadSpeedChanged;
                App.NetHandler.Downloader.DownloadCompleted += Downloader_DownloadCompleted;
            }

            CancelButtonCommand = new DelegateCommand(async (a)=> {
                await CancelDownload();
            });

            PauseBeginButtonCommand = new DelegateCommand((a) => {
                if (App.NetHandler.Downloader.IsBusy)
                {
                    App.NetHandler.Downloader.RequestPause();
                }
                else
                {
                    App.NetHandler.Downloader.RequestContinue();
                }
            });

            NewDownloadButtonCommand = new DelegateCommand((a) => {
                new NewDownloadTaskWindow().ShowDialog();
            });

            MainWindowVM = App.MainWindowVM;
        }

        public void InitChart()
        {
            SpeedValues = new ChartValues<double>(new List<double>(50));
            ChartSeries = new SeriesCollection() { new LineSeries()
            { Values = SpeedValues, PointGeometry = null, LineSmoothness = 0, Title = "下载速度" } };
        }

        private void ClearSpeedValues()
        {
            for (int i = 0; i < 50; i++)
            {
                SpeedValues[i] = 0;
            }
        }

        public Func<double, string> YFormatter { get; set; } = new Func<double, string>((value) =>
        {
            string speedUnit;
            double speedValue;
            if (value > 1048576)
            {
                speedUnit = "MB/s";
                speedValue = Math.Round(value / 1048576);
            }
            else if (value > 1024)
            {
                speedUnit = "KB/s";
                speedValue = Math.Round(value / 1024);
            }
            else
            {
                speedUnit = "B/s";
                speedValue = value;
            }
            return string.Format("{0}{1}", speedValue, speedUnit);
        });
        private async void Downloader_DownloadCompleted(object sender, DownloadCompletedArg e)
        {
            SpeedStr = "0Kb/s";
            ProgressMaximum = 1;
            ProgressValue = 0;
            Percentage = 0;
            ClearSpeedValues();
            if (e.ErrorList == null || e.ErrorList.Count == 0)
            {
                await MainWindowVM.ShowMessageAsync(App.GetResourceString("String.Downloadwindow.DownloadComplete"),
                    App.GetResourceString("String.Downloadwindow.DownloadComplete2"));
                //undo close window
            }
            else
            {
                await MainWindowVM.ShowMessageAsync(App.GetResourceString("String.Downloadwindow.DownloadCompleteWithError"),
                    string.Format(App.GetResourceString("String.Downloadwindow.DownloadCompleteWithError2"), e.ErrorList.Count, e.ErrorList.First().Value.Message));
            }
        }

        private void Downloader_DownloadSpeedChanged(object sender, DownloadSpeedChangedArg e)
        {
            SpeedStr = e.SpeedValue.ToString() + e.SpeedUnit;
            SpeedValues.Add(e.SizePerSec);
            SpeedValues.RemoveAt(0);
        }

        private void Downloader_DownloadProgressChanged(object sender, DownloadProgressChangedArg e)
        {
            int total = e.LeftTasksCount + e.DoneTaskCount;
            ProgressMaximum = total;
            ProgressValue = e.DoneTaskCount;
            Percentage = (double)e.DoneTaskCount / (e.DoneTaskCount + e.LeftTasksCount);
        }

        public async Task CancelDownload()
        {
            if (App.NetHandler.Downloader.IsBusy)
            {
                var result = await MainWindowVM.ShowMessageAsync(App.GetResourceString("String.Downloadwindow.MakesureCancel"),
               App.GetResourceString("String.Downloadwindow.MakesureCancel"),
               MessageDialogStyle.AffirmativeAndNegative,
               new MetroDialogSettings()
               {
                   AffirmativeButtonText = App.GetResourceString("String.Base.Yes"),
                   NegativeButtonText = App.GetResourceString("String.Base.Cancel")
               });
                if (result == MessageDialogResult.Affirmative)
                {
                    App.NetHandler.Downloader.RequestCancel();
                    ProgressValue = 0;
                }
            }
            else
            {
                await MainWindowVM.ShowMessageAsync("没有需要取消下载的任务", "当前下载器并没有在工作");
            }
        }
    }
}
