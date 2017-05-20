using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newbe.CQP.Framework;

namespace com.Doge.GroupGame.Database
{
    public class SqliteHelper
    {
        #region Singleton

        private volatile static SqliteHelper m_Instance = null;
        private static readonly object lockHelper = new object();
        private SqliteHelper() { }
        public static SqliteHelper Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    lock (lockHelper)
                    {
                        if (m_Instance == null)
                            m_Instance = new SqliteHelper();
                    }
                }
                return m_Instance;
            }
        }

        #endregion

        private SQLiteConnection m_DbConnection;

        private string m_DbUrl = @"D://GameDb.sqlite";

        public void CreateNewDatabase(string dbName)
        {
            //SQLiteConnection.CreateFile("MyDatabase.sqlite");
            SQLiteConnection.CreateFile(dbName);
            string constr = $"Data Source={dbName};";
            m_DbConnection = new SQLiteConnection(constr);
            m_DbConnection.Open();
        }


        public void ConnectToDatabase(string dbName)
        {
            //m_dbConnection = new SQLiteConnection("Data Source=MyDatabase.sqlite;");
            //m_dbConnection.Open();
            string constr = $"Data Source={dbName};";
            m_DbConnection = new SQLiteConnection(constr);
            m_DbConnection.Open();
        }


        #region Sql查询

        public Dictionary<int, string> GetLevelsName()
        {
            Dictionary<int, string> dictionary = new Dictionary<int, string>();

            try
            {
                string sql = string.Format("Select * from Level");
                string constr = $"Data Source={m_DbUrl};";
                m_DbConnection = new SQLiteConnection(constr);
                m_DbConnection.Open();
                SQLiteCommand command = new SQLiteCommand(sql, m_DbConnection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    int level = int.Parse(reader["Level"].ToString());
                    string description = reader["Description"].ToString();
                    dictionary.Add(level, description);
                }
                m_DbConnection.Close();
            }
            catch (Exception ee)
            {
                throw ee;
            }

            return dictionary;
        }


        #endregion

    }
}
