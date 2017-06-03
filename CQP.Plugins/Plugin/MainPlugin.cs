using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity.Infrastructure.Interception;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newbe.CQP.Framework;
using Newbe.CQP.Framework.Extensions;
using com.Doge.GroupGame.Database;

namespace com.Doge.GroupGame.Plugin
{
    public class MainPlugin : PluginBase
    {

        #region property

        private ICoolQApi m_API;
        public override string AppId => "com.Doge.GroupGame";

        /// <summary>
        /// 是否已经加载数据库
        /// </summary>
        private bool m_IsLoadDb = false;

        /// <summary>
        /// 游戏列表
        /// </summary>
        private List<Game> m_GameList = new List<Game>();


        #endregion

        #region flag and Timer

        /// <summary>
        /// 忙碌标志
        /// </summary>
        private bool m_flag;
        /// <summary>
        /// 忙碌计数器
        /// </summary>
        private System.Timers.Timer m_Timer = new System.Timers.Timer(300);

        /// <summary>
        /// 保存数据库timer 20分钟保存一次游戏数据
        /// </summary>
        private System.Timers.Timer m_SaveGameTimer = new System.Timers.Timer(1200000);

        /// <summary>
        /// 活力timer 1分钟添加一次活力
        /// </summary>
        private System.Timers.Timer m_EnergyTimer = new System.Timers.Timer(60000);

        private int m_EnergyCount = 10;

        #endregion


        public MainPlugin(ICoolQApi coolQApi) : base(coolQApi)
        {
            m_API = coolQApi;
        }


        /// <summary>
        /// 处理群里信息 
        /// </summary>
        /// <param name="subType"></param>
        /// <param name="sendTime"></param>
        /// <param name="fromGroup"></param>
        /// <param name="fromQq"></param>
        /// <param name="fromAnonymous"></param>
        /// <param name="msg"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        public override int ProcessGroupMessage(int subType, int sendTime, long fromGroup, long fromQq, string fromAnonymous, string msg, int font)
        {
            try
            {
                if (m_GameList.Exists(t => t.QQGroup == fromGroup))
                {
                    if (msg.StartsWith("#") && m_flag == false)
                    {
                        if (msg.Contains("#查询信息") || msg.Contains("#查询我的信息"))
                        {
                            GetPlayerInfoThread(fromGroup, msg, fromQq);
                        }
                        else if (msg.Contains("#查询等级名称"))
                        {
                            AskForLevelDescriptionThread(fromQq, msg, fromGroup);
                        }
                        else if (msg.Contains("#查询活力恢复"))
                        {
                            AskForAddEnergyTimeThread(msg, fromGroup);
                        }
                        else if (msg.Contains("攻击") || msg.Contains("战斗"))
                        {
                            BattleThread(fromGroup, msg, fromQq);
                        }
                        SetBusy();
                        return 1;
                    }
                }
                return base.ProcessGroupMessage(subType, sendTime, fromGroup, fromQq, fromAnonymous, msg, font);
            }
            catch (Exception ee)
            {
                CoolQApi.AddLog(1, CoolQLogLevel.Error, "接收群消息失败。原因：" + ee.Message);
                return base.ProcessGroupMessage(subType, sendTime, fromGroup, fromQq, fromAnonymous, msg, font);
            }

        }


        /// <summary>
        /// 处理群成员增加
        /// </summary>
        /// <param name="subType"></param>
        /// <param name="sendTime"></param>
        /// <param name="fromGroup"></param>
        /// <param name="fromQq"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public override int ProcessGroupMemberIncrease(int subType, int sendTime, long fromGroup, long fromQq, long target)
        {
            UpdateQQListThread(fromGroup, fromQq);
            return base.ProcessGroupMemberIncrease(subType, sendTime, fromGroup, fromQq, target);
        }

