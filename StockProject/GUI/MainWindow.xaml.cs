using StockProject.GUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace StockProject
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SubmitingProps Props;
        private StocksAlgo algo;

        public MainWindow()
        {
            Props = new SubmitingProps();
            InitializeComponent();
            SAmount.DataContext = Props;
            SDays.DataContext = Props;
            SClusters.DataContext = Props;
            SFeatureOpen.DataContext = Props;
            SFeatureLow.DataContext = Props;
            SFeatureHigh.DataContext = Props;
            SFeatureClose.DataContext = Props;
            initFileds();
        }

        private void buttonDefaults_Click(object sender, RoutedEventArgs e)
        {
            initFileds();
        }

        private void initFileds()
        {
            Props.StocksAmount = "100";
            Props.StocksDays = "5";
            Props.StocksClusters = "5";
            Props.SOpen = true;
            Props.SLow = true;
            Props.SHigh = true;
            Props.SClose = true;
        }

        private void Submit_Click(object sender, RoutedEventArgs e)
        {
            var connectionManager = new ConnectionManager();

            if(!connectionManager.createConnection())
            {
                MessageBox.Show("Connection to server has failed!");
                return;
            }

            connectionManager.InitServerFS();
            algo = new StocksAlgo(Props, connectionManager);
            startClac();
        }

        private void startClac()
        {
            algo.StartCalculat(stopClac);
            Reset.Visibility = Visibility.Hidden;
            Loading.Visibility = Visibility.Visible;
        }

        private void stopClac(bool isSuccess)
        {
            Loading.Visibility = Visibility.Hidden;
            Reset.Visibility = Visibility.Visible;

            if(isSuccess)
                openStocksClustringForm();
        }

        private void openStocksClustringForm()
        {
            ResultView resultView = new ResultView();
            resultView.Show();
                
               // ClustersView cv = new ClustersView(StocksDictionary, DataIndexes, (int)this.nudDays.Value, File.ReadAllLines("results.txt"));
               // cv.Show();
        }


    }
}
