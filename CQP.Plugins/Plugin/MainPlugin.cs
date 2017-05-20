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
        /// 随机数生成
        /// </summary>
        private Random m_Rand = new Random();
        /// <summary>
        /// qq群里qq号字典
        /// </summary>
        private Dictionary<long, List<long>> m_QQdics = new Dictionary<long, List<long>>();
        /// <summary>
        /// 开启qq群列表
        /// </summary>
        private List<long> m_StartQQGroupList = new List<long>();
        /// <summary>
        /// 等级描述字典
        /// </summary>
        private Dictionary<int, string> m_LevelDic = new Dictionary<int, string>();

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
            UpdateQQListThread(fromGroup);
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
            //if (msg == "连接sqlite数据库")
            //{
            //    //ConnectDbThread();
            //    if (!m_IsLoadDb)
            //    {
            //        StartThead(ConnectDb);
            //    }
            //    m_IsLoadDb = true;
            //}
            if (msg.Contains("QQ群大战游戏开启"))
            {
                //GetLevelDictionary(fromQq);
                StartGameThread(msg,fromQq);
            }
            //if (msg == "加载等级字典")
            //{
            //    GetLevelDictionary(fromQq);
            //}
            else if(msg.Contains("查询等级名称"))
            {
                AskForLevelDescriptionThread(fromQq, msg);
            }

            return base.ProcessPrivateMessage(subType, sendTime, fromQq, msg, font);
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
        /// 开启处理群消息进程
        /// </summary>
        /// <param name="subType"></param>
        /// <param name="sendTime"></param>
        /// <param name="fromGroup"></param>
        /// <param name="fromQq"></param>
        /// <param name="fromAnonymous"></param>
        /// <param name="msg"></param>
        /// <param name="font"></param>
        private void DealWithGroupMessageThread(int subType, int sendTime, long fromGroup, long fromQq, string fromAnonymous, string msg, int font)
        {
            object[] paramaters = new object[] { subType, sendTime, fromGroup, fromQq, fromAnonymous, msg, font };
            Thread mainThread = new Thread(new ParameterizedThreadStart(DealWithGroupMessage));
            mainThread.Start(paramaters);
        }
        /// <summary>
        /// 处理群消息方法
        /// </summary>
        /// <param name="obj"></param>
        private void DealWithGroupMessage(object obj)
        {
            try
            {
                object[] paramaters = (object[])obj;

                long fromGroup = (long)paramaters[2];
                long fromQq = (long)paramaters[3];
                string msg = (string)paramaters[5];

                if (!m_QQdics.ContainsKey(fromGroup))
                {
                    //获取群成员 需要api权限160
                    List<GroupMemberInfo> memebers = CoolQApi.GetGroupMemberList(fromGroup).Model.ToList();
                    m_QQdics.Add(fromGroup, memebers.Select(t => t.Number).ToList());

                }
                List<long> qqList = CheckHasAtQQ(m_QQdics[fromGroup], msg);

                if (qqList.Count > 0)
                {

                }
            }
            catch (Exception e)
            {
                CoolQApi.AddLog(1, CoolQLogLevel.Error, e.Message);
            }

        }

        /// <summary>
        /// 更新QQ列表进程
        /// </summary>
        /// <param name="groupqq"></param>
        private void UpdateQQListThread(long groupqq)
        {
            object[] paramaters = new object[] { groupqq };
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

            //获取群成员 需要api权限160
            List<GroupMemberInfo> memebers = CoolQApi.GetGroupMemberList(groupQQ).Model.ToList();

            if (!m_QQdics.ContainsKey(groupQQ))
            {
                m_QQdics.Add(groupQQ, memebers.Select(t => t.Number).ToList());
            }
            else
            {
                m_QQdics[groupQQ] = memebers.Select(t => t.Number).ToList();
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


            string str = startmsg.Replace("QQ群大战游戏开启", "");
            str = str.Replace(" ", "");
            str = str.Replace(".", "");
            str = str.Replace("。", "");
            str = str.Replace(",", "");
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
                            if (!m_QQdics.ContainsKey(qqgroup))
                            {
                                m_QQdics.Add(qqgroup, memebers.Select(t => t.Number).ToList());
                            }
                            else
                            {
                                m_QQdics[qqgroup] = memebers.Select(t => t.Number).ToList();
                            }

                            foreach (var gameqq in m_QQdics[qqgroup])
                            {
                                CoolQApi.SetGroupSpecialTitle(qqgroup, gameqq, m_LevelDic[1], -1);
                            }

                        }
                        catch (Exception ee)
                        {
                            CoolQApi.AddLog(1, CoolQLogLevel.Error, "设置qq成员等级失败。原因：" + ee.Message);
                        }
                    };

                    newBackgroundWorker.RunWorkerCompleted += (sender, args) =>
                    {
                        CoolQApi.SendPrivateMsg(fromqq, "设置qq成员等级完成！");
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

            if (msg.Contains("查询等级名称"))
            {
                string str = msg.Replace("查询等级名称", "");
                str = str.Replace("级", "");
                str = str.Replace(".", "");
                str = str.Replace("。", "");
                str = str.Replace(",", "");

                int level = 0;
                if (int.TryParse(str, out level))
                {
                    string ans = "";
                    if (m_LevelDic != null && m_LevelDic.ContainsKey(level))
                    {
                        ans = m_LevelDic[level];
                        if (!string.IsNullOrWhiteSpace(ans))
                        {
                            CoolQApi.SendPrivateMsg(fromqq, ans);
                        }
                    }
                }
            }
        }

        private void AskForLevelDescriptionThread(long fromqq, string msg)
        {
            object[] paramaters = new object[] { fromqq, msg };
            Thread mainThread = new Thread(new ParameterizedThreadStart(AskForLevelDescription));
            mainThread.Start(paramaters);
        }




        #endregion



    }
}