        /// <summary>
        /// 处理私聊事件
        /// </summary>
        /// <param name="subType"></param>
        /// <param name="sendTime"></param>
        /// <param name="fromQq"></param>
        /// <param name="msg"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        public override int ProcessPrivateMessage(int subType, int sendTime, long fromQq, string msg, int font)
        {
            try
            {
                if (msg.StartsWith("#") && m_flag == false)
                {
                    if (msg.Contains("#游戏开启") && fromQq == 719539302)
                    {
                        StartGameThread(msg, fromQq);
                    }
                    else if (msg.Contains("#游戏关闭") && fromQq == 719539302)
                    {
                        StopGameThread(msg, fromQq);
                    }
                    else if (msg.Contains("#保存游戏") && fromQq == 719539302)
                    {
                        SaveGameThread(msg, fromQq);
                    }
                    SetBusy();
                    return 1;
                }
                return base.ProcessPrivateMessage(subType, sendTime, fromQq, msg, font);
            }
            catch (Exception ee)
            {
                CoolQApi.AddLog(1, CoolQLogLevel.Error, "接收私聊消息失败。原因：" + ee.Message);
                return base.ProcessPrivateMessage(subType, sendTime, fromQq, msg, font);
            }

        }


        /// <summary>
        /// 设置忙碌
        /// </summary>
        public void SetBusy()
        {
            m_flag = true;
            m_Timer.Elapsed += (sender, args) =>
            {
                m_Timer.Stop();
                m_flag = false;
            };
            m_Timer.Start();
        }




        #region CommonMethod

        /// <summary>
        /// 获取信息中@的qq列表
        /// </summary>
        /// <param name="allqqList">群里所有qq成员</param>
        /// <param name="msg">信息</param>
        /// <returns></returns>
        private List<long> CheckHasAtQQ(List<long> allqqList, string msg)
        {
            List<long> qqList = new List<long>();
            bool flag = false;

            Dictionary<long, string> QQAtDic = new Dictionary<long, string>();

            QQAtDic.Add(-1, CoolQCode.At(-1));

            foreach (var tempqq in allqqList)
            {
                QQAtDic.Add(tempqq, CoolQCode.At(tempqq));
            }

            foreach (var qqkey in QQAtDic.Keys)
            {
                if (msg.Contains(QQAtDic[qqkey]))
                {
                    qqList.Add(qqkey);
                }
            }
            return qqList;
        }

        /// <summary>
        /// 更新QQ列表进程
        /// </summary>
        /// <param name="groupqq"></param>
        private void UpdateQQListThread(long groupqq, long newqq)
        {
            object[] paramaters = new object[] { groupqq, newqq };
            Thread mainThread = new Thread(new ParameterizedThreadStart(UpdateQQList));
            mainThread.Start(paramaters);
        }

        /// <summary>
        /// 更新群里qq列表
        /// </summary>
        /// <param name="groupQQ"></param>
        private void UpdateQQList(object obj)
        {
            object[] paramaters = (object[])obj;

            long groupQQ = (long)paramaters[0];
            long newqq = (long)paramaters[1];
            var game = m_GameList.FirstOrDefault(t => t.QQGroup == groupQQ);
            if (game != null)
            {
                //List<GroupMemberInfo> memebers = CoolQApi.GetGroupMemberList(groupQQ).Model.ToList();
                //查库，如果库里有取库里数据，没有新添加角色
                var newPlayer = game.Players.FirstOrDefault(t => t.QQ == newqq);
                if (newPlayer == null)
                {
                    Player player = SqliteHelper.Instance.GetPlayerFromDb(groupQQ, newqq);
                    if (player == null)
                    {
                        player = new Player()
                        {
                            Level = 1,
                            QQ = newqq,
                            State = 0,
                        };
                    }

                    var modelsource = CoolQApi.GetGroupMemberInfoV2(groupQQ, newqq, true);
                    if (modelsource != null && modelsource.Model != null)
                    {
                        player.Name = modelsource.Model.NickName;
                    }
                    game.Players.Add(player);
                    SqliteHelper.Instance.UpdatePlayers(groupQQ, game.Players);
                }
            }



        }

