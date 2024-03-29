﻿using System;
using System.Collections.Generic;
using System.Data;
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
using System.Windows.Shapes;

namespace Spectra
{
    /// <summary>
    /// QuickViewWindow.xaml 的交互逻辑
    /// </summary>
    public partial class QuickViewWindow : Window
    {
        /// <summary>
        /// 1列排放几张图像
        /// </summary>
        private int split = 2;
        DataTable dtSelect = new DataTable();

        /// <summary>
        /// 构造函数
        /// </summary>
        public QuickViewWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 将选中的文件进行显示
        /// </summary>
        /// <param name="dtTree">存储显示文件的详细列表</param>
        public void ShowView(DataTable dtTree)
        {
            dtSelect = dtTree;
            int subCnt = 0, subSum = dtTree.Rows.Count;
            panelBack.Children.Clear();
            while (subCnt < subSum)
            {
                Border bord = new Border();
                Image img = new Image();
                bord.Child = img;
                bord.Margin = new Thickness(5);
                bord.BorderThickness = new Thickness(1);
                bord.Background = new SolidColorBrush(Colors.GreenYellow);
                bord.BorderBrush = new SolidColorBrush(Colors.Yellow);
                try
                {
                    img.Source = new BitmapImage(new Uri($"{dtTree.Rows[subCnt]["SavePath"]}\\{dtTree.Rows[subCnt]["SubId"]}.bmp"));
                    img.MouseLeftButtonDown += Img_MouseLeftButtonDown;
                }
                catch { }
                SetImgInfo(img, dtTree.Rows[subCnt]);
                panelBack.Children.Add(bord);
                subCnt++;
            }
            Show();
        }

        /// <summary>
        /// Image控件选中事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var img = sender as Image;
            if (img.Opacity == 1)
                img.Opacity = 0.6;
            else
                img.Opacity = 1;
        }

        /// <summary>
        /// 设置图像信息
        /// </summary>
        /// <param name="img">Image控件</param>
        /// <param name="md5">MD5</param>
        /// <param name="id">子编号</param>
        public void SetImgInfo(Image img, DataRow dr)
        {
            string str;
            str = $"文件名:{dr["MD5"]}\n";
            str += $"编号:{dr["SubId"]}\n";
            str += $"像宽:{dr["FrameSum"]}\n";
            str += $"起始时间:{dr["StartTime"]}\n";
            str += $"结束时间:{dr["EndTime"]}\n";
            str += $"起始经纬:{dr["StartCoord"]}\n";
            str += $"结束经纬:{dr["EndCoord"]}";
            img.ToolTip = str;
        }

        /// <summary>
        /// 设置图像控件大小
        /// </summary>
        /// <param name="size">当前窗体尺寸</param>
        public void SetImgSize(Size size)
        {
            panelBack.Height = size.Height - 57;
            panelBack.ItemHeight = panelBack.Height / split;
            panelBack.ItemWidth = (panelBack.ItemHeight - 12) * 2 + 12;
        }

        /// <summary>
        /// 随窗口大小变化改变Item值
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void winQuickView_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetImgSize(e.NewSize);
        }

        /// <summary>
        /// 该窗体永不关闭，只隐藏
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Hide();
            e.Cancel = true;
        }

        /// <summary>
        /// 图像缩放
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panelBack_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            split -= e.Delta / 120;
            if (split < 1)
                split = 1;
            if (split > 16)
                split = 16;
            SetImgSize(RenderSize);
        }

        public DataTable getSelects()
        {
            for (int i = App.global_QuickViewWindow.panelBack.Children.Count - 1; i >= 0; i--)
            {
                if (((Border)App.global_QuickViewWindow.panelBack.Children[i]).Child.Opacity == 1)
                {
                    dtSelect.Rows.RemoveAt(i);
                }
            }
            return dtSelect;
        }

        public Task<int> SaveFiles(DataTable dtS,string desPath,IProgress<double> prog)
        {
            return Task.Factory.StartNew(()=> {
                int sum = dtS.Rows.Count;
                if (sum < 1) return 0;
                desPath = desPath + DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + "\\";
                Directory.CreateDirectory(desPath);
                int cnt = 0;
                foreach (DataRow dr in dtS.Rows)
                {
                    Directory.CreateDirectory($"{desPath}{dr["MD5"]}");
                    Directory.CreateDirectory($"{desPath}{dr["MD5"]}\\{dr["SubId"]}");
                    for (int i = 0; i < 160; i++)
                    {
                        File.Copy($"{dr["SavePath"]}\\{i}.raw", $"{desPath}{dr["MD5"]}\\{dr["SubId"]}\\{i}.raw", true);
                        prog.Report((i + 1 + 160.0 * cnt) / 160.0 / sum);
                    }
                    File.Copy($"{dr["SavePath"]}\\{dr["SubId"]}.bmp", $"{desPath}{dr["MD5"]}\\{dr["SubId"]}\\{dr["SubId"]}.bmp", true);
                    File.Copy($"{dr["SavePath"]}\\数据库_{dr["SubId"]}.sqlite", $"{desPath}{dr["MD5"]}\\{dr["SubId"]}\\数据库_{dr["SubId"]}.sqlite", true);
                    File.Copy($"{dr["SavePath"]}\\辅助数据_{dr["SubId"]}.xls", $"{desPath}{dr["MD5"]}\\{dr["SubId"]}\\辅助数据_{dr["SubId"]}.xls", true);
                    cnt++;
                }
                prog.Report(1);
                return 1;
            });
        }
    }
}
