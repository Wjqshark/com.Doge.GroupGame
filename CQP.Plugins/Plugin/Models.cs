using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.Doge.GroupGame.Plugin
{
    /// <summary>
    /// 用户
    /// </summary>
    public class Player
    {
        /// <summary>
        /// QQ
        /// </summary>
        public long QQ { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// 状态
        /// </summary>
        public int State { get; set; }
        /// <summary>
        /// HP
        /// </summary>
        public int HP
        {
            get
            {
                return Level * 20 + 30;
            }
        }
    }

    /// <summary>
    /// 状态
    /// </summary>
    public enum State
    {
        /// <summary>
        /// 正常
        /// </summary>
        Normal = 0,
        /// <summary>
        /// 死
        /// </summary>
        Dead = 1
    }


    public class Game
    {
        public List<Player> Players { get; set; }
        public long QQGroup { get; set; }
    }
}
