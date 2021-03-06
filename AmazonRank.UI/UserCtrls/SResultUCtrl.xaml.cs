﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AmazonRank.UI.UserCtrls
{
    /// <summary>
    /// SResultUCtrl.xaml 的交互逻辑
    /// </summary>
    public partial class SResultUCtrl : UserControl
    {
        public SResultUCtrl()
        {
            InitializeComponent();
        }

        public void UpdateDataSource(List<SearchModel> sModelList)
        {
            if (sModelList != null && sModelList.Count > 0)
            {
                this.TBox_Asin.Text = sModelList[0].Asin;
                var source = new List<dynamic>();
                sModelList.ForEach(sModel =>
                {
                    bool IsError = !string.IsNullOrEmpty(sModel.ErrorMsg);
                    if (sModel.SearchType == 0)
                    {
                        sModel.FindModels.RemoveAll(o => o.IsSponsored);
                    }
                    else if (sModel.SearchType == 1)
                    {
                        sModel.FindModels.RemoveAll(o => !o.IsSponsored);
                    }
                    string SponsoredRank = string.Empty, SponsoredPos = string.Empty, SponsoredPage = string.Empty;
                    if (sModel.FindModels.Count > 0)
                    {
                        var noSponsoredModel = sModel.FindModels.FirstOrDefault(o => !o.IsSponsored);
                        var SponsoredModel = sModel.FindModels.FirstOrDefault(o => o.IsSponsored);
                        source.Add(new
                        {
                            sModel.KeyWord,
                            Page = IntToString(noSponsoredModel?.Page ?? 0),
                            Rank = IntToString(noSponsoredModel?.Rank ?? 0),
                            Pos = IntToString(noSponsoredModel?.Pos ?? 0),
                            sModel.ResultNumString,
                            SponsoredRank = IntToString(SponsoredModel?.Rank ?? 0),
                            SponsoredPos = IntToString(SponsoredModel?.Pos ?? 0),
                            SponsoredPage = IntToString(SponsoredModel?.Page ?? 0),
                            sModel.ErrorMsg,
                            KeyWordFColor = IsError ? Brushes.Red : Brushes.Black
                        });
                    }
                    else
                    {
                        source.Add(new
                        {
                            sModel.KeyWord,
                            Page = string.Empty,
                            Rank = string.Empty,
                            Pos = string.Empty,
                            sModel.ResultNumString,
                            sModel.ErrorMsg,
                            SponsoredRank,
                            SponsoredPos,
                            SponsoredPage,
                            KeyWordFColor = IsError ? Brushes.Red : Brushes.Black
                        });
                    }

                });
                this.DGrid_SResult.ItemsSource = source;
            }
        }

        private string IntToString(int value) {
            if (value <= 0) {
                return string.Empty;
            }
            return value.ToString();
        }

        private void Btn_Export_Click(object sender, RoutedEventArgs e)
        {
            var exportList = this.DGrid_SResult.ItemsSource as List<dynamic>;
            if (exportList == null || exportList.Count == 0)
            {
                MessageBox.Show("没有数据可导出!", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }


            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "CSV files (*.csv)|*.csv";
            saveFileDialog.FilterIndex = 0;
            saveFileDialog.RestoreDirectory = true;
            saveFileDialog.CreatePrompt = true;
            saveFileDialog.FileName = null;
            saveFileDialog.Title = "保存";
            bool? isOk = saveFileDialog.ShowDialog();
            if (isOk.HasValue && isOk.Value)
            {
                Stream stream = saveFileDialog.OpenFile();
                StreamWriter sw = new StreamWriter(stream, System.Text.Encoding.GetEncoding(-0));
                StringBuilder csvString = new StringBuilder();
                try
                {
                    // Asin：
                    sw.WriteLine($"Asin:,{this.TBox_Asin.Text}");

                    //表头
                    sw.WriteLine("关键字,搜索结果数,页码,排名,页面位置,广告页码,广告排名,广告位置,异常");

                    // 表内容
                    exportList.ForEach(l =>
                    {
                        sw.WriteLine($"{formatCsvField(Convert.ToString(l.KeyWord))},{formatCsvField(Convert.ToString(l.ResultNumString))},{formatCsvField(Convert.ToString(l.Page))},{formatCsvField(Convert.ToString(l.Rank))},{formatCsvField(Convert.ToString(l.Pos))},{formatCsvField(Convert.ToString(l.SponsoredPage))},{formatCsvField(Convert.ToString(l.SponsoredRank))},{formatCsvField(Convert.ToString(l.SponsoredPos))},{formatCsvField(Convert.ToString(l.ErrorMsg))}");
                    });
                    sw.Close();
                    stream.Close();
                    MessageBox.Show("数据被导出到：" + saveFileDialog.FileName.ToString(), "导出完毕", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "导出错误", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

        }

        /// <summary>
        /// CSV格式化
        /// </summary>
        /// <param name="data">数据</param>
        /// <returns>格式化数据</returns>
        private string formatCsvField(string data)
        {
            if (string.IsNullOrEmpty(data)) { return string.Empty; }
            data = data.Replace("\"", "\"\"");
            data = "\"" + data + "\"";
            return data;
        }

        //private void DGrid_SResult_LoadingRow(object sender, DataGridRowEventArgs e)
        //{
        //    //状态判断
        //    if (((dynamic)(e.Row.DataContext)).IsError)
        //    {
        //        //红色
        //        (DGrid_SResult.Columns[0].GetCellContent(e.Row).FindName("KeyWord") as DataGridTextColumn).Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0));
        //    }
        //}
    }
}
