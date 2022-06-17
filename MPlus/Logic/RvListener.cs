using MPlus.Ref;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TIBCO.Rendezvous;
using System.Diagnostics;
using System.Threading;
using tech_library.Tools;
using System.Text.RegularExpressions;
using System.Reflection;

namespace MPlus.Logic
{
    public class EventArgRvtate : EventArgs
    {
        public RvState state;
    }
    public class CallVehicleID : EventArgs
    {
        public string vehicleid;
    }

    public partial class RvListener : Global
    {
        #region singleton RvListener
        private static volatile RvListener instance;
        private static object syncRv = new object();
        public static RvListener Init
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRv)
                    {
                        if (instance == null)
                            instance = new RvListener();
                    }
                }
                return instance;
            }
        }
        #endregion
        public event EventHandler<EventArgRvtate> OnChangeRvConnection = delegate { };

        public delegate void ReceivedMessage(object listener, MessageReceivedEventArgs messageReceivedEventArgs);
        //public event ReceivedMessage reveivedMessage;

        TIBCO.Rendezvous.Transport transport;
        TIBCO.Rendezvous.Listener listener;
        string _service = "9100";
        string _network = ";239.20.111.50";
        //string _network = ";127.0.0.1";
        string _daemon = "9100";
        string _listenerTopics = "KDS1.LH.MPLUS";
        string _senderSubject = "KDS1.LH";
        public string SndSubjct = "KDS1.LH.";

        public TIBCO.Rendezvous.Transport Transport
        {   get { return transport; }
        }
        public string SenderSubject
        {   get { return _senderSubject; }
        }
        public RvListener()
        {
        }
        public string VehicleID = string.Empty;
        public RvListener(string vecID) : this()
        {
            VehicleID = vecID;
        }

        public async void Start()
        {
            await Run();
        }

        public async Task Run()
        {
            Logger.Inst.Write(CmdLogType.Rv, $"RVHandler 시작");

            EventArgRvtate rvState = new EventArgRvtate() { state = RvState.disconnected };
            try
            {
                TIBCO.Rendezvous.Environment.Open();

                transport = new TIBCO.Rendezvous.NetTransport(_service, _network, _daemon);
                listener = new TIBCO.Rendezvous.Listener(Queue.Default, transport, _listenerTopics, new object());
                listener.MessageReceived += OnMessageReceived;

                while (true)
                {
                    await Task.Delay(1);
                    try
                    {
                        Queue.Default.Dispatch();
                    }
                    catch (RendezvousException ex)
                    {
                        Logger.Inst.Write(CmdLogType.Rv, $"TIBCO.RendezvousException.\r\n{ex.Message}\r\n{ex.StackTrace}");

                        rvState.state = RvState.disconnected;
                        OnChangeRvConnection?.Invoke(this, rvState);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Inst.Write(CmdLogType.Rv, $"TIBCO.Exception.\r\n{ex.Message}\r\n{ex.StackTrace}");

                rvState.state = RvState.disconnected;
                OnChangeRvConnection?.Invoke(this, rvState);
            }
            finally
            {
                Logger.Inst.Write(CmdLogType.Rv, $"RVHandler 종료");

                TIBCO.Rendezvous.Environment.Close();

                rvState.state = RvState.disconnected;
                OnChangeRvConnection?.Invoke(this, rvState);
            }
        }

        public void OnMessageReceived(object listener, MessageReceivedEventArgs messageReceivedEventArgs)
        {
            if (!Cfg.Data.UseRv)
                return;

            Message message = messageReceivedEventArgs.Message;
            string msg = message.GetField("DATA").Value.ToString();

            //reveivedMessage?.Invoke(listener, messageReceivedEventArgs);
            OnMessageReceiving(listener, messageReceivedEventArgs);
        }



        public void OnMessageReceiving(object listener, MessageReceivedEventArgs messageReceivedEventArgs)
        {
            if (!Cfg.Data.UseRv)
                return;

            Message message = messageReceivedEventArgs.Message;
            string msg = message.GetField("DATA").Value.ToString();
            string[] words = msg.Split(' ');
            string vecID = string.Empty;

            if (msg.Contains("MRNO"))
            {
                vecID = UtilMgr.FindKeyStringToValue(words, "MRNO");
                if (vecID.Contains("Not found") == true)
                {
                    return;
                }
                if (vecID != "")
                {
                    vehicle vec_chk = Db.Vechicles.Where(p => p.ID == vecID).SingleOrDefault();
                    if (vec_chk != null && vec_chk.TRANSFERTYPE != "FS")
                    {
                        Logger.Inst.Write(vecID, CmdLogType.Rv, "R <= " + msg);
                        if (VehicleList[vecID].IsConnected)
                        {
                            switch (words[0])
                            {
                                case "EQTRAYMOVECHECK_REP": EQTRAYMOVECHECK_REP(msg, words, vecID); break;
                                case "EQTRAYMOVEINFO_REP": break;
                                case "EQTRAYMOVECOMP": EQTRAYMOVECOMP(msg, words, vecID); break;
                                case "EQTEMPDOWNREQ_REP": EQTEMPDOWNREQ_REP(words, vecID); break;
                                case "EQTRAYUNLOADINFO_REP": break;// EQTRAYUNLOADINFO_REP(words, vecID); break;
                                case "EQTRAYUNLOADCOMPLETE": EQTRAYUNLOADCOMPLETE(words, vecID); break;
                                case "EQTRAYLOADINFO_REP": break;// EQTRAYLOADINFO_REP(words, vecID); break;
                                case "EQTRAYLOADCOMPLETE": EQTRAYLOADCOMPLETE(msg, words, vecID); break;
                                case "EQTRAYLOADJOBSTANDBY_REP": EQTRAYLOADJOBSTANDBY_REP(msg, words, vecID); break;
                                case "REFLOWRECIPESET_REP": REFLOWRECIPESET_REP(words, vecID); break;
                                case "REFLOWLOADERINFOSET_REP": REFLOWLOADERINFOSET_REP(words, vecID); break;
                                // 작업 진행중 설비에서 AbnormalWarning이 발생하였을 때
                                case "EQTRAYABNORMALWARNING": EQTRAYABNORMALWARNING(words, vecID); break;
                                default: break;
                            }
                        }
                        else
                        {
                            Logger.Inst.Write(CmdLogType.Rv, "R <= " + msg);
                        }
                    }
                }
                return;
            }
            else
            {
                if ((words[0] == "EQTRAYMOVEREQ"))
                {
                    Logger.Inst.Write(CmdLogType.Rv, "R <= " + msg);
                    EQTRAYMOVEREQ(words, Logger.Inst.First_Vehicle);
                }
                return;
            }
        }

        /// <summary>
        /// EQTRAYMOVECHECK 의 REP 의 처리메인
        /// </summary>
        /// <param name="words"></param>
        public void EQTRAYMOVECHECK_REP(string msg, string[] words, string vecID)
        {
            unit gtype = MOVECHECK_GoalName_Unit_Check(msg, words);

            if ((int)EqpGoalType.STK == gtype.goaltype)
            {
                EQTRAYMOVECHECK_REP_STK(msg, words, vecID);
            }
            else
            if ((int)EqpGoalType.SYSWIN == gtype.goaltype || (int)EqpGoalType.SYSWIN_OVEN == gtype.goaltype || (int)EqpGoalType.SYSWIN_OVEN_t == gtype.goaltype || (int)EqpGoalType.BUFFER_STK == gtype.goaltype)
            {
                EQTRAYMOVECHECK_REP_SYSWIN(words, vecID);
            }
            else
            if ((int)EqpGoalType.HANDLER == gtype.goaltype)
            {
                EQTRAYMOVECHECK_REP_HANDLER(words, vecID);
            }
            else
            if ((int)EqpGoalType.HANDLER_STACK == gtype.goaltype)
            {
                EQTRAYMOVECHECK_REP_HANDLER_STACK(words, vecID);
            }
            else
            if ((int)EqpGoalType.REFLOW == gtype.goaltype)
            {
                EQTRAYMOVECHECK_REP_REFLOW(words, vecID);
            }
        }

        private void EQTRAYMOVECHECK_REP_STK(string msg, string[] words, string vecID)
        {
            try
            {
                if (RvSenderList[vecID].CurJob == null)
                {
                    Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYMOVECHECK_REP_STK. No work in progress to Mplus. JobCancel Please.\r\n");
                    RvSenderList[vecID].RvComm._berror = true;
                    return;
                }
                string s_eqpid = RvSenderList[vecID].CurJob.S_EQPID;
                if (string.IsNullOrEmpty(s_eqpid))
                {
                    Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYMOVECHECK_REP_STK. No work in progress to Mplus. JobCancel Please.\r\n");
                    RvSenderList[vecID].RvComm._berror = true;
                    return;
                }
                unit s_unit = Db.Units.Where(p => p.GOALNAME == s_eqpid).SingleOrDefault();
                string t_eqpid = RvSenderList[vecID].CurJob.T_EQPID.Split(',')[0];
                if (string.IsNullOrEmpty(t_eqpid))
                {
                    Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYMOVECHECK_REP_STK. No work in progress to Mplus. JobCancel Please.\r\n");
                    RvSenderList[vecID].RvComm._berror = true;
                    return;
                }
                unit d_unit = Db.Units.Where(p => p.GOALNAME == t_eqpid).SingleOrDefault();

                STK_LP_Check(RvSenderList[vecID].CurJob, words, s_unit, vecID);

                STK_Status_Check(words);

                string stkport = STK_Port_Ready_Check(RvSenderList[vecID].CurJob, words, vecID);

                string[] slotS = STK_Slot_Number_Check(words, stkport);
                if (slotS == null)
                {
                    Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYMOVECHECK_REP_STK. STK_Slot_Number_Check\r\n Message : {words}, stkport : {stkport}\r\n");
                    RvSenderList[vecID].RvComm._bWait = true;
                    return;
                }

                int traycount = Use_Tray_Check(words);

                string slot = Slot_Data(slotS, traycount, stkport);

                if (slot.Length <= 0)
                {
                    slot = Slot_Data(slotS, traycount, stkport, true);
                }

                STK_slot_port_DataSave(s_unit, d_unit, stkport, slot, vecID);

                RvSenderList[vecID].RvComm._bSucc = true;
                return;
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYMOVECHECK_REP_STK. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._berror = true;
            }
            RvSenderList[vecID].RvComm._bWait = true;
        }
        private void EQTRAYMOVECHECK_REP_SYSWIN(string[] words, string vecID)
        {
            try
            {
                SYSWIN_EQSTATUS_Check(words);

                SYSWIN_STATUS_Check(words);

                RvSenderList[vecID].RvComm._bSucc = true;
                return;
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYMOVECHECK_REP_SYSWIN. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._berror = true;
            }
            RvSenderList[vecID].RvComm._bWait = true;
        }
        private void EQTRAYMOVECHECK_REP_HANDLER(string[] words, string vecID)
        {
            try
            {
                Handler_Status_Check(words);

                Handler_TestStatus_Check(words);

                Handler_Port_Check(words);

                Handler_LoaderState_Check(words, vecID);

                RvSenderList[vecID].RvComm._bSucc = true;
                return;
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYMOVECHECK_REP_HANDLER. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._berror = true;
            }
            RvSenderList[vecID].RvComm._bWait = true;
        }
        private void EQTRAYMOVECHECK_REP_HANDLER_STACK(string[] words, string vecID)
        {
            try
            {
                // Handler의 Status 확인
                Handler_Status_Check(words);

                // Handler의 TestStatus 확인
                Handler_TestStatus_Check(words);

                // Handler의 Port 확인
                //Handler_Port_Check(words);

                // Handler Port의 상태 확인
                // Load와 Unload 시 상태 확인법이 다름
                Handler_LoaderState_Check(words, vecID);
                RvSenderList[vecID].RvComm._bSucc = true;
                return;
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYMOVECHECK_REP_HANDLER. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._berror = false;
            }
            RvSenderList[vecID].RvComm._bWait = true;
        }
        private void EQTRAYMOVECHECK_REP_REFLOW(string[] words, string vecID)
        {
            try
            {
                string reflowPort = string.Empty;
                string slot = string.Empty;
                string eqpid_chk = UtilMgr.FindKeyStringToValue(words, "EQPID");
                // 설비가 LOADER 일때
                if (eqpid_chk.Contains('-') == true)
                {
                    Reflow_Loader_Process(words, vecID);
                    return;
                }
                // 설비가 REFLOW 일때
                else if (eqpid_chk.Contains('-') == false)
                {
                    Reflow_Process(words, vecID);
                    return;
                }
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYMOVECHECK_REP_REFLOW. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._berror = false;
            }
            RvSenderList[vecID].RvComm._bWait = true;
        }
        public void EQTRAYMOVECOMP(string msg, string[] words, string vecID)
        {
            if (RvSenderList[vecID].CurJob != null)
            {
                string slot = string.Empty;
                try
                {
                    bool tray_chk = false;
                    string slotinfo = string.Empty;
                    string destport = string.Empty;
                    
                    destport = UtilMgr.FindKeyStringToValue(words, string.Format("DESTPORT"));
                    if (destport != "" && destport == RvSenderList[vecID].CurJob.S_SLOT)
                    {
                        slotinfo = UtilMgr.FindKeyStringToValue(words, string.Format("SLOTINFO"));
                        for (int i = 0; i < RvSenderList[vecID].CurJob.TRAYID.Split(',').Count(); i++)
                        {
                            if (slotinfo.Contains(RvSenderList[vecID].CurJob.TRAYID.Split(',')[i]))
                                tray_chk = true;
                            else
                            {
                                tray_chk = false;
                                break;
                            }
                        }
                        if (tray_chk) 
                        {
                            RvSenderList[vecID].RvComm._MoveCompbSucc = true;
                            return;
                        }
                        else
                        {
                            RvSenderList[vecID].RvComm._MoveCompberror = true;
                            return;
                        }
                    }
                }
                catch (UtilMgrCustomException ex)
                {
                    RvSenderList[vecID].RvComm._MoveCompberror = true;
                    Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. {ex.Message}\r\n{ex.StackTrace}");
                }
                RvSenderList[vecID].RvComm._MoveCompbWait = true;
            }
        }
        public void EQTEMPDOWNREQ_REP(string[] words, string vecID)
        {
            try
            {
                EQTEMPDOWNREQ_Status_Check(words);

                EQTEMPDOWNREQ_Send_List_Check(words, vecID);
                return;
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTEMPDOWNREQ_REP. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._TempDownberror = true;
            }
            RvSenderList[vecID].RvComm._TempDownbWait = true;

        }
        public void EQTRAYUNLOADINFO_REP(string[] words, string vecID)
        {
            try
            {
                EQTRAYLOAD_UNLOADINFO_REP_Status_Check(words, vecID);
                return;
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYUNLOADINFO_REP. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._berror = true;
            }
            RvSenderList[vecID].RvComm._bWait = true;

        }
        public void EQTRAYUNLOADCOMPLETE(string[] words, string vecID)
        {
            try
            {
                LOAD_UNLOADCOMPLETE_Reflow_Loader_Check(words, vecID);
                return;

            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYUNLOADCOMPLETE. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._berror = true;
            }
            RvSenderList[vecID].RvComm._bWait = true;

        }
        public void EQTRAYLOADINFO_REP(string[] words, string vecID)
        {
            try
            {
                EQTRAYLOAD_UNLOADINFO_REP_Status_Check(words, vecID);
                return;
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYLOADINFO_REP. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._berror = true;
            }
            RvSenderList[vecID].RvComm._bWait = true;

        }
        public void EQTRAYLOADCOMPLETE(string msg, string[] words, string vecID)
        {
            try
            {
                LOAD_UNLOADCOMPLETE_Reflow_Loader_Check(words, vecID);
                return;

            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYLOADCOMPLETE. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._berror = true;
            }
            RvSenderList[vecID].RvComm._bWait = true;
        }

        const string ERR_JOBSTANDBY_REP_HANDLER_STAUTS = "error. EQTRAYMOVECHECK_REP_HANDLER. JOBSTANDBY is not PASS";
        public void EQTRAYLOADJOBSTANDBY_REP(string msg, string[] words, string vecID)
        {
            try
            {
                if (UtilMgr.FindKeyStringToValue(words, "STATUS") == "PASS")
                {
                    RvSenderList[vecID].RvComm._bSucc = true;
                    return;
                }
                else
                {
                    RvSenderList[vecID].RvComm._bWait = true;
                    Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYMOVECHECK_REP_HANDLER. JOBSTANDBY is not PASS");
                }
            }
            catch (Exception e)
            {
            }
        }
        public void REFLOWRECIPESET_REP(string[] words, string vecID)
        {
            try
            {
                REFLOWRECIPESET_LOADERINFOSET_REP_Status_Check(words);

                REFLOWRECIPESET_LOADERINFOSET_REP_Runmode_Check(words, vecID);
                RvSenderList[vecID].RvComm._bReflowSucc = true;
                return;
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. REFLOWRECIPESET_REP. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._bReflowerror = true;
            }
            RvSenderList[vecID].RvComm._bReflowWait = true;
        }
        public void REFLOWLOADERINFOSET_REP(string[] words, string vecID)
        {
            try
            {
                REFLOWRECIPESET_LOADERINFOSET_REP_Status_Check(words);

                REFLOWRECIPESET_LOADERINFOSET_REP_Runmode_Check(words, vecID);
                RvSenderList[vecID].RvComm._bLoaderSucc = true;
                return;
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. REFLOWLOADERINFOSET_REP. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._bLoadererror = true;
            }
            RvSenderList[vecID].RvComm._bLoaderWait = true;
        }
        public void EQTRAYMOVEREQ(string[] words, string vecID)
        {
            try
            {
                EQTRAYMOVEREQ_sndMsg_Send(words, vecID);
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYMOVEREQ. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._berror = true;
            }
        }
        public void EQTRAYABNORMALWARNING(string[] words, string vecID)
        {
            try
            {
                // EQTRAYMOVEREQ는 받은 내용에 STATUS=PASS만 추가하여 REP를 보낸다. (예상)
                EQTRAYABNORMALWARNING_sndMsg_Send(words, vecID);

                // EQTRAYABNORMALWARNING Message를 받으면 REP를 대기중인 작업을 Error 처리
                // 대기중인 작업을 계속 대기하려면 아래 내용 주석
                RvSenderList[vecID].RvComm._berror = false;
                RvSenderList[vecID].RvComm._bLoaderWait = false;
                RvSenderList[vecID].RvComm._TempDownberror = false;
                RvSenderList[vecID].RvComm._MoveCompberror = false;
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vecID, CmdLogType.Rv, $"error. EQTRAYABNORMALWARNING. {ex.Message}\r\n{ex.StackTrace}");
                RvSenderList[vecID].RvComm._berror = true;
            }
        }
    }

}
