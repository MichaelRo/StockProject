using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace StockProject
{
    class StocksAlgo
    {
        private SubmitingProps props;
        private Action<bool> stopAction;
        List<string> StocksSigns;
        private Object counterLocker = new object();
        private int loadedStocks;
        private Dictionary<string, DataTable> outputDictionary;
        private string dataDirectoryName = "DataModel";
        private string downLoadPath = "http://ichart.yahoo.com/table.csv?s=";
        private string requestPath = "ftp://ftp.nasdaqtrader.com/symboldirectory/nasdaqlisted.txt";
        private int failedLoadedStocks;
        private int featersCount = 0;
        private ConnectionManager connectionManager;
        private string ServerDirectoryName;
        private string ServerJavaDirectoryName;
        private string ServerClassesDirectoryName;
        private string ServerStocksDirectoryName;
        private string ServerInputDir;
        private string JavaFilesFolder;

        public StocksAlgo(SubmitingProps props, ConnectionManager connectionManager) 
        {
            this.props = props;
            this.connectionManager = connectionManager;

            outputDictionary = new Dictionary<string, DataTable>();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
            ServerDirectoryName = ConfigurationManager.AppSettings["ServerDirectoryName"];
            ServerJavaDirectoryName = ConfigurationManager.AppSettings["ServerJavaDirectoryName"];
            ServerClassesDirectoryName = ConfigurationManager.AppSettings["ServerClassesDirectoryName"];
            ServerStocksDirectoryName = ConfigurationManager.AppSettings["ServerStocksDirectoryName"];
            ServerInputDir = ConfigurationManager.AppSettings["ServerInputDir"];
            JavaFilesFolder = ConfigurationManager.AppSettings["JavaFilesFolder"];
        }

        public void StartCalculat(Action<bool> stopAct)
        {
            stopAction = stopAct;
            StartCalculat();
        }

        private void StartCalculat()
        {
            initDir();
            Task<bool> StockPull = new Task<bool>(PullStocks);
            StockPull.Start();
            StockPull.ContinueWith((resut) => stopAction);
        }

        private void initDir()
        {
            if (Directory.Exists(dataDirectoryName))
                Directory.Delete(dataDirectoryName, true);

            Directory.CreateDirectory(dataDirectoryName);
        }
        

        private bool PullStocks()
        {
            StocksSigns = new List<string>();
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(requestPath);

            using (WebResponse response = request.GetResponse())
            {
                using (StreamReader responseReader = new StreamReader(response.GetResponseStream()))
                {
                    string[] allStocks = responseReader.ReadToEnd().Split('\n');

                    for (int i = 1; i < allStocks.Length; i++)
                    {
                        StocksSigns.Add(allStocks[i].Split('|')[0]);
                    }
                }
            }

            Parallel.For(0, props.GetStocksAmount(), index =>
            {
                string stockSign = StocksSigns[index];
                bool isSuccess = DownloadCurrStock(stockSign);
                if (!isSuccess)
                    MessageBox.Show("Stock " + stockSign + " failed to load");
            });

            // Wait till all files finish download
            while (props.GetStocksAmount() != loadedStocks) { Thread.Sleep(150); }

            var stocksString =  ProcessStocks();
            SendDataToServer(stocksString);
            Compile();
            GetDataFromServer();
            return Finish();
        }

        private bool DownloadCurrStock(string stockSign)
        {
            bool isSuccess = true;
            try
            {
                using (WebClient client = new WebClient())
                {
                    var address = downLoadPath + stockSign;
                    var fileName = dataDirectoryName + "\\" + stockSign + ".csv";
                    client.DownloadFile(address, fileName);
                }
            }
            catch
            {
                failedLoadedStocks++;
                isSuccess = false;
            }
            finally
            {
                lock (counterLocker)
                {
                    loadedStocks++;
                }
            }

            return isSuccess;
        }

        private string ProcessStocks()
        {
            List<int> DataIndexes = new List<int>();

            if (props.SOpen) DataIndexes.Add(1);
            if (props.SHigh) DataIndexes.Add(2);
            if (props.SLow) DataIndexes.Add(3);
            if (props.SClose) DataIndexes.Add(4);
            int nNumOfDays = props.GetStocksDays() + 1;
            featersCount = DataIndexes.Count;

            // Creat single data string for the Cludera
            StringBuilder sbToReturn = new StringBuilder();
            DataTable dtCurrStock = null;

            // Run over the stocks
            foreach (string currStockSign in StocksSigns.GetRange(0, props.GetStocksAmount()))
            {
                dtCurrStock = DataStorageCsv.LoadTable(dataDirectoryName, currStockSign + ".csv");

                if (dtCurrStock != null)
                {
                    sbToReturn.Append(currStockSign + " ");

                    // Normlize data
                    this.NormalizeTable(dtCurrStock, nNumOfDays, DataIndexes, 0, 1);

                    // Run over the rows
                    for (int nRowIndex = 1; nRowIndex < nNumOfDays; nRowIndex++)
                    {
                        // Run over the cols by the user selection
                        foreach (int nDataIndex in DataIndexes)
                        {
                            sbToReturn.Append(dtCurrStock.Rows[nRowIndex][nDataIndex] + ",");
                        }
                    }

                    sbToReturn.Remove(sbToReturn.Length - 1, 1);
                    sbToReturn.Append('\n');
                }
            }

            return (sbToReturn.ToString());
        }

        private void NormalizeTable(DataTable dtTable, int nNumOfRows, List<int> lstColsIndexes, int nMinRange, int nMaxRange)
        {
            Dictionary<int, double> dicMin = new Dictionary<int, double>();
            Dictionary<int, double> dicMax = new Dictionary<int, double>();
            double dCurr;

            // Run over each col
            foreach (int nColIndex in lstColsIndexes)
            {
                dicMin.Add(nColIndex, double.MaxValue);
                dicMax.Add(nColIndex, double.MinValue);

                // Run over each row
                for (int nRowIndex = 1; nRowIndex < nNumOfRows; nRowIndex++)
                {
                    dCurr = (double)dtTable.Rows[nRowIndex][nColIndex];

                    // Find min\max
                    if (dicMax[nColIndex] < dCurr) { dicMax[nColIndex] = dCurr; }
                    if (dicMin[nColIndex] > dCurr) { dicMin[nColIndex] = dCurr; }
                }
            }


            //Parallel.For(0, lstColsIndexes.Count, nCol => 
            for (int nCol = 0; nCol < lstColsIndexes.Count; nCol++)
            {
                //Parallel.For(1, nNumOfRows, nRowIndex =>
                for (int nRowIndex = 1; nRowIndex < nNumOfRows; nRowIndex++)
                {
                    int nColIndex = lstColsIndexes[nCol];
                    dtTable.Rows[nRowIndex][nColIndex] = (((double)dtTable.Rows[nRowIndex][nColIndex] - dicMin[nColIndex]) * (nMaxRange - nMinRange)) / (dicMax[nColIndex] - dicMin[nColIndex]);
                }//);
            }//;//);
        }


        private void SendDataToServer(string stocksData)
        {
            File.WriteAllText("data.csv", stocksData);

            connectionManager.CopyWindowsToLinux("data.csv", ServerStocksDirectoryName + "/data.csv");

            foreach(var file in Directory.GetFiles(JavaFilesFolder)) {
                var fileName = Path.GetFileName(file);
                var linuxPath = ServerJavaDirectoryName + "/" + fileName;
                connectionManager.CopyWindowsToLinux(file, linuxPath);
            }

            connectionManager.executeCommand("hadoop fs -rm -r " + ServerDirectoryName);
            connectionManager.executeCommand("hadoop fs -mkdir " + ServerInputDir);
            connectionManager.executeCommand("hadoop fs -put " + ServerStocksDirectoryName + "/* " + ServerInputDir);
        }


        private void Compile()
        {
            connectionManager.executeCommand("javac -cp /usr/lib/hadoop/*:/usr/lib/hadoop/client-0.20/* -d " + 
                ServerClassesDirectoryName + " " + ServerJavaDirectoryName + "/*.java");

            connectionManager.executeCommand("jar -cvf " + ServerDirectoryName + "/stocksRed.jar -C " + ServerClassesDirectoryName + "/ .");

            connectionManager.executeCommand("hadoop jar " + "/home/training/" + ServerDirectoryName + 
                "/stocksRed.jar solution.Main " + ServerInputDir + " " + ServerDirectoryName + "/output.txt "
                                    + props.GetStocksDays() * featersCount * 0.4 + " " + props.GetStocksClusters());

        }

        private void GetDataFromServer()
        {

            connectionManager.executeCommand("hadoop fs -get " + ServerDirectoryName + "/output.txt "  
                + ServerDirectoryName + "/output.txt");

            string output = "StocksOutput.txt";
            if (File.Exists(output))
                File.Delete(output);

            connectionManager.CopyLinuxToWindows(ServerDirectoryName + "/output.txt", output);
        }

        private bool Finish()
        {
            DataTable dtCurrStock = null;

            // Run over the stocks
            foreach (string strCurrSymbol in StocksSigns.GetRange(0, props.GetStocksAmount()))
            {
                dtCurrStock = DataStorageCsv.LoadTable(dataDirectoryName, strCurrSymbol + ".csv");

                if (dtCurrStock != null)
                {
                    outputDictionary.Add(strCurrSymbol, dtCurrStock);
                }
            }

            return true;
        }


    }
}
