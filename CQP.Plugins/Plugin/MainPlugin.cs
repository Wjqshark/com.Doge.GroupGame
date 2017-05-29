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

        /// <summary>
        /// 开启qq群列表
        /// </summary>
        private List<long> m_StartQQGroupList = new List<long>();
        /// <summary>
        /// 等级描述字典
        /// </summary>
        private Dictionary<int, string> m_LevelDic = new Dictionary<int, string>();

        #endregion

        #region flag

        private bool m_flag;

        System.Timers.Timer m_Timer = new System.Timers.Timer(300);

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
                    else if (msg.Contains("攻击") || msg.Contains("战斗"))
                    {
                        BattleThread(fromGroup, msg, fromQq);
                    }
                    SetBusy();
                    return 1;
                }
                //else if()
                //{

                //}
            }
            return base.ProcessGroupMessage(subType, sendTime, fromGroup, fromQq, fromAnonymous, msg, font);
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
            if (msg.StartsWith("#") && m_flag == false)
            {
                if (msg.Contains("#游戏开启") && fromQq == 719539302)
                {
                    StartGameThread(msg, fromQq);
                }
                SetBusy();
                return 1;
            }

            return base.ProcessPrivateMessage(subType, sendTime, fromQq, msg, font);
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
        /// 连接数据库
        /// </summary>
        private void ConnectDb()
        {
            SqliteHelper.Instance.ConnectToDatabase("D://GameDb.sqlite");
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
                if (!m_StartQQGroupList.Contains(qqgroup))
                {

                    BackgroundWorker newBackgroundWorker = new BackgroundWorker();
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
                    };

                    RunWorkerCompletedEventHandler GetLevelDictionaryFinished = (sender, args) =>
                    {
                        CoolQApi.SendPrivateMsg(fromqq, "级别字典加载完成！");
                        newBackgroundWorker.RunWorkerAsync();
                    };
                    GetLevelDictionary(GetLevelDictionaryFinished);
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
                    m_LevelDic = SqliteHelper.Instance.GetLevelsName();
                }
                catch (Exception ee)
                {
                    CoolQApi.AddLog(1, CoolQLogLevel.Error, "字典未加载成功。原因：" + ee.Message);
                }

            };
            //bgBackgroundWorker.RunWorkerCompleted += (sender, args) =>
            //{
            //CoolQApi.SendPrivateMsg(fromqq, "级别字典加载完成！");
            //};
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
                    if (m_LevelDic != null)
                    {
                        foreach (var level in m_LevelDic.Keys)
                        {
                            ans += string.Format("第{0}级 {1}\r\n", level, m_LevelDic[level]);
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
                        if (m_LevelDic != null && m_LevelDic.ContainsKey(level))
                        {
                            ans = m_LevelDic[level];
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
                            string ans = "玩家" + CoolQCode.At(targetqq) + "等级 ：" + m_LevelDic[player.Level] + "  状态：" + (player.State == 0 ? "正常" : "死亡");
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
                        string ans = "玩家" + CoolQCode.At(fromqq) + "等级 ：" + m_LevelDic[player.Level] + "  状态：" + (player.State == 0 ? "正常" : "死亡");
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
                            var result = GamePlaying.Battle(player1, player2,m_LevelDic);



                            string outmsg = result.BattleDescription.Replace("AAA", player1.Name);
                            outmsg = outmsg.Replace("BBB", player2.Name);
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





        #endregion



    }
}
