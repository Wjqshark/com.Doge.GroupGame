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
        /// 名字
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 等级
        /// </summary>
        public int Level { get; set; }
        /// <summary>
        /// 状态  0 正常，1轻伤，2重伤，9修炼中
        /// </summary>
        public int State { get; set; }

        /// <summary>
        /// 活力值
        /// </summary>
        public int Energy { get; set; }



        #region 相关衍生数据


        /// <summary>
        /// HP
        /// </summary>
        public int HP
        {
            get
            {
                return Level * 10 + 40;
            }
        }
        /// <summary>
        /// 最大攻击力
        /// </summary>
        public int MaxAttack
        {
            get
            {
                return Level * 3 + 7;
            }
        }
        /// <summary>
        /// 最小攻击力
        /// </summary>
        public int MinAttack
        {
            get
            {
                return Level + 5;
            }
        }

        #endregion
    }

    public class Game
    {
        public List<Player> Players { get; set; }
        public long QQGroup { get; set; }
    }
    /// <summary>
    /// 战斗细节
    /// </summary>
    public class BattleInfo
    {
        /// <summary>
        /// 攻击方
        /// </summary>
        public Player playerA { get; set; }
        /// <summary>
        /// 受攻击方
        /// </summary>
        public Player playerB { get; set; }
        /// <summary>
        /// 攻击类型
        /// </summary>
        public int BattleType { get; set; }
        /// <summary>
        /// 伤害量
        /// </summary>
        public int Damage { get; set; }
        /// <summary>
        /// 战斗描述
        /// </summary>
        public string BattleDescription { get; set; }
    }

    /// <summary>
    /// 战斗结果
    /// </summary>
    public class BattleResult
    {
        /// <summary>
        /// 胜者
        /// </summary>
        public Player Winner { get; set; }
        /// <summary>
        /// 败者
        /// </summary>
        public Player Loser { get; set; }
        /// <summary>
        /// 战斗细节
        /// </summary>
        public List<BattleInfo> BattleInfos { get; set; }
        /// <summary>
        /// 战斗描述
        /// </summary>
        public string BattleDescription { get; set; }

    }

}
