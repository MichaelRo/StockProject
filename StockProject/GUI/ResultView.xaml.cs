//using Schementi.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.DataVisualization.Charting;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace StockProject.GUI
{
    /// <summary>
    /// Interaction logic for ResultView.xaml
    /// </summary>
    public partial class ResultView : Window
    {
        Dictionary<string, DataTable> AllSymbols;
        List<int> DataIndexes;
        int NumOfDays;
        private List<TabItem> tabItems;

        public ResultView()
        {
            InitializeComponent();
            tabItems = new List<TabItem>();
        }

        public ResultView(Dictionary<string, DataTable> lstAllSymbols, List<int> lstDataIndexes, int nNumOfDays, string[] strClustersData)
        {
            InitializeComponent();
            this.NumOfDays = nNumOfDays;
            this.DataIndexes = lstDataIndexes;
            this.AllSymbols = lstAllSymbols;
            int nClustersCount = 1;
            int nNumOfStocks;
            strClustersData[0] = strClustersData[0].Remove(0, 2);

            tabControl.DataContext = tabItems;

            // Run over all clusters
            foreach (string strCurrCluster in strClustersData)
            {
                TabItem tabItem = AddTabItem(nClustersCount);
                
                //TabPage tbNewTabCluster = new TabPage("קבוצה " + nClustersCount);
                //tbNewTabCluster.AutoScroll = true;
                //tbNewTabCluster.AccessibleRole = AccessibleRole.ScrollBar;
                //tabContainer.Controls.Add(tbNewTabCluster);
                // tbNewTabCluster.Show();
                nNumOfStocks = 0;
                
                // Run over each stock
                foreach (string strCurrSymbol in strCurrCluster.Split(','))
                {
                    Chart c = new Chart();
                    c.Width = tabItem.Width;
                    c.Height = 100;
                    c.Name = strCurrSymbol;
                    c.Title = strCurrSymbol;
                    tabItem.Content = c;

                    tabItems.Add(tabItem);
                    
                    // Chart chart = new Chart();
                    // chart.Name = strCurrSymbol;
                    // chart.Titles.Add(strCurrSymbol);
                    //  tbNewTabCluster.Controls.Add(chart);
                    //    chart.Width = this.tabContainer.Width;
                    //   chart.Height = 100;
                    //   chart.Top = 100 * nNumOfStocks; ;
                    

                    // Make Chart for the current stock
                    this.MakeChart(c, AllSymbols[strCurrSymbol]);

                   // chart.Show();
                    nNumOfStocks++;
                }

                nClustersCount++;
            }

            tabControl.SelectedIndex = 0;
        }

        private void createChart(TabItem tabItem, string strCurrSymbol)
        {
          //  Sparkline chart = new Sparkline();
           // chart.Width = tabItem.Width;
           // chart.AddTimeValue();
          //  
          //  tabItem.Content = chart;

        }

        private TabItem AddTabItem (int clusterNum)
        {
            TabItem tabItem = new TabItem();
            tabItem.Header = " Cluster no " + clusterNum;
            tabItem.Name = " Cluster" + clusterNum;

            return tabItem;
        }

    //    public void MakeChart2(Chart c, DataTable dtData)
    //    {
    //        LineSeries line = new LineSeries();
     //       // c.ChartAreas.Add("1
     //       line.Points.Add(new Point());
     //       c.Series.Add(line);
    //        c.Series.Add("1");
    //        c.Series["1"].XValueType = ChartValueType.Int32;
    //        c.Series["1"].ChartType = SeriesChartType.Line;
     //       c.Series["1"].BorderWidth = 3;
     //       c.Series["1"].BorderColor = Color.FromName("Orange");
     //       c.ChartAreas["1"].AxisY.Minimum = double.MaxValue;
    //        c.ChartAreas["1"].AxisY.Enabled = AxisEnabled.False;
//
    //        double dValue;

            // Run over stock days
   //         for (int nDaysIndex = 1; nDaysIndex <= this.NumOfDays; nDaysIndex++)
   //         {
    //            dValue = 0;
                // Sample each price and make an avarage
   //             foreach (int nCurrCol in DataIndexes)
   //             {
   //                 dValue += (double)dtData.Rows[nDaysIndex][nCurrCol];
    //            }

    //            dValue /= DataIndexes.Count;
   //             c.Series["1"].Points.AddXY(nDaysIndex, dValue);
    //            if (c.ChartAreas["1"].AxisY.Minimum > dValue)
   //                 c.ChartAreas["1"].AxisY.Minimum = dValue;
   //         }
   //     }

        public void MakeChart(Chart c, DataTable dtData)
        {
            LineSeries line = new LineSeries();
            // c.ChartAreas.Add("1
            //line.Points.Add(new Point());
            

            double dValue;

            // Run over stock days
            for (int nDaysIndex = 1; nDaysIndex <= this.NumOfDays; nDaysIndex++)
            {
                dValue = 0;
                // Sample each price and make an avarage
                foreach (int nCurrCol in DataIndexes)
                {
                    dValue += (double)dtData.Rows[nDaysIndex][nCurrCol];
                }

                dValue /= DataIndexes.Count;
                line.Points.Add(new Point(nDaysIndex, dValue));
               // if (c.ChartAreas["1"].AxisY.Minimum > dValue)
               //     c.ChartAreas["1"].AxisY.Minimum = dValue;
            }

            c.Series.Add(line);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            
        }
    }
}
