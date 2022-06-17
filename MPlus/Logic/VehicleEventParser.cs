using MPlus.Ref;
using MPlus.Vehicles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace MPlus.Logic
{
    public class VehicleEventParser : Global
    {
        #region singleton VEP
        private static volatile VehicleEventParser instance;
        private static object syncVsp = new object();
        public static VehicleEventParser Init
        {
            get
            {
                if (instance == null)
                {
                    lock (syncVsp)
                    {
                        instance = new VehicleEventParser();
                    }
                }
                return instance;
            }
        }
        #endregion
        private Configuration _cfg = Configuration.Init;
        private DbHandler _db = DbHandler.Inst;
        private Logger _log = Logger.Inst;

        public VehicleEventParser()
        {
        }
        public void Vehicle_OnRecvMsg(object sender, RecvMsgArgs e)
        {
            if (sender is VehicleEntity vec)
            {
                var targetVec = Db.Vechicles.Where(p => p.ID == vec.Id).Single();

                switch (e.Cmd)
                {
                    case Cmd4Vehicle.STATUS:
                        targetVec_Update(e, ref targetVec);

                        if (targetVec_Status_Change(e, ref targetVec, vec))
                        {
                            Logger.Inst.Write(vec.Id, CmdLogType.Comm, $"Vehicle State Change Event[{vec.Id}] : {e.Cmd}/{e.Status.state}");
                        }
                        Db.DbUpdate(TableType.VEHICLE);
                        Vehicle_Auto_Resume_Send(e, vec);
                        _MapDraw.ChangeStatus(vec.Id, new System.Drawing.Point(e.Status.posX, e.Status.posY), e.Status.angle, e.Status.charge);
                        break;
                    case Cmd4Vehicle.ERROR:
                        switch (e.ErrState.state)
                        {
                            case 0: // Clear
                                AlarmControl(false, vec.Id, e.ErrState.ErrCode);
                                break;
                            case 1: // Occur
                                AlarmControl(true, vec.Id, e.ErrState.ErrCode);
                                break;
                            default: break;
                        }
                        Db.DbUpdate(TableType.ALARM);
                        break;
                    case Cmd4Vehicle.JOB:
                        {
                            var targetCmd = Db.Peps.Where(p => p.BATCHID == e.JobState.batchID).FirstOrDefault();
                            if (targetCmd == null)
                            {
                                Logger.Inst.Write(vec.Id, CmdLogType.Db, $"차량({vec.Id})으로부터 알수 없는 작업 이름이 수신되었습니다. {e.JobState.batchID}");
                                return;
                            }

                            Logger.Inst.Write(vec.Id, CmdLogType.Db, $"Vehicle Job Event[{vec.Id}] : {e.Cmd}/{e.JobState.state}");

                            List<vehicle_part> targetPart = new List<vehicle_part>();
                            targetPart = Db.VecParts.Where(p => p.VEHICLEID == targetVec.ID).ToList();


                            switch (e.JobState.state)
                            {
                                case VehicleCmdState.ASSIGN: // 비클 할당 완료.
                                    vec.jobResponse = true;
                                    {
                                        string compareStr = (targetCmd.C_srcFinishTime != null) ? targetCmd.T_EQPID : targetCmd.S_EQPID;
                                        var targetPos = Db.Units.Where(p => p.ID == compareStr).FirstOrDefault();
                                        _MapDraw.ChangeStatus(vec.Id, targetPos.GOALNAME, targetCmd.BATCHID, $"{targetCmd.S_EQPID} => {targetCmd.T_EQPID}");
                                    }
                                    break;
                                case VehicleCmdState.GO_END: // 비클 설비 앞 도착 (alive 후 PIO 동작 전)
                                    vec.goResponse = true;
                                    break;
                                default:
                                    break;
                            }

                            if (!Jobstate_Go_End(e) && !Jobstate_None(e))
                            {
                                EventDbUpdate(JobProcList[vec.Id].jobType, e.JobState, targetVec, targetCmd, targetPart);

                                if (Jobstate_Trans_End(e))
                                {
                                    string msg = JOB_msg_create(e);

                                    RvSenderList[vec.Id].MRSM_Send(vec.Id, e.recvMsg.ToUpper().Split(';')[2], e.JobState.batchID, msg);
                                }
                                else if (Jobstate_Trans_Comp(e))
                                    RvSenderList[vec.Id].MRSM_Send(vec.Id, e.recvMsg.ToUpper().Split(';')[2], e.JobState.batchID, e.JobState.all);
                                else
                                    RvSenderList[vec.Id].MRSM_Send(vec.Id, e.recvMsg.ToUpper().Split(';')[2], e.JobState.batchID);

                            }
                        }

                        break;
                    case Cmd4Vehicle.GOAL_LD: // Goal 리스트를 비클이 M+에게 물어봄
                    case Cmd4Vehicle.GOAL_UL: // Goal 리스트를 비클이 M+에게 물어봄
                        {
                            var goalList = Db.Units.Select(p => p.GOALNAME).ToList();
                            var goalListStr = "";
                            foreach (var goals in goalList)
                            {
                                goalListStr += $"{goals};";
                            }
                            var sendGoalList = $"GOAL_LD;{goalList.Count};{goalListStr}";
                            vec.SendMessageToVehicle(sendGoalList);
                        }
                        break;
                    case Cmd4Vehicle.SCAN:
                        {
                            vehicle_part targetPart = new vehicle_part();
                            targetPart = Db.VecParts.Where(p => p.VEHICLEID == e.Scan.vehicleid).SingleOrDefault();
                            ScanProcess(targetVec, targetPart, e.Scan);
                        }
                        break;
                    case Cmd4Vehicle.GOAL_FAIL:
                        {
                            string eqpid = e.recvMsg.Split(';')[1];

                            if (eqpid != "")
                            {
                                if (vec.goRetry_count < Cfg.Data.goRetryMax)
                                {
                                    _MainHandler.StopVehicle(vec.Id);
                                    System.Threading.Thread.Sleep(1000);
                                    VehicleList[vec.Id].SendMessageToVehicle(string.Format("GO;{0};", RvSenderList[vec.Id].CurJob.BATCHID));
                                    Logger.Inst.Write(vec.Id, CmdLogType.Rv, string.Format("GO;{0};", RvSenderList[vec.Id].CurJob.BATCHID));
                                    Logger.Inst.Write(vec.Id, CmdLogType.Comm, $"차량[{vec.Id}]에 명령[{JobProcList[vec.Id].jobType};{RvSenderList[vec.Id].CurJob.BATCHID};]을 재 전달했습니다.");
                                    vec.goReSend = true;
                                    vec.goRetry_count++;
                                }
                                else
                                {
                                    vec.goRetry_Fail = true;
                                    Logger.Inst.Write(vec.Id, CmdLogType.Comm, $"차량[{vec.Id}]에 명령[{JobProcList[vec.Id].jobType};{RvSenderList[vec.Id].CurJob.BATCHID};] 재시도 횟수를 초과하였습니다. {vec.goRetry_count.ToString()}/{Cfg.Data.goRetryMax.ToString()}");
                                }
                            }
                            else
                            {
                                _MainHandler.StopVehicle(vec.Id);
                                System.Threading.Thread.Sleep(1000);
                                VehicleList[vec.Id].SendMessageToVehicle(string.Format("CHARGE;"));
                                Logger.Inst.Write(vec.Id, CmdLogType.Rv, string.Format("CHARGE"));
                                Logger.Inst.Write(vec.Id, CmdLogType.Comm, $"차량[{vec.Id}]에 명령[CHARGE;]을 재 전달했습니다.");
                            }
                        }
                        break;
                    case Cmd4Vehicle.None:
                    default: break;
                }

            }
        }
        /// <summary>
        /// alarm 을 db 에 설정하거나 삭제한다
        /// </summary>
        /// <param name="isSet">false이면 삭제, true이면 추가</param>
        /// <param name="vecId">EQPID OR VEHICLEID</param>
        /// <param name="errorCode">code</param>
        public void AlarmControl(bool isSet, string vecId, int errorCode)
        {
            if (isSet)
            {
                var alrmDf = Db.AlarmsDef.Where(p => p.code == errorCode).SingleOrDefault();
                var alrmDb = Db.Alarms.Where(p => p.ID == vecId && p.code == errorCode && p.releaseTime == null).SingleOrDefault();
                if (alrmDf == null)
                {
                    Console.WriteLine("정의 되지 않은 알람 code");
                    alrmDf = Db.AlarmsDef.Where(p => p.code == 999).SingleOrDefault();

                    Logger.Inst.Write(vecId, CmdLogType.All, log_msg(dbalarm_create(alrmDf, vecId, errorCode)));
                }
                else if (alrmDb == null)
                {
                    Console.WriteLine("발생 기록이 확인되지 않는 alarm");

                    Logger.Inst.Write(vecId, CmdLogType.All, log_msg(dbalarm_create(alrmDf, vecId, errorCode)));
                }
            }
            else
            {
                var dbAlarm = Db.Alarms.Where(p => p.ID == vecId && p.releaseTime == null).ToList();

                if (dbAlarm != null)
                {
                    if (dbAlarm.Count > 0)
                    {
                        for (int i = dbAlarm.Count; i > 0; i--)
                        {
                            dbAlarm[i - 1].releaseTime = DateTime.Now;
                            Db.CopyAlarmToHistory(dbAlarm[i - 1].idx);
                            Db.Delete(dbAlarm[i - 1]);
                        }
                        Db.DbUpdate(true, new TableType[] { TableType.ALARM });
                        Logger.Inst.Write(vecId, CmdLogType.All, log_msg(dbAlarm[0]));
                    }
                }
            }
        }

        private alarm dbalarm_create(alarm_define alrmDf, string vecId, int errorCode)
        {
            alarm dbalarm = new alarm
            {
                ID = vecId,
                code = errorCode,
                msg = alrmDf.msg,
                level = alrmDf.level,
                eventTime = DateTime.Now,
                releaseTime = null,
            };
            Db.Add(dbalarm);
            return dbalarm;
        }
        private string log_msg(alarm dbalarm)
        {
            return string.Format($"VEHICLE_ID={dbalarm.ID} CODE={dbalarm.code} MSG={dbalarm.msg} LEVEL={dbalarm.level} EVENT_TIME={dbalarm.eventTime} RELAEASE_TIME={dbalarm.releaseTime}");
        }

        private void targetVec_Update(RecvMsgArgs e, ref vehicle targetVec)
        {
            targetVec.loc_x = e.Status.posX;
            targetVec.loc_y = e.Status.posY;
            targetVec.C_loc_th = e.Status.angle;
            targetVec.C_mode = (int)e.Status.mode;
            targetVec.C_chargeRate = e.Status.charge;
        }
        private bool targetVec_Status_Change(RecvMsgArgs e, ref vehicle targetVec, VehicleEntity vec)
        {
            if (targetVec.C_state != (int)e.Status.state)
            {
                targetVec.C_state = (int)e.Status.state;
                if (e.Status.state == VehicleState.NOT_ASSIGN)
                {
                    if (DoubleMoveJob != null && DoubleMoveJob.ContainsKey(targetVec.ID) && !string.IsNullOrEmpty(DoubleMoveJob[targetVec.ID]))
                    {
                        targetVec.C_BATCHID = DoubleMoveJob[targetVec.ID];
                        DoubleMoveJob.Remove(targetVec.ID);
                    }
                    //else
                    //{
                    //    targetVec.C_BATCHID = "";
                    //    targetVec.C_lastArrivedUnit = string.Empty;
                    //    _MapDraw.ChangeStatus(vec.Id, string.Empty);                        
                    //}
                }
                string msg = Status_msg_create(e);
                RvSenderList[vec.Id].MRSM_Send(vec.Id, e.recvMsg.ToUpper().Split(';')[4], "", msg);
                return true;

            }
            return false;
        }
        private void Vehicle_Auto_Resume_Send(RecvMsgArgs e, VehicleEntity vec)
        {
            if (VehicleList[vec.Id].controllerState == ControllerState.AUTO && e.Status.mode == VehicleMode.MANUAL)
                VehicleList[vec.Id].SendResumeCmd();
        }
        private bool Jobstate_Go_End(RecvMsgArgs e)
        {
            return e.JobState.state == VehicleCmdState.GO_END;
        }
        private bool Jobstate_Trans_End(RecvMsgArgs e)
        {
            return e.JobState.state == VehicleCmdState.TRANS_END;
        }
        private bool Jobstate_Trans_Comp(RecvMsgArgs e)
        {
            return e.JobState.state == VehicleCmdState.TRANS_COMPLETE;
        }
        private bool Jobstate_None(RecvMsgArgs e)
        {
            return e.JobState.state == VehicleCmdState.None;
        }
        private string Status_msg_create(RecvMsgArgs e)
        {
            return e.recvMsg.ToUpper().Split(';')[5] + ";" + e.recvMsg.ToUpper().Split(';')[6]; ;
        }
        private string JOB_msg_create(RecvMsgArgs e)
        {
            string result = string.Empty;

            for (int i = 3; i < 8; i++)
            {
                if (i != 3)
                    result += ";";

                result += e.recvMsg.ToUpper().Split(';')[i];
            }

            return result;
        }
        private void EventParser_OnDeleteCmd(string e)
        {
            _MainHandler.DeleteCmd(e);
        }

        private void EventParser_OnTransEnd(EventArgsTransEnd e)
        {
            RvSenderList[e.vecId].OnTransEnd(e);
        }
        public async Task EventDbUpdate(string jobtype, VecJobStatus jobst, vehicle vec, pepschedule cmd, List<vehicle_part> parts)
        {
            switch (jobst.state)
            {
                case VehicleCmdState.None:
                    break;
                case VehicleCmdState.ASSIGN:
                    await AssignProcess(vec, cmd);
                    break;
                case VehicleCmdState.ENROUTE:
                    await EnrouteProcess(vec, cmd);
                    break;
                case VehicleCmdState.ARRIVED:
                    await ArriveProcess(vec, cmd);
                    break;
                case VehicleCmdState.PIO_START:
                    await PIOStartPorcess(jobtype, vec, cmd);
                    break;
                case VehicleCmdState.TRANS_START:
                    await TransferStartPorcess(jobtype, vec, cmd);
                    break;
                case VehicleCmdState.TRANS_ERROR:
                    await TransferErrorPorcess(jobtype, vec, cmd);
                    break;
                case VehicleCmdState.TRANS_BEGIN:
                    await TransferBeginProcess(jobtype, vec, cmd);
                    break;
                case VehicleCmdState.TRANS_END:
                    await TransferEndProcess(jobtype, vec, cmd, parts, jobst);
                    break;
                case VehicleCmdState.TRANS_COMPLETE:
                    await TransferCompleteProcess(jobtype, vec, cmd, parts, jobst);
                   break;
                default:
                    break;
            }
        }

        private async Task AssignProcess(vehicle vec, pepschedule cmd)
        {
            await Task.Delay(1);
            if (cmd.C_srcFinishTime == null)    //src
            {
                vec.C_BATCHID = cmd.BATCHID;
                vec.C_state = (int)VehicleState.PARKED;
                vec.C_lastArrivedUnit = cmd.S_EQPID;

                cmd.C_VEHICLEID = vec.ID;
                cmd.C_state = (int)CmdState.ASSIGN;
                //cmd.C_srcAssignTime = DateTime.Now;
                _db.DbUpdate(true, new TableType[] { TableType.PEPSCHEDULE, TableType.VEHICLE });
                await Task.Delay(100);
                _log.Write(vec.ID, CmdLogType.Db, $"{CollectionEvent.VehicleAssigned.ToString()} : {cmd.BATCHID}");
            }
            else
            {
                vec.C_BATCHID = cmd.BATCHID;
                vec.C_state = (int)VehicleState.PARKED;
                vec.C_lastArrivedUnit = cmd.T_EQPID;

                if(cmd.C_VEHICLEID != vec.ID)
                {
                    Debug.WriteLine($"Warning: oldVec={cmd.C_VEHICLEID}, curVec={vec.ID}");
                    cmd.C_VEHICLEID = vec.ID;
                }                
                cmd.C_state = (int)CmdState.DEPARTED;
                cmd.C_dstAssignTime = DateTime.Now;
                _db.DbUpdate(true, new TableType[] { TableType.PEPSCHEDULE, TableType.VEHICLE });

                _log.Write(vec.ID, CmdLogType.Db, $"{CollectionEvent.VehicleDeparted.ToString()} : {cmd.BATCHID}");
            }
        }

        private async Task EnrouteProcess(vehicle vec, pepschedule cmd)
        {
            await Task.Delay(1);
            if (cmd.C_srcFinishTime == null)
            {
                cmd.C_state = (int)CmdState.SRC_ENROUTE;
            }
            else
            {
                cmd.C_state = (int)CmdState.DST_ENROUTE;
            }
            _db.DbUpdate(TableType.PEPSCHEDULE);
        }

        private async Task ArriveProcess(vehicle vec, pepschedule cmd)
        {
            await Task.Delay(1);
            if (cmd.C_srcFinishTime == null)
            {
                cmd.C_srcArrivingTime = DateTime.Now;
                cmd.C_state = (int)CmdState.SRC_ARRIVED;

                _db.DbUpdate(TableType.PEPSCHEDULE);

                SrcUnitArriveEventReport(vec, cmd);
            }
            else
            {
                cmd.C_dstArrivingTime = DateTime.Now;
                cmd.C_state = (int)CmdState.DST_ARRIVED;

                _db.DbUpdate(TableType.PEPSCHEDULE);

                DestUnitArriveEventReport(vec, cmd);
            }
        }

        private void DestUnitArriveEventReport(vehicle vec, pepschedule cmd)
        {
            _log.Write(vec.ID, CmdLogType.Db, $"{CollectionEvent.VehicleArrived_dst.ToString()} : {cmd.ID}");
        }

        private void SrcUnitArriveEventReport(vehicle vec, pepschedule cmd)
        {
            _log.Write(vec.ID, CmdLogType.Db, $"{CollectionEvent.VehicleArrived_src.ToString()} : {cmd.ID}");
        }

        private async Task PIOStartPorcess(string jobtype, vehicle vec, pepschedule cmd)
        {
            await Task.Delay(1);
            if (jobtype.CompareTo("SRC") == 0)
            {
                cmd.C_srcStartTime = DateTime.Now;
            }
            else
            {
                cmd.C_dstStartTime = DateTime.Now;
            }
            _db.DbUpdate(TableType.PEPSCHEDULE);
        }

        private async Task TransferStartPorcess(string jobtype, vehicle vec, pepschedule cmd)
        {
            await Task.Delay(1);
            if (jobtype.CompareTo("SRC") == 0)
            {
                cmd.C_state = (int)CmdState.SRC_START;
            }
            else
            {
                cmd.C_state = (int)CmdState.DST_START;
            }
            _db.DbUpdate(TableType.PEPSCHEDULE);
        }

        private async Task TransferErrorPorcess(string jobtype, vehicle vec, pepschedule cmd)
        {
            await Task.Delay(1);
            cmd.C_state = (int)CmdState.UR_ERROR;
            _db.DbUpdate(TableType.PEPSCHEDULE);
        }
        private async Task TransferBeginProcess(string jobtype, vehicle vec, pepschedule cmd)
        {
            await Task.Delay(1);
            if (jobtype.CompareTo("SRC") == 0)
            {
                cmd.C_state = (int)CmdState.SRC_BEGIN;
            }
            else
            {
                cmd.C_state = (int)CmdState.DST_BEGIN;
            }
            _db.DbUpdate(TableType.PEPSCHEDULE);

            _log.Write(vec.ID, CmdLogType.Db, $"{CollectionEvent.VehicleDepositStarted.ToString()} : {cmd.ID}");
        }

        private async Task TransferEndProcess(string jobtype, vehicle vec, pepschedule cmd, List<vehicle_part> parts, VecJobStatus jobst)
        {
            bool FindTrayIdx(ref int idx)
            {
                string[] tray_split = cmd.TRAYID.Split(',');
                for (int i = 0; i < tray_split.Length; i++)
                {   if (jobst.trayid == tray_split[i])
                    {   idx = i;
                        return true;
                    }
                }
                return false;
            }                       

            await Task.Delay(1);

            try
            {
                /// Robot이 Tray 1장에 대한 Load 또는 Unload 작업 완료 시 TC에서 해당 Tray가 Load 또는 Unload가 완료 되었는지 확인을 하기 위해
                /// Robot에서 Trans_End Message를 Mplus가 받으면 Mplus에서 TC로 EQTRAYLOADED 또는 EQTRAYUNLOADED Message를 전송한다.
                /// EQTRAYLOADED 또는 EQTRAYUNLOADED Message에서 요구하는 Data는 EQPID, SUBEQPID, TRAYID, SLOTID, STEPID, JOBTYPE, EXECUTE_TIME 이다.
                /// Single Job을 진행 할 때는 pepschedule에 Job이 있어 해당 Job에서 Data를 가공하여 전송
                /// Multi Job의 경우 pepschedule과 pepschedule_history를 확인해야하는 상황이 발생한다.
                /// Multi Job으로 묶을 때 기존 Job을 유지하는 경우, 기존 Job을 pepschedule_history로 넘기는 경우가 있으며
                /// 해당 Tray에 대한 정확한 값을 가져오기 위해서는 기존의 Job에서 해당 Tray의 Data를 찾아야하므로
                /// pepschedule과 pspschedule_history에서 해당 기존 Job을 찾는 행위가 필요하다.
                /// DB.FindMultiId 함수에서 기존 Job을 찾는 행위를 진행하며 각각 cmd_sec와 cmd_history에 해당 Job을 저장한다.
                /// Tray에 대한 StepID는 Job에서 하나만 있기때문에
                /// cmd_sec와 cmd_history에서 Tray가 포함된 Job이 확인되면 지금 진행중인 Jobtype(SRC/DST)에 따라 step_id에 S_StepID 또는 T_StepID를 저장하고 이후 찾는 로직은 무시한다.
                /// 만약 DB.FindMultid 함수에서 해당하는 Job을 찾지 못하였으면 현재 진행중인 Job에 있는 Data를 활용한다.

                /// 사양서에 맞춘 실제 Data
                /// EQTRAYUNLOADED HDR=(RR4201,LH.MPLUS,GARA,TEMP) EQPID=RR4201 SUBEQPID=RR4201-1 TRAYID=(R0N0) SLOTID=(AUTO01.LP) STEPID=F1902303_HTDR3_4320_1 JOBTYPE=UNLOAD EXECUTETIME=1571803200
                /// EQTRAYLOADED HDR=(RO1504,LH.MPLUS,GARA,TEMP) EQPID=RO1504 SUBEQPID=RO1504-2 TRAYID=(R0N0) SLOTID=(A4) STEPID=F1902303_HTDR3_4320_1 JOBTYPE=LOAD EXECUTETIME=1571803200

                ///MultiJob일 때 현재 진행중인 Job의 DB Data (변수명 cmd)
                ///ID       MULTIID           BATCHID               S_EQPID     S_PORT S_SLOT       T_EQPID     T_PORT T_SLOT          _mgtype TRAYID                 WORKTYPE TRANSFERTYPE WINDOW_TIME            EXECUTE_TIME  REAL_TIME     STATUS LOT_NO                 QTY            STEPID                                                               S_STEPID                                                             T_STEPID                                                             URGENCY FLOW_STATUS _VEHICLEID   _bufSlot             _state _srcAssignTime         _srcArrivingTime _srcStartTime          _srcFinishTime         _dstAssignTime         _dstArrivingTime       _dstStartTime          _dstFinishTime         _isChecked _priority DOWNTEMP EVENT_DATE             ORDER     
                ///'30206', 'MI_31064602588', 'MDST_31064602588_0', 'RR4201-1', NULL,  'AUTO01.LP', 'RO1502-2', NULL,  'A4,A1,A9,A10', NULL,   'R0N0,R183,R197,R198', 'I',     'TRAY',      '2019-07-31 07:00:00', '1564524000', '1564524000', NULL,  '1111,2222,3333,4444', '15,10,12,12', 'F1902303_HTDR3_4320_1,F1902303_HTDR3_4320_2,F1902303_HTDR3_4320_3', 'F1902303_HTDR3_4320_1,F1902303_HTDR3_4320_2,F1902303_HTDR3_4320_3', 'F1902303_HTDR3_4320_1,F1902303_HTDR3_4320_2,F1902303_HTDR3_4320_3', 'HOT',  'IDLE',     'VEHICLE01', '4,000,4,001,4,002', '15',  '2019-07-31 06:54:53', NULL,            '2019-07-31 06:56:48', '2019-07-31 07:05:28', '2019-07-31 07:14:25', NULL,                  '2019-07-31 07:17:27', '2019-07-31 07:22:26', '1',       '2',      NULL,    '2019-07-31 06:30:00', '1'

                ///MultiJob으로 묶기 전 Job의 DB Data
                ///'30200', 'MI_31064602588', 'PI_1907310630000',   'RR4201-1', NULL,  'AUTO01.LP', 'RO1502-2', NULL,  'A4',           NULL,   'R0N0',                'I',     'TRAY',      '2019-07-31 07:00:00', '1564524000', '1564524000', NULL,  '1111',                '15',          'F1902303_HTDR3_4320_1',                                             'F1902303_HTDR3_4320_1',                                             'F1902303_HTDR3_4320_1',                                             'HOT',  'IDLE',     NULL,        NULL,                NULL,  NULL,                  NULL,            NULL,                  NULL,                  NULL,                  '2019-07-31 06:46:02', NULL,                  NULL,                  '1',       '2',      NULL,    '2019-07-31 06:30:00', '1'
                ///'30202', 'MI_31064602588', 'PI_1907310630001',   'RR4201-1', NULL,  'AUTO01.LP', 'RO1502-2', NULL,  'A1',           NULL,   'R183',                'I',     'TRAY',      '2019-07-31 07:00:00', '1564524000', '1564524000', NULL,  '2222',                '10',          'F1902303_HTDR3_4320_2',                                             'F1902303_HTDR3_4320_2',                                             'F1902303_HTDR3_4320_2',                                             'HOT',  'IDLE',     NULL,        NULL,                NULL,  NULL,                  NULL,            NULL,                  NULL,                  NULL,                  '2019-07-31 06:46:02', NULL,                  NULL,                  '1',       '2',      NULL,    '2019-07-31 06:30:01', '1'
                ///'30203', 'MI_31064602588', 'PI_1907310630003',   'RR4201-1', NULL,  'AUTO01.LP', 'RO1502-2', NULL,  'A9,A10',       NULL,   'R197,R198',           'I',     'TRAY',      '2019-07-31 07:00:00', '1564524000', '1564524000', NULL,  '3333,4444',           '12,12',       'F1902303_HTDR3_4320_3',                                             'F1902303_HTDR3_4320_3',                                             'F1902303_HTDR3_4320_3',                                             'HOT',  'IDLE',     NULL,        NULL,                NULL,  NULL,                  NULL,            NULL,                  NULL,                  NULL,                  '2019-07-31 06:46:02', NULL,                  NULL,                  '1',       '2',      NULL,    '2019-07-31 06:30:01', '1'

                ///위 DB Data에 보여지는것 처럼 MultiJob으로 묶이는 경우 STEPID가 하나로 합쳐지는데 TRAYID의 개수만큼이 아닌 Job의 수만큼 합쳐지므로 해당 Tray의 STEPID를 정확하게 판별 할 수 없다.
                ///그로 인해 기존 Job을 찾아 STEPID를 가져와야한다.


                if (_cfg.Data.UseRv)
                {
                    int trayIdx = 0;
                    if (!FindTrayIdx(ref trayIdx))
                        return;                     
                    
                    string step_id = string.Empty;
                    string multiid = string.Empty;


                    string slot = (string.Compare(jobtype, "SRC") == 0) ? cmd.S_SLOT : cmd.T_SLOT;
                    string eqp = (string.Compare(jobtype, "SRC") == 0) ? cmd.S_EQPID : cmd.T_EQPID;
                    eqp = eqp.Split(',')[0];
                    string eqpId = eqp.Split('-')[0];
                    string rvcmd = (string.Compare(jobtype, "SRC") == 0) ? "EQTRAYUNLOADED" : "EQTRAYLOADED";
                    string[] slot_split = slot.Split(',');

                    unit _unit = Db.Units.Where(p => p.GOALNAME == eqp).Single();

                    if (_unit.goaltype != (int)EqpGoalType.HANDLER && _unit.goaltype != (int)EqpGoalType.HANDLER_STACK)
                    {
                        string sub_type = cmd.BATCHID.Split('_')[0];
                        List<pepschedule_history> cmd_history = null;
                        List<pepschedule> cmd_sec = null;

                    multiid = string.Format($"M{cmd.WORKTYPE}_{cmd.BATCHID.Split('_')[1]}");
                    cmd_history = Db.FindMultiId(multiid, sub_type, cmd.WORKTYPE, ref cmd_sec);

                    if (step_id == "" && cmd_history != null && cmd_history.Count() > 0)
                    {
                        for (int i = 0; i < cmd_history.Count(); i++)
                        {
                            if (cmd_history[i].TRAYID.Contains(jobst.trayid))
                            {
                                if (jobtype.CompareTo("SRC") == 0)
                                    step_id = cmd_history[i].S_STEPID;
                                else if (jobtype.CompareTo("DST") == 0)
                                    step_id = cmd_history[i].T_STEPID;
                                break;
                            }
                        }
                    }

                    if (step_id == "" && cmd_sec != null && cmd_sec.Count() > 0)
                    {
                        for (int i = 0; i < cmd_sec.Count(); i++)
                        {
                            if (cmd_sec[i].TRAYID.Contains(jobst.trayid))
                            {
                                if (jobtype.CompareTo("SRC") == 0)
                                    step_id = cmd_sec[i].S_STEPID;
                                else if (jobtype.CompareTo("DST") == 0)
                                    step_id = cmd_sec[i].T_STEPID;
                                break;
                            }
                        }
                    }

                        if (step_id == "")
                        {
                            if (jobtype.CompareTo("SRC") == 0)
                                step_id = cmd.S_STEPID;
                            else if (jobtype.CompareTo("DST") == 0)
                                step_id = cmd.T_STEPID;
                            else
                                step_id = cmd.STEPID;
                        }
                    }

                    string msg_hdr      = (_unit.goaltype == (int)EqpGoalType.REFLOW) ? eqp : eqpId;
                    string msg_eqpid    = (_unit.goaltype == (int)EqpGoalType.REFLOW) ? eqp : eqpId;
                    string msg_subeqpid = eqp;
                    string msg_trayid   = jobst.trayid;
                    string msg_slotid   = (_unit.goaltype == (int)EqpGoalType.STK) ? slot_split[0] : slot_split[trayIdx];
                    string msg_stepid   = step_id;
                    string msg_execute  = cmd.EXECUTE_TIME;
                    string loaded = (string.Compare(jobtype, "SRC") == 0) ? "UNLOAD" : "LOAD";

                    if (msg_trayid.Split(',').Count() > 1)
                        msg_trayid = string.Format($"({msg_trayid})");

                    if (msg_slotid.Split(',').Count() > 1)
                        msg_slotid = string.Format($"({msg_slotid})");

                    string rvMsg = string.Empty;

                    if (_unit.goaltype == (int)EqpGoalType.HANDLER || _unit.goaltype == (int)EqpGoalType.HANDLER_STACK)
                    {
                        rvMsg = string.Format($"{rvcmd} HDR=({msg_hdr},LH.MPLUS,GARA,TEMP) EQPID={msg_eqpid} SUBEQPID={msg_subeqpid} ");
                        for (int i = 0; i < RvSenderList[vec.ID].RvComm.Handler_LD_peps.Count; i++)
                        {
                            rvMsg += string.Format($"LD{i + 1}TRAYID={RvSenderList[vec.ID].RvComm.Handler_LD_TrayID[i]} LD{i + 1}STEPID={RvSenderList[vec.ID].RvComm.Handler_LD_StepID[i]} ");
                        }
                        rvMsg += string.Format($"JOBTYPE={loaded} EXECUTETIME={msg_execute}");
                    }
                    else
                        rvMsg = string.Format($"{rvcmd} HDR=({msg_hdr},LH.MPLUS,GARA,TEMP) EQPID={msg_eqpid} SUBEQPID={msg_subeqpid} TRAYID=({msg_trayid}) SLOTID=({msg_slotid}) STEPID={msg_stepid} JOBTYPE={loaded} EXECUTETIME={msg_execute}");

                    var args = new EventArgsTransEnd();
                    args.rvMsg = rvMsg;
                    args.eqpId = eqpId;
                    args.vecId = vec.ID;
                    EventParser_OnTransEnd(args);
                }
            }
            catch(Exception ex)
            {
                Logger.Inst.Write(CmdLogType.All, $"{ex}");
            }

            if (jobtype.CompareTo("SRC") == 0)
            {
                cmd.C_state = (int)CmdState.SRC_END;
                _db.DbUpdate(TableType.PEPSCHEDULE);

                var part = parts.Where(p => (p.VEHICLEID == vec.ID) && (p.portNo == jobst.port) && (p.slotNo == jobst.slot)).Single();
                if (part != null)
                {
                    part.C_trayId = jobst.trayid;
                    _db.DbUpdate(TableType.VEHICLE_PART);
                }

                _log.Write(vec.ID, CmdLogType.Db, $"{CollectionEvent.VehicleAcquireStarted.ToString()} : {cmd.ID}");
            }
            else
            {
                cmd.C_state = (int)CmdState.DST_END;
                _db.DbUpdate(TableType.PEPSCHEDULE);

                // jm.choi 추가 - 190305
                // TransEnd 메세지에 추가된 Port/Slot을 적용
                var part = parts.Where(p => (p.VEHICLEID == vec.ID) && (p.portNo == jobst.port_dst) && (p.slotNo == jobst.slot_dst)).Single();
                if (part != null)
                {
                    part.C_trayId = "";
                    _db.DbUpdate(TableType.VEHICLE_PART);
                }

                _log.Write(vec.ID, CmdLogType.Db, $"{CollectionEvent.VehicleDepositStarted.ToString()} : {cmd.ID}");
            }
        }

        private async Task TransferCompleteProcess(string jobtype, vehicle vec, pepschedule cmd, List<vehicle_part> parts, VecJobStatus jobst)
        {
            void UpdatePepschedule(ref pepschedule peps, string type)
            {
                switch(type)
                {
                    case "SRC":
                        cmd.C_srcFinishTime = DateTime.Now;
                        cmd.C_state = (int)CmdState.SRC_COMPLETE;
                        break;
                    case "DST":
                        cmd.C_dstFinishTime = DateTime.Now;
                        cmd.C_state = (int)CmdState.DST_COMPLETE;
                        break;
                }
            }

            await Task.Delay(1);
            UpdatePepschedule(ref cmd, jobtype);
            
            if (jobtype.CompareTo("SRC") == 0)
            {
                _db.DbUpdate(true, new TableType[] { TableType.PEPSCHEDULE, TableType.VEHICLE_PART });
                _log.Write(vec.ID, CmdLogType.Db, $"TransferCompleteProcess : SRC");
            }
            else
            {
                string batch = string.Empty;

                var pep = _db.Peps.Where(p => p.C_VEHICLEID == vec.ID && p.C_state == 8).FirstOrDefault();
                if (pep == null)
                {
                    vec.C_BATCHID = null;
                }
                _db.DbUpdate(true, new TableType[] { TableType.PEPSCHEDULE, TableType.VEHICLE_PART, TableType.VEHICLE });
                _log.Write(vec.ID, CmdLogType.Db, $"TransferCompleteProcess : DST");

                if (batch == null || batch == "")
                    batch = cmd.BATCHID;
                // TI인 Handler Stack Job은 JobCancel을 대비하여
                // 기존 Job을 유지한채 Multi Job을 만들고 완료한다.
                // Multi Job이 완료되면 기존 Job은 필요없음으로 _db에서 삭제한다.

                // 진행중인 Job의 Worktype이 TI 이고 BatchID에 MSRC가 포함되어있으면
                if (cmd.WORKTYPE == "TI" && batch.Contains("MSRC"))
                {
                    // _db에서 진행중인 Job의 Dst 설비를 가져오기
                    unit un = _db.Units.Where(p => p.GOALNAME == cmd.T_EQPID).SingleOrDefault();
                    // 가져온 설비의 goaltype이 Handler_Stack 이면
                    if (un.goaltype == (int)EqpGoalType.HANDLER_STACK)
                    {
                        // _db에서 MultiID가 진행중인 Job의 MultiID와 같고 BatchID는 다른 Job을 가져오기
                        List<pepschedule> peps = _db.Peps.Where(p => p.MULTIID == cmd.MULTIID && p.BATCHID != batch).ToList();

                        // 가져온 Job의 개수만큼 반본
                        foreach (var x in peps)
                        {
                            // Job 삭제
                            _log.Write(vec.ID, CmdLogType.Db, $"Job Delete : {x.BATCHID}");
                            EventParser_OnDeleteCmd(x.BATCHID);                            
                        }
                    }
                }
                // Job 삭제
                _log.Write(vec.ID, CmdLogType.Db, $"Job Delete : {batch}");
                EventParser_OnDeleteCmd(batch);

            }
            RvSenderList[vec.ID].RvComm.tempdown_check = true;
            RvSenderList[vec.ID].RvComm._bVehicleSucc = true;
        }

        public async void ScanProcess(vehicle vec, vehicle_part part, VecScanArgs scan)
        {
            await Task.Delay(1);
            int sparecnt = 10;
            for(int i = 0; i < 4; i++)
            {
                sparecnt = 10;
                /// 해당 portNo 에 해당하는 열이 slot 갯수만큼 검색된다.
                var items = _db.VecParts.Where(p => p.VEHICLEID == vec.ID && p.portNo == i).OrderBy(p=> p.portNo).ThenBy(p=>p.slotNo).ToList();
                if (items == null)
                {
                    Debug.WriteLine($"portNo:{i} is not define!");
                    continue;
                }
                for(int j = 0; j < 10; j++)
                {
                    if(items[j] == null)
                        continue;

                    switch(j)
                    {
                        case 0: items[j].C_trayId = scan.trayid[i, j]; break;
                        case 1: items[j].C_trayId = scan.trayid[i, j]; break;
                        case 2: items[j].C_trayId = scan.trayid[i, j]; break;
                        case 3: items[j].C_trayId = scan.trayid[i, j]; break;
                        case 4: items[j].C_trayId = scan.trayid[i, j]; break;
                        case 5: items[j].C_trayId = scan.trayid[i, j]; break;
                        case 6: items[j].C_trayId = scan.trayid[i, j]; break;
                        case 7: items[j].C_trayId = scan.trayid[i, j]; break;
                        case 8: items[j].C_trayId = scan.trayid[i, j]; break;
                        case 9: items[j].C_trayId = scan.trayid[i, j]; break;
                    }

                    if (scan.trayid[i, j] != null)
                        sparecnt--;
                }
                _db.DbUpdate(TableType.VEHICLE_PART);
            }
        }
    }

    public class EventArgsTransEnd : EventArgs
    {
        public string rvMsg = string.Empty;
        public string eqpId = string.Empty;
        public string vecId = string.Empty; // jm.choi - 190410 OnTransEnd 시 사용
    }
}
