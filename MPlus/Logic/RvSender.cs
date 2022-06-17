using MPlus.Ref;
using MPlus.Vehicles;
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

    public partial class RvSender : RvSender_member
    {
        #region singleton RvSender
        private static volatile RvSender instance;
        private static object syncRv = new object();
        public static RvSender Init
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRv)
                    {
                        if (instance == null)
                            instance = new RvSender();
                    }
                }
                return instance;
            }
        }
        #endregion
        public RvSender()
        {
        }

        public RvSender(RvListener rvLsner, string vehicleId) : this()
        {
            RvLsner = rvLsner;
            //RvLsner.reveivedMessage += OnMessageReceiving;

            VehicleId = vehicleId;
        }

        public bool Chk_EqStatus(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit)
        {
            Logger.Inst.Write(e.vecID, CmdLogType.Rv
                , $"Src Job. job_assign. B:{e.job.BATCHID},S:{e.job.S_EQPID},D:{e.job.T_EQPID}");

            RvComm.Reset();
            RvComm.Alloc(e);

            int errordelaytime = Cfg.Data.RvErrDelayTime
                       , limit = Cfg.Data.RvMainRetrylimit
                       , count = 0;

            bool err = false;
            string fail_unit = string.Empty;
            RvComm.jobcancel_check = false;
            while (count < limit)
            {
                bool ret = IsControllerStop();
                if (ret)
                    return !ret;
                if (err)
                {
                    Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"main error cnt:({count}/{limit})");
                    Thread.Sleep(errordelaytime * 1000);
                }
                if (RvComm.jobcancel_check)
                    return false;

                if (JobType == "UNLOAD")
                {
                    if (dstUnit.goaltype == (int)EqpGoalType.REFLOW)
                    {
                        if (!CHK_EQorSTK(e, srcUnit, dstUnit, RvStatusChk.eChkStepTo, ref count, "REFLOW"))
                        {
                            fail_unit = dstUnit.GOALNAME;
                            continue;
                        }

                        if (!CHK_EQorSTK(e, srcUnit, dstUnit, RvStatusChk.eChkStepTo, ref count))
                        {
                            fail_unit = srcUnit.GOALNAME;
                            continue;
                        }
                    }
                    else
                    {
                        if (!CHK_EQorSTK(e, srcUnit, dstUnit, RvStatusChk.eChkStepFrom, ref count))
                        {
                            fail_unit = srcUnit.GOALNAME;
                            continue;
                        }
                    }
                }

                if ((JobType == "UNLOAD" && e.job.WORKTYPE != "TO" && e.job.WORKTYPE != "O") || JobType == "LOAD")
                {
                    if (dstUnit.goaltype == (int)EqpGoalType.REFLOW)
                    {
                        if (!CHK_EQorSTK(e, srcUnit, dstUnit, RvStatusChk.eChkStepFrom, ref count))
                        {
                            fail_unit = dstUnit.GOALNAME;
                            continue;
                        }
                    }
                    else
                    {
                        if (!CHK_EQorSTK(e, srcUnit, dstUnit, RvStatusChk.eChkStepTo, ref count))
                        {
                            fail_unit = dstUnit.GOALNAME;
                            continue;
                        }
                    }
                }

                if (RvComm.jobcancel_check)
                    return false;

                Thread.Sleep(500);
                return true;
            }
            e.MoveCheck_Fail_unit = fail_unit;
            return false;
        }

        public bool STKMOVECHECKRETRY(SendJobToVecArgs1 e, unit srcUnit)
        {
            string sndMsg = string.Format($"EQTRAYMOVECHECK HDR=({srcUnit.ID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={srcUnit.ID.Split('-')[0]} TRAYID=({e.job.TRAYID}) BATCHJOBID={e.job.BATCHID} MULTIJOBID= JOBTYPE=UNLOAD MRNO={e.vecID}");

            string newSubject = string.Format("{0}{1}", SndSubjct, srcUnit.ID.Split('-')[0]);
            if (!RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name))
            {
                Logger.Inst.Write(VehicleId, CmdLogType.Rv, $"STKMOVECHECKRETRY Send Fail, S=>{sndMsg}");
                return false;
            }
            return WaitRvMessage();
        }
        public bool EQTRAYJOBCOMPCHECK(SendJobToVecArgs1 e)
        {
            try
            {
                bool bret = false;
                RvComm.ResetvehicleFlag();
                string subeqpid = EQTRAYJOBCOMPCHECK_subeqpid_Check(e);

                string status = (WaitVehicleJobCompMessage(ref bret)) ? "PASS" : "FAIL";
                if (!bret)
                    return bret;

                string isComp = (Db.IsJobCompCheck(CurJob.EXECUTE_TIME, subeqpid, CurJob.BATCHID, CurJob.WORKTYPE) > 0) ? "BUSY" : "COMP";

                EQTRAYJOBCOMPCHECK_sndMsg_Send(subeqpid, status, isComp);
                return true;
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(VehicleId, CmdLogType.Rv, $"error. EQTRAYJOBCOMPCHECK. {ex.Message}\r\n{ex.StackTrace}");
                RvComm._berror = false;
                return false;
            }
        }
        public async Task<bool> SYSWIN_TempPreDownRequest(unit srcUnit, unit dstUnit, Nullable<int> downtemp)
        {
            await Task.Delay(1);
            string sndMsg = string.Empty;
            string eqp = string.Empty;
            bool bRet = true;
            for (int i = 0; i < 2; i++)
            {
                if (!SYSWIN_TempPreDownRequest_sndMsg_Create(i, srcUnit, dstUnit, downtemp, ref eqp, ref sndMsg))
                    continue;

                try
                {
                    RvComm.ResetFlag();

                    //PreTempDown은 TempDown과 마찬가지로 설비가 2대일때 동시에 데이터를 내려보내야하므로 NoWait로 변경
                    bRet = SendRvMessageNoWait(eqp, sndMsg);
                }
                catch (Exception ex)
                {
                    Logger.Inst.Write(CmdLogType.Rv, $"SYSWIN_TempPreDownRequest. Exception Occur. exit.\r\n{ex.Message}\r\n{ex.StackTrace}");
                    return false;
                }
            }
            return bRet;
        }
        internal void OnTransEnd(EventArgsTransEnd e)
        {
            RvMsg_Send(SndSubjct + e.eqpId, e.rvMsg, MethodBase.GetCurrentMethod().Name);
        }

        /// <summary>
        /// EQPRETEMPDOWNREQ 수행후에도 실제 장비 작업전까지 온도증가예상된다. 보험으로 EQTEMPDOWNREQ 다시 때린다
        /// - for 문 2회에서 조건에 해당하는 경우 메시지 보내고 return, 실제 1회
        /// </summary>
        /// <param name="e"></param>
        /// <param name="srcUnit"></param>
        /// <param name="dstUnit"></param>
        /// <returns></returns>
        public bool SYSWIN_TempDownSingleRequest(string batchId, unit srcUnit, unit dstUnit, string vecID)
        {
            Task.Delay(1);

            SYSWIN_TempDownSingleRequest_lstTempDown_Add(srcUnit, dstUnit);

            if (lstTempDown.Count() == 0)
            {
                return false;
            }

            lock (syncTempDown)
            {
                for (int i = 0; i < lstTempDown.Count(); i++)
                {
                    if (!SYSWIN_TempDownSingleRequest_sndMsg_Send(i))
                        return false;
                }
            }
            return WaitTempDownRvMessage();
        }
        private bool WaitMoveCompRvMessage(bool b = true)
        {
            if (b) RvComm.MoveCompResetFlag();

            //40분 대기 (TempDown 의 결과 40분가지도 갈 수 있다는 command)
            //40분 * 60초 * 1000 msec, 루프안에 delay 100 이 있으니 1000 mesc ==> 10
            int limit = 40 * 60 * 10, count = 0;
            while (count < limit)
            {
                if (RvComm._MoveCompbWait)
                {
                    RvComm._MoveCompbWait = false;
                    break;
                }

                if (RvComm._MoveCompbSucc)
                    return true;
                if (RvComm._MoveCompberror)
                    return false;

                bool ret = IsControllerStop();
                if (ret)
                    return !ret;
                Thread.Sleep(100);
                count++;
            }
            Logger.Inst.Write(VehicleId, CmdLogType.All, "MoveComp RV Message Time Out");
            return false;
        }
        private bool WaitRvMessage(bool b = true)
        {
            if (b) RvComm.ResetFlag();

            //40분 대기 (TempDown 의 결과 40분가지도 갈 수 있다는 command)
            //40분 * 60초 * 1000 msec, 루프안에 delay 100 이 있으니 1000 mesc ==> 10
            int limit = 40 * 60 * 10, count = 0;
            while (count < limit)
            {
                if (RvComm._bWait)
                {
                    RvComm._bWait = false;
                    break;
                }

                if (RvComm._bSucc)
                    return true;
                if (RvComm._berror)
                    return false;

                bool ret = IsControllerStop();
                if (ret)
                    return !ret;
                Thread.Sleep(100);
                count++;
            }
            Logger.Inst.Write(VehicleId, CmdLogType.All, "Unload/Load Info RV Message Time Out");
            return false;
        }
        private bool WaitTempDownRvMessage()
        {
            //40분 대기 (TempDown 의 결과 40분가지도 갈 수 있다는 command)
            //40분 * 60초 * 1000 msec, 루프안에 delay 100 이 있으니 1000 mesc ==> 10
            int limit = 40 * 60 * 10, count = 0;
            while (count < limit)
            {
                if (RvComm._TempDownbWait)
                {
                    RvComm._TempDownbWait = false;
                    count++;
                    continue;
                }

                if (RvComm._TempDownbSucc)
                {
                    RvComm._TempDownbSucc = false;
                    return true;
                }
                if (RvComm._TempDownberror)
                {
                    RvComm._TempDownberror = false;
                    return false;
                }

                bool ret = IsControllerStop();
                if (ret)
                    return !ret;
                Thread.Sleep(100);
                count++;
            }
            Logger.Inst.Write(VehicleId, CmdLogType.All, "Temp Down RV Message Time Out");
            return false;
        }
        public bool WaitReflowRvMessage(bool b = true)
        {
            if (b) RvComm.ResetReflowFlag();

            //40분 대기 (TempDown 의 결과 40분가지도 갈 수 있다는 command)
            //40분 * 60초 * 1000 msec, 루프안에 delay 100 이 있으니 1000 mesc ==> 10
            int limit = 40 * 60 * 10, count = 0;
            while (count < limit)
            {
                if (RvComm._bReflowWait || RvComm._bLoaderWait)
                {
                    RvComm._bReflowWait = false;
                    RvComm._bLoaderWait = false;
                    count++;
                    continue;
                }

                if (RvComm._bLoaderSucc && RvComm._bReflowSucc)
                    return true;
                if (RvComm._bLoadererror || RvComm._bReflowerror)
                    return false;

                bool ret = IsControllerStop();
                if (ret)
                    return !ret;

                Thread.Sleep(100);
                count++;
            }
            Logger.Inst.Write(VehicleId, CmdLogType.All, "Reflow RV Message Time Out");
            return false;
        }
        private bool WaitVehicleJobCompMessage(ref bool bret)
        {
            RvComm.jobcancel_check = false;
            int limit = 18000, count = 0;    //30분 대기
            while (count < limit)
            {
                if (RvComm._bVehicleWait)
                {
                    RvComm._bVehicleWait = false;
                    count++;
                    continue;
                }

                if (RvComm._bVehicleSucc)
                {
                    RvComm._bVehicleSucc = false;
                    bret = true;
                    return true;
                }
                if (RvComm.jobcancel_check)
                    bret = false;

                bool ret = IsControllerStop();
                if (ret)
                    return !ret;
                Thread.Sleep(100);
                count++;
            }
            Logger.Inst.Write(VehicleId, CmdLogType.All, "Vehicle Trans Complete Time Out");
            return false;
        }

        public bool HANDLER_TrayLoadJobStandby(SendJobToVecArgs1 e)
        {
            // MoveCheck시 저장하였던 Handler Loader 변수를 불러와 Message 작성
            string sndMsg = string.Empty;
            unit un = Db.Units.Where(p => p.GOALNAME == e.job.T_EQPID).SingleOrDefault();
            sndMsg = string.Format($"EQTRAYLOADJOBSTANDBY HDR=({e.job.T_EQPID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={e.job.T_EQPID.Split('-')[0]} ");
            for (int i = 0; i < RvComm.Handler_LD_peps.Count; i++)
            {
                sndMsg += string.Format($"LD{i + 1}TRAYID={RvComm.Handler_LD_TrayID[i]} ");
            }
            sndMsg += string.Format($"JOBTYPE=LOAD MRNO={e.vecID}");
            try
            {
                if (SendRvMessage(e.job.T_EQPID.Split('-')[0], sndMsg, e))
                    return true;
            }
            catch (Exception ex)
            {
                Logger.Inst.Write(CmdLogType.Rv, $"HANDLER_TrayLoadJobStandby. Exception Occur. exit.\r\n{ex.Message}\r\n{ex.StackTrace}");
            }

            return false;
        }

        private bool SendRvMessage(string eqpId, string sndMsg, SendJobToVecArgs1 e)
        {
            RvComm.ResetFlag();

            string newSubject = string.Format("{0}{1}", SndSubjct, eqpId);

            if (!RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name))
                return false;

            return WaitRvMessage();
        }
        public bool TrayMoveInfo(int goaltype, SendJobToVecArgs1 e)
        {
            (string sndMsg, string devTyp) = TrayMoveInfo_sndMsg_Create(goaltype, e);

            string newSubject = string.Format("{0}{1}", SndSubjct, e.job.S_EQPID.Split('-')[0]);

            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"SendSubject=>{newSubject}");

            return RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
        }

        public bool TrayLoadInfo(int goaltype, SendJobToVecArgs1 e)
        {
            (string sndMsg, string devTyp) = TrayLoadInfo_sndMsg_Create(goaltype, e, MethodBase.GetCurrentMethod().Name);

            string newSubject = TrayLoad_UnloadInfo_newSubject_Create(goaltype, e);

            return RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
        }
        public bool WaitTrayMoveComplete(int goaltype, SendJobToVecArgs1 e)
        {
            return WaitMoveCompRvMessage();
        }

        public bool TrayLoadComplete(SendJobToVecArgs1 e)
        {
            return WaitRvMessage();
        }
        public bool TrayUnLoadComplete(SendJobToVecArgs1 e)
        {
            return WaitRvMessage();
        }

        public bool TrayUnLoadInfo(int goaltype, SendJobToVecArgs1 e)
        {
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, "unload start");
            Thread.Sleep(5000);

            (string sndMsg, string devTyp) = TrayUnLoadInfo_sndMsg_Create(goaltype, e, MethodBase.GetCurrentMethod().Name);

            string newSubject = TrayLoad_UnloadInfo_newSubject_Create(goaltype, e);

            return RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
        }

        public bool ReflowRecipeSet(SendJobToVecArgs1 e)
        {
            string traystepid = ReflowRecipeSet_traystepid_Create(e);

            // 191016
            // RecipeSet에서도 전체 TrayID 입력
            // 전체 TrayID 판단을 위한 all_reflow 생성
            // ReflowRecipeSet_trayid_Create에서 TrayID 가공
            List<pepschedule> all_reflow = Db.Peps.Where(p => p.EXECUTE_TIME == e.job.EXECUTE_TIME && p.WORKTYPE == e.job.WORKTYPE && p.T_EQPID == e.job.T_EQPID && p.C_isChecked == 1
                                                            && p.STEPID == e.job.STEPID && p.C_dstFinishTime == null).OrderBy(p => p.T_SLOT).ToList();

            string trayid = ReflowRecipeSet_trayid_Create(all_reflow);

            string sndMsg = sndMsg = string.Format($"REFLOWRECIPESET HDR=({e.job.T_EQPID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={e.job.T_EQPID.Split('-')[0]} TRAYID={trayid} BATCHJOBID={e.job.BATCHID} MULTIJOBID= STEPID={traystepid} RUNMODE={RvComm.reflowRunMode} MRNO={e.vecID}");
            string devTyp = devTyp = "REFLOW";

            string newSubject = string.Format("{0}{1}", SndSubjct, e.job.T_EQPID.Split('-')[0]);

            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"SendSubject=>{newSubject}");

            return RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
        }
        public bool ReflowLoaderInfoSet(SendJobToVecArgs1 e)
        {
            Task.Delay(1);
            ReflowLoaderInfoSet_val RLIS_val = new ReflowLoaderInfoSet_val();

            List<pepschedule> all_reflow = Db.Peps.Where(p => p.EXECUTE_TIME == e.job.EXECUTE_TIME && p.WORKTYPE == e.job.WORKTYPE && p.T_EQPID == e.job.T_EQPID && p.C_isChecked == 1
                                                            && p.BATCHID != e.job.BATCHID && p.STEPID == e.job.STEPID && p.C_dstFinishTime == null).OrderBy(p => p.T_SLOT).ToList();

            ReflowLoaderInfoSet_Data_Create(e.job.TRAYID.Split(',').Count(), e.job, ref RLIS_val.tray01id, ref RLIS_val.qty01id, ref RLIS_val.lot01id, ref RLIS_val.tray01stepid, ref RLIS_val.au01stepid);

            if (all_reflow != null && all_reflow.Count() > 0)
            {
                ReflowLoaderInfoSet_Data_Create(all_reflow[0].TRAYID.Split(',').Count(), all_reflow[0], ref RLIS_val.tray02id, ref RLIS_val.qty02id, ref RLIS_val.lot02id, ref RLIS_val.tray02stepid
                                                , ref RLIS_val.au02stepid);
            }

            (string sndMsg, string devTyp) = ReflowLoaderInfoSet_sndMsg_Create(e, RLIS_val, all_reflow);

            string newSubject = string.Format("{0}{1}", SndSubjct, e.job.T_EQPID);

            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"SendSubject=>{newSubject}");

            if (!RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name))
                return false;

            return WaitReflowRvMessage(false);
        }
        public bool JOBCANCEL(SendJobToVecArgs1 e)
        {
            Task.Delay(1);

            string jobtype_msg = string.Empty;
            string newSubject = string.Empty;
            string eqpid = string.Empty;
            unit cancel_unit = null;
            if (e.vecID == "PROGRAM")
                e.vecID = "VEHICLE01";

            if (e.job.C_state < (int)CmdState.SRC_COMPLETE || e.job.C_state == null)
            {
                jobtype_msg = "UNLOAD";
                newSubject = string.Format("{0}{1}", SndSubjct, e.job.S_EQPID.Split('-')[0]);
                eqpid = e.job.S_EQPID;
                cancel_unit = Db.Units.Where(p => p.GOALNAME == eqpid).Single();

                if (cancel_unit.goaltype == (int)EqpGoalType.REFLOW)
                {
                    return JOBCANCEL_UnLoad_Load_Reflow(e, jobtype_msg, eqpid, cancel_unit);
                }
                else if (cancel_unit.goaltype == (int)EqpGoalType.STK)
                {
                    return JOBCANCEL_UnLoad_STK(e, jobtype_msg, eqpid, cancel_unit, newSubject);
                }
                else
                {
                    return JOBCANCEL_UnLoad_Load_another(e, jobtype_msg, eqpid, cancel_unit, newSubject);
                }
            }
            else
            {
                jobtype_msg = "LOAD";
                newSubject = string.Format("{0}{1}", SndSubjct, e.job.T_EQPID.Split('-')[0]);
                eqpid = e.job.T_EQPID.Split(',')[0];
                cancel_unit = Db.Units.Where(p => p.GOALNAME == eqpid).Single();

                if (cancel_unit.goaltype == (int)EqpGoalType.REFLOW)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        if (i == 1)
                        {
                            eqpid = e.job.T_EQPID.Split('-')[0];
                        }
                        return JOBCANCEL_UnLoad_Load_Reflow(e, jobtype_msg, eqpid, cancel_unit);
                    }
                }
                else
                {
                    return JOBCANCEL_UnLoad_Load_another(e, jobtype_msg, eqpid, cancel_unit, newSubject);
                }
            }
            return true;
        }
        /// <summary>
        /// M+ controller 의 상태가 stop 이면 true, 아니면 false
        /// </summary>
        /// <param name="form"></param>
        /// <param name="b">동일상태의 중복메시지가 발생되지 않도록 flag 처리</param>
        /// <returns></returns>
        public bool IsControllerStop()
        {
            var controller = Db.Controllers.SingleOrDefault();
            if (controller == null)
            {
                return false;
            }

            if (IsStop)
                return true;

            if (controller.C_state != (int)ControllerState.STOP)
                return false;

            return true;
        }

        public bool TrayLoadMove(int goaltype, SendJobToVecArgs1 e)
        {
            (string sndMsg, string devTyp) = TrayLoadInfo_sndMsg_Create(goaltype, e, MethodBase.GetCurrentMethod().Name);

            string newSubject = TrayLoad_UnloadInfo_newSubject_Create(goaltype, e);

            return RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
        }

        public bool TrayUnLoadMove(int goaltype, SendJobToVecArgs1 e)
        {
            (string sndMsg, string devTyp) = TrayUnLoadInfo_sndMsg_Create(goaltype, e, MethodBase.GetCurrentMethod().Name);

            string newSubject = TrayLoad_UnloadInfo_newSubject_Create(goaltype, e);

            return RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);

        }

        public bool MRSM_Send(string vecid, string alarm = "", string subalarm = "", string message_data = "")
        {
            string now_time = DateTime.Now.ToString();
            string sndMsg = string.Format($"MRSTATUSMONITORING HDR=(KDS1.LH.MRSM,LH.MPLUS,GARA,TEMP) EVENT_TIME={now_time} LINEID=DSR7F EQPID={vecid} ALARMTYPE={alarm} SUBALARMTYPE={subalarm} MESSAGE={message_data}");


            string newSubject = string.Format("{0}{1}", SndSubjct, "MRSM");

            return RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
        }

        public async void _Vsp_OnSendPreTempDown(object sender, SendPreTempDown e)
        {
            if (!string.Equals(e.vec.ID, VehicleId))
                return;

            Logger.Inst.Write(CmdLogType.Rv, $"EVENT:_Vsp_OnSendTempDown. S:{e.srcUnit},D:{e.dstUnit}");
            if (Cfg.Data.UseRv)
            {
                e.job.C_srcAssignTime = DateTime.Now;
                bool b = await RvSenderList[e.vec.ID].SYSWIN_TempPreDownRequest(e.srcUnit, e.dstUnit, e.downtemp);
                e.job.C_dstFinishTime = DateTime.Now;

                e.job.C_state = (b == true) ? (int)CmdState.PRETEMPDOWN_SUCC : (int)CmdState.PRETEMPDOWN_FAIL;

                Db.DbUpdate(TableType.PEPSCHEDULE);    // FormMonitor update				
            }
        }
    }
}
