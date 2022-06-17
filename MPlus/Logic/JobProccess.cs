using MPlus.Ref;
using MPlus.Vehicles;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MPlus.Logic
{
    public enum ProcStep
    {
        None,
        Pre_Assign,
        Chk_EqStatus,
        Err_EqStatus,
        Req_EqTempDown,
        Err_EqTempDown,
        Job_Assign,
        Err_JobAssign,
        Job_Initialization,
        Err_Job_Initialization,
        Tray_MoveInfo,
        Err_TrayMoveInfo,
        Tray_WaitMoveComplete,
        Err_WaitTrayMoveComplete,

        Err_WaitTrayJobCompCheck,

        Tray_LoadInfo,
        Err_TrayLoadInfo,
        Tray_WaitLoadComplete,
        Err_WaitTrayLoadComplete,

        Tray_UnLoadInfo,
        Err_TrayUnloadInfo,
        Tray_WaitUnloadComplete,
        Err_WaitTrayUnloadComplete,
        Go_Mobile,
        Req_LoadStandby,
        Err_LoadStandby,
            
        //---------------------------------------------
        // Reflow 관련 ProcStep jm.choi 추가 190215            
        Reflow_Recipe_Set,
        Err_Reflow_Recipe_Set,
        Reflow_LoaderInfo_Set,
        Err_Reflow_LoaderInfo_Set,
        //---------------------------------------------

        Job_Cancel,
        Tray_JobComplete,
        Err_JobComplete,
        Err_Go_Mobile,
        Tray_UnLoadMove,
        Err_TrayUnloadMove,
        Tray_LoadMove,
        Err_TrayLoadMove,
        Tray_WaitUnLoadInfo,
        Err_WaitUnLoadInfo,
        Tray_WaitLoadInfo,
        Err_WaitLoadInfo,
        Job_Start,
        Err_Job_Start,
        //EQTRAYMOVECHECK,

        STK_MOVECHECK_RETRY,
    }

    public partial class JobProcess_member : Global
    {
        private vehicle _vec;
        public vehicle Vec
        { get { return _vec; }
            set { _vec = value; }
        }

        private string _vecId;
        protected string VecId
        { get { return _vecId; }
            set { _vecId = value; }
        }

        public bool IsStop { get; set; }
        private bool _jobSendComplete;
        protected bool JobSendComplete
        {   get { lock(this) { return _jobSendComplete; } }
            set { lock(this) { _jobSendComplete = value; } }
        }

        // VSP.cs에서 JobProccess.cs로 이벤트 전송을 위한 함수들
        #region CallcmddProcArg1
        private List<CallCmdProcArgs1> _callCmdProcArgslist = new List<CallCmdProcArgs1>();
        private object _syncCallCmdProcArgslist = new object();

        protected int CntCallCmdProcArgslist()
        {
            return _callCmdProcArgslist.Count();
        }

        protected void AddCallCmdProcArgslist(CallCmdProcArgs1 v)
        {
            lock (_syncCallCmdProcArgslist)
            {
                _callCmdProcArgslist.Add(v);
            }
        }

        protected CallCmdProcArgs1 RemoveCallCmdProcArgslist()
        {
            CallCmdProcArgs1 v = new CallCmdProcArgs1();
            if (CntCallCmdProcArgslist() <= 0)
                return null;

            lock(_syncCallCmdProcArgslist)
            {
                v = _callCmdProcArgslist.First();
                _callCmdProcArgslist.RemoveAt(0);
            }
            return v;
        }
        #endregion

        // STK 이중 동작을 위한 첫번째 Thread, CallCmdProcArgs1에서 이벤트 전송
        #region CallSTKSrcArgs1
        private List<CallSTKSrcArgs1> _callSTKSrcArgsList1 = new List<CallSTKSrcArgs1>();
        private object _syncCallSTKSrcArgslist1 = new object();

        protected int CntCallSTKSrcArgslist1()
        {
            return _callSTKSrcArgsList1.Count();
        }

        protected void AddCallSTKSrcArgslist1(CallSTKSrcArgs1 v)
        {
            lock (_syncCallSTKSrcArgslist1)
            {
                _callSTKSrcArgsList1.Add(v);
            }
        }

        protected CallSTKSrcArgs1 RemoveCallSTKSrcArgslist1()
        {
            CallSTKSrcArgs1 v = new CallSTKSrcArgs1();
            if (CntCallSTKSrcArgslist1() <= 0)
                return null;

            lock (_syncCallSTKSrcArgslist1)
            {
                v = _callSTKSrcArgsList1.First();
                _callSTKSrcArgsList1.RemoveAt(0);
            }
            return v;
        }
        #endregion

        // STK 이중 동작을 위한 두번째 Thread, CallCmdProcArgs1에서 이벤트 전송
        #region CallSTKSrcArgs2
        private List<CallSTKSrcArgs2> _callSTKSrcArgsList2 = new List<CallSTKSrcArgs2>();

        private object _syncCallSTKSrcArgslist2 = new object();

        protected int CntCallSTKSrcArgslist2()
        {
            return _callSTKSrcArgsList2.Count();
        }

        protected void AddCallSTKSrcArgslist2(CallSTKSrcArgs2 v)
        {
            lock (_syncCallSTKSrcArgslist2)
            {
                _callSTKSrcArgsList2.Add(v);
            }
        }

        protected CallSTKSrcArgs2 RemoveCallSTKSrcArgslist2()
        {
            CallSTKSrcArgs2 v = new CallSTKSrcArgs2();
            if (CntCallSTKSrcArgslist2() <= 0)
                return null;

            lock (_syncCallSTKSrcArgslist2)
            {
                v = _callSTKSrcArgsList2.First();
                _callSTKSrcArgsList2.RemoveAt(0);
            }
            return v;
        }
        #endregion

        // STK 이중 동작시 두번째 Thread 진행을 위한 함수, CallCmdProcArgs1에서 이벤트 전송
        #region CallSTKSrcArgs2_Flag
        private List<CallSTKSrcArgs2_Flag> CallSTKSrcArgs2_Flag = new List<CallSTKSrcArgs2_Flag>();
        private object _syncCallSTKSrcArgslist2_Flag = new object();

        protected int CntCallSTKSrcArgslist2_Flag()
        {
            return CallSTKSrcArgs2_Flag.Count();
        }

        protected void AddCallSTKSrcArgslist2_Flag(CallSTKSrcArgs2_Flag v)
        {
            lock (_syncCallSTKSrcArgslist2_Flag)
            {
                CallSTKSrcArgs2_Flag.Add(v);
            }
        }
        protected CallSTKSrcArgs2_Flag RemoveCallSTKSrcArgslist2_Flag()
        {
            CallSTKSrcArgs2_Flag v = new CallSTKSrcArgs2_Flag();
            if (CntCallSTKSrcArgslist2_Flag() <= 0)
                return null;

            lock (_syncCallSTKSrcArgslist2)
            {
                v = CallSTKSrcArgs2_Flag.First();
                CallSTKSrcArgs2_Flag.RemoveAt(0);
            }
            return v;
        }
        #endregion

    }

    public partial class JobProccess : JobProcess_member
    {
        public event EventHandler<CallExecuteTime> OnCallExecuteTime;

        public string jobType = string.Empty;
        public bool bFront = false;
        public CallCmdProcArgs1 CmdProcList;
        public CallSTKSrcArgs1 StackSrcList1;
        public CallSTKSrcArgs2 StackSrcList2;

        private struct TempNearestGoalStruct
        {
            public string goalName;
            public float goalDist;
        }
        public JobProccess()
        {
            // Job 진행을 위한 Main Thread
            Thread t1 = new Thread(new ThreadStart(Run));
            t1.Start();
            // STK 이중 동작을 위한 첫번째 Thread
            Thread t2 = new Thread(new ThreadStart(StackSrcRun1));
            t2.Start();
            // STK 이중 동작을 위한 두번째 Thread
            Thread t3 = new Thread(new ThreadStart(StackSrcRun2));
            t3.Start();
        }

        public JobProccess(vehicle vec) : this()
        {
            Vec = vec;
            VecId = vec.ID;
        }

        private VertualPep _virtualPep = new VertualPep();

        private async void Run()
        {
            CallCmdProcArgs1 e = null;
            DateTime dtold = DateTime.Now;
            bool bret = false;
            while(!IsStop)
            {
                Thread.Sleep(500);

                // Thread에 할당된 Vec의 isAssigned 상태가 0 이면 continue
                if (Vec.isAssigned == 0)
                    continue;

                if (!bFront)
                    bFront = true;

                // VSP.cs에서 넘겨준 이벤트가 있으면
                if (CntCallCmdProcArgslist() > 0)
                {
                    // 해당 이벤트 가져오고 내부에서 삭제
                    e = RemoveCallCmdProcArgslist();
                    if (bFront)
                    {
                        CmdProcList = e;
                    }
                    // Job Proccess 시작
                    bret = CallCmdProcedure(e);
                    
                    dtold = DateTime.Now;
                    CallExecuteTime execute = new CallExecuteTime();
                    execute.executeTime = "";
                    OnCallExecuteTime?.Invoke(this, execute);
                }
            }
        }

        private async void StackSrcRun1()
        {
            CallSTKSrcArgs1 e = null;
            DateTime dtold = DateTime.Now;
            bool bret = false;
            while (!IsStop)
            {
                Thread.Sleep(500);

                if (Vec.isAssigned == 0)
                    continue;

                if (!bFront)
                    bFront = true;

                // CallCmdProcedure에서 넘겨준 이벤트가 있으면
                if (CntCallSTKSrcArgslist1() > 0)
                {
                    // 해당 이벤트 가져오고 내부에서 삭제
                    e = RemoveCallSTKSrcArgslist1();
                    if (bFront)
                    {
                        StackSrcList1 = e;
                    }
                    // STK 첫번째 Proccess 시작
                    bret = StackSrcProcedure1(e);
                }
            }
        }
        private async void StackSrcRun2()
        {
            CallSTKSrcArgs2 e = null;
            DateTime dtold = DateTime.Now;
            bool bret = false;
            while (!IsStop)
            {
                Thread.Sleep(500);

                if (Vec.isAssigned == 0)
                    continue;

                if (!bFront)
                    bFront = true;

                // CallCmdProcedure에서 넘겨준 이벤트가 있으면
                if (CntCallSTKSrcArgslist2() > 0)
                {
                    // 해당 이벤트 가져오고 내부에서 삭제
                    e = RemoveCallSTKSrcArgslist2();
                    if (bFront)
                    {
                        StackSrcList2 = e;
                    }
                    // STK 두번째 Thread 시작 Flag 상태 변경
                    STKSrc_sec = true;
                    // STK 두번째 Proccess 시작
                    bret = StackSrcProcedure2(e);
                }
            }
        }
        public void Vehicle_OnChageConnected(object sender, ChangeConnectedArgs e)
        {
            if (sender is VehicleEntity vec)
            {
                var targetVec = Db.Vechicles.Where(p => p.ID == vec.Id && p.isUse == 1).SingleOrDefault();
                if (targetVec != null && targetVec.installState != Convert.ToInt32(e.connected))
                {
                    if (e.connected)
                    {
                        targetVec.installState = (int)VehicleInstallState.INSTALLED;
                    }
                    else
                    {
                        targetVec.installState = (int)VehicleInstallState.REMOVED;
                        targetVec.C_lastArrivedUnit = string.Empty;
                    }
                    Db.DbUpdate(true, new TableType[] { TableType.VEHICLE });
                }
            }
        }

        /// <summary>
        /// 설비별 타입을 보고 SRC JOB, DST JOB 에 사용될 함수를 구분하고, RV 인터페이스한다
        /// S_EQPID 가 STK 이면, STK 는 UNLOAD, EQ 는 LOAD
        /// T_EQPID 가 STK 이면, STK 는 LOAD, EQ 는 UNLOAD
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public bool OnSendJobToVehicle(SendJobToVecArgs1 e)
        {
            // 진행중인 Job의 Robot ID와 Thread의 할당된 Robot ID가 같은지 확인
            if (!string.Equals(e.vecID, VecId))
                return false;

            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"EVENT:_Vsp_OnSendJobToVehicle. JOBTYPE:{e.cmd},S:{e.job.S_EQPID},D:{e.job.T_EQPID}");

            // Multi Job으로 Remake 시 Dst 설비가 여러대가 되어 T_EQPID에 설비명이 여러개 들어가는 경우가 생김
            // 설비명이 여러개 일 경우 맨 앞의 설비로 적용
            string T_eqpid = e.job.T_EQPID.Split(',')[0];
            var src_units = Db.Units.Where(p => p.ID == e.job.S_EQPID).ToList();
            var dst_units = Db.Units.Where(p => p.ID == T_eqpid).ToList();

            Debug.Assert(src_units.Count() == 1);
            Debug.Assert(dst_units.Count() == 1);

            bool bret = false;
            // 진행 중인 Job이 SRC 작업이면
            if (e.cmd == "SRC")
            {
                RvSenderList[e.vecID].RvComm.Reset();
                RvSenderList[e.vecID].RvComm.Alloc(e);
                // Src 설비가 STK 일 경우
                if (src_units[0].goaltype == (int)EqpGoalType.STK)
                {
                    bret = From_STK(e, src_units[0], dst_units[0]);
                }
                // Src 설비가 SYSWIN, SYSWIN_OVEN, SYSWIN_OVEN_t 일 경우
                else if (src_units[0].goaltype == (int)EqpGoalType.SYSWIN || 
                    src_units[0].goaltype == (int)EqpGoalType.SYSWIN_OVEN || src_units[0].goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || src_units[0].goaltype == (int)EqpGoalType.BUFFER_STK)
                {
                    bret = From_SYSWIN(e, src_units[0], dst_units[0]);
                }
                // Src 설비가 HANDLER, HANDLER_STACK 일 경우
                else if (src_units[0].goaltype == (int)EqpGoalType.HANDLER || src_units[0].goaltype == (int)EqpGoalType.HANDLER_STACK)
                {
                    bret = From_HANDLER(e, src_units[0], dst_units[0]);
                }
                // Src 설비가 REFLOW 일 경우
                else if (src_units[0].goaltype == (int)EqpGoalType.REFLOW) // Reflow type jm.choi 추가 - 190215
                {
                    bret = From_REFLOW(e, src_units[0], dst_units[0]);
                }
            }
            // 진행 중인 Job이 DST 작업이면
            else if (e.cmd == "DST")
            {
                // Dst 설비가 STK 일 경우
                if (dst_units[0].goaltype == (int)EqpGoalType.STK)
                {
                    bret = to_STK(e, src_units[0], dst_units[0]);
                }
                // Dst 설비가 SYSWIN, SYSWIN_OVEN, SYSWIN_OVEN_t 일 경우
                else if (dst_units[0].goaltype == (int)EqpGoalType.SYSWIN ||
                    dst_units[0].goaltype == (int)EqpGoalType.SYSWIN_OVEN || dst_units[0].goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || dst_units[0].goaltype == (int)EqpGoalType.BUFFER_STK)
                {
                    bret = to_SYSWIN(e, src_units[0], dst_units[0]);
                }
                // Dst 설비가 HANDLER, HANDLER_STACK 일 경우
                else if (dst_units[0].goaltype == (int)EqpGoalType.HANDLER ||
                    dst_units[0].goaltype == (int)EqpGoalType.HANDLER_STACK)
                {
                    bret = to_HANDLER(e, src_units[0], dst_units[0]);
                }
                // Dst 설비가 REFLOW 일 경우
                else if (dst_units[0].goaltype == (int)EqpGoalType.REFLOW) // Reflow type jm.choi 추가 - 190215
                {
                    bret = to_REFLOW(e, src_units[0], dst_units[0]);
                }
            }

            if (!bret)
                return bret;
            else
            {                
                JobSendComplete = true;
                return bret;
            }

        }

        // stk unload -ok
        private bool From_STK(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit)
        {
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"From_STK. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}],D:[{e.job.T_EQPID}]");
            e.rvAssign = RvAssignProc.From_STK;

            RvSenderList[e.vecID].JobType = "UNLOAD";

            bool bret = false;
            // job pre assign
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Pre_Assign);
            if (Cfg.Data.UseRv)
            {
                // 설비 상태 체크
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Chk_EqStatus);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_EqStatus);
                    return false;
                }
            }

            if (Cfg.Data.UseRv)
            {
                // Multi Job을 이중동작으로 진행 할 때 STK 용 Thread의 두번째 Thread 가 아닐 때
                if (!STKSrc_sec)
                {
                    // Dst 설비가 REFLOW 일 때
                    if (dstUnit != null && dstUnit.goaltype == (int)EqpGoalType.REFLOW)
                    {
                        // ReflowRecipeSet 전송
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Reflow_Recipe_Set);
                        if (!bret)
                        {
                            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Reflow_Recipe_Set);
                            return false;
                        }
                        // ReflowLoaderInfoSet 전송
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Reflow_LoaderInfo_Set);
                        if (!bret)
                        {
                            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Reflow_LoaderInfo_Set);
                            return false;
                        }
                    }

                    // Dst 설비가 HANDLER, HANDLER_STACK 일 때
                    if (EqpGoalType.HANDLER == (EqpGoalType)dstUnit.goaltype || EqpGoalType.HANDLER_STACK == (EqpGoalType)dstUnit.goaltype)
                    {
                        // STK에서 나오는 Tray가 Empty이면 Handler에 LoadStandby 메세지 Skip
                        if (e.job.WORKTYPE != "EI" && e.job.WORKTYPE != "EO")
                        {
                            // LoadStandby 전송
                            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Req_LoadStandby);
                            if (!bret)
                                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_LoadStandby);

                            // TI 작업 시 Src 작업 완료 후 Dst 작업 진행 중 Abnormal 상황 발생으로 JobCancel 후 재 진행 시
                            // 진행되는 Job이 LoadStandby Data를 전송했는지 확인하기 위한 Flag
                            // 정상적으로 SRC->DST 진행시 True
                            // JobCancel 후 진행 시 False
                            RvSenderList[e.vec.ID].HandlerStandby = true;
                        }
                    }
                }

                // STK에 대한 MoveCheck 시 해당 Port의 LP에 나와있는 Tray ID와 Job의 Tray ID가 같으면 MoveInfo Skip
                if (!RvSenderList[VecId].RvComm.lp_tray)
                {
                    // MoveInfo 전송
                    // STK에 해당 Job의 Tray를 해당 Port로 배출 요청
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_MoveInfo);
                    if (!bret)
                        return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayMoveInfo);

                    // STK에서 보내줄 Tray 배출 완료 Message 대기
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitMoveComplete);
                    if (!bret)
                    {
                        Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitTrayMoveComplete);

                        RvSenderList[VecId].RvComm.STK_Retry = true;
                        Thread.Sleep(3000);
                        // eq status check
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.STK_MOVECHECK_RETRY);
                        if (!RvSenderList[VecId].RvComm.lp_tray)
                        {
                            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"STK {e.job.S_SLOT}의 TrayID 정보가 맞지 않습니다.");
                            return false;
                        }
                    }
                }
            }

            // Worktype이 EI 또는 TI 일 경우
            if (RvSenderList[VecId].RvComm.job.WORKTYPE == "EI" || RvSenderList[VecId].RvComm.job.WORKTYPE == "TI")
            {
                // STK 용 Thread의 두번째 Thread가 아니면
                if (!STKSrc_sec)
                {
                    // STK 용 First Thread가 MoveComp에 도달했음에 대한 Flag 설정
                    // WorkType이 EI 또는 TI이고 이 Flag가 True이고 Jobs가 2개이면(Multi Job이면) STK 용 Second Thread를 실행
                    STKSrc_First_check = true;
                }
            }

            // STK 용 Thread의 두번째 Thread 일 떄
            while (STKSrc_sec)
            {
                // STK용 First Thread의 완료 이벤트 Flag 대기
                // STK용 First Thread가 완료 되어야
                // STK용 Second Thread가 동작하여 Robot을 다음 Port로 이동시킴
                if (CntCallSTKSrcArgslist2_Flag() > 0)
                {
                    // 이벤트 Flag 확인 시
                    // Flag 삭제 후 STK 동시 진행이므로 MoveCheck에서 STK의 Port가 변경되었을수있으므로
                    // BuffSlot Data 변경하여 진행
                    Thread.Sleep(10);
                    CallSTKSrcArgs2_Flag EF = null;
                    EF = RemoveCallSTKSrcArgslist2_Flag();

                    string buf_portslot = string.Empty;
                    int traycount = Proc_Atom.Init.traycount_result(e.job);
                    MakeBuffSlot(e.cmd, e.job, e.vec, ref buf_portslot, traycount);
                    e.bufslot = buf_portslot;
                    break;
                }

            }

            if (Cfg.Data.UseRv)
            { 
                // Robot을 설비로 이동하겠다는 Message 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_UnLoadMove);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayUnloadMove);
            }

            // Robot에게 Src 작업 Assign 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Assign);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobAssign);

            // Robot을 설비로 이동 명령 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Go_Mobile);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Go_Mobile);

            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"차량[{e.vecID}]이 명령[{e.cmd};{e.job.BATCHID};] 위치에 도착했습니다.");

            if (Cfg.Data.UseRv)
            {
                // 설비에 진행될 Job의 Data 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_UnLoadInfo);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayUnloadInfo);

                // 잠시 제외
                //if (!PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitUnLoadInfo))
                //    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitUnLoadInfo);
            }

            // Robot에게 Assign 된 Job 진행 명령
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Start);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Job_Start);

            if (Cfg.Data.UseRv)
            {
                // 설비에서 보내줄 작업 완료 Message 대기
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitUnloadComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitTrayUnloadComplete);
                    return false;
                }
            }
            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"From_STK. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}] 작업을 마칩니다");
            return true;
        }

        // eq unload
        private bool From_SYSWIN(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit)
        {
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"From_SYSWIN. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}],D:[{e.job.T_EQPID}]");
            e.rvAssign = RvAssignProc.From_SYSWIN;

            RvSenderList[e.vecID].JobType = "UNLOAD";

            bool bret = false;
            // ------------------------------------------------------ sts chk
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Pre_Assign);
            if (Cfg.Data.UseRv)
            {
                // 설비 상태 체크
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Chk_EqStatus);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_EqStatus);
                    return false;
                }
            }

            if (Cfg.Data.UseRv)
            {
                // Dst 설비가 REFLOW 일 때
                if (dstUnit != null && dstUnit.goaltype == (int)EqpGoalType.REFLOW)
                {
                    // Reflow에 Recipe Set Data 전송
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Reflow_Recipe_Set);
                    if (!bret)
                    {
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Reflow_Recipe_Set);
                        return false;
                    }

                    // Reflow의 Loader에 Set Data 전송
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Reflow_LoaderInfo_Set);
                    if (!bret)
                    {
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Reflow_LoaderInfo_Set);
                        return false;
                    }
                }

                // Robot을 설비로 이동하겠다는 Message 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_UnLoadMove);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayUnloadMove);
            }

            // Robot에게 Src 작업 Assign 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Assign);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobAssign);

            // Robot을 설비로 이동 명령 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Go_Mobile);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Go_Mobile);

            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"차량[{e.vecID}]이 명령[{e.cmd};{e.job.BATCHID};] 위치에 도착했습니다.");

            if (Cfg.Data.UseRv)
            {
                // 설비에 진행될 Job의 Data 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_UnLoadInfo);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayUnloadInfo);

                //if (!PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitUnLoadInfo))
                //    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitUnLoadInfo);

                // 설비의 온도를 낮추도록 Message 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Req_EqTempDown);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_EqTempDown);
            }

            // Robot에게 Assign 된 Job 진행 명령
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Start);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Job_Start);

            if (Cfg.Data.UseRv)
            {
                // 설비에서 보내줄 작업 완료 Message 대기
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitUnloadComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitTrayUnloadComplete);
                    return false;
                }

                // Robot에서 보내줄 작업 완료 Message를 받고 설비와 Robot의 작업이 완료되었다는 Message를 설비로 전송
                // Message Data중 JobComp는 동일 ExecuteTime에 해당 설비의 진행되지않은 Job이 존재하면 Busy로, 해당 설비의 Job이 없으면 Comp로 전송
                // TC에서 JobComp의 Data를 확인하여 설비 가동/대기 여부 결정
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_JobComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobComplete);
                    return false;
                }
            }

            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"From_SYSWIN. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}] 작업을 마칩니다");
            return true;
        }

        // eq unload
        private bool From_HANDLER(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit)
        {
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"From_HANDLER. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}],D:[{e.job.T_EQPID}]");
            e.rvAssign = RvAssignProc.From_HANDLER;

            RvSenderList[e.vecID].JobType = "UNLOAD";

            bool bret = false;
            // ------------------------------------------------------ sts chk
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Pre_Assign);
            if (Cfg.Data.UseRv)
            {
                // 설비 상태 체크
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Chk_EqStatus);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_EqStatus);
                    return false;
                }

