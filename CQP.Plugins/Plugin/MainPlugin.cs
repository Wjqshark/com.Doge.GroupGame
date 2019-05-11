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
using com.Doge.Cha2.Database;

namespace com.Doge.Cha2.Plugin
{
    public class MainPlugin : PluginBase
    {

        #region property

        private ICoolQApi m_API;
        public override string AppId => "com.doge.cha2";

        /// <summary>
        /// 是否已经加载数据库
        /// </summary>
        private bool m_IsLoadDb = false;


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

        private List<string> qqgroups = new List<string>() { "641357548", "176640505" };

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

                //接收消息的群
                if (qqgroups.Contains(fromGroup.ToString()))
                {
                    if (msg.StartsWith("查"))
                    {
                        string indexstr = msg.Replace("查", "");
                        int searchfrom = 0;
                        if (int.TryParse(indexstr, out searchfrom))
                        {
                            if (searchfrom > 0)
                            {
                                Messages msgresult = Cha(searchfrom);
                                if (msgresult != null)
                                {
                                    CoolQApi.SendGroupMsg(fromGroup, $"该消息是由{CoolQCode.At(long.Parse(msgresult.Qq))}发送的：\n {msgresult.Message}");
                                }
                            }
                        }
                    }
                    SaveMessage(fromQq.ToString(), msg);

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
        /// 开启进程
        /// </summary>
        /// <param name="method"></param>
        private void StartThead(ThreadStart method)
        {
            Thread newThread = new Thread(method);
            newThread.Start();
        }

        #endregion


        #region Message

        /// <summary>
        /// 查
        /// </summary>
        /// <param name="chanum"></param>
        /// <returns></returns>
        public Messages Cha(int chanum)
        {
            Messages result = null;
            //BackgroundWorker backgroundWorker = new BackgroundWorker();
            //backgroundWorker.DoWork += (sender, e) =>
            //{
            long lastindex = SqliteHelper.Instance.GetLastMessageIndex();
            long findindex = 0;
            if (lastindex >= chanum)
            {
                findindex = lastindex - (chanum - 1);
                //e.Result = findindex;
            }
            //};

            //backgroundWorker.RunWorkerCompleted += (sender, e) =>
            //{
            result = SqliteHelper.Instance.GetMessageIndex(findindex);
            //};

            //backgroundWorker.RunWorkerAsync();


            return result;
        }


        public void SaveMessage(string qq, string msg)
        {
            Messages message = new Messages
            {
                Qq = qq,
                Message = msg,
                ReceivedTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };
            BackgroundWorker backgroundWorker = new BackgroundWorker();
            backgroundWorker.DoWork += (sender, e) =>
            {
                SqliteHelper.Instance.InsertMessage(message);
            };
            backgroundWorker.RunWorkerAsync();

        }




        #endregion



    }
}
