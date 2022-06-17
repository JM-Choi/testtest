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
    public partial class RvListener
    {
        // Global Start
        #region Global
        public bool IsControllerStop(string log_vecid)
        {
            var controller = Db.Controllers.SingleOrDefault();
            if (controller == null)
            {
                return false;
            }

            if (RvSenderList[log_vecid].IsStop)
                return true;

            if (controller.C_state != (int)ControllerState.STOP)
                return false;

            return true;
        }


        private int Use_Tray_Check(string[] words)
        {
            string trayid = UtilMgr.FindKeyStringToValue(words, "TRAYID");
            trayid = Regex.Replace(trayid, "[()]", "");
            int traycount = trayid.Split(',').Count();
            if (traycount <= 0)
            {
                Use_Tray_Error(words);
            }
            return traycount;
        }
        private void Use_Tray_Error(string[] words)
        {
            string eqpid = UtilMgr.FindKeyStringToValue(words, "EQPID");
            unit error_unit = Db.Units.Where(p => p.GOALNAME == eqpid).Single();

            switch (error_unit.goaltype)
            {
                case (int)EqpGoalType.STK:
                    throw new UtilMgrCustomException(ERR_MOVECHK_REP_STK_TRAYINFO);
                case (int)EqpGoalType.REFLOW:
                    if (eqpid.Contains('-'))
                        throw new UtilMgrCustomException(ERR_MOVECHK_REP_REFLOW_LOADER_TRAYINFO);
                    else
                        throw new UtilMgrCustomException(ERR_MOVECHK_REP_REFLOW_TRAYINFO);
                default:
                    break;
            }
        }
        private string Slot_Data(string[] slotS, int traycount, string port, bool not = false)
        {
            string slot = string.Empty;
            if (!not)
            {
                for (int i = 0, slot_cnt = 0; i < slotS.Count() && slot_cnt < traycount; i++)
                {
                    string[] slice = slotS[i].Split(':');
                    if (slice.Count() > 1 && slice[1] != "")
                    {
                        if (slot.Length > 0)
                            slot += ',';
                        slot += slot_check(port);
                        slot += ',';
                        slot += ((int)Convert.ToInt32(slice[0])).ToString("D2");
                        slot_cnt++;
                    }
                }
            }
            else
            {
                for (int i = 1; i <= traycount; i++)
                {
                    if (slot.Length > 0)
                        slot += ',';
                    slot += slot_check(port);
                    slot += ',';
                    slot += i.ToString("D2");
                }
            }
            return slot;
        }
        public string slot_check(string port)
        {
            string slot = string.Empty;
            if (port.Contains("AUTO01"))
                slot += 1.ToString();
            else if (port.Contains("AUTO02"))
                slot += 2.ToString();
            else if (port.Contains("STACK01"))
                slot += 3.ToString();
            else if (port.Contains("STACK02"))
                slot += 4.ToString();

            return slot;
        }
        private void Reflow_Loader_Runmode_Error(string word)
        {
            switch (word)
            {
                case "EQTRAYMOVECHECK_REP":
                    throw new UtilMgrCustomException(ERR_MOVECHK_REP_REFLOW_RUNMODE);
                case "EQTRAYUNLOADCOMPLETE":
                    throw new UtilMgrCustomException(ERR_EQTRAYUNLOADCOMPLETE);
                case "ERR_EQTRAYLOADCOMPLETE":
                    throw new UtilMgrCustomException(ERR_EQTRAYLOADCOMPLETE);

                default:
                    break;
            }
        }

        #endregion
        //Global End


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
                if (!goalname.Contains('-'))
                {
                    goalname += "-1";
                }
            }

            return Db.Units.Where(p => p.GOALNAME == goalname).Single();
        }
        #endregion
        // EQTRAYMOVECHECK_REP Method End

        // EQTRAYMOVECHECK_REP_STK Method Start
        const string ERR_MOVECHK_REP_STK_STATUS = "error. EQTRAYMOVECHECK_REP_STK. STKSTATUS is not RUN and IDLE.";
        const string ERR_MOVECHK_REP_STK_AUTOPORT = "error. EQTRAYMOVECHECK_REP_STK. PORT is all not IDLE.";
        const string ERR_MOVECHK_REP_STK_SLOTINFO = "error. EQTRAYMOVECHECK_REP_STK. SLOTINFO is not Valid.";
        const string ERR_MOVECHK_REP_STK_TRAYINFO = "error. EQTRAYMOVECHECK_REP_STK. TRAYINFO is not Valid.";
        const string ERR_MOVECHK_REP_STK_INVALID_EQPTYPE = "error. EQTRAYMOVECHECK_REP_STK. Check EqpId";
        #region EQTRAYMOVECHECK_REP_STK
        private void STK_LP_Check(pepschedule pes, string[] words, unit s_unit, string vecID)
        {
            RvSenderList[vecID].RvComm.lp_tray = false;
            if (STK_goaltype_Check(s_unit))
            {
                string slotinfo = UtilMgr.FindKeyStringToValue(words, string.Format("{0}LP_SLOTINFO", pes.S_SLOT.Split('.')[0]));

                for (int i = 0; i < pes.TRAYID.Split(',').Count(); i++)
                {
                    if (slotinfo.Contains(pes.TRAYID.Split(',')[i]))
                        RvSenderList[vecID].RvComm.lp_tray = true;
                    else
                    {
                        RvSenderList[vecID].RvComm.lp_tray = false;
                        break;
                    }
                }
            }
        }
        private bool STK_goaltype_Check(unit unit)
        {
            return (unit.goaltype == (int)EqpGoalType.STK);
        }
        private void STK_Status_Check(string[] words)
        {
            string stkstatus = UtilMgr.FindKeyStringToValue(words, "STKSTATUS");
            if (stkstatus != "RUN" && stkstatus != "IDLE")
            {
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_STK_STATUS);
            }
        }
        private string STK_Port_Ready_Check(pepschedule pes, string[] words, string vecID)
        {
            string stkport = string.Empty;
            if (RvSenderList[vecID].RvComm.lp_tray)
            {
                stkport = pes.S_SLOT;
            }
            else if (RvSenderList[vecID].RvComm.STK_Retry)
            {
                stkport = pes.S_SLOT;
                RvSenderList[vecID].RvComm.STK_Retry = false;
            }
            else if (pes.TRANSFERTYPE == "TRAY")
            {
                if (UtilMgr.FindKeyStringToValue(words, "AUTO01") == "IDLE")
                    stkport = "AUTO01.LP";
                else if (UtilMgr.FindKeyStringToValue(words, "AUTO02") == "IDLE")
                    stkport = "AUTO02.LP";
                else
                {
                    // 20220105
                    // O 작업 Dst 출발에서 MoveCheck 삭제되어 STK 앞에서만 MoveCheck 진행
                    // 도착 후 MoveCheck 시 IDLE인 Port가 없으면 STANDBY로 이동 명령어 추가

                    if (Cfg.Data.UseSTKStandby && pes.WORKTYPE == "O")
                        VehicleList[vecID].SendMessageToVehicle(string.Format("GO;STANDBY;"));
                    throw new UtilMgrCustomException(ERR_MOVECHK_REP_STK_AUTOPORT);
                }
            }
            else if (pes.TRANSFERTYPE == "STACK")
            {
                if (UtilMgr.FindKeyStringToValue(words, "STACK01") == "IDLE")
                    stkport = "STACK01.LP";
                else if (UtilMgr.FindKeyStringToValue(words, "STACK02") == "IDLE")
                    stkport = "STACK02.LP";
                else
                {
                    if (Cfg.Data.UseSTKStandby && pes.WORKTYPE == "TO")
                        VehicleList[vecID].SendMessageToVehicle(string.Format("GO;STANDBY;"));
                    throw new UtilMgrCustomException(ERR_MOVECHK_REP_STK_AUTOPORT);
                }
            }
            return stkport;
        }
        private string[] STK_Slot_Number_Check(string[] words, string stkport)
        {
            string slotinfo = UtilMgr.FindKeyStringToValue(words, string.Format("{0}LP_SLOTINFO", stkport.Split('.')[0]));
            if (slotinfo.Contains("Not found") == true)
            {
                return null;
            }
            slotinfo = Regex.Replace(slotinfo, "[()]", "");
            string[] slotS = slotinfo.Split(',');
            if (slotS.Count() <= 0)
            {
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_STK_SLOTINFO);
            }
            return slotS;
        }
        private void STK_slot_port_DataSave(unit s_unit, unit d_unit, string stkport, string slot, string vecID)
        {
            if (STK_goaltype_Check(s_unit))
            {
                RvSenderList[vecID].CurJob.S_SLOT = stkport;
                RvSenderList[vecID].CurJob.S_PORT = slot;
            }
            else if (STK_goaltype_Check(d_unit))
            {
                RvSenderList[vecID].CurJob.T_SLOT = stkport;
                RvSenderList[vecID].CurJob.T_PORT = slot;
            }
            else
            {
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_STK_INVALID_EQPTYPE);
            }

            Db.DbUpdate(TableType.PEPSCHEDULE);    // FormMonitor update
        }
        #endregion
        // EQTRAYMOVECHECK_REP_STK Method End

        // EQTRAYMOVECHECK_REP_SYSWIN Method Start
        #region EQTRAYMOVECHECK_REP_SYSWIN
        const string ERR_MOVECHK_REP_SYSWIN_EQSTATUS = "error. EQTRAYMOVECHECK_REP_SYSWIN. EQSTATUS is not RUN and IDLE";
        const string ERR_MOVECHK_REP_SYSWIN_STATUS = "error. EQTRAYMOVECHECK_REP_SYSWIN. STATUS is PASS";

        private void SYSWIN_EQSTATUS_Check(string[] words)
        {
            string eqstatus = UtilMgr.FindKeyStringToValue(words, "EQSTATUS");
            if (eqstatus != "RUN" && eqstatus != "IDLE")
            {
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_SYSWIN_EQSTATUS);
            }
        }
        private void SYSWIN_STATUS_Check(string[] words)
        {
            if (UtilMgr.FindKeyStringToValue(words, "STATUS") != "PASS")
            {
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_SYSWIN_STATUS);
            }
        }
        #endregion
        // EQTRAYMOVECHECK_REP_SYSWIN Method End

        // EQTRAYMOVECHECK_REP_HANDLER Method Start
        #region EQTRAYMOVECHECK_REP_HANDLER
        const string ERR_MOVECHK_REP_HANDLER_JOBTYPE = "error. EQTRAYMOVECHECK_REP_HANDLER. JOBTYPE is not valid";
        const string ERR_MOVECHK_REP_HANDLER_NOEMPTY = "error. EQTRAYMOVECHECK_REP_HANDLER. LOADER1 OR LOADER2 is not empty";
        const string ERR_MOVECHK_REP_HANDLER_STATUS = "error. EQTRAYMOVECHECK_REP_HANDLER. STATUS is not PASS";
        const string ERR_MOVECHK_REP_HANDLER_TESTSTATUS = "error. EQTRAYMOVECHECK_REP_HANDLER. TESTSTATUS is DOWN";
        const string ERR_MOVECHK_REP_HANDLER_ALLEMPTY = "error. EQTRAYMOVECHECK_REP_HANDLER. LOADER1 OR LOADER2 is all empty";
        const string ERR_MOVECHK_REP_HANDLER_LDSTATUS_IDLE = "error. EQTRAYMOVECHECK_REP_HANDLER. LDSTATUS is not COMP";
        const string ERR_MOVECHK_REP_HANDLER_LDSTATUS_COMP = "error. EQTRAYMOVECHECK_REP_HANDLER. LDSTATUS is not COMP";

        private void Handler_Status_Check(string[] words)
        {
            // Status가 PASS가 아니면
            if (UtilMgr.FindKeyStringToValue(words, "STATUS") != "PASS")
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_HANDLER_STATUS);
        }
        private void Handler_TestStatus_Check(string[] words)
        {
            // Status가 DOWN이 아니면
            if (UtilMgr.FindKeyStringToValue(words, "TESTSTATUS") == "DOWN")
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_HANDLER_TESTSTATUS);
        }
        private void Handler_Port_Check(string[] words)
        {
            string jobtype = UtilMgr.FindKeyStringToValue(words, "JOBTYPE");
            if (jobtype == "LOAD")
            {
                string loader1 = Handler_Port_Data_Check(words, "LOADER1");
                string loader2 = Handler_Port_Data_Check(words, "LOADER2");
                // Loader1 또는 Loader2 가 Empty가 아니면
                if (!string.IsNullOrWhiteSpace(loader1) || !string.IsNullOrWhiteSpace(loader2))
                    throw new UtilMgrCustomException(ERR_MOVECHK_REP_HANDLER_NOEMPTY);
            }
            // jobtype이 Unload 이면
            else if (jobtype == "UNLOAD")
            {
                string loader1 = Handler_Port_Data_Check(words, "LOADER1");
                string loader2 = Handler_Port_Data_Check(words, "LOADER2");
                // Loader1 또는 Loader2 가 Empty 이면
                if (string.IsNullOrWhiteSpace(loader1) && string.IsNullOrWhiteSpace(loader2))
                    throw new UtilMgrCustomException(ERR_MOVECHK_REP_HANDLER_ALLEMPTY);
            }
            // 둘 다 아니면
            else
            {
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_HANDLER_JOBTYPE);
            }
        }
        private void Handler_LoaderState_Check(string[] words, string vecID)
        {
            try
            {
                string jobtype = UtilMgr.FindKeyStringToValue(words, "JOBTYPE");
                for (int i = 0; i < RvSenderList[vecID].RvComm.Handler_LD_peps.Count; i++)
                {
                    int j = i + 1;
                    string loader = Handler_Port_Data_Check(words, string.Format($"LD{j}TRAYID"));
                    if (!string.IsNullOrWhiteSpace(loader))
                    {
                        // jobtype이 Load 면
                        if (jobtype == "LOAD")
                        {
                            // LD1STATUS가 IDLE이 아니면
                            if (UtilMgr.FindKeyStringToValue(words, string.Format($"LD{j}STATUS")) == "COMP")
                                throw new UtilMgrCustomException(ERR_MOVECHK_REP_HANDLER_LDSTATUS_IDLE);
                                //throw new UtilMgrCustomException($"error. EQTRAYMOVECHECK_REP_HANDLER. LD{j}STATUS is not IDLE");
                        }
                        else
                        {
                            // LD1STATUS가 COMP가 아니면
                            if (UtilMgr.FindKeyStringToValue(words, string.Format($"LD{j}STATUS")) != "COMP")
                                throw new UtilMgrCustomException(ERR_MOVECHK_REP_HANDLER_LDSTATUS_COMP);
                                //throw new UtilMgrCustomException($"error. EQTRAYMOVECHECK_REP_HANDLER. LD{j}STATUS is not COMP");
                        }
                    }
                }
            }
            catch (Exception e)
            {
            }
        }
        private string Handler_Port_Data_Check(string[] words, string val)
        {
            string loader = UtilMgr.FindKeyStringToValue(words, val);
            return Regex.Replace(loader, "[ ()]", "");
        }

        #endregion
        // EQTRAYMOVECHECK_REP_HANDLER Method End

        // EQTRAYMOVECHECK_REP_HANDLER_STACK Method Start
        #region EQTRAYMOVECHECK_REP_HANDLER_STACK
        const string ERR_MOVECHK_REP_HANDLER_STACK_JOBTYPE = "error. EQTRAYMOVECHECK_REP_HANDLER. JOBTYPE is not valid";
        const string ERR_MOVECHK_REP_HANDLER_STACK_NOEMPTY = "error. EQTRAYMOVECHECK_REP_HANDLER. LOADER1 OR GOOD OR FAIL is not empty";
        const string ERR_MOVECHK_REP_HANDLER_STACK_TESTSTATUS = "error. EQTRAYMOVECHECK_REP_HANDLER. TESTSTATUS is not IDLE";
        const string ERR_MOVECHK_REP_HANDLER_STACK_ALLEMPTY = "error. EQTRAYMOVECHECK_REP_HANDLER. GOOD AND FAIL is all empty";

        private void HandlerSTACK_Port_Check(string[] words)
        {
            string jobtype = UtilMgr.FindKeyStringToValue(words, "JOBTYPE");
            if (jobtype == "LOAD")
            {
                string loader1 = Handler_Port_Data_Check(words, "LOADER1");
                string loader2 = Handler_Port_Data_Check(words, "LOADER2");
                string good = Handler_Port_Data_Check(words, "GOOD");
                string fail = Handler_Port_Data_Check(words, "FAIL");
                if (!string.IsNullOrWhiteSpace(loader1) || !string.IsNullOrWhiteSpace(loader2) || !string.IsNullOrWhiteSpace(good)
                        || !string.IsNullOrWhiteSpace(fail))
                    throw new UtilMgrCustomException(ERR_MOVECHK_REP_HANDLER_STACK_NOEMPTY);
            }
            else if (jobtype == "UNLOAD")
            {
                string good = Handler_Port_Data_Check(words, "GOOD");
                string fail = Handler_Port_Data_Check(words, "FAIL");
                if (string.IsNullOrWhiteSpace(good) && string.IsNullOrWhiteSpace(fail))
                    throw new UtilMgrCustomException(ERR_MOVECHK_REP_HANDLER_STACK_ALLEMPTY);
            }
            else
            {
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_HANDLER_JOBTYPE);
            }
        }
        #endregion
        // EQTRAYMOVECHECK_REP_HANDLER_STACK Method End

        // EQTRAYMOVECHECK_REP_REFLOW Method Start
        #region EQTRAYMOVECHECK_REP_REFLOW
        const string ERR_MOVECHK_REP_REFLOW_STATUS = "error. EQTRAYMOVECHECK_REP_REFLOW. LOADERSTATUS is not RUN and IDLE.";
        const string ERR_MOVECHK_REP_REFLOW_RUNMODE = "error. EQTRAYMOVECHECK_REP_REFLOW. RUNMODE is EMPTY.";
        const string ERR_MOVECHK_REP_REFLOW_RECIPE = "error. EQTRAYMOVECHECK_REP_REFLOW. RECIPE is FAIL.";
        const string ERR_MOVECHK_REP_REFLOW_AUTOPORT = "error. EQTRAYMOVECHECK_REP_REFLOW. PORT is all not IDLE.";
        const string ERR_MOVECHK_REP_REFLOW_SLOTINFO = "error. EQTRAYMOVECHECK_REP_REFLOW. SLOTINFO is not Valid.";
        const string ERR_MOVECHK_REP_REFLOW_TRAYINFO = "error. EQTRAYMOVECHECK_REP_REFLOW. TRAYINFO is not Valid.";
        const string ERR_MOVECHK_REP_REFLOW_LOADER_TRAYINFO = "error. EQTRAYMOVECHECK_REP_REFLOW_LOADER. TRAYINFO is not Valid.";
        const string ERR_MOVECHK_REP_REFLOW_INVALID_EQPTYPE = "error. EQTRAYMOVECHECK_REP_REFLOW. Check EqpId";

        private void Reflow_Loader_Process(string[] words, string vecID)
        {
            Reflow_Loader_Status_Check(words);

            Reflow_Loader_Runmode_Check(words, vecID);

            string reflowPort = Reflow_Loader_Port_Check(words, vecID);

            string[] slotS = Reflow_Loader_Slot_Number_Check(words, reflowPort);

            int traycount = Use_Tray_Check(words);

            string slot = Slot_Data(slotS, traycount, reflowPort);

            if (slot.Length <= 0)
            {
                slot = Slot_Data(slotS, traycount, reflowPort, true);
            }

            Reflow_Loader_slot_port_DataSave(reflowPort, slot, vecID);

            RvSenderList[vecID].RvComm._bSucc = true;
        }
        private void Reflow_Loader_Status_Check(string[] words)
        {
            string loaderstatus = UtilMgr.FindKeyStringToValue(words, "LOADERSTATUS");
            if (loaderstatus != "RUN" && loaderstatus != "IDLE")
            {
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_REFLOW_STATUS);
            }
        }


        private void Reflow_Loader_Runmode_Check(string[] words, string vecID)
        {
            string runmode = UtilMgr.FindKeyStringToValue(words, "RUNMODE");
            if (!string.IsNullOrWhiteSpace(runmode))
                RvSenderList[vecID].RvComm.reflowRunMode = runmode;
            else
            {
                Reflow_Loader_Runmode_Error(words[0]);
            }
        }
        private string Reflow_Loader_Port_Check(string[] words, string vecID)
        {
            string reflowPort = string.Empty;
            if (UtilMgr.FindKeyStringToValue(words, "JOBTYPE") == "UNLOAD")
            {
                reflowPort = RvSenderList[vecID].CurJob.S_SLOT;
            }
            else
            {
                if ((UtilMgr.FindKeyStringToValue(words, "AUTO01") == "IDLE") && (UtilMgr.FindKeyStringToValue(words, "AUTO02") == "IDLE"))
                    reflowPort = RvSenderList[vecID].CurJob.T_SLOT;
                else
                {
                    throw new UtilMgrCustomException(ERR_MOVECHK_REP_REFLOW_AUTOPORT);
                }
            }
            return reflowPort;
        }
        private string[] Reflow_Loader_Slot_Number_Check(string[] words, string reflowPort)
        {
            string slotinfo = UtilMgr.FindKeyStringToValue(words, string.Format("{0}_SLOTINFO", reflowPort));
            slotinfo = Regex.Replace(slotinfo, "[()]", "");
            string[] slotS = slotinfo.Split(',');
            if (slotS.Count() <= 0)
            {
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_REFLOW_SLOTINFO);
            }
            return slotS;
        }
        private void Reflow_Loader_slot_port_DataSave(string reflowPort, string slot, string vecID)
        {
            if (Reflow_Loader_goaltype_Check(RvSenderList[vecID].CurJob.S_EQPID))
            {
                RvSenderList[vecID].CurJob.S_SLOT = reflowPort;
                RvSenderList[vecID].CurJob.S_PORT = slot;
            }
            else if (Reflow_Loader_goaltype_Check(RvSenderList[vecID].CurJob.T_EQPID))
            {
                RvSenderList[vecID].CurJob.T_SLOT = reflowPort;
                RvSenderList[vecID].CurJob.T_PORT = slot;
            }
            else
            {
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_REFLOW_INVALID_EQPTYPE);
            }

            Db.DbUpdate(TableType.PEPSCHEDULE);
        }
        private bool Reflow_Loader_goaltype_Check(string eqpid)
        {
            return (eqpid.Contains("RO") && eqpid.Contains('-') == true);
        }
        private void Reflow_Process(string[] words, string vecID)
        {
            Reflow_RecipeCheck(words);

            Use_Tray_Check(words);

            RvSenderList[vecID].RvComm._bSucc = true;
        }
        private void Reflow_RecipeCheck(string[] words)
        {
            if (UtilMgr.FindKeyStringToValue(words, "RECIPECHECK") != "PASS")
                throw new UtilMgrCustomException(ERR_MOVECHK_REP_REFLOW_RECIPE);
        }

        #endregion
        // EQTRAYMOVECHECK_REP_REFLOW Method End

        // EQTEMPDOWNREQ_REP Method Start
        #region EQTEMPDOWNREQ_REP
        const string ERR_EQTEMPDOWNREQ_REP_STATUS = "error. EQTEMPDOWNREQ_REP. STATUS is not pass";
        private void EQTEMPDOWNREQ_Status_Check(string[] words)
        {
            if (UtilMgr.FindKeyStringToValue(words, "STATUS") != "PASS")
            {
                throw new UtilMgrCustomException(ERR_EQTEMPDOWNREQ_REP_STATUS);
            }
        }
        private void EQTEMPDOWNREQ_Send_List_Check(string[] words, string vecID)
        {
            lock (RvSenderList[vecID].syncTempDown)
            {
                int v = RvSenderList[vecID].lstTempDown.IndexOf(UtilMgr.FindKeyStringToValue(words, "SUBEQPID"));
                if (v >= 0)
                    RvSenderList[vecID].lstTempDown.RemoveAt(v);
            }

            if (RvSenderList[vecID].lstTempDown.Count() == 0)
            {
                RvSenderList[vecID].RvComm._TempDownbSucc = true;
            }
        }

        #endregion
        // EQTEMPDOWNREQ_REP Method End

        // EQTRAYLOADINFO_REP/EQTRAYUNLOADINFO_REP Method Start
        #region EQTRAYLOADINFO_REP/EQTRAYUNLOADINFO_REP
        const string ERR_EQTRAYLOADINFO_REP_STATUS = "error. EQTRAYLOADINFO_REP. STATUS is not pass";
        const string ERR_EQTRAYUNLOADINFO_REP_STATUS = "error. EQTRAYUNLOADINFO_REP. STATUS is not pass";
        private void EQTRAYLOAD_UNLOADINFO_REP_Status_Check(string[] words, string vecID)
        {
            if (UtilMgr.FindKeyStringToValue(words, "STATUS") != "PASS")
            {
                LOAD_UNLOADINFO_Error(words[0]);
            }
            RvSenderList[vecID].RvComm._bSucc = true;
        }
        private void LOAD_UNLOADINFO_Error(string word)
        {
            switch (word)
            {
                case "EQTRAYLOADINFO_REP":
                    throw new UtilMgrCustomException(ERR_EQTRAYLOADINFO_REP_STATUS);
                case "EQTRAYUNLOADINFO_REP":
                    throw new UtilMgrCustomException(ERR_EQTRAYUNLOADINFO_REP_STATUS);
                default:
                    break;
            }
        }

        #endregion
        // EQTRAYLOADINFO_REP/EQTRAYUNLOADINFO_REP Method End

        // EQTRAYUNLOADCOMPLETE/EQTRAYLOADCOMPLETE Method Start
        #region EQTRAYUNLOADCOMPLETE/EQTRAYLOADCOMPLETE
        const string ERR_EQTRAYUNLOADCOMPLETE = "error. EQTRAYUNLOADCOMPLETE. RUNMODE is EMPTY.";
        const string ERR_EQTRAYLOADCOMPLETE = "error. EQTRAYLOADCOMPLETE. RUNMODE is EMPTY.";
        private void LOAD_UNLOADCOMPLETE_Reflow_Loader_Check(string[] words, string vecID)
        {
            string eqpid_chk = UtilMgr.FindKeyStringToValue(words, "EQPID");
            
            // 설비가 LOADER 일때
            if (Reflow_Loader_goaltype_Check(eqpid_chk))
            {
                Reflow_Loader_Runmode_Check(words, vecID);
            }

            RvSenderList[vecID].RvComm._bSucc = true;
        }
        #endregion
        // EQTRAYUNLOADCOMPLETE/EQTRAYLOADCOMPLETE Method End

        // REFLOWRECIPESET_REP Method Start
        #region REFLOWRECIPESET_REP
        const string ERR_REFLOWRECIPESET_REP_STATUS = "error. REFLOWRECIPESET_REP. STATUS is not RUN and IDLE.";
        const string ERR_REFLOWRECIPESET_REP_RUNMODE = "error. REFLOWRECIPESET_REP. RUNMODE is EMPTY.";
        const string ERR_REFLOWRECIPESET_REP_ONLINEREMOTE = "error. REFLOWRECIPESET_REP. RUNMODE is NOT ONLINEREMOTE.";
        const string ERR_REFLOWLOADERINFOSET_REP_STATUS = "error. REFLOWLOADERINFOSET_REP. STATUS is not RUN and IDLE.";
        const string ERR_REFLOWLOADERINFOSET_REP_RUNMODE = "error. REFLOWLOADERINFOSET_REP. RUNMODE is EMPTY.";
        const string ERR_REFLOWLOADERINFOSET_REP_ONLINEREMOTE = "error. REFLOWLOADERINFOSET_REP. RUNMODE is NOT ONLINEREMOTE.";

        private void REFLOWRECIPESET_LOADERINFOSET_REP_Status_Check(string[] words)
        {
            if (UtilMgr.FindKeyStringToValue(words, "STATUS") != "PASS")
            {
                REFLOWRECIPESET_LOADERINFOSET_REP_Status_Error(words[0]);
            }
        }

        private void REFLOWRECIPESET_LOADERINFOSET_REP_Status_Error(string word)
        {
            switch (word)
            {
                case "REFLOWRECIPESET_REP":
                    throw new UtilMgrCustomException(ERR_REFLOWRECIPESET_REP_STATUS);
                case "REFLOWLOADERINFOSET_REP":
                    throw new UtilMgrCustomException(ERR_REFLOWLOADERINFOSET_REP_STATUS);
                default:
                    break;
            }
        }
        private void REFLOWRECIPESET_LOADERINFOSET_REP_Runmode_Check(string[] words, string vecID)
        {
            string runmode = UtilMgr.FindKeyStringToValue(words, "RUNMODE");
            if (!string.IsNullOrWhiteSpace(runmode))
            {
                if (runmode == "ONLINEREMOTE")
                {
                    RvSenderList[vecID].RvComm.reflowRunMode = runmode;
                }
                else
                {
                    REFLOWRECIPESET_REP_Runmode_Not(words[0]);
                }
            }
            else
            {
                REFLOWRECIPESET_REP_Runmode_Error(words[0]);
            }
        }
        private void REFLOWRECIPESET_REP_Runmode_Not(string word)
        {
            switch (word)
            {
                case "REFLOWRECIPESET_REP":
                    throw new UtilMgrCustomException(ERR_REFLOWRECIPESET_REP_ONLINEREMOTE);
                case "REFLOWLOADERINFOSET_REP":
                    throw new UtilMgrCustomException(ERR_REFLOWLOADERINFOSET_REP_ONLINEREMOTE);
                default:
                    break;
            }
        }
        private void REFLOWRECIPESET_REP_Runmode_Error(string word)
        {
            switch (word)
            {
                case "REFLOWRECIPESET_REP":
                    throw new UtilMgrCustomException(ERR_REFLOWRECIPESET_REP_RUNMODE);
                case "REFLOWLOADERINFOSET_REP":
                    throw new UtilMgrCustomException(ERR_REFLOWLOADERINFOSET_REP_RUNMODE);
                default:
                    break;
            }
        }
        #endregion
        // REFLOWRECIPESET_REP Method End

        // EQTRAYMOVEREQ Method Start
        #region EQTRAYMOVEREQ
        private void EQTRAYMOVEREQ_sndMsg_Send(string[] words, string vecID)
        {
            string sndMsg = EQTRAYMOVEREQ_sndMsg_Create(words);

            string newSubject = string.Format("{0}{1}", SndSubjct, UtilMgr.FindKeyStringToValue(words, "EQPID"));

            RvSenderList[vecID].RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
        }
        private string EQTRAYMOVEREQ_sndMsg_Create(string[] words)
        {
            string sndMsg = string.Empty;
            string hdr = string.Format($"HDR=({UtilMgr.FindKeyStringToValue(words, "EQPID")},LH.MPLUS,GARA,TEMP)");
            for (int i = 0; i < words.Count(); i++)
            {
                if (words[i].Contains("HDR="))
                {
                    sndMsg += string.Format($" {hdr}");
                    sndMsg += " STATUS=PASS";
                }
                else if (words[i].Contains("EQTRAYMOVEREQ"))
                {
                    sndMsg += string.Format($" {words[i]}_REP");
                }
                else
                {
                    sndMsg += string.Format($" {words[i]}");
                }
            }
            return sndMsg;
        }

        #endregion
        // EQTRAYMOVEREQ Method End

        // EQTRAYABNORMALWARNING Method Start
        #region EQTRAYABNORMALWARNING
        private void EQTRAYABNORMALWARNING_sndMsg_Send(string[] words, string vecID)
        {
            // EQTRAYABNORMALWARNING의 Data를 가공하여 PopUp 창을 보여준다.
            EQTRAYABNORMALWARNING_PopUp(words, vecID);

            // Data 가공하여 Message 작성
            string sndMsg = EQTRAYABNORMALWARNING_sndMsg_Create(words);

            string newSubject = string.Format("{0}{1}", SndSubjct, UtilMgr.FindKeyStringToValue(words, "EQPID"));

            RvSenderList[vecID].RvMsg_Send(newSubject, sndMsg, MethodBase.GetCurrentMethod().Name);
        }
        private string EQTRAYABNORMALWARNING_sndMsg_Create(string[] words)
        {
            string sndMsg = string.Empty;
            // Header 작성
            string hdr = string.Format($"HDR=({UtilMgr.FindKeyStringToValue(words, "EQPID")},LH.MPLUS,GARA,TEMP)");

            for (int i = 0; i < words.Count(); i++)
            {
                // words에 HDR= 가 포함되어있으면
                if (words[i].Contains("HDR="))
                {
                    // 만들어둔 hdr와 STATUS=PASS를 저장
                    sndMsg += string.Format($" {hdr}");
                    sndMsg += " STATUS=PASS";
                }
                // words에 EQTRAYMOVEREQ가 포함되어있으면
                else if (words[i].Contains("EQTRAYABNORMALWARNING"))
                {
                    // _REP를 붙혀서 저장
                    sndMsg += string.Format($" {words[i]}_REP");
                }
                // 그 외
                else
                {
                    // words에 있는 데이터를 그대로 저장
                    sndMsg += string.Format($" {words[i]}");
                }
            }
            return sndMsg;
        }

        private void EQTRAYABNORMALWARNING_PopUp(string[] words, string vecID)
        {
            // PopUp 창에 Display 할 Message
            string setmsg = string.Empty;
            // EQTRAYABNORMALWARNING Data에서 설비명 가져오기
            string eqpid = UtilMgr.FindKeyStringToValue(words, "EQPID");
            // EQTRAYABNORMALWARNING Data에서 TrayID 가져오기
            string trayid = UtilMgr.FindKeyStringToValue(words, "TRAYID");
            // EQTRAYABNORMALWARNING Data에서 ErrorCode 가져오기
            string errorcode = UtilMgr.FindKeyStringToValue(words, "ERRORCODE");
            // EQTRAYABNORMALWARNING Data에서 ErrorMSG 가져오기
            string errormsg = UtilMgr.FindKeyStringToValue(words, "ERRORMSG");

            // PopUp 창에 Display 할 Message Setup
            setmsg = string.Format($"EQTRAYABNORMALWARNING EQPID={eqpid} TRAYID={trayid} ERRORCODE={errorcode} ERRORMSG={errormsg}");

            // PopUp 창 초기화
            // 창 제목, 창 타입, 버튼 타입
            AlphaMessageForm.FormMessage form
                    = new AlphaMessageForm.FormMessage("", AlphaMessageForm.MsgType.Warn, AlphaMessageForm.BtnType.Cancel);
            // PopUp 창에 Message set
            form.SetMsg(setmsg);
            // PopUp 창 Display
            form.ShowDialog();
        }
        #endregion
        // EQTRAYABNORMALWARNING Method End


    }
}
