using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.Doge.GroupGame.Plugin
{
    /// <summary>
    /// 游戏操作类
    /// </summary>
    public static class GamePlaying
    {
        /// <summary>
        /// 随机数生成
        /// </summary>
        private static Random m_Rand = new Random();

        /// <summary>
        /// 战斗进程
        /// </summary>
        /// <param name="fap">先攻选手</param>
        /// <param name="sap">后攻选手</param>
        /// <param name="levelDictionary">等级词典</param>
        public static BattleResult Battle(Player fap, Player sap, Dictionary<int, string> levelDictionary)
        {
            BattleResult result = new BattleResult();
            //双方血量
            int fapHP = fap.HP;
            int sapHP = sap.HP;
            //轮次，最大20回合
            int round = 1;
            //先攻出手
            bool firstatk = true;

            bool secondatk = false;

            result.BattleInfos = new List<BattleInfo>();

            result.BattleDescription = $"先攻方： {fap.Name}  等级：{levelDictionary[fap.Level]} \r\n后攻方： {sap.Name} 等级：{levelDictionary[sap.Level]}\r\n";

            while (fapHP > 0 && sapHP > 0 && round < 20)
            {
                BattleInfo fight = null;
                if (firstatk)
                {
                    fight = BattleRound(fap, sap, sapHP, firstatk);
                    sapHP -= fight.Damage;

                }
                else
                {
                    fight = BattleRound(sap, fap, fapHP, firstatk);
                    fapHP -= fight.Damage;
                }
                result.BattleInfos.Add(fight);
                if (secondatk)
                {
                    result.BattleDescription += "连击！" + fight.BattleDescription + "\r\n";
                }
                else
                {
                    result.BattleDescription += fight.BattleDescription + "\r\n";
                }

                int secondatkr = m_Rand.Next(100);

                if (secondatkr >= 90)
                {
                    secondatk = true;
                }
                else
                {
                    firstatk = !firstatk;
                    secondatk = false;
                }
                round++;
            }
            if (sapHP <= 0)
            {
                result.Winner = fap;
                result.Loser = sap;
                result.BattleDescription += $"{result.Winner.Name}击败了{result.Loser.Name}";
            }
            else if (fapHP <= 0)
            {
                result.Winner = sap;
                result.Loser = fap;
                result.BattleDescription += $"{result.Winner.Name}击败了{result.Loser.Name}";
            }
            //平局
            else if (round >= 20)
            {
                result.Winner = null;
                result.Loser = null;
                result.BattleDescription += $"双方并未分出胜负";
            }
            return result;

        }
        /// <summary>
        /// 战斗回合
        /// </summary>
        /// <param name="attack">攻击方</param>
        /// <param name="attacked">受攻击方</param>
        /// <param name="attackedHP">受攻击方之前血量</param>
        /// <param name="firstatk">先攻出手</param>
        /// <returns></returns>
        private static BattleInfo BattleRound(Player attack, Player attacked, int attackedHP, bool firstatk)
        {
            BattleInfo battleInfo = new BattleInfo();
            battleInfo.playerA = attack;
            battleInfo.playerB = attacked;

            string p1 = "";
            string p2 = "";

            if (firstatk)
            {
                p1 = "AAA";
                p2 = "BBB";
            }
            else
            {
                p2 = "AAA";
                p1 = "BBB";
            }

            bool miss = false;
            bool critical = false;
            bool onehitdeath = false;

            int hitr = m_Rand.Next(100);
            // 命中率 70(基准命中率) + (攻击方等级 - 受攻击方等级)* 3
            int hitPercent = 70 + (attack.Level - attacked.Level) * 3;
            if (hitr <= hitPercent)
            {
                int criticalr = m_Rand.Next(100);
                // 暴击率 10(基准暴击率) + (攻击方等级 - 受攻击方等级)* 5
                int criticalPercent = 10 + (attack.Level - attacked.Level) * 5;
                if (criticalPercent < 10)
                {
                    // 暴击率不会低于10
                    criticalPercent = 10;
                }
                if (criticalr <= criticalPercent)
                {
                    critical = true;
                    int onedeathr = m_Rand.Next(100);
                    if (onedeathr < 3 || onedeathr >= 97)
                    {
                        onehitdeath = true;
                        battleInfo.Damage = attackedHP;
                    }
                    else
                    {
                        //暴击打出普通攻击最大值的两倍伤害
                        battleInfo.Damage = 2 * attack.MaxAttack;
                    }
                }
                //普通攻击
                else
                {
                    battleInfo.Damage = m_Rand.Next(attack.MinAttack, attack.MaxAttack + 1);
                }


            }
            else
            {
                battleInfo.Damage = 0;
                miss = true;
            }

            if (miss)
            {
                battleInfo.BattleType = 0;
                battleInfo.BattleDescription = $"{p1}向{p2}发起攻击，但是被{p2}闪开了！未造成伤害。";
                return battleInfo;
            }
            if (onehitdeath)
            {
                battleInfo.BattleType = 3;
                battleInfo.BattleDescription = $"{p1}发动致命一击！，{p2}被一拳打飞！{p2}受到{battleInfo.Damage}点伤害。";
                return battleInfo;
            }
            if (critical)
            {
                battleInfo.BattleType = 2;
                battleInfo.BattleDescription = $"{p1}打出一记暴击！{p2}受到{battleInfo.Damage}点伤害。";
                return battleInfo;
            }
            battleInfo.BattleType = 1;
            battleInfo.BattleDescription = $"{p1}向{p2}发起攻击！{p2}受到{battleInfo.Damage}点伤害。";
            return battleInfo;
        }

    }
}