        /// <summary>
        /// 开启进程
        /// </summary>
        /// <param name="method"></param>
        private void StartThead(ThreadStart method)
        {
            Thread newThread = new Thread(method);
            newThread.Start();
        }

        #endregion

        #region GameMethod

        /// <summary>
        /// 开始游戏设置进程
        /// </summary>
        /// <param name="startmsg"></param>
        /// <param name="fromqq"></param>
        private void StartGameThread(string startmsg, long fromqq)
        {
            object[] paramaters = new object[] { fromqq, startmsg };
            Thread mainThread = new Thread(new ParameterizedThreadStart(StartGame));
            mainThread.Start(paramaters);
        }

        /// <summary>
        /// 开始游戏设置
        /// </summary>
        /// <param name="obj"></param>
        private void StartGame(object obj)
        {
            object[] paramaters = (object[])obj;
            long fromqq = (long)paramaters[0];
            string startmsg = paramaters[1].ToString();


            string str = startmsg.Replace("#游戏开启", "");
            long qqgroup = 0;
            if (long.TryParse(str, out qqgroup))
            {
                string ans = "";
                if (!m_GameList.Exists(t => t.QQGroup == qqgroup))
                {
                    BackgroundWorker newBackgroundWorker = new BackgroundWorker();
                    RunWorkerCompletedEventHandler GetLevelDictionaryFinished = (sender, args) =>
                    {
                        CoolQApi.SendPrivateMsg(fromqq, "级别字典加载完成！");
                        newBackgroundWorker.RunWorkerAsync();
                    };

                    BackgroundWorker getConnectionworker = new BackgroundWorker();
                    getConnectionworker.DoWork += (sender, e) =>
                    {
                        string path = CoolQApi.GetAppDirectory();
                        string dbpath = path.Replace(@"酷Q Pro\app\com.doge.groupgame\", @"酷Q Pro\com.Doge.GroupGame\GameDb.sqlite");
                        //string dbpath = System.IO.Path.Combine(path, "com.Doge.GroupGame", "GameDb.sqlite");
                        SqliteHelper.Instance.SetDBUrl(@dbpath);
                        e.Result = dbpath;
                    };
                    getConnectionworker.RunWorkerCompleted += (sender, args) =>
                    {
                        string dbstr = args.Result.ToString();
                        CoolQApi.SendPrivateMsg(fromqq, "数据库字段匹配完毕！连接为 ：" + dbstr);
                        SaveGame(qqgroup);
                        CoolQApi.SendPrivateMsg(fromqq, "保存当前qq群游戏数据" + dbstr);
                        GetLevelDictionary(GetLevelDictionaryFinished);
                    };

                    newBackgroundWorker.DoWork += (sender, args) =>
                    {
                        try
                        {
                            //获取群成员 需要api权限160
                            List<GroupMemberInfo> memebers = CoolQApi.GetGroupMemberList(qqgroup).Model.ToList();
                            var game = m_GameList.FirstOrDefault(t => t.QQGroup == qqgroup);
                            if (game == null)
                            {
                                game = new Game() { QQGroup = qqgroup };
                                m_GameList.Add(game);
                            }
                            game.Players = SqliteHelper.Instance.GetPlayersFromDb(qqgroup);

                            List<Player> newadd = new List<Player>();
                            foreach (var member in memebers)
                            {
                                var player = game.Players.FirstOrDefault(t => t.QQ == member.Number);
                                if (player == null)
                                {
                                    player = new Player()
                                    {
                                        QQ = member.Number,
                                        Name = member.NickName,
                                        Level = 1,
                                        State = 0
                                    };
                                    newadd.Add(player);
                                    game.Players.Add(player);
                                }
                                else
                                {
                                    player.Name = member.NickName;
                                }
                            }
                            SqliteHelper.Instance.UpdatePlayers(qqgroup, newadd);

                        }
                        catch (Exception ee)
                        {
                            CoolQApi.AddLog(1, CoolQLogLevel.Error, "获取群成员玩家信息失败。原因：" + ee.Message);
                        }
                    };

                    newBackgroundWorker.RunWorkerCompleted += (sender, args) =>
                    {
                        CoolQApi.SendPrivateMsg(fromqq, "获取群成员玩家信息完成！");
                        AutoSaveGameStart();
                        CoolQApi.SendPrivateMsg(fromqq, "已开启自动保存！");
                        SetEnergyFull(qqgroup);
                        AutoAddEnergy();
                        CoolQApi.SendPrivateMsg(fromqq, "已开启自动补充活力！");
                    };






                    getConnectionworker.RunWorkerAsync();
                }
            }
        }


        /// <summary>
        /// 获取等级字典
        /// </summary>
        /// <param name="fromqq"></param>
        private void GetLevelDictionary(RunWorkerCompletedEventHandler finishHandler)
        {
            BackgroundWorker bgBackgroundWorker = new BackgroundWorker();
            bgBackgroundWorker.DoWork += (sender, args) =>
            {
                try
                {
                    GamePlaying.LevelDic = SqliteHelper.Instance.GetLevelsName();
                }
                catch (Exception ee)
                {
                    CoolQApi.AddLog(1, CoolQLogLevel.Error, "字典未加载成功。原因：" + ee.Message);
                }

            };
            if (finishHandler != null)
            {
                bgBackgroundWorker.RunWorkerCompleted += finishHandler;
            }

            bgBackgroundWorker.RunWorkerAsync();
        }

        /// <summary>
        /// 获取等级描述
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public void AskForLevelDescription(object obj)
        {
            object[] paramaters = (object[])obj;
            long fromqq = (long)paramaters[0];
            string msg = paramaters[1].ToString();
            long fromgroup = (long)paramaters[2];

            if (msg.Contains("#查询等级名称"))
            {
                string str = msg.Replace("#查询等级名称", "");
                if (string.IsNullOrWhiteSpace(str))
                {
                    string ans = "";
                    if (GamePlaying.LevelDic != null)
                    {
                        foreach (var level in GamePlaying.LevelDic.Keys)
                        {
                            ans += string.Format("第{0}级 {1}\r\n", level, GamePlaying.LevelDic[level]);
                        }
                        if (!string.IsNullOrWhiteSpace(ans))
                        {
                            CoolQApi.SendGroupMsg(fromgroup, ans);
                        }
                    }

                }
                else
                {
                    str = str.Replace("级", "");
                    str = str.Replace(".", "");
                    str = str.Replace("。", "");
                    str = str.Replace(",", "");
                    str = str.Trim();
                    int level = 0;
                    if (int.TryParse(str, out level))
                    {
                        string ans = "";
                        if (GamePlaying.LevelDic != null && GamePlaying.LevelDic.ContainsKey(level))
                        {
                            ans = GamePlaying.LevelDic[level];
                            if (!string.IsNullOrWhiteSpace(ans))
                            {
                                CoolQApi.SendGroupMsg(fromgroup, ans);
                            }
                        }
                    }
                }


            }
        }

        private void AskForLevelDescriptionThread(long fromqq, string msg, long fromgroup)
        {
            object[] paramaters = new object[] { fromqq, msg, fromgroup };
            Thread mainThread = new Thread(new ParameterizedThreadStart(AskForLevelDescription));
            mainThread.Start(paramaters);
        }

        /// <summary>
        /// 获取玩家信息
        /// </summary>
        /// <param name="obj"></param>
        public void GetPlayerInfo(object obj)
        {
            object[] paramaters = (object[])obj;
            long groupqq = (long)paramaters[0];
            string msg = paramaters[1].ToString();
            long fromqq = (long)paramaters[2];

            if (msg.Contains("#查询信息"))
            {
                var game = m_GameList.FirstOrDefault(t => t.QQGroup == groupqq);
                if (game != null)
                {
                    var qqlist = CheckHasAtQQ(game.Players.Select(t => t.QQ).ToList(), msg);
                    if (qqlist != null && qqlist.Count > 0)
                    {
                        long targetqq = qqlist[0];
                        var player = game.Players.FirstOrDefault(t => t.QQ == targetqq);
                        if (player != null)
                        {
                            string statestring = "";
                            switch (player.State)
                            {
                                case 0:
                                    statestring = "正常";
                                    break;
                                case 1:
                                    statestring = "轻伤";
                                    break;
                                case 2:
                                    statestring = "重伤";
                                    break;
                                case 9:
                                    statestring = "修炼中";
                                    break;
                            }

                            string ans = "玩家" + CoolQCode.At(targetqq)
                                + "  等级 ：[" + player.Level + "]" + GamePlaying.LevelDic[player.Level]
                                + "  状态 ： " + statestring
                                + "  活力值 ： " + player.Energy;
                            CoolQApi.SendGroupMsg(groupqq, ans);
                        }
                    }
                }
            }
            if (msg.Contains("#查询我的信息"))
            {
                var game = m_GameList.FirstOrDefault(t => t.QQGroup == groupqq);
                if (game != null)
                {
                    var player = game.Players.FirstOrDefault(t => t.QQ == fromqq);
                    if (player != null)
                    {
                        string statestring = "";
                        switch (player.State)
                        {
                            case 0:
                                statestring = "正常";
                                break;
                            case 1:
                                statestring = "轻伤";
                                break;
                            case 2:
                                statestring = "重伤";
                                break;
                            case 9:
                                statestring = "修炼中";
                                break;
                        }

                        string ans = "玩家" + CoolQCode.At(fromqq)
                            + "  等级 ：[" + player.Level + "]" + GamePlaying.LevelDic[player.Level]
                            + "  状态 ： " + statestring
                            + "  活力值 ： " + player.Energy;
                        CoolQApi.SendGroupMsg(groupqq, ans);
                    }
                }
            }
        }

        /// <summary>
        /// 获取玩家信息进程
        /// </summary>
        /// <param name="groupqq"></param>
        /// <param name="msg"></param>
        private void GetPlayerInfoThread(long groupqq, string msg, long fromqq)
        {
            object[] paramaters = new object[] { groupqq, msg, fromqq };
            Thread mainThread = new Thread(new ParameterizedThreadStart(GetPlayerInfo));
            mainThread.Start(paramaters);
        }
        /// <summary>
        /// 设置活力满,健康状态正常
        /// </summary>
        /// <param name="groupqq"></param>
        public void SetEnergyFull(long groupqq)
        {
            var game = m_GameList.FirstOrDefault(t => t.QQGroup == groupqq);
            if (game != null)
            {
                game.Players.ForEach(t => t.Energy = 3);
                game.Players.ForEach(t => t.State = 0);
            }
        }
        /// <summary>
        /// 自动补充活力
        /// </summary>
        public void AutoAddEnergy()
        {
            m_EnergyTimer.Stop();
            m_EnergyCount = 10;
            m_EnergyTimer.Elapsed += (sender, args) =>
            {
                m_EnergyCount --;
                if (m_EnergyCount == 0)
                {
                    foreach (var game in m_GameList)
                    {
                        foreach (var player in game.Players)
                        {
                            if (player.Energy < 3)
                            {
                                player.Energy++;
                            }
                        }
                    }
                    m_EnergyCount = 10;
                }

            };
            m_EnergyTimer.Start();

        }

        private void AskForAddEnergyTimeThread(string msg, long fromgroup)
        {
            object[] paramaters = new object[] {msg, fromgroup };
            Thread mainThread = new Thread(new ParameterizedThreadStart(AskForAddEnergyTime));
            mainThread.Start(paramaters);
        }

        public void AskForAddEnergyTime(object o)
        {
            object[] paramaters = (object[])o;
            string msg = paramaters[0].ToString();
            long groupqq = (long)paramaters[1];
            if (msg.Contains("#查询活力恢复"))
            {
                string ans = $"距离恢复活力还有{m_EnergyCount}分钟！";
                CoolQApi.SendGroupMsg(groupqq, ans);
            }
        }

        /// <summary>
        /// 受伤自动康复
        /// </summary>
        /// <param name="qqgroup"></param>
        /// <param name="player"></param>
        /// <param name="state"></param>
        public void AutoRemoveInjury(long qqgroup, Player player, int state)
        {
            System.Timers.Timer injurytimer = null;
            if (state == 1)
            {
                injurytimer = new System.Timers.Timer(1800000);
            }
            else if (state == 2)
            {
                injurytimer = new System.Timers.Timer(3600000);
            }
            if (injurytimer != null)
            {
                injurytimer.Elapsed += (sender, args) =>
                {
                    player.State = 0;
                    injurytimer.Stop();
                    CoolQApi.SendGroupMsg(qqgroup, $"{player.Name}的伤已经好转了！");
                };
                injurytimer.Start();
            }
        }

        /// <summary>
        /// 战斗方法
        /// </summary>
        /// <param name="obj"></param>
        public void Battle(object obj)
        {
            object[] paramaters = (object[])obj;
            long groupqq = (long)paramaters[0];
            string msg = paramaters[1].ToString();
            long fromqq = (long)paramaters[2];

            if (msg.Contains("攻击") || msg.Contains("战斗"))
            {
                var game = m_GameList.FirstOrDefault(t => t.QQGroup == groupqq);
                if (game != null)
                {
                    var qqlist = CheckHasAtQQ(game.Players.Select(t => t.QQ).ToList(), msg);
                    if (qqlist != null && qqlist.Count > 0)
                    {
                        long targetqq = qqlist[0];
                        var player1 = game.Players.FirstOrDefault(t => t.QQ == fromqq);
                        var player2 = game.Players.FirstOrDefault(t => t.QQ == targetqq);
                        if (player1 != null && player2 != null)
                        {
                            if (player1.Energy <= 0)
                            {
                                CoolQApi.SendGroupMsg(groupqq, $"{player1.Name}没有活力值了，无法采取行动！");
                                return;
                            }
                            if (player1.State == 1 || player1.State == 2)
                            {
                                CoolQApi.SendGroupMsg(groupqq, $"{player1.Name}现在受伤了，不能采取行动！");
                                return;
                            }
                            if (player2.State == 1 || player2.State == 2)
                            {
                                CoolQApi.SendGroupMsg(groupqq, $"{player2.Name}现在受伤了，不能进行攻击！");
                                return;
                            }
                            //耗费1个活力值
                            player1.Energy--;

                            //获取胜负结果
                            var battleresult = GamePlaying.Battle(player1, player2);
                            //人名替换
                            string outmsg = battleresult.BattleDescription.Replace("AAA", player1.Name);
                            outmsg = outmsg.Replace("BBB", player2.Name);

                            if (battleresult.Winner != null && battleresult.Loser != null)
                            {
                                //判断受伤
                                int state = GamePlaying.Injury(battleresult.Winner.Level, battleresult.Loser.Level);
                                if (state == 1)
                                {
                                    battleresult.Loser.State = state;
                                    AutoRemoveInjury(groupqq, battleresult.Loser, state);
                                    outmsg += $"\n{battleresult.Loser.Name}受了轻伤！好像无法战斗了！";
                                }
                                else if (state == 2)
                                {
                                    battleresult.Loser.State = state;
                                    AutoRemoveInjury(groupqq, battleresult.Loser, state);
                                    outmsg += $"\n{battleresult.Loser.Name}被打成重伤！好惨啊！";
                                }

                                //判断升级
                                int newlevel = 0;
                                if (GamePlaying.LevelUp(battleresult.Winner.Level, battleresult.Loser.Level,
                                    out newlevel))
                                {
                                    battleresult.Winner.Level = newlevel;
                                    outmsg += $"\n{battleresult.Winner.Name}升级了！，达到了[{battleresult.Winner.Level}]{GamePlaying.LevelDic[battleresult.Winner.Level]}！";
                                }

                            }
                            CoolQApi.SendGroupMsg(groupqq, outmsg);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 战斗进程
        /// </summary>
        /// <param name="groupqq"></param>
        /// <param name="msg"></param>
        /// <param name="fromqq"></param>
        private void BattleThread(long groupqq, string msg, long fromqq)
        {
            object[] paramaters = new object[] { groupqq, msg, fromqq };
            Thread mainThread = new Thread(new ParameterizedThreadStart(Battle));
            mainThread.Start(paramaters);
        }


        /// <summary>
        /// 停止游戏设置进程
        /// </summary>
        /// <param name="startmsg"></param>
        /// <param name="fromqq"></param>
        private void StopGameThread(string endmsg, long fromqq)
        {
            object[] paramaters = new object[] { fromqq, endmsg };
            Thread mainThread = new Thread(new ParameterizedThreadStart(StopGame));
            mainThread.Start(paramaters);
        }
        /// <summary>
        /// 停止游戏
        /// </summary>
        /// <param name="obj"></param>
        private void StopGame(object obj)
        {
            object[] paramaters = (object[])obj;
            long fromqq = (long)paramaters[0];
            string msg = paramaters[1].ToString();
            if (fromqq == 719539302 && msg.Contains("#游戏关闭"))
            {
                string str = msg.Replace("#游戏关闭", "");
                long qqgroup = 0;
                if (long.TryParse(str, out qqgroup))
                {
                    StopGame(qqgroup);
                }
            }

        }

        /// <summary>
        /// 停止游戏
        /// </summary>
        /// <param name="groupqq"></param>
        private void StopGame(long groupqq)
        {
            var game = m_GameList.FirstOrDefault(t => t.QQGroup == groupqq);
            m_GameList.Remove(game);
            CoolQApi.SendPrivateMsg(719539302, $"{game.QQGroup}群游戏已经停止！");
        }


        /// <summary>
        /// 保存游戏进程
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="fromqq"></param>
        private void SaveGameThread(string msg, long fromqq)
        {
            object[] paramaters = new object[] { fromqq, msg };
            Thread mainThread = new Thread(new ParameterizedThreadStart(SaveGame));
            mainThread.Start(paramaters);
        }
        /// <summary>
        /// 保存游戏
        /// </summary>
        /// <param name="obj"></param>
        private void SaveGame(object obj)
        {
            object[] paramaters = (object[])obj;
            long fromqq = (long)paramaters[0];
            string msg = paramaters[1].ToString();
            if (fromqq == 719539302 && msg.Contains("#保存游戏"))
            {
                string str = msg.Replace("#保存游戏", "");
                long qqgroup = 0;
                if (long.TryParse(str, out qqgroup))
                {
                    SaveGame(qqgroup);
                }
            }

        }


        /// <summary>
        /// 保存游戏
        /// </summary>
        /// <param name="groupqq"></param>
        private void SaveGame(long groupqq)
        {
            var game = m_GameList.FirstOrDefault(t => t.QQGroup == groupqq);
            if (game != null)
            {
                SqliteHelper.Instance.UpdatePlayers(groupqq, game.Players);
                CoolQApi.SendPrivateMsg(719539302, $"{game.QQGroup}群的数据已经保存！");
            }
        }

        /// <summary>
        /// 自动保存
        /// </summary>
        private void AutoSaveGameStart()
        {
            m_SaveGameTimer.Stop();
            m_SaveGameTimer.Elapsed += (sender, args) =>
            {
                foreach (var game in m_GameList)
                {
                    SqliteHelper.Instance.UpdatePlayers(game.QQGroup, game.Players);
                    //CoolQApi.SendPrivateMsg(719539302, $"{game.QQGroup}群的数据已经保存！");
                }
            };
            m_SaveGameTimer.Start();
        }


        #endregion



    }
}