#if true
                // Dst 설비가 REFLOW 일 때
                if (dstUnit != null && dstUnit.goaltype == (int)EqpGoalType.REFLOW)
                {
                    // ReflowRecipeSet 전송
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Reflow_Recipe_Set);
                    if (!bret)
                    {
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Reflow_Recipe_Set);
                        return false;
                    }
                    // ReflowLoaderInfoSet 전송
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Reflow_LoaderInfo_Set);
                    if (!bret)
                    {
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Reflow_LoaderInfo_Set);
                        return false;
                    }
                }

                // Dst 설비가 HANDLER, HANDLER_STACK 일 때
                if (EqpGoalType.HANDLER == (EqpGoalType)dstUnit.goaltype || EqpGoalType.HANDLER_STACK == (EqpGoalType)dstUnit.goaltype)
                {
                    // STK에서 나오는 Tray가 Empty이면 Handler에 LoadStandby 메세지 Skip
                    if (e.job.WORKTYPE != "EI" && e.job.WORKTYPE != "EO")
                    {
                        // LoadStandby 전송
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Req_LoadStandby);
                        if (!bret)
                            return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_LoadStandby);

                        // TI 작업 시 Src 작업 완료 후 Dst 작업 진행 중 Abnormal 상황 발생으로 JobCancel 후 재 진행 시
                        // 진행되는 Job이 LoadStandby Data를 전송했는지 확인하기 위한 Flag
                        // 정상적으로 SRC->DST 진행시 True
                        // JobCancel 후 진행 시 False
                        RvSenderList[e.vec.ID].HandlerStandby = true;
                    }
                }

