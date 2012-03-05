using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using MySql.Data.MySqlClient;
using System.Data;

namespace DessertHouse
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CDessertHouse : Window
    {
        MySqlConnection conn;
        DataTable memberData;
        DataTable commodityData;
        MySqlDataAdapter da;
        MySqlCommandBuilder cb;

        public CDessertHouse()
        {
            InitializeComponent();
            ConnectDatabase();
            InitDataGrid(ref memberData, "select * from member", memberDataGrid);
            InitDataGrid(ref commodityData, "select * from commodity");
            InitMidComboBox();
        }

        void ConnectDatabase()
        {
            string connStr = "server=localhost;user id=root; password=pwd; database=mysql; pooling=false";
            try
            {
                conn = new MySqlConnection(connStr);
                conn.Open();
                conn.ChangeDatabase("desserthouse");
            }
            catch (MySqlException ex)
            {
                MessageBox.Show("Error connecting to the server: " + ex.Message);
            }
        }

        void InitMidComboBox()
        {
            ComboBoxItem item = new ComboBoxItem();
            item.Content = "Not VIP";
            item.Tag = "0";
            midComboBox.Items.Add(item);
            midComboBox.SelectedItem = midComboBox.Items[0];

            foreach (DataRow row in memberData.Rows)
            {
                item = new ComboBoxItem();
                item.Content = row.ItemArray[0];
                item.Tag = row.ItemArray[7];
                midComboBox.Items.Add(item);
            }
        }

        void InitDataGrid(ref DataTable dt, string cmd, DataGrid dg = null)
        {
            dt = new DataTable();
            da = new MySqlDataAdapter(cmd, conn);
            cb = new MySqlCommandBuilder(da);
            da.Fill(dt);
            if (dg != null)
            {
                dg.DataContext = dt;    
            }
        }

        void OnLoaded(object sender, RoutedEventArgs e)
        {
            Transparent();
        }

        void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Transparent();
        }

        void Transparent()
        {
            try
            {
                IntPtr mainWindowPtr = new WindowInteropHelper(this).Handle; 
                HwndSource mainWindowSrc = HwndSource.FromHwnd(mainWindowPtr);
                mainWindowSrc.CompositionTarget.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

                NonClientRegionAPI.MARGINS margins = new NonClientRegionAPI.MARGINS();
                margins.cxLeftWidth = margins.cxRightWidth = margins.cyBottomHeight = margins.cyTopHeight = -1;
                int hr = NonClientRegionAPI.DwmExtendFrameIntoClientArea(mainWindowSrc.Handle, ref margins);
            }
            catch (DllNotFoundException)
            {
                Application.Current.MainWindow.Background = Brushes.White;
            }
        }

        void ClearOldGraph(Canvas canvas)
        {
            for (int i = 0; i < canvas.Children.Count; i++)
            {
                if (canvas.Children[i] is Rectangle || canvas.Children[i] is Label)
                {
                    canvas.Children.RemoveAt(i);
                    i--;
                }
            }
        }

        int GetMaxNumber(SortedList<string, int> list)
        {
            int max = 1;
            for (int i = 0; i < list.Count; i++)
                if (list.Values[i] > max)
                    max = list.Values[i];

            return max;
        }

        //the draw area width=300 height=220, predefine and locked
        //the rectangle is horizontal
        //30 is the max height
        //interval between rectangle is 10
        void DrawAnimaGraph(Canvas canvas, SortedList<string, int> data, int animaDelay)
        {
            ClearOldGraph(canvas);
            Brush[] graphBrush = 
            {
                    Brushes.DodgerBlue, 
                    Brushes.Red,
                    Brushes.Green,
                    Brushes.DarkOrchid,
                    Brushes.DarkOrange,
                    Brushes.Pink, 
                    Brushes.LightGray, 
                    Brushes.Yellow, 
                    Brushes.Orange, 
                    Brushes.LightGreen 
            };

            int cvaWidth = 300, cvaHeight = 220;
            int maxHeight = 30;
            int interval = (data.Count > 5) ? 5 : 10;
            int pretop = 30;
            int preleft = 75;
            int rightMargin = 30;

            int height = cvaHeight / (data.Count + 1);
            int top = interval;
            if (height > maxHeight)
            {
                height = maxHeight;
                top = (cvaHeight - (height + interval) * (data.Count - 1) - height) / 2;
            }

            double scale = 1.0 * (cvaWidth - rightMargin) / GetMaxNumber(data);

            for (int i = 0; i < data.Count; i++)
            {
                Label name = new Label();
                name.Content = data.Keys[i];
                canvas.Children.Add(name);
                Canvas.SetLeft(name, 0);
                Canvas.SetTop(name, pretop + top + i * (height + interval));

                Label number = new Label();
                number.Content = data.Values[i];
                canvas.Children.Add(number);
                Canvas.SetTop(number, pretop + top + i * (height + interval));
                DoubleAnimation anima = new DoubleAnimation();
                anima.From = preleft;
                anima.To = preleft + data.Values[i] * scale;
                anima.Duration = new Duration(TimeSpan.FromMilliseconds(1000));
                anima.FillBehavior = FillBehavior.HoldEnd;
                anima.BeginTime = TimeSpan.FromMilliseconds(animaDelay);
                number.BeginAnimation(Canvas.LeftProperty, anima);

                Rectangle rect = new Rectangle();
                rect.Height = height;
                rect.Stroke = Brushes.Transparent;
                rect.Fill = graphBrush[(i % graphBrush.Length)];
                canvas.Children.Add(rect);
                Canvas.SetTop(rect, pretop + top + i * (height + interval));
                Canvas.SetLeft(rect, preleft);
                anima = new DoubleAnimation();
                anima.From = 0;
                anima.To = data.Values[i] * scale;
                anima.Duration = new Duration(TimeSpan.FromMilliseconds(1000));
                anima.FillBehavior = FillBehavior.HoldEnd;
                anima.BeginTime = TimeSpan.FromMilliseconds(animaDelay);
                rect.BeginAnimation(Rectangle.WidthProperty, anima);
            }
        }

        void ResetMenuButtonBackground(SolidColorBrush brush = null)
        {
            if (brush == null)
                brush = SystemColors.MenuBrush;
            memberBtn.Background = brush;
            salesBtn.Background = brush;
            statisticsBtn.Background = brush;
        }

        void TransSubMainPanel(Grid oldPanel, Grid newPanel)
        {
            DoubleAnimation anima = new DoubleAnimation();
            anima.From = 0;
            anima.To = -oldPanel.ActualWidth;
            anima.Duration = new Duration(TimeSpan.FromMilliseconds(600));
            anima.FillBehavior = FillBehavior.HoldEnd;
            
            var trans = new TranslateTransform();
            oldPanel.RenderTransform = trans;
            trans.BeginAnimation(TranslateTransform.XProperty, anima);
            
            anima = new DoubleAnimation();
            anima.From = newPanel.ActualWidth;
            anima.To = 0;
            anima.Duration = new Duration(TimeSpan.FromMilliseconds(600));
            trans = new TranslateTransform();
            newPanel.RenderTransform = trans;
            trans.BeginAnimation(TranslateTransform.XProperty, anima);

            Panel.SetZIndex(newPanel, 1);
            Panel.SetZIndex(oldPanel, 0);

            newPanel.Visibility = Visibility.Visible;
        }

        Grid GetOldPanel()
        {
            Grid oldPanel = null;
            foreach (UIElement child in mainPanel.Children)
                if (child is Grid && Panel.GetZIndex(oldPanel = (Grid)child) == 1)
                        return oldPanel;
 

            return null;
        }

        private void memberBtn_Click(object sender, RoutedEventArgs e)
        {
            ResetMenuButtonBackground();
            ((Button)sender).Background = Brushes.DodgerBlue;

            Grid oldPanel = GetOldPanel();

            if (oldPanel == memberPanel)
                return;

            TransSubMainPanel(oldPanel, memberPanel);
        }

        private void salesBtn_Click(object sender, RoutedEventArgs e)
        {
            ResetMenuButtonBackground();
            ((Button)sender).Background = Brushes.DodgerBlue;

            Grid oldPanel = GetOldPanel();
            
            if (oldPanel == salePanel)
                return;
            
            TransSubMainPanel(oldPanel, salePanel);
        }

        void GetDataAndDrawGraph(Canvas canvas, int delay, string[] cmds)
        {
            MySqlCommand sqlcmd;
            SortedList<string, int> data = new SortedList<string, int>();
            int result;
            
            for (int i = 0; i < cmds.Length; i += 2)
            {
                sqlcmd = new MySqlCommand(cmds[i], conn);
                try
                {
                    result = Convert.ToInt32(sqlcmd.ExecuteScalar());
                }
                catch (System.Exception ex)
                {
                    result = 0;
                }
                
                data.Add(cmds[i + 1], result);
            }

            DrawAnimaGraph(canvas, data, delay);
        }

        void DrawMemberGenderGraph(Canvas canvas, int delay)
        {
            string[] cmds = 
            {
                "select count(*) from member where gender='male'", "male",
                "select count(*) from member where gender='female'", "female"
            };

            GetDataAndDrawGraph(canvas, delay, cmds);
        }

        void DrawMemberAgeGraph(Canvas canvas, int delay)
        {
            string[] cmds = 
            {
                "select count(*) from member where age >= 10 && age <= 19", "10~19",
                "select count(*) from member where age >= 20 && age <= 29", "20~29",
                "select count(*) from member where age >= 30 && age <= 39", "30~39",
                "select count(*) from member where age >= 40 && age <= 49", "40~49",
                "select count(*) from member where age >= 50 && age <= 59", "50~59",
                "select count(*) from member where age >= 60 && age <= 69", "60~69",
                "select count(*) from member where age >= 70 && age <= 79", "70~79",
                "select count(*) from member where age >= 80 && age <= 89", "80~89",
                "select count(*) from member where age >= 90 && age <= 99", "90~99",
            };

            GetDataAndDrawGraph(canvas, delay, cmds);
        }

        void DrawMemberExpiredGraph(Canvas canvas, int delay)
        {
            string[] cmds = 
            {
                "select count(*) from member where to_days(now()) - to_days(id) > active * 365", "expired",
                "select count(*) from member where to_days(now()) - to_days(id) < active * 365", "unexpired"
            };

            GetDataAndDrawGraph(canvas, delay, cmds);
        }

        MySqlDataReader GetDataReader(string cmd)
        {
            MySqlCommand sqlcmd = new MySqlCommand(cmd, conn);
            MySqlDataReader reader = sqlcmd.ExecuteReader();

            return reader;
        }

        void DrawMemberAddressGraph(Canvas canvas, int delay)
        {
            MySqlDataReader reader = GetDataReader("select address from member group by address");
            LinkedList<string> cmds = new LinkedList<string>();

            while (reader.Read())
            {
                cmds.AddLast("select count(*) from member where address='" + reader[0].ToString() + "'");
                cmds.AddLast(reader[0].ToString());
            }
            reader.Close();

            GetDataAndDrawGraph(canvas, delay, cmds.ToArray<string>());
        }

        void DrawSalesBuyGraph(Canvas canvas, int delay)
        {
            MySqlDataReader reader = GetDataReader("select * from commodity");
            LinkedList<string> cmds = new LinkedList<string>();

            while (reader.Read())
            {
                cmds.AddLast("select sum(number) from sale where type='1' && cid='" + reader["id"].ToString() + "'");
                cmds.AddLast(reader["name"].ToString());
            }
            reader.Close();

            GetDataAndDrawGraph(canvas, delay, cmds.ToArray<string>());
        }

        void DrawSalesReserveGraph(Canvas canvas, int delay)
        {
            MySqlDataReader reader = GetDataReader("select * from commodity");
            LinkedList<string> cmds = new LinkedList<string>();

            while (reader.Read())
            {
                cmds.AddLast("select sum(number) from sale where type='2' && cid='" + reader["id"].ToString() + "'");
                cmds.AddLast(reader["name"].ToString());
            }
            reader.Close();

            GetDataAndDrawGraph(canvas, delay, cmds.ToArray<string>());
        }

        void DrawSalesTotalGraph(Canvas canvas, int delay)
        {
            MySqlDataReader reader = GetDataReader("select * from commodity");
            LinkedList<string> cmds = new LinkedList<string>();

            while (reader.Read())
            {
                cmds.AddLast("select sum(number) from sale where cid='" + reader["id"].ToString() + "'");
                cmds.AddLast(reader["name"].ToString());
            }
            reader.Close();

            GetDataAndDrawGraph(canvas, delay, cmds.ToArray<string>());
        }

        private void statisticsBtn_Click(object sender, RoutedEventArgs e)
        {
            ResetMenuButtonBackground();
            ((Button)sender).Background = Brushes.DodgerBlue;

            Grid oldPanel = GetOldPanel();

            if (oldPanel == statPanel)
                return;

            TransSubMainPanel(oldPanel, statPanel);

            memberGraphComboBox.SelectedIndex = 3;
            DrawMemberGenderGraph(memberCanvas, 600);
            saleGraphComboBox.SelectedIndex = 0;
            DrawSalesBuyGraph(saleCanvas, 600);
        }

        private void filterComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((ComboBoxItem)filterComboBox.SelectedItem).Content.ToString())
            {
                case "Expired":
                    InitDataGrid(ref memberData, "select * from member where to_days(now()) - to_days(id) > active * 365", memberDataGrid);
                    break;
                case "All":
                    InitDataGrid(ref memberData, "select * from member", memberDataGrid);
                    break;
            }
        }

        private void addBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void editBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        private void deleteBtn_Click(object sender, RoutedEventArgs e)
        {         
            
        }

        private void payBtn_Click(object sender, RoutedEventArgs e)
        {

        }

        enum SaleType
        {
            Buy = 1,
            Reserve = 2,
        }
        void OperateBuyOrReserve(SaleType type)
        {
            int stock = Convert.ToInt32(stockLabel.Content);
            int number = Convert.ToInt32(numberTextBox.Text);

            if (stock == 0)
                return;

            if (number > stock)
            {
                ColorAnimation anima = new ColorAnimation();
                anima.From = Colors.White;
                anima.To = Colors.Red;
                anima.Duration = new Duration(TimeSpan.FromMilliseconds(200));
                anima.AutoReverse = true;
                anima.RepeatBehavior = new RepeatBehavior(TimeSpan.FromMilliseconds(1200));
                Storyboard storyBoard = new Storyboard();
                storyBoard.Duration = new Duration(TimeSpan.FromMilliseconds(1200));
                storyBoard.Children.Add(anima);
                Storyboard.SetTarget(anima, numberTextBox);
                Storyboard.SetTargetProperty(anima, new PropertyPath("Background.Color"));
                storyBoard.Begin();

                return;
            }
            else
            {
                int newStock = stock - number;
                stockLabel.Content = newStock;
                stockLabel.Tag = newStock;
                string cmd = "update commodity set stock='" + newStock +
                    "' where id='" + nameLabel.Tag.ToString() + "'";

                MySqlCommand sqlcmd = new MySqlCommand(cmd, conn);
                sqlcmd.ExecuteNonQuery();

                string mid = ((ComboBoxItem)midComboBox.SelectedItem).Content.ToString();
                if (mid == "Not VIP")
                    mid = "0000-00-00 00:00:00";
                cmd = "insert into sale values('" + DateTime.Now + "', '" + mid + "', '" +
                    nameLabel.Tag.ToString() + "', '" + number + "', '" + (int)type + "')";

                sqlcmd = new MySqlCommand(cmd, conn);
                sqlcmd.ExecuteNonQuery();
            }
        }

        private void buyButton_Click(object sender, RoutedEventArgs e)
        {
            OperateBuyOrReserve(SaleType.Buy);
        }

        private void reserve_Click(object sender, RoutedEventArgs e)
        {
            OperateBuyOrReserve(SaleType.Reserve);
        }

        private void updateBtn_Click(object sender, RoutedEventArgs e)
        {
            progressBar.Visibility = Visibility.Visible;

            try
            {
                DataTable changes = memberData.GetChanges();
                if (changes == null)
                    return;
                da.Update(changes);
                memberData.AcceptChanges();
            }
            catch (System.Exception ex)
            {
                MessageBox.Show(this, "Error: " + ex.Message, "Update Database", MessageBoxButton.OK);
            }
        }

        private void progressBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (e.NewValue == 100)
                progressBar.Visibility = Visibility.Hidden;
        }

        public void SetCommodityInfo(string path)
        {
            string fileName = System.IO.Path.GetFileName(path);
            bool set = false;

            MySqlCommand sqlcmd = new MySqlCommand("select * from commodity where img='" + fileName + "'", conn);
            MySqlDataReader reader = sqlcmd.ExecuteReader();

            while (reader.Read())
            {
                nameLabel.Tag = reader["id"].ToString();
                nameLabel.Content = reader[1].ToString();
                priceLabel.Content = reader["price"].ToString();
                stockLabel.Content = reader["stock"].ToString();

                buyButton.IsEnabled = true;
                reserveButton.IsEnabled = true;

                set = true;
                SetTotalPay();  
            }
            reader.Close();

            if (!set)
            {
                nameLabel.Content = "NULL";
                priceLabel.Content = "NULL";
                stockLabel.Content = "NULL";
            }

            if (!set || stockLabel.Content.ToString() == "0")
            {
                buyButton.IsEnabled = false;
                reserveButton.IsEnabled = false;
            }

            DoubleAnimation anima = new DoubleAnimation();
            anima.From = -commodityInfoPanel.ActualWidth;
            anima.To = 0;
            anima.Duration = new Duration(TimeSpan.FromMilliseconds(400));
            anima.FillBehavior = FillBehavior.HoldEnd;

            var trans = new TranslateTransform();
            commodityInfoPanel.RenderTransform = trans;
            trans.BeginAnimation(TranslateTransform.XProperty, anima);
        }

        void SetTotalPay()
        {
            try
            {
                int number = Convert.ToInt32(numberTextBox.Text);
                int price = Convert.ToInt32(priceLabel.Content);
                int rank = Convert.ToInt32(rankLabel.Content);
                totalLabel.Content = number * price * (10 - rank) / 10;
            }
            catch (System.Exception ex)
            {
            	
            }

        }

        private void midComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            rankLabel.Content = ((ComboBoxItem)midComboBox.SelectedItem).Tag;
            SetTotalPay();
        }

        private void numberTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SetTotalPay();
        }

        private void memberGraphComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((ComboBoxItem)memberGraphComboBox.SelectedItem).Content.ToString())
            {
                case "Gender":
                    DrawMemberGenderGraph(memberCanvas, 0);
                    break;
                case "Expired":
                    DrawMemberExpiredGraph(memberCanvas, 0);
                    break;
                case "Age":
                    DrawMemberAgeGraph(memberCanvas, 0);
                    break;
                case "Address":
                    DrawMemberAddressGraph(memberCanvas, 0);
                    break;
            }
        }

        private void saleGraphComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (((ComboBoxItem)saleGraphComboBox.SelectedItem).Content.ToString())
            {
                case "Buy":
                    DrawSalesBuyGraph(saleCanvas, 0);
                    break;
                case "Reserve":
                    DrawSalesReserveGraph(saleCanvas, 0);
                    break;
                case "Total":
                    DrawSalesTotalGraph(saleCanvas, 0);
                    break;
            }
        }
    }

    class NonClientRegionAPI
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MARGINS
        {
            public int cxLeftWidth;
            public int cxRightWidth;
            public int cyTopHeight;
            public int cyBottomHeight;
        };
        [DllImport("DwmApi.dll")]
        public static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref MARGINS pMarInset);
    }
}
