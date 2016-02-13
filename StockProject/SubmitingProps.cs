using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockProject
{
    class SubmitingProps : INotifyPropertyChanged
    {
        private readonly int defAmount = 0;
        private readonly int defDays = 0;
        private readonly int defClust = 0;

        private String stocksAmount;
        private String stocksDays;
        private String stocksClusters;

        public String StocksAmount
        {
            get
            {
                return this.stocksAmount;
            }
            set
            {
                this.stocksAmount = (parseString(value).Item1) ? (value) : ("");
                NotifyPropertyChanged("StocksAmount");
                
            }
        }
       
        public String StocksDays
        {
            get
            {
                return this.stocksDays;
            }
            set
            {
                this.stocksDays = (parseString(value).Item1) ? (value) : ("");
                NotifyPropertyChanged("StocksDays");
                
            }
        }

        public String StocksClusters
        {
            get
            {
                return this.stocksClusters;
            }
            set
            {
                this.stocksClusters = (parseString(value).Item1) ? (value) : ("");
                NotifyPropertyChanged("StocksClusters");
            }
        }

        public int GetStocksAmount()
        {
            var amount = parseString(StocksAmount);
            return amount.Item1 ? amount.Item2 : defAmount;
        }

        public int GetStocksDays()
        {
            var days = parseString(StocksDays);
            return days.Item1 ? days.Item2 : defDays;
        }

        public int GetStocksClusters()
        {
            var clust = parseString(StocksClusters);
            return clust.Item1 ? clust.Item2 : defClust;
        }

        private Tuple<bool,int> parseString(String s)
        {
            int result;
            var isParsed = int.TryParse(s, out result);
            return new Tuple<bool, int>(isParsed, result);
        }
        
        private bool sOpen = true;
        private bool sLow = true;
        private bool sHigh = true;
        private bool sClose = true;

        public bool SOpen
        {
            get { return sOpen; }
            set
            {
                if (sOpen != value)
                {
                    sOpen = value;
                    NotifyPropertyChanged("SOpen");
                }
            }
        }

        public bool SLow
        {
            get { return sLow; }
            set
            {
                if (sLow != value)
                {
                    sLow = value;
                    NotifyPropertyChanged("SLow");
                }
            }
        }

        public bool SHigh
        {
            get { return sHigh; }
            set
            {
                if (sHigh != value)
                {
                    sHigh = value;
                    NotifyPropertyChanged("SHigh");
                }
            }
        }

        public bool SClose
        {
            get { return sClose; }
            set
            {
                if (sClose != value)
                {
                    sClose = value;
                    NotifyPropertyChanged("SClose");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            Console.WriteLine(info + " Have changed");
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }
}
