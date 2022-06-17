using MPlus.Ref;
using MPlus.Vehicles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPlus.Logic
{
    public class Proc_Atom : Global
    {
        #region singleton POA
        private static volatile Proc_Atom instance;
        private static object syncVsp = new object();
        public static Proc_Atom Init
        {
            get
            {
                if (instance == null)
                {
                    lock (syncVsp)
                    {
                        instance = new Proc_Atom();
                    }
                }
                return instance;
            }
        }
        #endregion

        public bool PROC_ATOM(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit, ProcStep eStep)
        {
            bool bret = false;
            string[] words = e.job.TRAYID.Split(',');   // TRAY COUNT 계산
            int tray_count = words.Count();
            string sendMsg = string.Empty;
            switch (eStep)
            {
                case ProcStep.Pre_Assign:
                    {
                        job_pre_assign(e);          // src job 에서만 발생시킬 것이다.
                    }
                    break;
                case ProcStep.Chk_EqStatus:
                    {                               // 하나의 루틴에서 설비 및 stk 상태를 체크한다. Retry 시도를 포함하고 있는 이유
                        bret = RvSenderList[e.vecID].Chk_EqStatus(e, srcUnit, dstUnit);
                    }
                    break;
                case ProcStep.STK_MOVECHECK_RETRY:
                    {
                        bret = RvSenderList[e.vecID].STKMOVECHECKRETRY(e, srcUnit);
                    }
                    break;
                case ProcStep.Req_EqTempDown:
                    {                               //SYSWIN 만 해당. load 에서만 작업한다
                        bret = RvSenderList[e.vecID].SYSWIN_TempDownSingleRequest(e.job.BATCHID, srcUnit, dstUnit, e.vecID);
                    }
                    break;
                case ProcStep.Tray_MoveInfo:
                    {
                        bret = RvSenderList[e.vecID].TrayMoveInfo(srcUnit.goaltype, e);
                    }
                    break;
                case ProcStep.Tray_WaitMoveComplete:
                    {
                        bret = RvSenderList[e.vecID].WaitTrayMoveComplete(srcUnit.goaltype, e);
                    }
                    break;
                case ProcStep.Tray_UnLoadMove:
                    {
                        bret = RvSenderList[e.vecID].TrayUnLoadMove(srcUnit.goaltype, e);
                    }
                    break;
                case ProcStep.Tray_LoadMove:
                    {
                        bret = RvSenderList[e.vecID].TrayLoadMove(dstUnit.goaltype, e);
                    }
                    break;
                case ProcStep.Tray_UnLoadInfo:
                    {
                        bret = RvSenderList[e.vecID].TrayUnLoadInfo(srcUnit.goaltype, e);
                    }
                    break;
                case ProcStep.Tray_WaitUnloadComplete:
                    {
                        bret = RvSenderList[e.vecID].TrayUnLoadComplete(e);
                    }
                    break;
                case ProcStep.Tray_LoadInfo:
                    {
                        bret = RvSenderList[e.vecID].TrayLoadInfo(dstUnit.goaltype, e);
                    }
                    break;
                case ProcStep.Tray_WaitLoadComplete:
                    {
                        bret = RvSenderList[e.vecID].TrayLoadComplete(e);
                    }
                    break;
                case ProcStep.Tray_JobComplete:
                    {
                        bret = RvSenderList[e.vecID].EQTRAYJOBCOMPCHECK(e);
                    }
                    break;
                case ProcStep.Job_Assign:
                    {
                        bret = job_assign(e);
                    }
                    break;
                case ProcStep.Go_Mobile:
                    {
                        Go_End(e);
                        bret = WaitGoToVehicle(VehicleList[e.vecID]);
                    }
                    break;
                case ProcStep.Job_Start:
                    {
                        Job_Start(e);
                        bret = true;
                    }
                    break;
                case ProcStep.Req_LoadStandby:
                    {
                        bret = RvSenderList[e.vecID].HANDLER_TrayLoadJobStandby(e);
                    }
                    break;
                case ProcStep.Reflow_Recipe_Set:
                    {
                        bret = RvSenderList[e.vecID].ReflowRecipeSet(e);
                    }
                    break;
                case ProcStep.Reflow_LoaderInfo_Set:
                    {
                        bret = RvSenderList[e.vecID].ReflowLoaderInfoSet(e);
                    }
                    break;
                case ProcStep.Job_Cancel:
                    {
                        bret = RvSenderList[e.vecID].JOBCANCEL(e);
                        Job_Cancel(e, bret);
                    }
                    break;
                case ProcStep.Job_Initialization:
                    {                        
                        bret = Job_Init(e);         // Job Initialization 함수, 진행이 어디까지 되었던 상관없이 최초 상태로 되돌리는 함수
                    }
                    break;
                case ProcStep.Err_Job_Initialization:
                    Logger.Inst.Write(e.vecID, CmdLogType.Db, $"작업 {e.job.BATCHID}을 초기화 하지 못했습니다.");
                    bret = false;
                    break;
                case ProcStep.Err_JobAssign:
                    {
                        //e.job.C_VEHICLEID = null;
                        if (e.job.C_state < 8)
                        {
                            e.job.C_state = (int)CmdState.SRC_NOT_ASSIGN;    // 우선순위를 밀어서 ReOrdering 이 필요하겠다
                        }
                        else
                        {
                            e.job.C_state = (int)CmdState.DST_NOT_ASSIGN;
                        }
                        Db.DbUpdate(TableType.PEPSCHEDULE);
                        Db.DbUpdate(false);
                        Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"차량[{e.vecID}]이 명령[{e.cmd};{e.job.BATCHID};]을 수신하지 못했습니다.");
                        bret = false;
                    }
                    break;
                case ProcStep.Err_EqStatus:
                    RvSenderList[e.vecID].MRSM_Send(e.MoveCheck_Fail_unit, "", "", "EQPMOVECHECK_FAIL");
                    Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"ProcStep.Err_EqStatus: MoveCheck Fail");
                    bret = false;
                    break;
                case ProcStep.Err_TrayMoveInfo:
                case ProcStep.Err_TrayUnloadInfo:
                case ProcStep.Err_TrayLoadInfo:
                case ProcStep.Err_WaitTrayMoveComplete:
                case ProcStep.Err_EqTempDown:
                case ProcStep.Err_WaitTrayLoadComplete:
                case ProcStep.Err_WaitTrayUnloadComplete:
                case ProcStep.Err_WaitTrayJobCompCheck:
                case ProcStep.Err_LoadStandby:
                case ProcStep.Err_Reflow_Recipe_Set:
                case ProcStep.Err_Reflow_LoaderInfo_Set:
                case ProcStep.Err_JobComplete:
                case ProcStep.Err_Go_Mobile:
                case ProcStep.Err_TrayUnloadMove:
                case ProcStep.Err_TrayLoadMove:
                case ProcStep.Err_WaitUnLoadInfo:
                case ProcStep.Err_WaitLoadInfo:
                case ProcStep.Err_Job_Start:
                    {
                        bret = false;
                    }
                    break;
            }
            return bret;
        }

        private bool job_pre_assign(SendJobToVecArgs1 e)
        {
            Logger.Inst.Write(e.vecID, CmdLogType.Rv
                , $"job_pre_assign. Src:{e.job.S_EQPID}, Dst:{e.job.T_EQPID}");

            try
            {
                e.vec.C_BATCHID = e.job.BATCHID;
                e.job.C_state = (int)CmdState.PRE_ASSIGN;
                Db.DbUpdate(true, new TableType[] { TableType.PEPSCHEDULE, TableType.VEHICLE });
                Db.DbUpdate(false);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"Exception. job_pre_assign. {ex.Message}\r\n{ex.StackTrace}");
            }
            return false;
        }

        private bool job_assign(SendJobToVecArgs1 e)
        {
            int chager_flag = 0;

            if (e.cmd == "DST" && Db.Peps.Where(p => p.C_VEHICLEID == e.vecID).ToList().Count() > 1)
            {
                chager_flag = 1;
            }
            int tray_count = e.job.TRAYID.Split(',').Count();
            string sendMsg = string.Empty;

            (string eqpslot, string eqpport) = cmd_Check(e);
            string[] slot_Split = null;
            string[] port_Split = null;

            if (eqpslot != null)
                slot_Split = eqpslot.Split(',');
            if (eqpport != null)
                port_Split = eqpport.Split(',');
            eqpslot = string.Empty;
            string reword_GoalName = string.Empty;

            // 2021.11.25
            // MultiJob Check Flag
            // 해당 Flag 확인하여 M6X에서 충전소 이동 여부 판단
            // mgtype 위치에 해당 flag 입력
            //int multiflag = 0;
            //if (Db.Peps.Where(p => p.C_VEHICLEID == e.vecID && p.C_state == 8).ToList().Count() > 1)
            //    multiflag = 1;

            if (e.eqp.goaltype == (int)EqpGoalType.STK)
            {
                if (Cfg.Data.UseRv)
                {
                    if (eqpport.Contains("STACK"))
                    {
                        tray_count = traycount_result(e.job);
                    }

                    for (int i = 0; i < tray_count; i++)
                    {
                        if (eqpslot.Length > 0)
                            eqpslot += ',';

                        eqpslot += slot_add_num(eqpport);
                        eqpslot += ',';
#if false
                        if (Convert.ToInt32(slot_Split[i]) - 1 >= 0)
                            eqpslot += (Convert.ToInt32(slot_Split[(i * 2) + 1]) - 1);
                        else
                            eqpslot += slot_Split[i];
#else
                        eqpslot += i.ToString();
#endif
                    }

                    if (!e.eqp.GOALNAME.Contains("RR4201"))
                        reword_GoalName = e.eqp.GOALNAME;
                    else if (eqpport.Contains("AUTO01"))
                        reword_GoalName = "RR4201-1";
                    else if (eqpport.Contains("AUTO02"))
                        reword_GoalName = "RR4201-2";
                    else if (eqpport.Contains("STACK01"))
                        reword_GoalName = "RR4201-3";
                    else if (eqpport.Contains("STACK02"))
                        reword_GoalName = "RR4201-4";
                    else
                        return false;
                    e.eqp = Db.Units.Where(p => p.GOALNAME == reword_GoalName).First();

                    sendMsg = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};",
                                                e.cmd, e.job.BATCHID, reword_GoalName, e.eqp.goaltype, e.eqp.lds_dist, e.eqp.rf_id,
                                                e.eqp.rf_ch, tray_count, e.job.TRAYID, eqpslot, e.bufslot, chager_flag);
                }
                else
                {
                    sendMsg = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};",
                                           e.cmd, e.job.BATCHID, e.eqp.GOALNAME, e.eqp.goaltype, e.eqp.lds_dist, e.eqp.rf_id,
                                           e.eqp.rf_ch, tray_count, e.job.TRAYID, e.eqpslot, e.bufslot, chager_flag);
                }
            }
            else if (e.eqp.goaltype == (int)EqpGoalType.REFLOW)
            {
                if (Cfg.Data.UseRv)
                {
                    tray_count = traycount_result(e.job);

                    for (int i = 0; i < tray_count; i++)
                    {
                        if (eqpslot.Length > 0)
                            eqpslot += ',';

                        eqpslot += slot_add_num(eqpport);
                        eqpslot += ',';
                        if (Convert.ToInt32(slot_Split[i]) - 1 >= 0)
                            eqpslot += (Convert.ToInt32(slot_Split[(i * 2) + 1]) - 1) < 0 ? 0 : Convert.ToInt32(slot_Split[(i * 2) + 1]) - 1;
                        else
                            eqpslot += slot_Split[i];
                    }

                    if (eqpport.Contains("AUTO01"))
                        reword_GoalName = "RO3303-1";
                    else if (eqpport.Contains("AUTO02"))
                        reword_GoalName = "RO3303-2";
                    else
                        return false;

                    sendMsg = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};",
                                                e.cmd, e.job.BATCHID, reword_GoalName, e.eqp.goaltype, e.eqp.lds_dist, e.eqp.rf_id,
                                                e.eqp.rf_ch, tray_count, e.job.TRAYID, eqpslot, e.bufslot, chager_flag);
                }
                else
                {
                    sendMsg = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};",
                                           e.cmd, e.job.BATCHID, e.eqp.GOALNAME, e.eqp.goaltype, e.eqp.lds_dist, e.eqp.rf_id,
                                           e.eqp.rf_ch, tray_count, e.job.TRAYID, e.eqpslot, e.bufslot, chager_flag);
                }
            }
            else if (e.eqp.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                if (Cfg.Data.UseRv)
                {
                    for (int i = 0; i < port_Split.Count(); i++)
                    {
                        if (eqpslot != null && eqpslot.Length > 0)
                            eqpslot += ',';

                        eqpslot += slot_add_num(port_Split[i]);
                        eqpslot += ',';
                        eqpslot += '0';
                    }

                    tray_count = port_Split.Count();

                    sendMsg = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};",
                                                e.cmd, e.job.BATCHID, e.eqp.GOALNAME, e.eqp.goaltype, e.eqp.lds_dist, e.eqp.rf_id,
                                                e.eqp.rf_ch, tray_count, e.job.TRAYID, eqpslot, e.bufslot, chager_flag);
                }
                else
                {
                    sendMsg = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};",
                                           e.cmd, e.job.BATCHID, e.eqp.GOALNAME, e.eqp.goaltype, e.eqp.lds_dist, e.eqp.rf_id,
                                           e.eqp.rf_ch, tray_count, e.job.TRAYID, e.eqpslot, e.bufslot, chager_flag);
                }
            }
            else
            {
                if (e.eqp.goaltype == (int)EqpGoalType.BUFFER_STK)
                    tray_count = port_Split.Count();
                sendMsg = string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8};{9};{10};{11};",
                                            e.cmd, e.job.BATCHID, e.eqp.GOALNAME, e.eqp.goaltype, e.eqp.lds_dist, e.eqp.rf_id,
                                            e.eqp.rf_ch, tray_count, e.job.TRAYID, e.eqpslot, e.bufslot, chager_flag);
            }
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, sendMsg);
            return SendJobToVehicle(VehicleList[e.vecID], sendMsg);
        }
        
        private string slot_add_num(string eqpport)
        {
            if (eqpport.Contains("LOADER1") || eqpport.Contains("AUTO01"))
                return 1.ToString();
            else if (eqpport.Contains("LOADER2") || eqpport.Contains("AUTO02"))
                return 2.ToString();
            else if (eqpport.Contains("GOOD") || eqpport.Contains("STACK01"))
                return 3.ToString();
            else if (eqpport.Contains("FAIL") || eqpport.Contains("STACK02"))
                return 4.ToString();
            return 0.ToString();
        }

        public int traycount_result(pepschedule job)
        {
            return (job.TRAYID.Split(',').Count() / 10) + ((job.TRAYID.Split(',').Count() % 10 > 0) ? 1 : 0);
        }

        private void Go_End(SendJobToVecArgs1 e)
        {
            VehicleList[e.vecID].SendMessageToVehicle(string.Format("GO;{0};", e.job.BATCHID));
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, string.Format("GO;{0};", e.job.BATCHID));
            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"차량[{e.vecID}]에 명령[{e.cmd};{e.job.BATCHID};]을 전달했습니다.");
        }
        private void Job_Start(SendJobToVecArgs1 e)
        {
            VehicleList[e.vecID].SendMessageToVehicle(string.Format("JOB_START;"));
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, string.Format("JOB_START;"));
        }

        private void Job_Cancel(SendJobToVecArgs1 e, bool bret)
        {
            if (bret)
            {
                if (RvSenderList[e.vec.ID].IsMulti)
                {
                    pepschedule cancel_peps = RvSenderList[e.vec.ID].MultiList.Where(p => p.C_state != null).OrderBy(p => p.BATCHID).First();

                    if (cancel_peps.C_state < 8)
                    {
                        foreach (var v in RvSenderList[e.vec.ID].MultiList)
                        {
                            if (v.C_state == null || v.C_state < 8)
                            {
                                v.C_state = null;
                                v.C_isChecked = null;
                                v.C_priority = null;
                                v.C_srcArrivingTime = null;
                                v.C_srcAssignTime = null;
                                v.C_srcStartTime = null;
                                v.C_srcFinishTime = null;
                                v.C_dstArrivingTime = null;
                                v.C_dstAssignTime = null;
                                v.C_dstStartTime = null;
                                v.C_dstFinishTime = null;
                                v.C_VEHICLEID = null;
                                v.C_bufSlot = null;
                                v.S_PORT = null;
                                v.T_PORT = null;
                                v.MULTIID = null;
                            }
                        }
                    }
                    else
                    {
                        foreach (var v in RvSenderList[e.vec.ID].MultiList)
                        {
                            v.C_state = 8;
                            v.C_isChecked = null;
                            v.C_dstArrivingTime = null;
                            v.C_dstAssignTime = null;
                            v.C_dstStartTime = null;
                            v.C_dstFinishTime = null;
                        }
                    }
                }
                else
                {
                    if (e.job.C_state == null || e.job.C_state < 8)
                    {
                        e.job.C_state = null;
                        e.job.C_isChecked = null;
                        e.job.C_priority = null;
                        e.job.C_srcArrivingTime = null;
                        e.job.C_srcAssignTime = null;
                        e.job.C_srcStartTime = null;
                        e.job.C_srcFinishTime = null;
                        e.job.C_dstArrivingTime = null;
                        e.job.C_dstAssignTime = null;
                        e.job.C_dstStartTime = null;
                        e.job.C_dstFinishTime = null;
                        e.job.C_VEHICLEID = null;
                        e.job.C_bufSlot = null;
                        e.job.S_PORT = null;
                        e.job.T_PORT = null;
                        e.job.MULTIID = null;
                    }
                    else
                    {
                        e.job.C_state = 8;
                        e.job.C_isChecked = null;
                        e.job.C_dstArrivingTime = null;
                        e.job.C_dstAssignTime = null;
                        e.job.C_dstStartTime = null;
                        e.job.C_dstFinishTime = null;
                    }
                }
                Db.DbUpdate(TableType.PEPSCHEDULE);

                RvSenderList[e.vec.ID].RvComm.jobcancel_check = true;
                RvSenderList[e.vec.ID].IsMulti = false;
                if (RvSenderList[e.vec.ID].MultiList != null)
                    RvSenderList[e.vec.ID].MultiList.Clear();

                RvSenderList[e.vec.ID].CurJob = null;
                RvSenderList[e.vec.ID].HandlerStandby = false;
                //
            }
        }


        private bool Job_Init(SendJobToVecArgs1 e)
        {
            // 선택된 Job을 진행 상황에 상관없이 초기화
            try
            {
                e.job.C_state = null;
                e.job.C_isChecked = null;
                e.job.C_priority = null;
                e.job.C_srcArrivingTime = null;
                e.job.C_srcAssignTime = null;
                e.job.C_srcStartTime = null;
                e.job.C_srcFinishTime = null;
                e.job.C_dstArrivingTime = null;
                e.job.C_dstAssignTime = null;
                e.job.C_dstStartTime = null;
                e.job.C_dstFinishTime = null;
                e.job.C_VEHICLEID = null;
                e.job.C_bufSlot = null;
                e.job.S_PORT = null;
                e.job.T_PORT = null;
                e.job.MULTIID = null;

                Db.DbUpdate(TableType.PEPSCHEDULE);
                return true;
            }
            catch (Exception exp)
            {
                return false;
            }
        }
    

        private (string , string) cmd_Check(SendJobToVecArgs1 e)
        {
            string eqpslot = string.Empty;
            string eqpport = string.Empty;
            if (e.cmd == "SRC")
            {
                eqpslot = e.job.S_PORT;
                eqpport = e.job.S_SLOT;
            }
            else
            {
                eqpslot = e.job.T_PORT;
                eqpport = e.job.T_SLOT;
            }
            return (eqpslot, eqpport);
        }
        private bool SendJobToVehicle(VehicleEntity vec, string jobMsg)
        {             
            vec.jobResponse = false;

            RvSenderList[vec.Id].RvComm.jobcancel_check = false;
            Task<bool> bret = vec.SendMessageToVehicle(jobMsg);

            if (!bret.Result)
            {
                Logger.Inst.Write(vec.Id, CmdLogType.Comm, $"Message Not Send : {jobMsg}");
                return false;
            }

            int cnt = 0;
            while (cnt++ < 3000)
            {
                if (vec.jobResponse)
                {
                    return true;
                }

                if (RvSenderList[vec.Id].RvComm.jobcancel_check)
                {
                    return false;
                }
                Thread.Sleep(20);
            }
            return false;
        }

        private bool WaitGoToVehicle(VehicleEntity vec)
        {
            vec.goReset();
            RvSenderList[vec.Id].RvComm.jobcancel_check = false;
            int cnt = 0;
            while (cnt++ < 90000)
            {
                if (vec.goResponse)
                {
                    vec.goRetry_count = 0;
                    return true;
                }

                if (RvSenderList[vec.Id].RvComm.jobcancel_check)
                {
                    vec.goReset();
                    return false;
                }

                if (vec.goReSend)
                {
                    vec.goReSend = false;
                    cnt = 0;
                }

                if (vec.goRetry_Fail)
                {
                    vec.goRetry_count = 0;
                    vec.goRetry_Fail = false;
                    return false;
                }
                Thread.Sleep(20);
            }
            Logger.Inst.Write(vec.Id, CmdLogType.Rv, $"Exception. GO_END Recv Time Out");
            return false;
        }
    }
}
