using System;
using System.Data;
using System.Data.OleDb;

namespace StockProject
{
    public static class DataStorageCsv
    {
        private static readonly string CONNECTION_STRING = 
            "Provider=Microsoft.Jet.OLEDB.4.0;Data Source={0};Extended Properties=\"Text;HDR=No;FMT=Delimited\"";

        public static DataTable LoadTable(string strFilePath, string strFileName)
        {
            DataTable dtTableToRet = new DataTable();
            OleDbConnection conConnection = null;

            try
            {
                using (conConnection = new OleDbConnection(string.Format(CONNECTION_STRING, strFilePath)))
                {
                    using (OleDbDataAdapter dtAdapter = new OleDbDataAdapter("SELECT * FROM " + strFileName, conConnection))
                    {
                        OpenConnection(conConnection);
                        dtAdapter.Fill(dtTableToRet);
                    }
                }
            }
            catch (Exception ex)
            {
                return (null);
            }
            finally
            {
                CloseConnection(conConnection);
            }

            return (dtTableToRet);
        }

        private static void OpenConnection(OleDbConnection conConnection)
        {
            if ((conConnection != null) && (conConnection.State != ConnectionState.Open))
                conConnection.Open();
        }
        private static void CloseConnection(OleDbConnection conConnection)
        {
            if ((conConnection != null) && (conConnection.State == ConnectionState.Open))
                conConnection.Close();
        }
    }
}
