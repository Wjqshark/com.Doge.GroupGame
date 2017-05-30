using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.Doge.GroupGame.Plugin;
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

        /// <summary>
        /// 设置连接串
        /// </summary>
        /// <param name="str"></param>
        public void SetDBUrl(string str)
        {
            m_DbUrl = str;
        }


        #region Sql查询

        /// <summary>
        /// 获取等级名称
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// 从数据库查玩家列表
        /// </summary>
        /// <param name="groupqq"></param>
        /// <returns></returns>
        public List<Player> GetPlayersFromDb(long groupqq)
        {
            List<Player> players = new List<Player>();
            try
            {
                string sql = string.Format("SELECT * from Player WHERE [Group] = '{0}' ", groupqq);
                string constr = $"Data Source={m_DbUrl};";
                m_DbConnection = new SQLiteConnection(constr);
                m_DbConnection.Open();
                SQLiteCommand command = new SQLiteCommand(sql, m_DbConnection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    Player player = new Player();
                    player.QQ = long.Parse(reader["QQ"].ToString());
                    player.Level = int.Parse(reader["Level"].ToString());
                    player.State = int.Parse(reader["State"].ToString());
                    players.Add(player);
                }
                m_DbConnection.Close();
            }
            catch (Exception ee)
            {
                throw ee;
            }
            return players;
        }
        /// <summary>
        /// 从数据库查玩家
        /// </summary>
        /// <param name="groupqq"></param>
        /// <param name="qq"></param>
        /// <returns></returns>
        public Player GetPlayerFromDb(long groupqq, long qq)
        {
            Player player = null;
            try
            {
                string sql = $"SELECT * from Player WHERE [Group] = '{groupqq}' and QQ = '{qq}' ";
                string constr = $"Data Source={m_DbUrl};";
                m_DbConnection = new SQLiteConnection(constr);
                m_DbConnection.Open();
                SQLiteCommand command = new SQLiteCommand(sql, m_DbConnection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    player.QQ = long.Parse(reader["QQ"].ToString());
                    player.Level = int.Parse(reader["Level"].ToString());
                    player.State = int.Parse(reader["State"].ToString());
                    break;
                }
                m_DbConnection.Close();
            }
            catch (Exception ee)
            {
                throw ee;
            }
            return player;
        }
        /// <summary>
        /// 更新玩家数据入库
        /// </summary>
        /// <param name="group"></param>
        /// <param name="players"></param>
        public void UpdatePlayers(long group,List<Player> players)
        {
            try
            {
                string sql = "";
                foreach (var player in players)
                {
                    sql += $"Replace INTO Player VALUES ( '{player.QQ}' ,'{group}' ,{player.Level} , {player.State} );";
                }
                string constr = $"Data Source={m_DbUrl};";
                m_DbConnection = new SQLiteConnection(constr);
                m_DbConnection.Open();
                SQLiteCommand command = new SQLiteCommand(sql, m_DbConnection);
                command.ExecuteNonQuery();
                m_DbConnection.Close();
            }
            catch (Exception ee)
            {
                throw ee;
            }
        }

        #endregion

    }
}