#endif
				bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_UnLoadMove);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayUnloadMove);
            }

            // Robot에게 Src 작업 Assign 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Assign);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobAssign);

            // Robot을 설비로 이동 명령 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Go_Mobile);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Go_Mobile);

            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"차량[{e.vecID}]이 명령[{e.cmd};{e.job.BATCHID};] 위치에 도착했습니다.");

            if (Cfg.Data.UseRv)
            {
                // 설비에 진행될 Job의 Data 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_UnLoadInfo);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayUnloadInfo);

                //if (!PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitUnLoadInfo))
                //    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitUnLoadInfo);
            }

            // Robot에게 Assign 된 Job 진행 명령
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Start);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Job_Start);

            if (Cfg.Data.UseRv)
            {
                // 설비에서 보내줄 작업 완료 Message 대기
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitUnloadComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitTrayUnloadComplete);
                    return false;
                }
            }
            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"From_HANDLER. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}] 작업을 마칩니다");
            return true;
        }

        private bool From_REFLOW(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit)
        {
            Thread.Sleep(5000);
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"From_REFLOW. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}],D:[{e.job.T_EQPID}]");
            e.rvAssign = RvAssignProc.From_REFLOW;

            RvSenderList[e.vecID].JobType = "UNLOAD";

            bool bret = false;

            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Pre_Assign);
            if (Cfg.Data.UseRv)
            {
                // 설비 상태 체크
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Chk_EqStatus);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_EqStatus);
                    return false;
                }
            }

            // ------------------------------------------------------ src job
            if (Cfg.Data.UseRv)
            {
                // Dst 설비가 REFLOW 일 때
                if (dstUnit != null && dstUnit.goaltype == (int)EqpGoalType.REFLOW)
                {
                    // Reflow에 Recipe Set Data 전송
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Reflow_Recipe_Set);
                    if (!bret)
                    {
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Reflow_Recipe_Set);
                        return false;
                    }

                    // Reflow의 Loader에 Set Data 전송
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Reflow_LoaderInfo_Set);
                    if (!bret)
                    {
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Reflow_LoaderInfo_Set);
                        return false;
                    }
                }

#if true
                // Dst 설비가 HANDLER, HANDLER_STACK 일 때
                if (EqpGoalType.HANDLER == (EqpGoalType)dstUnit.goaltype || EqpGoalType.HANDLER_STACK == (EqpGoalType)dstUnit.goaltype)
                {
                    // STK에서 나오는 Tray가 Empty이면 Handler에 LoadStandby 메세지 Skip
                    if (e.job.WORKTYPE != "EI" && e.job.WORKTYPE != "EO")
                    {
                        // LoadStandby 전송
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Req_LoadStandby);
                        if (!bret)
                            return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_LoadStandby);

                        // TI 작업 시 Src 작업 완료 후 Dst 작업 진행 중 Abnormal 상황 발생으로 JobCancel 후 재 진행 시
                        // 진행되는 Job이 LoadStandby Data를 전송했는지 확인하기 위한 Flag
                        // 정상적으로 SRC->DST 진행시 True
                        // JobCancel 후 진행 시 False
                        RvSenderList[e.vec.ID].HandlerStandby = true;
                    }
                }

