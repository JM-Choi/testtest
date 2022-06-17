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
    public partial class RvSender
    {
        // Global Start
        #region Global
        public bool RvMsg_Send(string newSubject, string sndMsg, string devTyp)
        {
            try
            {
                if (!sndMsg.Contains("JOBCANCEL"))
                {
                    bool ret = IsControllerStop();
                    if (ret)
                        return !ret;
                }
                Message message = new Message { SendSubject = newSubject };
                message.AddField("DATA", sndMsg);
                Transport.Send(message);
                Logger.Inst.Write(VehicleId, CmdLogType.Rv, $"S=>{sndMsg}");
                return true;
            }
            catch (Exception ex)
            {
                Logger.Inst.Write(VehicleId, CmdLogType.Rv, $"exception {devTyp}, {ex.Message}\r\n{ex.StackTrace}");
                return false;
            }
        }

        #endregion
        //Global End

        // Chk_EqStatus Method Start
        #region Chk_EqStatus
        const string ERR_MOVECHK_REP_STK_STATUS = "error. EQTRAYMOVECHECK_REP_STK. STKSTATUS is not RUN and IDLE.";
        const string ERR_MOVECHK_REP_STK_AUTOPORT = "error. EQTRAYMOVECHECK_REP_STK. PORT is all not IDLE.";
        const string ERR_MOVECHK_REP_STK_SLOTINFO = "error. EQTRAYMOVECHECK_REP_STK. SLOTINFO is not Valid.";
        const string ERR_MOVECHK_REP_STK_TRAYINFO = "error. EQTRAYMOVECHECK_REP_STK. TRAYINFO is not Valid.";
        const string ERR_MOVECHK_REP_STK_INVALID_EQPTYPE = "error. EQTRAYMOVECHECK_REP_STK. Check EqpId";
        private bool CHK_EQorSTK(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit, RvStatusChk rvstatusval, ref int count, string val = null)
        {
            RvComm.ResetFlag();

            unit unt = null;
            bool err = CHK_EQorSTK_Sts(e, srcUnit, dstUnit, rvstatusval, val);
            if (!err)
            {
                if (JobType == "UNLOAD")
                    unt = srcUnit;
                else
                    unt = dstUnit;

                Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"{JobType}:{unt}. Status ERROR - {unt}");
                count++;
                return err;
            }

            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"{JobType}:{unt}. Status OK - To {unt}");

            return err;
        }
        private bool CHK_EQorSTK_Sts(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit, RvStatusChk eDirect, string type = null)
        {
            ResetFlag(srcUnit, dstUnit, eDirect);
            int count = 0, noresponse_time = 0;
            bool bSend = false, bNoResp = false;

            pepschedule send = e.job;
            string eqpid = string.Empty;
            DateTime dStart = DateTime.Now;

            while (count < Cfg.Data.RvSubRetrylimit)
            {
                bool ret = IsControllerStop();
                if (ret)
                    return !ret;

                if (RvComm._bWait)
                {
                    CHK_EQorSTK_Sts_rv_wait(e, ref count, ref bNoResp, ref bSend, ref dStart, ref noresponse_time);
                    continue;
                }

                if (!bSend)
                {
                    if (eDirect == RvStatusChk.eChkStepTo)
                    {
                        if (!SendLOADData(e, eqpid, srcUnit, eDirect, type, dStart, ref bSend, count, send))
                            return false;
                    }
                    else
                    {
                        if (!SendUNLOADData(e, srcUnit, dstUnit, eDirect, type, dStart, ref bSend, count))
                            return false;
                    }
                }

                if (RvComm._bSucc)
                {
                    Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"CHK_EQorSTK_Sts. {RvComm._bSucc}");
                    RvComm._bSucc = false;
                    return true;
                }

                if (RvComm.jobcancel_check)
                    return false;

                if (noresponse_time == Cfg.Data.RvErrDelayTime * 10)
                {
                    RvComm._bWait = true;
                    bNoResp = true;
                }
                noresponse_time++;
                Thread.Sleep(100);
            }

            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"CHK_EQorSTK_Sts. limit over");
            return false;
        }
        private void ResetFlag(unit srcUnit, unit dstUnit, RvStatusChk eDirect)
        {
            if (eDirect == RvStatusChk.eChkStepFrom)
            {
                GoalType = (EqpGoalType)srcUnit.goaltype;
            }
            else
            {
                GoalType = (EqpGoalType)dstUnit.goaltype;
            }
            RvComm.ResetFlag();
        }
        private void CHK_EQorSTK_Sts_rv_wait(SendJobToVecArgs1 e, ref int count, ref bool bNoResp, ref bool bSend, ref DateTime dStart, ref int noresponse_time)
        {
            count++;
            if (bNoResp)
            {
                bNoResp = false;
                Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"NoResponse, Retry({count}/{Cfg.Data.RvSubRetrylimit})");
            }
            else
            {
                Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"ResponseErr, Retry({count}/{Cfg.Data.RvSubRetrylimit})");
                Thread.Sleep(Cfg.Data.RvErrDelayTime * 1000);
            }


            // unset wait 
            RvComm.ResetFlag();
            bSend = false;
            dStart = DateTime.Now;
            noresponse_time = 0;
        }
        private bool SendLOADData(SendJobToVecArgs1 e, string eqpid, unit srcUnit, RvStatusChk eDirect, string type, DateTime dStart, ref bool bSend, int count, pepschedule send)
        {
            string[] eqpid_count = e.job.T_EQPID.Split(',');

            for (int i = 0; i < eqpid_count.Count(); i++)
            {
                if (i == 0)
                    eqpid = eqpid_count[i];
                unit split_dstUnit = Db.Units.Where(p => p.GOALNAME == eqpid_count[i]).Single();
                string eqp = string.Empty;
                if (eqpid_count.Count() > 1)
                {
                    e.job = Db.Peps.Where(p => p.EXECUTE_TIME == e.job.EXECUTE_TIME && p.T_EQPID == eqpid_count[i]
                                            && p.MULTIID == e.job.MULTIID).FirstOrDefault();
                }
                string sndMsg = SndMsg_Decision(e, srcUnit, split_dstUnit, eDirect, out eqp, type);
                try
                {
                    string newSubject = string.Format("{0}{1}", SndSubjct, eqp);

                    if (!RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name))
                        return false;

                    dStart = DateTime.Now;
                    bSend = true;
                    Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"({count}/{Cfg.Data.RvSubRetrylimit},{newSubject})");
                }
                catch (RendezvousException exception)
                {
                    Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"CHK_EQorSTK_Sts. RendezvousException Occur. retry exit.\r\n{exception.Message}\r\n{exception.StackTrace}");
                    return false;
                }
                catch (Exception ex)
                {
                    Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"CHK_EQorSTK_Sts. Exception Occur. retry exit.\r\n{ex.Message}\r\n{ex.StackTrace}");
                    return false;
                }

                if (i == eqpid_count.Count() - 1)
                {
                    break;
                }

                while (!RvComm._bSucc)
                {
                }
                if (i != eqpid_count.Count() - 1)
                    RvComm._bSucc = false;
            }

            e.job = send;
            e.job.T_EQPID = eqpid;
            return true;
        }
        private bool SendUNLOADData(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit, RvStatusChk eDirect, string type, DateTime dStart, ref bool bSend, int count)
        {
            string eqp = string.Empty;
            string sndMsg = SndMsg_Decision(e, srcUnit, dstUnit, eDirect, out eqp, type);
            try
            {
                string newSubject = string.Format("{0}{1}", SndSubjct, eqp);

                if (!RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name))
                    return false;

                dStart = DateTime.Now;
                bSend = true;
                Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"({count}/{Cfg.Data.RvSubRetrylimit},{newSubject})");
                return true;
            }
            catch (RendezvousException exception)
            {
                Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"CHK_EQorSTK_Sts. RendezvousException Occur. retry exit.\r\n{exception.Message}\r\n{exception.StackTrace}");
                return false;
            }
            catch (Exception ex)
            {
                Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"CHK_EQorSTK_Sts. Exception Occur. retry exit.\r\n{ex.Message}\r\n{ex.StackTrace}");
                return false;
            }
        }
        private string SndMsg_Decision(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit, RvStatusChk eDirect, out string eqpId, string type = null)
        {
            unit sendunit = null;

            sendunit = (eDirect == RvStatusChk.eChkStepFrom) ? srcUnit : dstUnit;

            eqpId = SndMsg_Decision_eqpId_Create(sendunit, type);
            return SndMsg_Decision_sndMsg_Create(e, eDirect, sendunit, type);
        }
        private string SndMsg_Decision_eqpId_Create(unit sendunit, string type)
        {
            string eqp = string.Empty;
            if ((EqpGoalType)sendunit.goaltype == EqpGoalType.REFLOW)
            {
                if (type != "REFLOW")
                    eqp = sendunit.ID;
                else
                    eqp = sendunit.ID.Split('-')[0];
            }
            else
            {
                eqp = sendunit.ID.Split('-')[0];
            }
            return eqp;
        }
        private string SndMsg_Decision_sndMsg_Create(SendJobToVecArgs1 e, RvStatusChk eDirect, unit sendunit, string type)
        {
            string sndMsg = string.Empty;
            string jobtype = string.Empty;

            jobtype = (eDirect == RvStatusChk.eChkStepFrom) ? "UNLOAD" : "LOAD";

            if ((EqpGoalType)sendunit.goaltype == EqpGoalType.STK)
            {
                sndMsg = string.Format($"EQTRAYMOVECHECK HDR=({sendunit.ID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={sendunit.ID.Split('-')[0]} TRAYID=({e.job.TRAYID}) BATCHJOBID={e.job.BATCHID} MULTIJOBID= JOBTYPE={jobtype} MRNO={e.vecID}");
            }
            else if ((EqpGoalType)sendunit.goaltype == EqpGoalType.SYSWIN || (EqpGoalType)sendunit.goaltype == EqpGoalType.SYSWIN_OVEN || (EqpGoalType)sendunit.goaltype == EqpGoalType.SYSWIN_OVEN_t || (EqpGoalType)sendunit.goaltype == EqpGoalType.BUFFER_STK)
            {
                sndMsg = string.Format($"EQTRAYMOVECHECK HDR=({sendunit.ID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={sendunit.ID.Split('-')[0]} SUBEQPID={sendunit.ID} TRAYID=({e.job.TRAYID}) JOBTYPE={jobtype} STEPID={e.job.T_STEPID.Split(',')[0]} MRNO={e.vecID}");
            }
            else if ((EqpGoalType)sendunit.goaltype == EqpGoalType.HANDLER || (EqpGoalType)sendunit.goaltype == EqpGoalType.HANDLER_STACK)
            {
                // Handler 관련 Loader Port에 대한 변수 초기화
                RvComm.Handler_LD_Reset();
                string LD_trayid = string.Empty;
                string slotName = string.Empty;
                string trayID = string.Empty;
                string EQPID = string.Empty;
                // RV Message의 공통된 Head
                sndMsg = string.Format($"EQTRAYMOVECHECK HDR=({sendunit.ID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={sendunit.ID.Split('-')[0]} ");

                if (jobtype == "UNLOAD")
                {
                    EQPID = e.job.S_EQPID;
                }
                else
                {
                    EQPID = e.job.T_EQPID;
                }
                // 현재 Job이 Multi Job이 아닌 경우 
                if (e.job.MULTIID == "" || e.job.MULTIID == null)
                {
                    LD_trayid = e.job.TRAYID;
                    var selEQPID = Db.Units.Where(p => p.ID == EQPID).SingleOrDefault();
                    for (int i = 0; i < selEQPID.max_col; i++)
                    {
                        slotName = string.Format($"LOADER{i + 1}");
                        trayID = string.Format($"LD{i + 1}TRAYID");
                        if ((jobtype == "UNLOAD" && e.job.S_SLOT == slotName) || (jobtype == "LOAD" && e.job.T_SLOT == slotName) ||
                            (i == 2 && (e.job.S_SLOT.Contains("GOOD") || e.job.T_SLOT.Contains("GOOD"))) || (i == 3 && (e.job.S_SLOT.Contains("FAIL") || e.job.T_SLOT.Contains("FAIL"))))
                        {
                            RvComm.Handler_LD_peps.Add(e.job);
                            RvComm.Handler_LD_TrayID.Add(LD_trayid);
                            RvComm.Handler_LD_StepID.Add(e.job.S_STEPID);
                            RvComm.Handler_LD_LotID.Add(e.job.LOT_NO);
                        }
                        else
                        {
                            RvComm.Handler_LD_peps.Add(null);
                            RvComm.Handler_LD_TrayID.Add("");
                            RvComm.Handler_LD_StepID.Add("");
                            RvComm.Handler_LD_LotID.Add("");
                        }
                        sndMsg += string.Format($"{trayID}={RvComm.Handler_LD_TrayID[i]} ");
                    }
                }
                else
                {
                    var selEQPID = Db.Units.Where(p => p.ID == EQPID).SingleOrDefault();
                    for (int i = 0; i < selEQPID.max_col; i++)
                    {
                        pepschedule Handler_peps = new pepschedule();
                        slotName = string.Format($"LOADER{i + 1}");
                        trayID = string.Format($"LD{i + 1}TRAYID");

                        if (jobtype == "UNLOAD")
                            Handler_peps = Db.Peps.Where(p => p.MULTIID == e.job.MULTIID && p.S_SLOT == slotName).SingleOrDefault();
                        else if (jobtype == "LOAD")
                            Handler_peps = Db.Peps.Where(p => p.MULTIID == e.job.MULTIID && p.T_SLOT == slotName).SingleOrDefault();
                        else
                            Handler_peps = null;

                        if (Handler_peps != null)
                        {
                            RvComm.Handler_LD_peps.Add(Handler_peps);
                            RvComm.Handler_LD_LotID.Add(Handler_peps.LOT_NO);
                            RvComm.Handler_LD_TrayID.Add(Handler_peps.TRAYID);
                            RvComm.Handler_LD_StepID.Add(Handler_peps.S_STEPID);
                        }
                        else
                        {
                            RvComm.Handler_LD_peps.Add(null);
                            RvComm.Handler_LD_TrayID.Add("");
                            RvComm.Handler_LD_StepID.Add("");
                            RvComm.Handler_LD_LotID.Add("");
                        }


                        sndMsg += string.Format($"{trayID}={RvComm.Handler_LD_TrayID[i]} ");
                    }
                }
                sndMsg += string.Format($"JOBTYPE={jobtype} MRNO={e.vecID}");
            }
            else if ((EqpGoalType)sendunit.goaltype == EqpGoalType.REFLOW)
            {
                if (type != "REFLOW")
                {
                    sndMsg = string.Format($"EQTRAYMOVECHECK HDR=({sendunit.ID},LH.MPLUS,GARA,TEMP) EQPID={sendunit.ID} TRAYID=({e.job.TRAYID}) BATCHJOBID={e.job.BATCHID} MULTIJOBID= JOBTYPE={jobtype} MRNO={e.vecID}");
                }
                else
                {
                    sndMsg = string.Format($"EQTRAYMOVECHECK HDR=({sendunit.ID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={sendunit.ID.Split('-')[0]} TRAYID=({e.job.TRAYID}) BATCHJOBID={e.job.BATCHID} MULTIJOBID= JOBTYPE={jobtype} MRNO={e.vecID}");
                }
            }
            return sndMsg;
        }
        #endregion
        // Chk_EqStatus Method End


        // EQTRAYMOVECHECK_REP Method Start
        #region EQTRAYMOVECHECK_REP
        private unit MOVECHECK_GoalName_Unit_Check(string msg, string[] words)
        {
            string goalname = string.Empty;
            if (msg.Contains("SUBEQPID"))
            {
                goalname = UtilMgr.FindKeyStringToValue(words, "SUBEQPID");
            }
            else
            {
                goalname = UtilMgr.FindKeyStringToValue(words, "EQPID");
            }

            return Db.Units.Where(p => p.GOALNAME == goalname).Single();
        }
        #endregion
        // EQTRAYMOVECHECK_REP Method End

        // EQTRAYJOBCOMPCHECK Method Start
        #region EQTRAYJOBCOMPCHECK
        const string ERR_EQTRAYJOBCOMPCHECK = "error. EQTRAYJOBCOMPCHECK. RUNMODE is EMPTY.";

        private string EQTRAYJOBCOMPCHECK_subeqpid_Check(SendJobToVecArgs1 e)
        {
            string subeqpid = (e.cmd == "SRC") ? e.job.S_EQPID : e.job.T_EQPID;

            if (subeqpid == "")
                throw new UtilMgrCustomException("error. EQTRAYMOVEREQ. EQPID or SUBEQPID is no data.");

            return subeqpid;
        }
        private void EQTRAYJOBCOMPCHECK_sndMsg_Send(string subeqpid, string status, string isComp)
        {
            string newSubject = string.Empty;
            string sndMsg = string.Empty;
            unit unit = Db.Units.Where(p => p.GOALNAME == subeqpid).SingleOrDefault();
            if (unit.goaltype == (int)EqpGoalType.REFLOW)
            {
                newSubject = string.Format("{0}{1}", SndSubjct, subeqpid);
                sndMsg = string.Format($"EQTRAYJOBCOMPCHECK_REP HDR=({subeqpid},LH.MPLUS,GARA,TEMP) STATUS={status} EQPID={subeqpid} JOBTYPE={JobType} JOBCOMPCHECK={isComp}");
                sndMsg += string.Format($" RUNMODE={RvComm.reflowRunMode}");
                sndMsg += string.Format($" ERRORCODE= ERRORMSG=");
            }
            else
            {
                newSubject = string.Format("{0}{1}", SndSubjct, subeqpid.Split('-')[0]);
                sndMsg = string.Format($"EQTRAYJOBCOMPCHECK_REP HDR=({subeqpid.Split('-')[0]},LH.MPLUS,GARA,TEMP) STATUS={status} EQPID={subeqpid.Split('-')[0]} SUBEQPID={subeqpid} JOBTYPE={JobType} JOBCOMPCHECK={isComp}");
                sndMsg += string.Format($" ERRORCODE= ERRORMSG=");
            }

            RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);

        }
        #endregion
        // EQTRAYJOBCOMPCHECK Method End

        // SYSWIN_TempPreDownRequest Method Start
        #region SYSWIN_TempPreDownRequest
        private bool SYSWIN_TempPreDownRequest_sndMsg_Create(int i, unit srcUnit, unit dstUnit, Nullable<int> downtemp, ref string eqp, ref string sndMsg)
        {
            if (i == 0 && srcUnit != null && srcUnit.ID.Substring(0, 2) == "RO" && (downtemp == 1 || downtemp == 3))
            {
                eqp = srcUnit.ID.Split('-')[0];
                sndMsg = string.Format($"EQPRETEMPDOWNREQ HDR=({srcUnit.ID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={srcUnit.ID.Split('-')[0]} SUBEQPID={srcUnit.ID}");
            }
            else
                if (i == 1 && dstUnit != null && dstUnit.ID.Substring(0, 2) == "RO" && (downtemp == 2 || downtemp == 3))
            {
                eqp = dstUnit.ID.Split('-')[0];
                sndMsg = string.Format($"EQPRETEMPDOWNREQ HDR=({dstUnit.ID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={dstUnit.ID.Split('-')[0]} SUBEQPID={dstUnit.ID}");
            }
            else
                return false;
            return true;
        }
        #endregion
        // SYSWIN_TempPreDownRequest Method End

        // SYSWIN_TempDownSingleRequest Method Start
        #region SYSWIN_TempDownSingleRequest
        private void SYSWIN_TempDownSingleRequest_lstTempDown_Add(unit srcUnit, unit dstUnit)
        {
            if (lstTempDown.Count() > 0)
            {
                lstTempDown.Clear();
            }
            unit TD_unit = (JobType == "UNLOAD") ? srcUnit : dstUnit;

            if (TD_unit != null &&
                (TD_unit.goaltype == (int)EqpGoalType.SYSWIN || TD_unit.goaltype == (int)EqpGoalType.SYSWIN_OVEN || TD_unit.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || TD_unit.goaltype == (int)EqpGoalType.BUFFER_STK))
                lstTempDown.Add(TD_unit.ID);
        }
        private bool SYSWIN_TempDownSingleRequest_sndMsg_Send(int i)
        {
            string eqpId = lstTempDown[i].Split('-')[0];
            string sndMsg = string.Format($"EQTEMPDOWNREQ HDR=({eqpId},LH.MPLUS,GARA,TEMP) EQPID={eqpId} SUBEQPID={lstTempDown[i]} JOBTYPE={JobType} SETTEMP= MRNO={VehicleId}");


            Logger.Inst.Write(VehicleId, CmdLogType.Rv, $"SYSWIN_TempDownRequest. List TempDown'Count is {lstTempDown.Count()}, {lstTempDown[i]}");

            if (!SendRvMessageNoWait(eqpId, sndMsg))
            {
                lstTempDown.Clear();
                Logger.Inst.Write(VehicleId, CmdLogType.Rv, $"Error. SYSWIN_TempDownRequest. {eqpId}'s msg sending fail.");
                return false;
            }
            RvComm.tempdown_check = false;
            return true;
        }
        private bool SendRvMessageNoWait(string eqpId, string sndMsg)
        {
            bool b = true;
            RvComm.ResetFlag();
            string newSubject = string.Format("{0}{1}", SndSubjct, eqpId);

            return RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
        }
        #endregion
        // SYSWIN_TempDownSingleRequest Method End

        // TrayMoveInfo Method Start
        #region TrayMoveInfo
        private (string, string) TrayMoveInfo_sndMsg_Create(int goaltype, SendJobToVecArgs1 e)
        {
            string sndMsg = string.Empty;
            string devTyp = string.Empty;
            switch ((EqpGoalType)goaltype)
            {
                case EqpGoalType.STK:
                    if (e.cmd == "SRC")
                        sndMsg = string.Format($"EQTRAYMOVEINFO HDR=({e.job.S_EQPID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={e.job.S_EQPID.Split('-')[0]} TRAYID=({e.job.TRAYID}) SOURCEPORT=SHELF DESTPORT={e.job.S_SLOT} JOBTYPE=UNLOAD MRNO={e.vecID}");

                    devTyp = "STOCKER";
                    break;
                case EqpGoalType.HANDLER_STACK:
                case EqpGoalType.HANDLER:
                    if (e.cmd == "SRC")
                        sndMsg = string.Format($"EQTRAYMOVEINFO HDR=({e.job.S_EQPID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={e.job.S_EQPID.Split('-')[0]} TRAYID=({e.job.TRAYID}) SOURCEPORT=SHELF DESTPORT={e.job.S_PORT} JOBTYPE=UNLOAD MRNO={e.vecID}");

                    devTyp = "HANDLER";
                    break;
            }
            return (sndMsg, devTyp);
        }
        #endregion
        // TrayMoveInfo Method End

        // TrayLoadInfo Method Start
        #region TrayLoadInfo
        private (string, string) TrayLoadInfo_sndMsg_Create(int goaltype, SendJobToVecArgs1 e, string RvMsgName)
        {
            string sndMsg = string.Empty;
            string devTyp = string.Empty;
            string EQPID = string.Empty;
            string traytype = string.Empty;
            switch ((EqpGoalType)goaltype)
            {
                case EqpGoalType.STK:
                    if (e.cmd == "DST")
                    {
                        string dstslot = TrayLoadInfo_sndMsg_slot(e.job.T_PORT);
                        sndMsg = string.Format($"EQ{RvMsgName.ToUpper()} HDR=({e.job.T_EQPID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={e.job.T_EQPID.Split('-')[0]} TRAYID=({e.job.TRAYID}) " +
                            $"BATCHJOBID={e.job.BATCHID} MULTIJOBID= SOURCEPORT={e.job.S_EQPID.Split('-')[0]} SOURCESLOTNO=({e.job.S_SLOT}) DESTPORT={e.job.T_SLOT} DESTSLOTNO=({dstslot}) " +
                            $"EXECUTETIME={e.job.EXECUTE_TIME} MRNO={e.vecID}");
                    }

                    devTyp = "STOCKER";
                    break;
                case EqpGoalType.SYSWIN_OVEN_t:
                case EqpGoalType.SYSWIN_OVEN:
                case EqpGoalType.SYSWIN:
                case EqpGoalType.BUFFER_STK:
                    sndMsg = string.Format($"EQ{RvMsgName.ToUpper()} HDR=({e.job.T_EQPID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={e.job.T_EQPID.Split('-')[0]} SUBEQPID={e.job.T_EQPID} " +
                        $"TRAYID=({e.job.TRAYID}) LOTID={e.job.LOT_NO} JOBTYPE=LOAD SLOTID=({e.job.T_SLOT}) STEPID={e.job.T_STEPID} EXECUTETIME={e.job.EXECUTE_TIME} MRNO={e.vecID}");
                    devTyp = "SYSWIN";
                    break;
                case EqpGoalType.HANDLER_STACK:
                case EqpGoalType.HANDLER:
                    sndMsg = string.Format($"EQ{RvMsgName.ToUpper()} HDR=({e.job.T_EQPID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={e.job.T_EQPID.Split('-')[0]} ");

                    int loader_num = Convert.ToInt32(e.job.T_SLOT.Substring(6, 1));
                    for (int i = 0; i < RvComm.Handler_LD_peps.Count; i++)
                    {
                        int j = i + 1;
                        if (RvComm.Handler_LD_peps[i] != null && j == loader_num)
                        {
                            traytype = TrayLoadInfo_sndMsg_Handler_traytype(RvComm.Handler_LD_peps[i]);
                        }
                        else
                            traytype = string.Empty;
                        sndMsg += string.Format($"LD{j}LOTID={RvComm.Handler_LD_LotID[i]} LD{j}TRAYID={RvComm.Handler_LD_TrayID[i]} " +
                            $"LD{j}STEPID={RvComm.Handler_LD_StepID[i]} LD{j}TRAYTYPE={traytype} ");
                    }
                    sndMsg += string.Format($"JOBTYPE=LOAD MRNO={e.vecID}");
                    devTyp = "HANDLER";
                    break;
                case EqpGoalType.REFLOW:
                    string reflow_dstslot = TrayLoadInfo_sndMsg_slot(e.job.T_PORT);
                    sndMsg = string.Format($"EQ{RvMsgName.ToUpper()} HDR=({e.job.T_EQPID},LH.MPLUS,GARA,TEMP) EQPID={e.job.T_EQPID} TRAYID=({e.job.TRAYID}) BATCHJOBID={e.job.BATCHID} MULTIJOBID= " +
                        $"SOURCEPORT={e.job.S_EQPID.Split('-')[0]} SOURCESLOTNO=({e.job.S_SLOT}) DESTPORT={e.job.T_SLOT} DESTSLOTNO=({reflow_dstslot}) JOBTYPE=LOAD EXECUTETIME={e.job.EXECUTE_TIME} " +
                        $"STEPID={e.job.STEPID} RUNMODE={RvComm.reflowRunMode} MRNO={e.vecID}");

                    devTyp = "REFLOW";
                    break;
            }

            return (sndMsg, devTyp);
        }
        private string TrayLoadInfo_sndMsg_slot(string port)
        {
            string[] slot_split = (port).Split(',');
            string slot = string.Empty;
            for (int i = 0; i < slot_split.Count() / 2; i++)
            {
                if (i > 0)
                    slot += ",";
                slot += slot_split[(i * 2) + 1];
            }
            return slot;
        }
        private string TrayLoadInfo_sndMsg_Handler_traytype(pepschedule e)
        {
            string traytype = string.Empty;
            if (e.WORKTYPE == "EI" || e.WORKTYPE == "EO")
            {
                traytype = "EMPTY";
            }
            else if (e.WORKTYPE == "TI" || e.WORKTYPE == "TO")
            {
                traytype = "NORMAL";
            }
            else
            {
                traytype = "UNKNOWN TYPE";
            }
            return traytype;
        }
        private string TrayLoad_UnloadInfo_newSubject_Create(int goaltype, SendJobToVecArgs1 e)
        {
            string newSubject = string.Empty;
            if (e.cmd == "SRC")
            {
                switch ((EqpGoalType)goaltype)
                {
                    case EqpGoalType.REFLOW:
                        newSubject = string.Format("{0}{1}", SndSubjct, e.job.S_EQPID);
                        break;
                    default:
                        newSubject = string.Format("{0}{1}", SndSubjct, e.job.S_EQPID.Split('-')[0]);
                        break;
                }
            }
            else
            {
                switch ((EqpGoalType)goaltype)
                {
                    case EqpGoalType.REFLOW:
                        newSubject = string.Format("{0}{1}", SndSubjct, e.job.T_EQPID);
                        break;
                    default:
                        newSubject = string.Format("{0}{1}", SndSubjct, e.job.T_EQPID.Split('-')[0]);
                        break;
                }
            }
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"SendSubject=>{newSubject}");

            return newSubject;
        }
        #endregion
        // TrayLoadInfo Method End

        // TrayUnLoadInfo Method Start
        #region TrayUnLoadInfo
        private (string, string) TrayUnLoadInfo_sndMsg_Create(int goaltype, SendJobToVecArgs1 e, string RvMsgName)
        {
            string sndMsg = string.Empty;
            string devTyp = string.Empty;
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, "UNLOAD GOALTYPE = " + ((EqpGoalType)goaltype).ToString());

            switch ((EqpGoalType)goaltype)
            {
                case EqpGoalType.STK:
                    string srcslot = TrayLoadInfo_sndMsg_slot(e.job.S_PORT);
                    sndMsg = string.Format($"EQ{RvMsgName.ToUpper()} HDR=({e.job.S_EQPID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={e.job.S_EQPID.Split('-')[0]} TRAYID=({e.job.TRAYID}) BATCHJOBID={e.job.BATCHID} MULTIJOBID= SOURCEPORT={e.job.S_SLOT} SOURCESLOTNO=({srcslot}) DESTPORT={e.job.T_EQPID.Split('-')[0]} DESTSLOTNO=({e.job.T_SLOT}) EXECUTETIME={e.job.EXECUTE_TIME} MRNO={e.vecID}");

                    devTyp = "STOCKER";
                    break;
                case EqpGoalType.SYSWIN_OVEN_t:
                case EqpGoalType.SYSWIN_OVEN:
                case EqpGoalType.SYSWIN:
                case EqpGoalType.BUFFER_STK:
                    sndMsg = string.Format($"EQ{RvMsgName.ToUpper()} HDR=({e.job.S_EQPID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={e.job.S_EQPID.Split('-')[0]} SUBEQPID={e.job.S_EQPID} TRAYID=({e.job.TRAYID}) LOTID={e.job.LOT_NO} JOBTYPE=UNLOAD RECIPE= SLOTID=({e.job.S_SLOT}) STEPID={e.job.S_STEPID} EXECUTETIME={e.job.EXECUTE_TIME} MRNO={e.vecID}");
                    devTyp = "SYSWIN";
                    break;
                case EqpGoalType.HANDLER_STACK:
                case EqpGoalType.HANDLER:
                    sndMsg = string.Format($"EQ{RvMsgName.ToUpper()} HDR=({e.job.S_EQPID.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={e.job.S_EQPID.Split('-')[0]} ");
                    for (int i = 0; i < RvComm.Handler_LD_peps.Count; i++)
                    {
                        sndMsg += string.Format($"LD{i + 1}TRAYID={RvComm.Handler_LD_TrayID[i]} ");
                    }
                    sndMsg += string.Format($"JOBTYPE=UNLOAD MRNO={e.vecID}");
                    devTyp = "HANDLER";
                    break;
                case EqpGoalType.REFLOW:
                    string reflow_srcslot = TrayLoadInfo_sndMsg_slot(e.job.S_PORT);
                    sndMsg = string.Format($"EQ{RvMsgName.ToUpper()} HDR=({e.job.S_EQPID},LH.MPLUS,GARA,TEMP) EQPID={e.job.S_EQPID} TRAYID=({e.job.TRAYID}) BATCHJOBID={e.job.BATCHID} MULTIJOBID= SOURCEPORT={e.job.S_SLOT} SOURCESLOTNO=({reflow_srcslot}) DESTPORT={e.job.T_EQPID.Split('-')[0]} DESTSLOTNO=({e.job.T_SLOT}) JOBTYPE=UNLOAD EXECUTETIME={e.job.EXECUTE_TIME} STEPID={e.job.STEPID} RUNMODE={RvComm.reflowRunMode} MRNO={e.vecID}");

                    devTyp = "REFLOW";
                    break;
            }
            return (sndMsg, devTyp);
        }
        #endregion
        // TrayUnLoadInfo Method End

        // ReflowRecipeSet Method Start
        #region ReflowRecipeSet
        private string ReflowRecipeSet_trayid_Create(List<pepschedule> all_reflow)
        {
            string trayid = string.Empty;
            foreach (var x in all_reflow)
            {
                if (trayid.Length > 1)
                    trayid += ",";

                trayid += x.TRAYID;

            }

            if (trayid.Split(',').Count() > 1)
            {
                trayid = string.Format($"({trayid})");
            }

            return trayid;
        }
        private string ReflowRecipeSet_traystepid_Create(SendJobToVecArgs1 e)
        {
            string traystepid = string.Empty;

            if (e.job.STEPID.Split(',').Count() > 1)
            {
                traystepid = string.Format($"({e.job.STEPID})");
            }
            else
            {
                traystepid = e.job.STEPID;
            }
            return traystepid;
        }
        #endregion
        // ReflowRecipeSet Method End

        // ReflowLoaderInfoSet Method Start
        #region ReflowLoaderInfoSet
        private void ReflowLoaderInfoSet_Data_Create(int traycount, pepschedule pes, ref string trayid, ref string qtyid, ref string lotid, ref string traystepid, ref string austepid)
        {
            if (traycount > 1)
            {
                trayid = string.Format($"({pes.TRAYID})");
                qtyid = string.Format($"({pes.QTY})");
                lotid = string.Format($"({pes.LOT_NO})");
            }
            else
            {
                trayid = pes.TRAYID;
                qtyid = pes.QTY;
                lotid = pes.LOT_NO;
            }
            //traystepid = string.Format($"{pes.STEPID}");
            //austepid = string.Format($"{pes.STEPID}");

            if (pes.STEPID.Split(',').Count() > 1)
            {
                traystepid = string.Format($"({pes.STEPID})");
                austepid = string.Format($"({pes.STEPID})");
            }
            else
            {
                traystepid = pes.STEPID;
                for (int i = 0; i < traycount; i++)
                {
                    if (i == 0)
                        austepid = "(";
                    else
                        austepid += ",";

                    austepid += string.Format($"{pes.STEPID}");

                    if (i == traycount - 1)
                        austepid += ")";
                }
            }
        }
        private (string, string) ReflowLoaderInfoSet_sndMsg_Create(SendJobToVecArgs1 e, ReflowLoaderInfoSet_val RLIS_val, List<pepschedule> all_reflow)
        {
            string sndMsg = string.Empty;
            string devTyp = string.Empty;
            string lot_id = string.Empty;
            string tray_id = string.Empty;
            string qty = string.Empty;
            string lot_id_1 = string.Empty;
            string tray_id_1 = string.Empty;
            string qty_1 = string.Empty;
            string lot_id_2 = string.Empty;
            string tray_id_2 = string.Empty;
            string qty_2 = string.Empty;
            string[] lot_ids = null;
            string[] tray_ids = null;
            string[] qtys = null;

            lot_id_1 = Regex.Replace(RLIS_val.lot01id, "[()]", "");
            lot_ids = lot_id_1.Split(',');

            tray_id_1 = Regex.Replace(RLIS_val.tray01id, "[()]", "");
            tray_ids = tray_id_1.Split(',');

            qty_1 = Regex.Replace(RLIS_val.qty01id, "[()]", "");
            qtys = qty_1.Split(',');

            if (all_reflow.Count() > 0)
            {
                if (lot_ids.Count() > 1)
                {
                    lot_id_2 = Regex.Replace(RLIS_val.lot02id, "[()]", "");
                    lot_id = string.Format($"({lot_id_1},{lot_id_2})");
                }

                if (tray_ids.Count() > 1)
                {
                    tray_id_2 = Regex.Replace(RLIS_val.tray02id, "[()]", "");
                    tray_id = string.Format($"({tray_id_1},{tray_id_2})");
                }

                if (qtys.Count() > 1)
                {
                    qty_2 = Regex.Replace(RLIS_val.qty02id, "[()]", "");
                    qty = string.Format($"({qty_1},{qty_2})");
                }
            }
            else
            {
                if (e.job.T_SLOT == "AUTO01")
                {
                    if (RLIS_val.lot01id.Count() > 1)
                        lot_id = string.Format($"({RLIS_val.lot01id})");
                    else
                        lot_id = RLIS_val.lot01id;

                    if (RLIS_val.tray01id.Count() > 1)
                        tray_id = string.Format($"({RLIS_val.tray01id})");
                    else
                        tray_id = RLIS_val.tray01id;

                    if (RLIS_val.qty01id.Count() > 1)
                        qty = string.Format($"({RLIS_val.qty01id})");
                    else
                        qty = RLIS_val.qty01id;
                }
                else
                {
                    if (RLIS_val.lot02id.Count() > 1)
                        lot_id = string.Format($"({RLIS_val.lot02id})");
                    else
                        lot_id = RLIS_val.lot02id;

                    if (RLIS_val.tray02id.Count() > 1)
                        tray_id = string.Format($"({RLIS_val.tray02id})");
                    else
                        tray_id = RLIS_val.tray02id;

                    if (RLIS_val.qty02id.Count() > 1)
                        qty = string.Format($"({RLIS_val.qty02id})");
                    else
                        qty = RLIS_val.qty02id;
                }
            }
            sndMsg = string.Format($"REFLOWLOADERINFOSET HDR=({e.job.T_EQPID},LH.MPLUS,GARA,TEMP) EQPID={e.job.T_EQPID} LOTID={lot_id}");
            sndMsg += string.Format($" TRAYID={tray_id} TRAYQTY={qty} STEPID={e.job.STEPID} BATCHJOBID={e.job.BATCHID} MULTIJOBID= JOBTYPE=LOAD MRNO={e.vecID}");

            if (e.job.T_SLOT == "AUTO01" && (all_reflow != null && all_reflow.Count() > 0))
            {
                sndMsg += string.Format($" AUTO01LOTID={RLIS_val.lot01id} AUTO01TRAY={RLIS_val.tray01id} AUTO01QTY={RLIS_val.qty01id} AU01STEPID={RLIS_val.au01stepid}");
                sndMsg += string.Format($" AUTO02LOTID={RLIS_val.lot02id} AUTO02TRAY={RLIS_val.tray02id} AUTO02QTY={RLIS_val.qty02id} AU02STEPID={RLIS_val.au02stepid}");
            }
            else if (e.job.T_SLOT == "AUTO02" && (all_reflow != null && all_reflow.Count() > 0))
            {
                sndMsg += string.Format($" AUTO01LOTID={RLIS_val.lot02id} AUTO01TRAY={RLIS_val.tray02id} AUTO01QTY={RLIS_val.qty02id} AU01STEPID={RLIS_val.au02stepid}");
                sndMsg += string.Format($" AUTO02LOTID={RLIS_val.lot01id} AUTO02TRAY={RLIS_val.tray01id} AUTO02QTY={RLIS_val.qty01id} AU02STEPID={RLIS_val.au01stepid}");
            }
            else if (e.job.T_SLOT == "AUTO01")
            {
                sndMsg += string.Format($" AUTO01LOTID={RLIS_val.lot01id} AUTO01TRAY={RLIS_val.tray01id} AUTO01QTY={RLIS_val.qty01id} AU01STEPID={RLIS_val.au01stepid}");
                sndMsg += string.Format($" AUTO02LOTID= AUTO02TRAY= AUTO02QTY= AU02STEPID=");

            }
            else if (e.job.T_SLOT == "AUTO02")
            {
                sndMsg += string.Format($" AUTO01LOTID= AUTO01TRAY= AUTO01QTY= AU01STEPID=");
                sndMsg += string.Format($" AUTO02LOTID={RLIS_val.lot01id} AUTO02TRAY={RLIS_val.tray01id} AUTO02QTY={RLIS_val.qty01id} AU02STEPID={RLIS_val.au01stepid}");
            }

            sndMsg += string.Format($" RUNMODE={RvComm.reflowRunMode}");
            devTyp = "REFLOW";

            return (sndMsg, devTyp);
        }
        #endregion
        // ReflowLoaderInfoSet Method End

        // JOBCANCEL Method Start
        #region JOBCANCEL
        private string JOBCANCEL_log_vecid(string vecid)
        {
            if (vecid == "PROGRAM")
                return "VEHICLE01";
            else
                return vecid;
        }
        private bool JOBCANCEL_UnLoad_Load_Reflow(SendJobToVecArgs1 e, string jobtype_msg, string eqpid, unit cancel_unit)
        {
            string newSubject = string.Empty;
            string sndMsg = string.Empty;
            newSubject = string.Format("{0}{1}", SndSubjct, eqpid);
            sndMsg = JobCancel_Send_Message_Reflow(e, jobtype_msg, eqpid, cancel_unit);

            return RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
        }
        public string JobCancel_Send_Message_Reflow(SendJobToVecArgs1 e, string jobtype_msg, string eqpid, unit cancel_unit)
        {
            string sndMsg = string.Empty;
            sndMsg = string.Format($"EQTRAYJOBCANCEL HDR=({eqpid},LH.MPLUS,GARA,TEMP) EQPID={eqpid} TRAYID=({e.job.TRAYID})");
            sndMsg += string.Format($" JOBTYPE={jobtype_msg} RUNMODE={RvComm.reflowRunMode}");

            return sndMsg;
        }
        private bool JOBCANCEL_UnLoad_STK(SendJobToVecArgs1 e, string jobtype_msg, string eqpid, unit cancel_unit, string newSubject)
        {
            string sndMsg = JobCancel_Send_Message(e, jobtype_msg, eqpid, cancel_unit);

            if (!RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name))
                return false;

            jobtype_msg = "LOAD";
            string[] eqpids = e.job.T_EQPID.Split(',');
            cancel_unit = Db.Units.Where(p => p.GOALNAME == eqpids[0]).Single();

            if (cancel_unit.goaltype == (int)EqpGoalType.REFLOW || cancel_unit.goaltype == (int)EqpGoalType.HANDLER
                || cancel_unit.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                (newSubject, sndMsg) = JOBCANCEL_UnLoad_STK_LOAD(cancel_unit, e, jobtype_msg, eqpids[0]);

                return RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
            }
            return true;
        }
        public string JobCancel_Send_Message(SendJobToVecArgs1 e, string jobtype_msg, string eqpid, unit cancel_unit)
        {
            string sndMsg = string.Empty;

            if (cancel_unit.goaltype == (int)EqpGoalType.STK)
            {
                sndMsg = string.Format($"EQTRAYJOBCANCEL HDR=({eqpid.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={eqpid.Split('-')[0]} TRAYID=({e.job.TRAYID})");
            }
            else if (cancel_unit.goaltype == (int)EqpGoalType.HANDLER || cancel_unit.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                sndMsg = string.Format($"EQTRAYJOBCANCEL HDR=({eqpid.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={eqpid.Split('-')[0]} SUBEQPID={eqpid} ");
                for (int i = 0; i < RvComm.Handler_LD_peps.Count; i++)
                {
                    sndMsg += string.Format($"LD{i + 1}TRAYID={RvComm.Handler_LD_TrayID[i]} ");
                }
                sndMsg += string.Format($"JOBTYPE={jobtype_msg}");
            }
            else
            {
                sndMsg = string.Format($"EQTRAYJOBCANCEL HDR=({eqpid.Split('-')[0]},LH.MPLUS,GARA,TEMP) EQPID={eqpid.Split('-')[0]} SUBEQPID={eqpid} TRAYID=({e.job.TRAYID})");
            }

            if (cancel_unit.goaltype != (int)EqpGoalType.HANDLER && cancel_unit.goaltype != (int)EqpGoalType.HANDLER_STACK)
                sndMsg += string.Format($" JOBTYPE={jobtype_msg} BATCHJOBID={e.job.BATCHID} MULTIJOBID= STEPID={e.job.STEPID}");

            return sndMsg;
        }
        private (string, string) JOBCANCEL_UnLoad_STK_LOAD(unit cancel_unit, SendJobToVecArgs1 e, string jobtype_msg, string eqpids)
        {
            string sndMsg = string.Empty;
            string newSubject = string.Empty;
            if (cancel_unit.goaltype == (int)EqpGoalType.REFLOW)
            {
                newSubject = string.Format("{0}{1}", SndSubjct, e.job.T_EQPID);
                sndMsg = JobCancel_Send_Message_Reflow(e, jobtype_msg, eqpids, cancel_unit);
            }
            else if (cancel_unit.goaltype == (int)EqpGoalType.HANDLER || cancel_unit.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                newSubject = string.Format("{0}{1}", SndSubjct, e.job.T_EQPID.Split('-')[0]);
                sndMsg = JobCancel_Send_Message(e, jobtype_msg, eqpids, cancel_unit);
            }
            return (newSubject, sndMsg);
        }
        private bool JOBCANCEL_UnLoad_Load_another(SendJobToVecArgs1 e, string jobtype_msg, string eqpid, unit cancel_unit, string newSubject)
        {
            string sndMsg = JobCancel_Send_Message(e, jobtype_msg, eqpid, cancel_unit);

            return RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
        }
        #endregion
        // JOBCANCEL Method End
    }
}