#endif
				// Robot을 설비로 이동하겠다는 Message 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_UnLoadMove);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayUnloadMove);
            }

            // Robot에게 Src 작업 Assign 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Assign);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobAssign);

            // Robot을 설비로 이동 명령 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Go_Mobile);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Go_Mobile);

            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"차량[{e.vecID}]이 명령[{e.cmd};{e.job.BATCHID};] 위치에 도착했습니다.");

            if (Cfg.Data.UseRv)
            {
                // 설비에 진행될 Job의 Data 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_UnLoadInfo);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayUnloadInfo);

                //if (!PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitUnLoadInfo))
                //    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitUnLoadInfo);
            }

            // Robot에게 Assign 된 Job 진행 명령
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Start);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Job_Start);

            if (Cfg.Data.UseRv)
            {
                // 설비에서 보내줄 작업 완료 Message 대기
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitUnloadComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitTrayUnloadComplete);
                    return false;
                }

                // Robot에서 보내줄 작업 완료 Message를 받고 설비와 Robot의 작업이 완료되었다는 Message를 설비로 전송
                // Message Data중 JobComp는 동일 ExecuteTime에 해당 설비의 진행되지않은 Job이 존재하면 Busy로, 해당 설비의 Job이 없으면 Comp로 전송
                // TC에서 JobComp의 Data를 확인하여 설비 가동/대기 여부 결정
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_JobComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobComplete);
                    return false;
                }
            }
            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"From_HANDLER. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}] 작업을 마칩니다");
            return true;
        }
        // load
        private bool to_STK(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit)
        {
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"to_STK. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}],D:[{e.job.T_EQPID}]");
            e.rvAssign = RvAssignProc.to_STK;

            RvSenderList[e.vecID].JobType = "LOAD";

            bool bret = false;

            // ------------------------------------------------------ dst job
            if (Cfg.Data.UseRv)
            {
                // 20220105
                // O, TO 작업 시 출발하기전 MoveCheck Skip 추가
                // to_STK는 O, TO 작업에서만 사용하므로 해당 MoveCheck 삭제

                //if (e.job.WORKTYPE != "TO")
                //{
                //    // STK 상태 체크
                //    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Chk_EqStatus);
                //    if (!bret)
                //    {
                //        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_EqStatus);
                //        return false;
                //    }
                //}
                
                // Robot을 설비로 이동하겠다는 Message 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_LoadMove);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayLoadMove);
            }

            // Robot이 출발 전 MoveCheck에서 할당된 Port와 Robot이 도착 후 MoveCheck에서 할당된 Port가 동일한지 확인하는 Flag
            // 할당된 Port가 동일하면 반복문에서 빠져나오고
            // 할당된 Port가 다르면 Assign 부터 다시 진행
            bool port_flag = false;
            while (!port_flag)
            {
                // Robot에게 Dst 작업 Assign 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Assign);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobAssign);

                // 현재 할당된 Port를 저장
                string T_port = e.job.T_SLOT;

                // Robot을 설비로 이동 명령 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Go_Mobile);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Go_Mobile);
                Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"차량[{e.vecID}]이 명령[{e.cmd};{e.job.BATCHID};] 위치에 도착했습니다.");

                if (Cfg.Data.UseRv)
                {
                    // STK 상태 체크
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Chk_EqStatus);
                    if (!bret)
                    {
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_EqStatus);
                        return false;
                    }
                }

                // MoveCheck에서 할당된 Port (e.job.T_SLOT)과 저장되어있는 Port (T_port)가 다르면
                if (T_port != e.job.T_SLOT)
                {
                    // Robot에게 Stop 명령 전송
                    VehicleList[VecId].SendStopCmd();
                    Thread.Sleep(1000);

                    // Robot에게 JobCancel 명령 전송
                    VehicleList[VecId].SendCancelCmd();
                    Thread.Sleep(1000);
                    // 반복문 재 실행
                    port_flag = false;
                }
                // MoveCheck에서 할당된 Port (e.job.T_SLOT)과 저장되어있는 Port (T_port)가 같으면
                else
                {
                    // 반복문 나가기
                    port_flag = true;
                }
            }

            if (Cfg.Data.UseRv)
            {
                // 설비에 진행될 Job의 Data 전송
                if (!Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_LoadInfo))
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayLoadInfo);

                // 잠시 제외
                //if (!PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitLoadInfo))
                //    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitLoadInfo);
            }

            // Robot에게 Assign 된 Job 진행 명령
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Start);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Job_Start);

            if (Cfg.Data.UseRv)
            {
                // 설비에서 보내줄 작업 완료 Message 대기
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitLoadComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitTrayLoadComplete);
                    return false;
                }
            }
            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"to_STK. B:[{e.job.BATCHID}],D:[{e.job.T_EQPID}] 작업을 마칩니다");
            return true;
        }

        // eq load - ok
        private bool to_SYSWIN(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit)
        {
            Logger.Inst.Write(e.vecID, CmdLogType.Rv,$"to_SYSWIN. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}],D:[{e.job.T_EQPID}]");
            e.rvAssign = RvAssignProc.to_SYSWIN;

            RvSenderList[e.vecID].JobType = "LOAD";

            bool bret = false;

            // ------------------------------------------------------ dst job
            if (Cfg.Data.UseRv)
            {
                // Robot을 설비로 이동하겠다는 Message 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_LoadMove);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayLoadMove);
            }

            // Robot에게 Dst 작업 Assign 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Assign);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobAssign);

            // Robot을 설비로 이동 명령 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Go_Mobile);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Go_Mobile);

            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"차량[{e.vecID}]이 명령[{e.cmd};{e.job.BATCHID};] 위치에 도착했습니다.");

            if (Cfg.Data.UseRv)
            {
                // 설비에 진행될 Job의 Data 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_LoadInfo);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayLoadInfo);

                //if (!PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitLoadInfo))
                //    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitLoadInfo);

                // 설비의 온도를 낮추도록 Message 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Req_EqTempDown);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_EqTempDown);
            }

            // Robot에게 Assign 된 Job 진행 명령
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Start);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Job_Start);

            if (Cfg.Data.UseRv)
            {
                // 설비에서 보내줄 작업 완료 Message 대기
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitLoadComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitTrayMoveComplete);
                    return false;
                }

                // Robot에서 보내줄 작업 완료 Message를 받고 설비와 Robot의 작업이 완료되었다는 Message를 설비로 전송
                // Message Data중 JobComp는 동일 ExecuteTime에 해당 설비의 진행되지않은 Job이 존재하면 Busy로, 해당 설비의 Job이 없으면 Comp로 전송
                // TC에서 JobComp의 Data를 확인하여 설비 가동/대기 여부 결정
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_JobComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobComplete);
                    return false;
                }
            }
            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"to_SYSWIN. B:[{e.job.BATCHID}],D:[{e.job.T_EQPID}] 작업을 마칩니다");
            return true;
        }

        // eq load - ok
        private bool to_HANDLER(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit)
        {
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"to_HANDLER. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}],D:[{e.job.T_EQPID}]");
            e.rvAssign = RvAssignProc.to_HANDLER;

            RvSenderList[e.vecID].JobType = "LOAD";

            bool bret = false;

            // Handler JobStandby Message를 전송하였는지에 대한 Flag(RvSenderList[e.vecID].HandlerStandby) 확인
            // JobStandby를 전송하였으면 RvSenderList[e.vecID].HandlerStandby == true 이므로 ! 로 반전
            if (!RvSenderList[e.vecID].HandlerStandby &&
            	(EqpGoalType.HANDLER == (EqpGoalType)dstUnit.goaltype || EqpGoalType.HANDLER_STACK == (EqpGoalType)dstUnit.goaltype))
            {
                // Cancel 후 Dst 진행시 Handler Loader 변수들이 초기화되므로 MoveCheck를 Handler에만 다시 전송
                // STK에서 나오는 Tray가 Empty이면 Handler에 LoadStandby 메세지 Skip   
                if ((Cfg.Data.UseRv) && (e.job.WORKTYPE != "EI" && e.job.WORKTYPE != "EO"))
                {
                    // 설비 상태 체크
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Chk_EqStatus);
                    if (!bret)
                    {
                        bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_EqStatus);
                        return false;
                    }

                    // LoadStandby 전송
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Req_LoadStandby);
                    if (!bret)
                        return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_LoadStandby);
                    
                }
            }
            // ------------------------------------------------------ dst job
            if (Cfg.Data.UseRv)
            {
                // Robot을 설비로 이동하겠다는 Message 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_LoadMove);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayLoadMove);
            }

            // Robot에게 Dst 작업 Assign 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Assign);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobAssign);

            // Robot을 설비로 이동 명령 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Go_Mobile);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Go_Mobile);

            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"차량[{e.vecID}]이 명령[{e.cmd};{e.job.BATCHID};] 위치에 도착했습니다.");

            if (Cfg.Data.UseRv)
            {
                // 설비에 진행될 Job의 Data 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_LoadInfo);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayLoadInfo);

                //if (!PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitLoadInfo))
                //    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitLoadInfo);
            }

            // Robot에게 Assign 된 Job 진행 명령
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Start);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Job_Start);

            if (Cfg.Data.UseRv)
            {
                // 설비에서 보내줄 작업 완료 Message 대기
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitLoadComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitTrayMoveComplete);
                    return false;
                }
            }
            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"to_HANDLER. B:[{e.job.BATCHID}],D:[{e.job.T_EQPID}] 작업을 마칩니다");
            return true;
        }

        private bool to_REFLOW(SendJobToVecArgs1 e, unit srcUnit, unit dstUnit)
        {
            Thread.Sleep(5000);
            Logger.Inst.Write(e.vecID, CmdLogType.Rv, $"to_REFLOW. B:[{e.job.BATCHID}],S:[{e.job.S_EQPID}],D:[{e.job.T_EQPID}]");
            e.rvAssign = RvAssignProc.to_REFLOW;

            RvSenderList[e.vecID].JobType = "LOAD";

            bool bret = false;

            // ------------------------------------------------------ dst job
            if (Cfg.Data.UseRv)
            {
                // Robot을 설비로 이동하겠다는 Message 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_LoadMove);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayLoadMove);
            }

            // Robot에게 Dst 작업 Assign 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Assign);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobAssign);

            // Robot을 설비로 이동 명령 전송
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Go_Mobile);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Go_Mobile);

            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"차량[{e.vecID}]이 명령[{e.cmd};{e.job.BATCHID};] 위치에 도착했습니다.");

            if (Cfg.Data.UseRv)
            {
                // 설비에 진행될 Job의 Data 전송
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_LoadInfo);
                if (!bret)
                    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_TrayLoadInfo);

                //if (!PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitLoadInfo))
                //    return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitLoadInfo);
            }

            // Robot에게 Assign 된 Job 진행 명령
            bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Job_Start);
            if (!bret)
                return Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_Job_Start);

            if (Cfg.Data.UseRv)
            {
                // 설비에서 보내줄 작업 완료 Message 대기
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_WaitLoadComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_WaitTrayLoadComplete);
                    return false;
                }

                // Robot에서 보내줄 작업 완료 Message를 받고 설비와 Robot의 작업이 완료되었다는 Message를 설비로 전송
                // Message Data중 JobComp는 동일 ExecuteTime에 해당 설비의 진행되지않은 Job이 존재하면 Busy로, 해당 설비의 Job이 없으면 Comp로 전송
                // TC에서 JobComp의 Data를 확인하여 설비 가동/대기 여부 결정
                bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Tray_JobComplete);
                if (!bret)
                {
                    bret = Proc_Atom.Init.PROC_ATOM(e, srcUnit, dstUnit, ProcStep.Err_JobComplete);
                    return false;
                }
            }
            Logger.Inst.Write(e.vecID, CmdLogType.Comm, $"to_REFLOW. B:[{e.job.BATCHID}],D:[{e.job.T_EQPID}] 작업을 마칩니다");
            return true;
        }
        /// <summary>
        /// SRC 또는 DST 양쪽에서 MULTIJOB 을 수행할 수 있는 모듈
        /// </summary>
        /// <param name="ismulti"></param>
        /// <param name="direct"></param>
        /// <param name="jobs"></param>
        /// <param name="vec"></param>
        /// <returns></returns>
        //public bool CallCmdProcedure(object sender, bool ismulti, eMultiJobWhere where, List<pepschedule> jobs, vehicle vec)
        public async void OnCallCmdProcedure(object sender, CallCmdProcArgs1 e)
        {
            if (e.vec.ID != VecId)
                return;

            await Task.Delay(1);
            AddCallCmdProcArgslist(e);
            return;
        }

        public bool CallCmdProcedure(CallCmdProcArgs1 e)
        {
            try
            {
                List<pepschedule> Jobs = null;
                bool IsMultiJob = false;
                bool bret = false;
                int remakecount = 0;
                int remakeJobcount = 0;
                int foreachcount = 0;
                int I_type_remakcount = 0;
                int I_type_remakeJobcount = 0;
                int I_type_foreachcount = 0;
                STKSrc_First_check = false;
                STKSrc_sec = false;
                RvSenderList[e.vec.ID].RvComm._bReflowSucc = false;
                RvSenderList[e.vec.ID].RvComm._bLoaderSucc = false;
                RvSenderList[e.vec.ID].RvComm._bReflowerror = false;
                RvSenderList[e.vec.ID].RvComm._bLoadererror = false;

                // 실제 작업에 사용될 Job을 VSP에서 이벤트로 넘겨준 정보를 토대로 선정
                Job_Select(ref Jobs, ref IsMultiJob, e);

                JobProcList[e.vec.ID].jobType = "SRC";
                // JobCancel 후 Dst만 진행을 위한 srcFinishTime 값 확인
                // srcFinishTime이 있으면 Src 작업은 진행한 상태로 인식
                if (Jobs[0].C_srcFinishTime == null)
                {
                    // Src 작업에 대한 Multi Job 생성 함수
                    bret = ProcessSrcJob(IsMultiJob, ref Jobs, e, ref remakecount, ref I_type_remakcount);
                    if (!bret)
                        return bret;
                    // remake 된 Job의 수 저장
                    remakeJobcount = remakecount;                    
                    Job_ordering(ref Jobs, I_type_remakcount, ref remakecount, e);
                    I_type_remakeJobcount = I_type_remakcount;
                }
                else
                {
                    bret = true;
                }

                RvSenderList[e.vec.ID].IsMulti = IsMultiJob;
                // TI 중 Reflow의 경우는 상위에서 Jobs을 1개만 내려주나
                // Tray 개수가 10장을 넘어가는 경우가 생겨
                // Multi Job 생성시 Job이 2개로 늘어남
                // 이 때 JobCancel을 대비하여 IsMulti를 true로 저장
                if (Jobs[0].WORKTYPE == "TI" && Jobs.Count() > 1)
                    RvSenderList[e.vec.ID].IsMulti = true;

                RvSenderList[e.vec.ID].MultiList = Jobs;

                if (Jobs[0].WORKTYPE != "EI" && Jobs[0].WORKTYPE != "TI")
                {
                    // 선정된 Job의 수 만큼 반복
                    foreach (var xxx in Jobs)
                    {
                        Thread.Sleep(1);
                        // Mplus의 상태가 Pause면 대기하는 함수
                        isControl_Pause();
                        
                        // Src 작업을 진행했는지 확인하는 조건
                        if (xxx.C_srcFinishTime != null)
                            continue;

                        // Job에 할당된 Src 설비를 DB에서 가져오기
                        unit unit_chk = Db.Units.Where(p => p.GOALNAME == Jobs[0].S_EQPID).SingleOrDefault();

                        // 현재 진행중인 Job에 할당된 Robot ID 저장
                        UpdateVehicleByID(VecId, xxx.BATCHID);
                        RvSenderList[e.vec.ID].CurJob = xxx;

                        // 실제 작업이 진행되는 함수
                        bret = OrderToVehicle(JobProcList[e.vec.ID].jobType, xxx, e.vec);
                        if (!bret)
                            break;

                        while (true)
                        {
                            // 진행중인 Job에 srcFinishTime이 저장되면 break;
                            if (xxx.C_srcFinishTime != null)
                            {
                                bret = true;
                                break;
                            }
                            // Mplus 또는 Robot 개별 stop 상태가 되면 강제 종료
                            // Stop 상태가되면 Cancel 후 재진행
                            bret = RvSenderList[e.vec.ID].IsControllerStop();
                            if (bret)
                            {
                                bret = !bret;
                                break;
                            }
                            Thread.Sleep(10);
                        }

                        if (!bret)
                            return bret;

                        // Src 작업을 Multi Job으로 생성하여 진행했을때 해당 Multi Job의 정보를 기존 Job에 저장하고 Multi Job은 삭제가 진행되는 로직
                        if (e != null && Jobs.Count > 0 && Jobs[foreachcount].C_dstFinishTime == null && Jobs[foreachcount].MULTIID != null)
                        {
                            // Src 설비가 1대 일 때
                            if (e.grp_count == 1)
                            {
                                // MultiID가 MI 이면
                                if (Jobs[0].MULTIID.Split('_')[0] == "MI")
                                {
                                    // Multi Job의 Data를 기존 Job에 저장하거나 유지하는 함수
                                    if (SrcMultiJobDeleteCheck_MI(ref Jobs, e, ref I_type_remakeJobcount, ref I_type_foreachcount, ref I_type_remakcount))
                                        break;
                                }
                                // MultiID가 MOI이거나 MEO, MTO이면서 Handler_Stack 설비 일 경우
                                else if (((Jobs[0].MULTIID.Split('_')[0] == "MOI") && Jobs[0].BATCHID.Split('_')[0].Contains("M"))||
                                    ((Jobs[foreachcount].MULTIID.Split('_')[0] == "MEO" || Jobs[foreachcount].MULTIID.Split('_')[0] == "MTO") && unit_chk.goaltype == (int)EqpGoalType.HANDLER_STACK))
                                {
                                    // Multi Job의 Data를 기존 Job에 저장하거나 유지하는 함수
                                    SrcMultiJobDeleteCheck_MOI_MEO_MTO(ref Jobs, e);
                                    break;
                                }
                            }
                            // Src 설비가 1대 초과 일 때
                            else
                            {
                                // MultiID가 MOI 이면
                                if (Jobs[0].MULTIID.Split('_')[0] == "MOI")
                                {
                                    // Multi Job의 Data를 기존 Job에 저장하거나 유지하는 함수
                                    if (SrcMultiJobDeleteCheck_MOI(ref Jobs, e, xxx, ref remakeJobcount, ref foreachcount, ref remakecount))
                                        break;
                                }
                            }
                        }
                    }
                }
                // WorkType이 EI 또는 TI 인 Job 일 경우
                else if (Jobs[0].WORKTYPE == "EI" || Jobs[0].WORKTYPE == "TI")
                {
                    // Job이 SRC 작업 완료 시간이 작성되지않았으면
                    if (Jobs[0].C_srcFinishTime == null)
                    {
                        Thread.Sleep(1);

                        // STK 용 First Thread로 이벤트 전송
                        CallSTKSrcArgs1 CSSA1 = new CallSTKSrcArgs1();
                        CSSA1.pep = Jobs[0];
                        CSSA1.vec = e.vec;

                        AddCallSTKSrcArgslist1(CSSA1);

                        var src_units = Db.Units.Where(p => p.ID == Jobs[0].S_EQPID).ToList();

                        if (src_units[0].goaltype == (int)EqpGoalType.STK)
                        {
                            // STK 용 First Thread에서 MoveComp까지 완료 했다는 Flag가 설정되면 While 넘어가기
                            while (!STKSrc_First_check)
                            {
                                if (RvSenderList[e.vec.ID].IsStop)
                                {
                                    bret = false;
                                    break;
                                }
                                Thread.Sleep(10);
                            }
                            if (!bret)
                                return bret;

                            // 위 Flag가 설정되었고 Jobs가 2개라면
                            // STK 용 Second Thread로 이벤트 전송
                            if (STKSrc_First_check && Jobs.Count == 2)
                            {
                                CallSTKSrcArgs2 CSSA2 = new CallSTKSrcArgs2();
                                CSSA2.pep = Jobs[1];
                                CSSA2.vec = e.vec;

                                AddCallSTKSrcArgslist2(CSSA2);
                                DoubleMoveJob.Add(e.vec.ID, Jobs[1].BATCHID);
                            }

                            // Jobs가 2개라면
                            if (Jobs.Count > 1)
                            {
                                while (true)
                                {
                                    bret = RvSenderList[e.vec.ID].IsControllerStop();
                                    if (bret)
                                    {
                                        bret = !bret;
                                        break;
                                    }
                                    // 첫번째 Job의 SRC 작업 완료 시간이 작성되면
                                    if (Jobs[0].C_srcFinishTime != null)
                                    {
                                        // STK 용 Second Thread에 Tray Take 작업 진행르 위한 Flag 이벤트 전송`
                                        CallSTKSrcArgs2_Flag CSSA2F = new CallSTKSrcArgs2_Flag();
                                        CSSA2F.STKSrc_Finish = true;

                                        AddCallSTKSrcArgslist2_Flag(CSSA2F);
                                        bret = true;
                                        break;
                                    }
                                    Thread.Sleep(10);
                                }
                                if (!bret)
                                    return bret;
                            }
                        }
                        
                        while (true)
                        {
                            bret = RvSenderList[e.vec.ID].IsControllerStop();
                            if (bret)
                            {
                                bret = !bret;
                                break;
                            }
                            // 현재 진행중인 Jobs 중 마지막 Job의 Src 완료 시간이 작성되면
                            if (Jobs[Jobs.Count() - 1].C_srcFinishTime != null)
                            {
                                // Flag 변수 초기화 후 Dst 작업으로 이동
                                STKSrc_First_check = false;
                                STKSrc_sec = false;
                                bret = true;
                                break;
                            }
                            Thread.Sleep(10);
                        }
                    }
                }
                if (!bret)
                    return bret;
                JobProcList[e.vec.ID].jobType = "DST";

                // Dst 작업에대한 Multi Job 생성 함수
                bret = ProcessDstJob(IsMultiJob, ref Jobs, e);

                if (!bret)
                    return bret;

                if (Jobs[0].WORKTYPE == "I" && e.grp_count == 1)
                    Jobs.Reverse();
                //    Jobs = Jobs.OrderBy(p => p.ORDER).ToList();

                //// Job이 TI 또는 EI 이고 Jobs가 2개 이상일때
                //if ((Jobs[0].WORKTYPE == "TI" || Jobs[0].WORKTYPE == "EI") && Jobs.Count() > 1)
                //    // Jobs의 순서를 뒤집는다
                //    // Tray의 수가 많은 Job부터 진행하기 위함
                //    Jobs.Reverse();

                foreach (var xxx in Jobs)
                {
                    Thread.Sleep(1);
                    isControl_Pause();

                    // 현재 Job의 DstFinishTime이 작성되어있지않으면
                    if (xxx.C_dstFinishTime != null)
                        continue;

                    // Robot과 Job에 각각 BatchID와 Robot ID Update
                    UpdateVehicleByID(VecId, xxx.BATCHID);

                    // 현재 Job을 CurJob에 저장
                    RvSenderList[e.vec.ID].CurJob = xxx;

                    bret = OrderToVehicle(JobProcList[e.vec.ID].jobType, xxx, e.vec);
                    if (!bret)
                        break;
                    while (true)
                    {
                        // 현재 Job의 DstFinishTime이 작성되면 While문 중지
                        if (xxx.C_dstFinishTime != null)
                        {
                            bret = true;
                            break;
                        }
                        // Robot 개별 Stop, Mpus Stop이되면 bret을 false로 변경 후 break;
                        bret = RvSenderList[e.vec.ID].IsControllerStop();
                        if (bret)
                        {
                            bret = !bret;
                            break;
                        }
                        Thread.Sleep(10);
                    }
                }
                if (!bret)
                    return bret;

                // JobProccess에서 사용되는 전역 변수 초기화
                VehicleList[e.vec.ID].JobAssign = true;
                RvSenderList[e.vec.ID].IsMulti = false;
                RvSenderList[e.vec.ID].MultiList = null;
                RvSenderList[e.vec.ID].CurJob = null;
                RvSenderList[e.vec.ID].HandlerStandby = false;
                // 현재 사용된 Robot의 isAssigned를 0으로 변경하여 다른 Job을 할당 받을수있게 함
                e.vec.isAssigned = 0;
                Db.DbUpdate(TableType.VEHICLE);
                return bret;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        bool STKSrc_First_check = false;
        bool STKSrc_sec = false;
        public bool StackSrcProcedure1(CallSTKSrcArgs1 e)
        {
            bool bret = false;
            Thread.Sleep(1);
            isControl_Pause();
            if (e.pep.C_dstFinishTime != null)
                return true;

            UpdateVehicleByID(VecId, e.pep.BATCHID);

            RvSenderList[e.vec.ID].CurJob = e.pep;

            bret = OrderToVehicle(JobProcList[e.vec.ID].jobType, e.pep, e.vec);
            if (!bret)
                return false;

            while (true)
            {
                if (e.pep.C_dstFinishTime != null)
                    break;
                bret = RvSenderList[e.vec.ID].IsControllerStop();
                if (bret)
                {
                    bret = !bret;
                    break;
                }

                Thread.Sleep(10);
            }

            return true;
        }
        public bool StackSrcProcedure2(CallSTKSrcArgs2 e)
        {
            bool bret = false;
            Thread.Sleep(1);
            isControl_Pause();
            if (e.pep.C_dstFinishTime != null)
                return true;

            UpdateVehicleByID(VecId, e.pep.BATCHID);

            RvSenderList[e.vec.ID].CurJob = e.pep;

            bret = OrderToVehicle(JobProcList[e.vec.ID].jobType, e.pep, e.vec);
            if (!bret)
                return false;

            while (true)
            {
                if (e.pep.C_dstFinishTime != null)
                    break;
                bret = RvSenderList[e.vec.ID].IsControllerStop();
                if (bret)
                {
                    bret = !bret;
                    break;
                }

                Thread.Sleep(10);
            }

            return true;
        }
        public void OnMultiSrcFinish(MultiSrcFinishArgs e, List<pepschedule> Jobs, int I_type_remakcount = 0)
        {
            // _virtualPep 초기화
            _virtualPep.ZeroMem();
            // _virtualPep에 현재 Job 저장
            _virtualPep.Copy(e.pep);

            // 지워야하는 Multi Job이 있는지 확인 후 삭제
            multiJob_Delete(ref Jobs, I_type_remakcount);            

            // Worktype이 EO, TO 일 경우 Jobs 순서 Reverse
            //if (Jobs[0].WORKTYPE == "EO" || Jobs[0].WORKTYPE == "TO")
                //Jobs.Reverse();

            // Stack 반송시 사용될 변수 저장
            // Jobs에 있는 모든 Job의 TrayID 저장
            string[] job_tray_num = stack_tray_check(Jobs);

            if (_virtualPep.BATCHID.Contains("MSRC") || _virtualPep.BATCHID.Contains("MDST"))
            {
                foreach (var v in Jobs)
                {
                    // _virtualPep의 Src 설비명과 현재 Job의 Src 설비명이 같고 BatchID가 MSRC 가 아닐 때
                    // BatchID가 MSRC가 아닌 조건이 들어가는 이유는 Multi로 묶이지 않은 Job에 Data를 저장해야하기 때문
                    if (_virtualPep.S_EQPID == v.S_EQPID && !(v.BATCHID.Contains("MSRC")))
                    {
                        v.C_VEHICLEID = _virtualPep.C_VEHICLEID;
                        v.C_state = _virtualPep.C_state;
                        v.C_srcStartTime = _virtualPep.C_srcStartTime;
                        v.C_srcArrivingTime = _virtualPep.C_srcArrivingTime;
                        v.C_srcAssignTime = _virtualPep.C_srcAssignTime;
                        v.C_srcFinishTime = _virtualPep.C_srcFinishTime;

                        // bufslot이 비어있으면
                        if (v.C_bufSlot == null || v.C_bufSlot == "")
                        {
                            // TransferType이 Tray 일 때
                            if (_virtualPep.TRANSFERTYPE == "TRAY")
                            {
                                // _virtualPep에 있는 Tray와 현재 Job의 Tray를 비교하여 같을 때 Data 저장
                                v.C_bufSlot = batchJob_tray_data(_virtualPep.TRAYID.Split(','), v.TRAYID.Split(','), _virtualPep.C_bufSlot.Split(','));
                            }
                            else if (_virtualPep.TRANSFERTYPE == "STACK")
                            {
                                v.C_bufSlot = batchJob_stack_data(_virtualPep.TRAYID.Split(','), v.TRAYID.Split(','), _virtualPep.C_bufSlot.Split(','), job_tray_num);
                            }
                        }
                        // bufslot이 비어있지않으면
                        else
                        {
                            // 정보가 없는 Tray 찾기
                            int emptyslotchk = tray_empty_check(v);

                            // 정보가 없는 Tray가 있으면
                            if (emptyslotchk != 0)
                            {
                                // 정보가 없는 Tray 부터 이어서 저장
                                v.C_bufSlot = batchJob_tray_empty_select(v, v.C_bufSlot.Split(','), emptyslotchk);
                            }
                        }
                    }
                }
            }
        }
        public List<pepschedule> SelectMultiList(bool ismulti, eMultiJobWhere eMulti, int worktype, string grplst_EQPID, string ExecuteTime)
        {
            List<pepschedule> Jobs = null;
            if (eMulti == eMultiJobWhere.SRC)
            {
                Jobs = Db.Peps.Where(p => p.REAL_TIME == ExecuteTime &&
                                            p.C_priority == (int)worktype &&
                                            p.C_dstFinishTime == null &&
                                            p.BATCHID.Split('_')[0] != "MSRC" &&
                                            p.C_isChecked == 1 &&
                                            p.S_EQPID == grplst_EQPID).ToList();
            }
            else
            {
                Jobs = Db.Peps.Where(p => p.REAL_TIME == ExecuteTime &&
                                            p.C_priority == (int)worktype &&
                                            p.C_dstFinishTime == null &&
                                            p.C_isChecked == 1 &&
                                            p.T_EQPID == grplst_EQPID).ToList();
            }

            return Jobs;

        }

        public List<pepschedule> SelectMultiList_OI(bool ismulti, eMultiJobWhere eMulti, int worktype, string ExecuteTime)
        {
            List<pepschedule> Jobs = null;
            if (eMulti == eMultiJobWhere.SRC)
            {
                Jobs = Db.Peps.Where(p => p.REAL_TIME == ExecuteTime &&
                                            p.C_priority == (int)worktype &&
                                            p.C_dstFinishTime == null &&
                                            p.BATCHID.Split('_')[0] != "MSRC").ToList();
            }
            else
            {
                Jobs = Db.Peps.Where(p => p.REAL_TIME == ExecuteTime &&
                                            p.C_priority == (int)worktype &&
                                            p.C_dstFinishTime == null).ToList();
            }
            return Jobs;

        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        private bool ProcessSrcJob(bool IsMultiJob, ref List<pepschedule> Jobs, CallCmdProcArgs1 e, ref int remakecount, ref int I_type_remakcount)
        {
            Thread.Sleep(1);
            try
            {
                List<MulGrp> mulgrplist = new List<MulGrp>();

                // 선정한 Job이 MultiJob 일 경우
                if (IsMultiJob)
                {
                    // Multi ID 작성
                    string multiId = string.Format("M{0}_{1}", Jobs[0].WORKTYPE.ToString(), DateTime.Now.ToString("ddHHmmssfff"));
                    // 선정된 Job들에 Multi ID 저장
                    foreach (var v in Jobs)
                    {
                        v.MULTIID = multiId;
                    }

                    // VSP에서 선정된 Multi Job 방향의 반대쪽에도 Multi Job이 생성될수 있는지 확인
                    mulgrplist = (e.where == eMultiJobWhere.SRC) ? FindSubMultiJob(Jobs, eMultiJobWhere.DST) :
                                                                              FindSubMultiJob(Jobs, eMultiJobWhere.SRC);

                    // Worktype이 OI 이고 Src 설비가 1대 초과 일 때
                    // OI는 시작할때 eMultiJobWhere가 DST로 시작
                    // Src 설비가 1대 초과이면 I 작업 로직을 따라야하여 SRC로 변경
                    if (Jobs[0].WORKTYPE == "OI" && e.grp_count > 1)
                        e.where = eMultiJobWhere.SRC;

                    if (!VehicleList[e.vec.ID].IsConnected)
                        return false;

                    // Multi Job의 방향이 SRC 일 때
                    if (e.where == eMultiJobWhere.SRC)
                    {
                        // SRC 작업의 SRC 방향 Multi Job 생성 함수
                        
                        return SRCJobwhereSRCProcces(ref Jobs, mulgrplist, e, ref remakecount, ref I_type_remakcount);
                    }
                    // Multi Job의 방향이 DST 일 때
                    else
                    {
                        // SRC 작업의 DST 방향 Multi Job 생성 함수
                        return SRCJobwhereDSTProcces(ref Jobs, mulgrplist, e, ref remakecount, ref I_type_remakcount);                        
                    }
                }       
                // Multi Job이 아닐 때
                else
                {
                    string dst_eqpid = Jobs[0].T_EQPID;
                    // DB에서 Job의 DST 설비를 가져오기
                    unit un = Db.Units.Where(p => p.GOALNAME == dst_eqpid).SingleOrDefault();
                    // Job의 Worktype이 EI 또는 TI 이고 DST 설비가 Reflow 일 때
                    if ((Jobs[0].WORKTYPE == "EI" || Jobs[0].WORKTYPE == "TI") && un.goaltype == (int)EqpGoalType.REFLOW)
                    {
                        // Tray 개수가 10개 이상일 때 remake Job 생성을 위해
                        // reamake 함수로 이동
                        if (!SRCremakejob_SRC_EI_TI(ref Jobs, e, Jobs[0].MULTIID, ref remakecount, ref I_type_remakcount, true))
                            return false;
                    }
                    SortMultiJobListToVehicle(ref mulgrplist, e.vec.ID);
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }

        }

        private bool ProcessDstJob(bool IsMultiJob, ref List<pepschedule> Jobs, CallCmdProcArgs1 e)
        {
            Thread.Sleep(1);
            try
            {
                if (e.vec == null)
                    return false;

                if (!VehicleList[e.vec.ID].IsConnected)
                    return false;

                List<MulGrp> mulgrplist = new List<MulGrp>();
                // OI 작업이 여러 설비로 Multi Job이 생성될 때
                if (Jobs[0].WORKTYPE == "OI" && e.grp_count > 1)
                {                    
                    mulgrplist = (e.where == eMultiJobWhere.SRC) ? FindSubMultiJob(Jobs, eMultiJobWhere.DST) :
                                                                              FindSubMultiJob(Jobs, eMultiJobWhere.SRC);
                    e.where = eMultiJobWhere.DST;
                }

                // Multi Job일 때
                if (IsMultiJob)
                {
                    int mulgrplist_count = 0;

                    if (e.where == eMultiJobWhere.DST)
                    {
                        // DST 설비를 기준으로 SRC 설비를 remake
                        return DSTJobwhereSRCProcces(ref Jobs, mulgrplist, e, ref mulgrplist_count);
                    }
                    else
                    {
                        // DST 설비를 기준으로 DST 설비를 remake
                        return DSTJobwhereDSTProcces(ref Jobs, mulgrplist, e, ref mulgrplist_count);
                    }
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        private List<OrderGrp> FindOrderGroup(List<MulGrp> multijobs)
        {
            List<OrderGrp> itemlist
                = multijobs.GroupBy(p => new { ORDER_NUM = p.ORDER_NUM})
                .Select(group => new OrderGrp() { ORDER_NUM = group.Key.ORDER_NUM, COUNT = group.Count()})
                .OrderBy(x => x.ORDER_NUM).ThenByDescending(x => x.COUNT)
                .ToList();

            return itemlist;
        }
        private List<MulGrp> FindSubMultiJob(List<pepschedule> multijobs, eMultiJobWhere where)
        {
            List<MulGrp> itemlist
                = multijobs.GroupBy(p => new { PRIORITY = p.C_priority, EQPID = (where == eMultiJobWhere.SRC ? p.S_EQPID : p.T_EQPID), ORDER_NUM = p.ORDER })
                           .Select(group => new MulGrp() { PRIORITY = group.Key.PRIORITY.Value, EQPID = group.Key.EQPID, ORDER_NUM = group.Key.ORDER_NUM, COUNT = group.Count() })
                           .OrderBy(x => x.PRIORITY).ThenByDescending(x => x.COUNT)
                           .ToList();
            return itemlist;
        }

        /// <summary>
        /// 상대 방향에 Sub 멀티그룹이 있는지 조사
        /// 만약 MultiJobs 이 Src 에서 그룹핑되어 있다고 가정하면, Dst 쪽에서 SubMultiGrp 조사
        /// </summary>
        /// <param name="multijobs">Src 또는 Dst 의 동일 설비명으로 그룹핑된 최상위 MultiJobs 리스트</param>
        /// <param name="where">eMultiJobWhere.SRC 또는 eMultiJobWhere.DST, 그룹핑 대상이 source 설비인지 target 설비인지</param>
        /// <returns> List<MulGrp> </returns>
        //private List<MulGrp> FindSubMultiJob(List<pepschedule> multijobs, eMultiJobWhere where)
        //{
        //    List<MulGrp> itemlist
        //        = multijobs.GroupBy(p => new { PRIORITY = p.C_priority, EQPID = (where == eMultiJobWhere.SRC ? p.S_EQPID : p.T_EQPID) })
        //                   .Select(group => new MulGrp() { PRIORITY = group.Key.PRIORITY.Value, EQPID = group.Key.EQPID, COUNT = group.Count() })
        //                   .OrderBy(x => x.PRIORITY).ThenByDescending(x => x.COUNT)
        //                   .ToList();
        //    return itemlist;
        //}

        private List<pepschedule> FindSubMultiJobList(List<pepschedule> multijobs, MulGrp obj, eMultiJobWhere where, string ExecuteTime)
        {
            // 기준이 SRC이면 SRC 설비명으로 Job 가져오기
            if (where == eMultiJobWhere.SRC)
            {
                List<pepschedule> itemlist
                = multijobs.Where(p => p.EXECUTE_TIME == ExecuteTime &&
                                       p.C_priority == obj.PRIORITY &&
                                       obj.EQPID == p.S_EQPID &&
                                       obj.ORDER_NUM == p.ORDER)
                           .OrderBy(p => p.C_priority)
                           .ToList();
                return itemlist;
            }
            // 기준이 DST이면 DST 설비명으로 Job 가져오기
            else
            {
                List<pepschedule> itemlist
                = multijobs.Where(p => p.EXECUTE_TIME == ExecuteTime &&
                                       p.C_priority == obj.PRIORITY &&
                                       obj.EQPID == p.T_EQPID &&
                                       obj.ORDER_NUM == p.ORDER)
                           .OrderBy(p => p.C_priority)
                           .ToList();
                return itemlist;
            }
        }

        //private List<pepschedule> FindSubMultiJobList(List<pepschedule> multijobs, MulGrp obj, eMultiJobWhere where, string ExecuteTime)
        //{
        //    // 기준이 SRC이면 SRC 설비명으로 Job 가져오기
        //    if (where == eMultiJobWhere.SRC)
        //    {
        //        List<pepschedule> itemlist
        //        = multijobs.Where(p => p.EXECUTE_TIME == ExecuteTime &&
        //                               p.C_priority == obj.PRIORITY &&
        //                               obj.EQPID == p.S_EQPID)
        //                   .OrderBy(p => p.C_priority)
        //                   .ToList();
        //        return itemlist;
        //    }
        //    // 기준이 DST이면 DST 설비명으로 Job 가져오기
        //    else
        //    {
        //        List<pepschedule> itemlist
        //        = multijobs.Where(p => p.EXECUTE_TIME == ExecuteTime &&
        //                               p.C_priority == obj.PRIORITY &&
        //                               obj.EQPID == p.T_EQPID)
        //                   .OrderBy(p => p.C_priority)
        //                   .ToList();
        //        return itemlist;
        //    }
        //}
        private void SortMultiJobListToVehicle(ref List<MulGrp> itemlist, string vecid)
        {
            List<OrderGrp> orderGrps = FindOrderGroup(itemlist);
            List<MulGrp> sortitemlist = new List<MulGrp>();
            List<MulGrp> whereitemlist = new List<MulGrp>();
            foreach (var val in orderGrps)
            {
                // 현시점 x,y 에서 각 장비들의 위치를 따져서 path를 구하고 eqpGrp 를 ordering 한다
                List<unit> unitlist = new List<unit>();
                whereitemlist = itemlist.Where(p => p.ORDER_NUM == val.ORDER_NUM).ToList();
                foreach (var w in whereitemlist)
                {
                    try
                    {
                        unit unt = Db.Units.Where(p => p.GOALNAME == w.EQPID).FirstOrDefault();
                        if (unt == null)
                            continue;
                        unitlist.Add(unt);
                    }
                    catch (Exception ex)
                    {
                        Logger.Inst.Write(vecid, CmdLogType.Rv, $"Exception. ProcessDstJob. {ex.Message}\r\n{ex.StackTrace}");
                    }
                }

                List<TempNearestGoalStruct> distlist = GetGoalNameNearestInUnitList(unitlist, VehicleList[vecid].Csby.propertyCurrVecStatus.posX, VehicleList[vecid].Csby.propertyCurrVecStatus.posY);


                var query = from A in distlist
                            join B in whereitemlist on A.goalName equals B.EQPID 
                            select new MulGrp() { PRIORITY = B.PRIORITY, EQPID = B.EQPID, ORDER_NUM = B.ORDER_NUM, COUNT = B.COUNT };

                foreach (var x in query)
                {
                    sortitemlist.Add(x);
                }
            }
            itemlist = sortitemlist;
        }

        private void SortMultiJobListToEqp(ref List<MulGrp> itemlist, string eqpid)
        {
            List<OrderGrp> orderGrps = FindOrderGroup(itemlist);
            List<MulGrp> sortitemlist = new List<MulGrp>();
            List<MulGrp> whereitemlist = new List<MulGrp>();
            foreach (var val in orderGrps)
            {
                // 현시점 x,y 에서 각 장비들의 위치를 따져서 path를 구하고 eqpGrp 를 ordering 한다
                List<unit> unitlist = new List<unit>();
                whereitemlist = itemlist.Where(p => p.ORDER_NUM == val.ORDER_NUM).ToList();
                foreach (var w in whereitemlist)
                {
                    try
                    {
                        unit unt = Db.Units.Where(p => p.GOALNAME == w.EQPID).FirstOrDefault();
                        if (unt == null)
                            continue;
                        unitlist.Add(unt);
                    }
                    catch (Exception ex)
                    {
                        Logger.Inst.Write(CmdLogType.Rv, $"Exception. ProcessDstJob. {ex.Message}\r\n{ex.StackTrace}");
                    }
                }

                unit unt1 = Db.Units.Where(p => p.GOALNAME == eqpid).FirstOrDefault();
                if (unt1 == null)
                    return;

                List<TempNearestGoalStruct> distlist = GetGoalNameNearestInUnitList(unitlist, (float)unt1.loc_x, (float)unt1.loc_y);


                var query = from A in distlist
                            join B in whereitemlist on A.goalName equals B.EQPID
                            select new MulGrp() { PRIORITY = B.PRIORITY, EQPID = B.EQPID, ORDER_NUM = B.ORDER_NUM, COUNT = B.COUNT };
                
                foreach (var x in query)
                {
                    sortitemlist.Add(x);
                }
            }
            itemlist = sortitemlist;
        }

        //private void SortMultiJobList1(ref List<MulGrp> itemlist, string eqpid)
        //{
        //    // 현시점 x,y 에서 각 장비들의 위치를 따져서 path를 구하고 eqpGrp 를 ordering 한다
        //    List<unit> unitlist = new List<unit>();
        //    foreach (var w in itemlist)
        //    {
        //        try
        //        {
        //            unit unt = Db.Units.Where(p => p.GOALNAME == w.EQPID).FirstOrDefault();
        //            if (unt == null)
        //                continue;
        //            unitlist.Add(unt);
        //        }
        //        catch (Exception ex)
        //        {
        //            Logger.Inst.Write(CmdLogType.Rv, $"Exception. ProcessDstJob. {ex.Message}\r\n{ex.StackTrace}");
        //        }
        //    }

        //    unit unt1 = Db.Units.Where(p => p.GOALNAME == eqpid).FirstOrDefault();
        //    if (unt1 == null)
        //        return;

        //    List<TempNearestGoalStruct> distlist = GetGoalNameNearestInUnitList(unitlist, (float)unt1.loc_x, (float)unt1.loc_y);


        //    var query = from A in distlist
        //                join B in itemlist on A.goalName equals B.EQPID
        //                select new MulGrp() { PRIORITY = B.PRIORITY, EQPID = B.EQPID, COUNT = B.COUNT };
        //    itemlist = query.ToList();
        //}

        private List<TempNearestGoalStruct> GetGoalNameNearestInUnitList(List<unit> goalList, float x, float y)
        {
            var resArry = new TempNearestGoalStruct[goalList.Count];
            Parallel.For(0, goalList.Count, i =>
            {
                resArry[i] = new TempNearestGoalStruct() { goalDist = GetDistanceBetweenPoints(new PointF(x, y), new PointF((float)goalList[i].loc_x, (float)goalList[i].loc_y)), goalName = goalList[i].GOALNAME };
            });

            return resArry.OrderBy(p => p.goalDist).ToList();
        }

        private float GetDistanceBetweenPoints(PointF a, PointF b)
        {
            return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }


        /// <summary>
        /// 택 타임을 고려하여 최대 10개씩 모아서 처리할 수 있도록 JOB 을 통합, 재생성한다.
        /// </summary>
        /// <param name="multijobs">선정된 Job을 가져와 Remake된 Job으로 변경</param>
        /// <param name="where">Multi Job 기준 방향 SRC : 1, DST : 2</param>
        /// <param name="mulgrplist_count">Multi Job 생성 번호</param>
        /// <param name="bDelete">Remake에 사용되는 Job을 삭제 여부 true = 삭제, false = 유지</param>
        /// <param name="tray_over">현재 Job의 Tray 개수가 설비 Port를 초과하는지 여부 true = 초과, false = 허용범위, 초과시 Job 분할</param>
        /// <returns></returns>
        /// <note>입력 받은 job schedule 과 새롭게 생성한 스케쥴을 어떻게 처리할 것인가???</note>
        /// <note>1. 기존 job schedule 은 시간 update 하고 history 테이블로 옮긴다.</note>
        /// <note>1.1. 새로 생성한 스케쥴은 리스트</note>
        private bool RemakeMultiJobs(ref List<pepschedule> multijobs, int where, ref int mulgrplist_count, bool bDelete = false, bool tray_over = false)
        {
            pepschedule pep = new pepschedule();
            // multijobs의 첫번째 Job을 pep에 복사
            PepsDeepCopy(pep, multijobs[0]);

            // Job Remake시 가공된 Data들을 저장할 Class
            JobRemakeWords jobrewords = new JobRemakeWords();

            multijobs = multijobs.OrderBy(p => p.ORDER).ToList();

            // multijob이 1개 초과 또는 tray_over가 true 일 때
            // multijob이 1개 초과 일 때는 MultiJob으로 Remake 진행을 해야함
            // tray_over가 true이면 Job을 분할해야함 (ex. STK의 경우 Tray 10장이 최대인 Job으로 분할)
            if (multijobs.Count > 1 || tray_over == true)
            {                
                multijobs = Job_remake(where, ref mulgrplist_count, tray_over, pep, ref multijobs, bDelete, jobrewords);
            }
            Thread.Sleep(2000);
            return true;
        }
        private bool OrderToVehicle(string cmd, pepschedule job, vehicle vec = null)
        {
            bool bret = false;
            vehicle _vec = vec;
            if (cmd == "DST")
            {
                List<vehicle> list = Db.Vechicles.Where(p => p.ID == job.C_VEHICLEID && p.isUse == 1).ToList();
                Debug.Assert(list.Count() != 0);
                // SRC job 에 사용되었던 vehicle 을 재할당
                _vec = list[0];
            }

            Thread.Sleep(1);

            int traycount = 0;
            string eqp_portslot = string.Empty;
            string buf_portslot = string.Empty;
            List<unit> units = new List<unit>();

            // 설비 PortSlot을 Robot에 전송될 Data로 가공
            if (!MakePortSlot(cmd, job, _vec, ref units, ref eqp_portslot, ref traycount)) return false;
            // Robot에 사용될 BuffSlot이 있는지 확인 및 Data 가공
            if (!MakeBuffSlot(cmd, job, _vec, ref buf_portslot, traycount)) return false;

            var args = new SendJobToVecArgs1() { vecID = _vec.ID, job = job, vec = _vec, eqp = units[0] };
            args.cmd = cmd;
            args.eqpslot = eqp_portslot;
            args.bufslot = buf_portslot;

            JobSendComplete = false;
            bret = OnSendJobToVehicle(args);

            if (!bret)
                return bret;
            int cnt = 0;
            DateTime dtold = DateTime.Now;
            while (cnt++ < 6000 * 60)
            {
                if (JobSendComplete)
                    break;

                Thread.Sleep(200);
            }
            return bret;
        }

        private void isControl_Pause()
        {
            var controller = Db.Controllers.SingleOrDefault();
            while (controller.C_state == (int)ControllerState.PAUSED)
            {
                continue;
            }
        }
        private void PepsDeepCopy(pepschedule dst, pepschedule src)
        {
            dst.ID = src.ID;
            dst.MULTIID = src.MULTIID;
            dst.BATCHID = src.BATCHID;
            dst.S_EQPID = src.S_EQPID;
            dst.S_PORT = src.S_PORT;
            dst.S_SLOT = src.S_SLOT;
            dst.T_EQPID = src.T_EQPID;
            dst.T_PORT = src.T_PORT;
            dst.T_SLOT = src.T_SLOT;
            dst.TRAYID = src.TRAYID;
            dst.WORKTYPE = src.WORKTYPE;
            dst.TRANSFERTYPE = src.TRANSFERTYPE;
            dst.WINDOW_TIME = src.WINDOW_TIME;
            dst.EXECUTE_TIME = src.EXECUTE_TIME;
            dst.REAL_TIME = src.REAL_TIME;
            dst.STATUS = src.STATUS;
            dst.LOT_NO = src.LOT_NO;
            dst.QTY = src.QTY;
            dst.STEPID = src.STEPID;
            dst.S_STEPID = src.S_STEPID;
            dst.T_STEPID = src.T_STEPID;
            dst.URGENCY = src.URGENCY;
            dst.FLOW_STATUS = src.FLOW_STATUS;
            dst.C_VEHICLEID = src.C_VEHICLEID;
            dst.C_bufSlot = src.C_bufSlot;
            dst.C_state = src.C_state;
            dst.C_srcAssignTime = src.C_srcAssignTime;
            dst.C_srcArrivingTime = src.C_srcArrivingTime;
            dst.C_srcStartTime = src.C_srcStartTime;
            dst.C_srcFinishTime = src.C_srcFinishTime;
            dst.C_dstAssignTime = src.C_dstAssignTime;
            dst.C_dstArrivingTime = src.C_dstArrivingTime;
            dst.C_dstStartTime = src.C_dstStartTime;
            dst.C_dstFinishTime = src.C_dstFinishTime;
            dst.C_isChecked = src.C_isChecked;
            dst.C_priority = src.C_priority;
            dst.DOWNTEMP = src.DOWNTEMP;
            dst.EVENT_DATE = src.EVENT_DATE;
            dst.ORDER = src.ORDER;
        }

        private void StringAdd(ref string total, string trayids, char delimeter)
        {
            if (total.Length == 0)
            {
                if (!string.IsNullOrEmpty(trayids))
                    total = trayids;
            }
            else
            {
                if (total[total.Length - 1] == delimeter)
                {
                    if (!string.IsNullOrEmpty(trayids))
                    {
                        total += trayids;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(trayids))
                        total += string.Format("{0}{1}", delimeter, trayids);
                    else
                        total += string.Format("{0}{1}", delimeter, "");
                }
            }
        }

#region makeportslot
        private string makeportslot_stk(pepschedule job, string slot, ref int traycount)
        {
            string tslot = string.Empty;
            // 설비의 Port에 따라 Data 저장
            tslot = portslot_stk_check(slot);
            if (string.IsNullOrEmpty(tslot))
                return string.Empty;

            // 설비의 Port가 AUTO (Tray 반송) 일 때
            if (slot.Contains("AUTO"))
            {
                traycount = job.TRAYID.Split(',').Count();
            }
            // 설비의 Port가 STACK (Stack 반송) 일 때
            else if (slot.Contains("STACK"))
            {
                // Stack 반송 시 Robot의 한 Slot에 들어가는 Tray는 10장이다.
                traycount = Proc_Atom.Init.traycount_result(job);
            }

            // traycount에 맞추어 설비의 PortSlot Data return
            return eqp_portslot_value(traycount, tslot);
        }
        private string makeportslot_handler(pepschedule job, unit unti, string slot, ref int traycount)
        {
            string eqp_portslot = string.Empty;

            string tslot = string.Empty;

            // 설비 goaltype과 Port에 따라 Data 저장
            tslot = portslot_handler_check(unti.goaltype, slot);
            if (string.IsNullOrEmpty(tslot))
                return string.Empty;

            if (unti.goaltype == (int)EqpGoalType.HANDLER)
            {                
                traycount = job.TRAYID.Split(',').Count();
            }
            else if (unti.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                traycount = slot.Split(',').Count();
                // portslot_handler_check에서 Robot으로 보낼 PortSlotNo을 만들었으므로
                // 다른 설비와 다르게 eqp_portslot_value를 거치지않고
                // 바로 return 한다.
                return tslot;
            }

            // traycount에 맞추어 설비의 PortSlot Data return
            return eqp_portslot_value(traycount, tslot);
        }
        private string makeportslot_syswin(string cmd, pepschedule job, vehicle vec, unit unti, string eqp, string slot, ref int traycount)
        {
            string eqp_portslot = string.Empty;

            // Job에 지정된 PortSlot을 ','으로 Split하여 저장
            string[] words = slot.Split(',');
            // Split된 Data의 수를 traycount에 저장
            traycount = words.Count();

            int portNo = -1;
            int slotNo = -1;
            for (int n = 0; n < words.Count(); n++)
            {
                // Job에 지정된 PortSlot은 알파벳+숫자의 형태로 Robot에서 사용하는 Data로 가공
                (portNo, slotNo) = makeportNo_slotNo(words[n], unti);

                if (portNo < 0 || slotNo < 0)
                    return string.Empty;

                // 가공된 portNo과 slotNo을 저장
                make_eqpportslot(n, ref eqp_portslot, portNo, slotNo);
            }

            // 저장된 Data를 return
            return eqp_portslot;
        }
        private string makeportslot_reflow(pepschedule job, string slot, ref int traycount)
        {
            string eqp_portslot = string.Empty;

            // 설비의 Port에 따라 Data 저장
            // Reflow의 Port 명은 STK의 Port 비교와 같으므로 동일 함수 사용
            string tslot = portslot_stk_check(slot);
            if(string.IsNullOrEmpty(tslot))
                return string.Empty;

            // Reflow는 Stack 반송만 있으므로 Stack traycount 계산방식 사용
            traycount = Proc_Atom.Init.traycount_result(job);

            // traycount에 맞추어 설비의 PortSlot Data return
            return eqp_portslot_value(traycount, tslot);
        }
        private void makeportslot_cancel(pepschedule job, string eqp, string msg, CmdState states)
        {
            Logger.Inst.Write(CmdLogType.Db, msg);
            job.C_srcAssignTime = DateTime.Now;
            job.C_dstAssignTime = DateTime.Now;
            job.C_state = (int)states;
            Db.DbUpdate(TableType.PEPSCHEDULE);
            Db.CopyCmdToHistory(job.ID.ToString());
            Db.Delete(job);
        }
        private bool MakePortSlot(string cmd, pepschedule job, vehicle vec, ref List<unit> units, ref string eqp_portslot, ref int traycount)
        {
            string eqp = (cmd == "DST") ? job.T_EQPID : job.S_EQPID;
            string slot = (cmd == "DST") ? job.T_SLOT : job.S_SLOT;

            try
            {
                // 해당 설비가 DB에 등록되어있는지 확인
                units = Db.Units.Where(p => p.ID == eqp).ToList();
                if (units == null || units.Count() == 0)
                {
                    Logger.Inst.Write(vec.ID, CmdLogType.All, $"SendMissionToVehicle. Invalid EQNAME = {eqp}");
                }
                
                // 설비가 STK 일 때
                if (units[0].goaltype == (int)EqpGoalType.STK)
                {
                    // 설비 PortSlot을 Robot에 보낼 Data로 가공
                    eqp_portslot = makeportslot_stk(job, slot, ref traycount);
                    portslot_error_msg(eqp_portslot, units[0], slot, vec.ID);
                }
                // 설비가 HANDLER, HANDLER_STACK 일 때
                else if (units[0].goaltype == (int)EqpGoalType.HANDLER || units[0].goaltype == (int)EqpGoalType.HANDLER_STACK)
                {
                    // 설비 PortSlot을 Robot에 보낼 Data로 가공
                    eqp_portslot = makeportslot_handler(job, units[0], slot, ref traycount);
                    portslot_error_msg(eqp_portslot, units[0], slot, vec.ID);
                }
                // 설비가 SYSWIN, SYSWIN_OVEN, SYSWIN_OVEN_t 일 때
                else if (units[0].goaltype == (int)EqpGoalType.SYSWIN_OVEN || units[0].goaltype == (int)EqpGoalType.SYSWIN
                     || units[0].goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || units[0].goaltype == (int)EqpGoalType.BUFFER_STK)
                {
                    // 설비 PortSlot을 Robot에 보낼 Data로 가공
                    eqp_portslot = makeportslot_syswin(cmd, job, vec, units[0], eqp, slot, ref traycount);
                    portslot_error_msg(eqp_portslot, units[0], slot, vec.ID);
                }
                // 설비가 REFLOW 일 때
                else if (units[0].goaltype == (int)EqpGoalType.REFLOW)
                {
                    // 설비 PortSlot을 Robot에 보낼 Data로 가공
                    eqp_portslot = makeportslot_reflow(job, slot, ref traycount);
                    portslot_error_msg(eqp_portslot, units[0], slot, vec.ID);
                }
                else
                {
                    eqp_portslot = string.Empty;
                    Logger.Inst.Write(vec.ID, CmdLogType.All, $"makeportslot. Invalid goaltype = {units[0].goaltype}");
                }
                if (cmd == "SRC")
                    job.S_PORT = eqp_portslot;
                else
                    job.T_PORT = eqp_portslot;
                Db.DbUpdate(TableType.PEPSCHEDULE);
            }
            catch (UtilMgrCustomException ex)
            {
                Logger.Inst.Write(vec.ID, CmdLogType.Rv, $"Exception. MakePortSlot. {ex.Message}\r\n{ex.StackTrace}");
                // 예외상황 시 처리
                makeportslot_cancel(job, eqp, ex.Message, CmdState.INVALID_SLOT);
                return false;
            }
            return true;
        }
        private bool MakeBuffSlot(string cmd, pepschedule job, vehicle vec, ref string buf_portslot, int traycount)
        {
            // 현재 DST 작업 진행중이면
            if (cmd == "DST")
            {
                // Job의 bufslot Data 저장
                buf_portslot = job.C_bufSlot;
            }
            // 현재 SRC 작업 진행중이면
            else
            {
                // DB 상에 비어있는 Robot의 PortSlot의 위치를 traycount 만큼 가져오기
                // Robot의 적재 순서는 4 -> 2 -> 3 -> 1 Port 순이다.
#if false
                var vecParts = Db.VecParts.Where(p => p.state == (int)VehiclePartState.ENABLE && (p.C_trayId == null || p.C_trayId == "") && p.VEHICLEID == vec.ID)
                                               .OrderBy(p => p.portNo == 4)
                                               .OrderBy(p => p.portNo == 2)
                                               .OrderBy(p => p.portNo == 3)
                                               .OrderBy(p => p.portNo == 1)
                                               .Take(traycount);
#else
                IEnumerable<vehicle_part> vecParts = null;

                if (job.TRANSFERTYPE == "TRAY")
                {
                    vecParts = Db.VecParts.Where(p => p.state == (int)VehiclePartState.ENABLE && (p.C_trayId == null || p.C_trayId == "") && p.VEHICLEID == vec.ID && (p.portNo == 4 || p.portNo == 2))
                                                   .OrderBy(p => p.portNo == 4)
                                                   .OrderBy(p => p.portNo == 2)
                                                   .Take(traycount);
                }
                else
                {
                    vecParts = Db.VecParts.Where(p => p.state == (int)VehiclePartState.ENABLE && (p.C_trayId == null || p.C_trayId == "") && p.VEHICLEID == vec.ID && (p.portNo == 3 || p.portNo == 1))
                                                   .OrderBy(p => p.portNo == 3)
                                                   .OrderBy(p => p.portNo == 1)
                                                   .Take(traycount);
                }
#endif



                if (vecParts == null || vecParts.Count() == 0)
                {
                    Logger.Inst.Write(vec.ID, CmdLogType.Db, "차량 Buffer 에 여유공간이 없습니다.");
                    MessageBox.Show($"{vec.ID}에 여유공간이 없습니다.");
                    return false;
                }

                // DB에서 가져온 Data를 Robot에게 보낼 Data로 가공
                job.C_bufSlot = bufslot_data(ref buf_portslot, vecParts);

                Logger.Inst.Write(vec.ID, CmdLogType.All, $"buf_portslot:{buf_portslot}");
            }
            return true;
        }

#endregion
    }

    public class JobRemakeWords : EventArgs
    {
        public string[] wordTrayIds = null;
        public string[] wordLotNos = null;
        public string[] wordQtys = null;
        public string[] wordSSlot = null;
        public string[] wordTSlot = null;
        public string[] wordStepIds = null;
        public string[] wordSStepIds = null;
        public string[] wordTStepIds = null;
        public string[] wordbuffslot = null;
        public string[] wordteqpid = null;

        public string submultiId = string.Empty;
        public string lotNos = string.Empty;
        public string trayIds = string.Empty;
        public string Qtys = string.Empty;
        public string sslot = string.Empty;
        public string tslot = string.Empty;
        public string stepids = string.Empty;
        public string sstepids = string.Empty;
        public string tstepids = string.Empty;
        public string teqpid = string.Empty;
        public string bufslot = string.Empty;

        public unit chkunit_src = null;
        public unit chkunit_dst = null;
    }

    public class VertualPep : pepschedule
    {
        public void Copy(pepschedule v)
        {
            this.ID = v.ID;
            this.MULTIID = v.MULTIID;
            this.BATCHID = v.BATCHID;
            this.S_EQPID = v.S_EQPID;
            this.S_PORT = v.S_PORT;
            this.S_SLOT = v.S_SLOT;
            this.T_EQPID = v.T_EQPID;
            this.T_PORT = v.T_PORT;
            this.T_SLOT = v.T_SLOT;
            this.TRAYID = v.TRAYID;
            this.WORKTYPE = v.WORKTYPE;
            this.TRANSFERTYPE = v.TRANSFERTYPE;
            this.WINDOW_TIME = v.WINDOW_TIME;
            this.EXECUTE_TIME = v.EXECUTE_TIME;
            this.REAL_TIME = v.REAL_TIME;
            this.STATUS = v.STATUS;
            this.LOT_NO = v.LOT_NO;
            this.QTY = v.QTY;
            this.STEPID = v.STEPID;
            this.S_STEPID = v.S_STEPID;
            this.T_STEPID = v.T_STEPID;
            this.URGENCY = v.URGENCY;
            this.FLOW_STATUS = v.FLOW_STATUS;
            this.C_VEHICLEID = v.C_VEHICLEID;
            this.C_bufSlot = v.C_bufSlot;
            this.C_state = v.C_state;
            this.C_srcAssignTime = v.C_srcAssignTime;
            this.C_srcArrivingTime = v.C_srcArrivingTime;
            this.C_srcStartTime = v.C_srcStartTime;
            this.C_srcFinishTime = v.C_srcFinishTime;
            this.C_dstAssignTime = v.C_dstAssignTime;
            this.C_dstArrivingTime = v.C_dstArrivingTime;
            this.C_dstStartTime = v.C_dstStartTime;
            this.C_dstFinishTime = v.C_dstFinishTime;
            this.C_isChecked = v.C_isChecked;
            this.C_priority = v.C_priority;
            this.DOWNTEMP = v.DOWNTEMP;
            this.EVENT_DATE = v.EVENT_DATE;
            this.ORDER = v.ORDER;
        }
        public void ZeroMem()
        {
            Copy(new pepschedule());
        }
    }
}
