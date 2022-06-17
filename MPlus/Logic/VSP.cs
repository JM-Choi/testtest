using MPlus.Ref;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MPlus.Logic
{
    public class UtilMgrCustomException : Exception
    {
        public UtilMgrCustomException(string msg) : base(msg)
        {
        }
    }

    public class VSP : Global
    {
        #region singleton VSP
        private static volatile VSP instance;
        private static object syncVsp = new object();
        public static VSP Init
        { get
            { if (instance == null)
                { lock (syncVsp)
                    { instance = new VSP();
                    }
                }
                return instance;
            }
        }
        #endregion
        #region property
        public bool IsStop { get; set; }                    /* Schedule 스레드를 종료하기 위한 플래그           */

        public string ExecuteTime { get; set; }             /* 실행 선택된 executeTime                         */

        public List<pepschedule> Jobs { get; set; }         /* 선택된 executeTime에 해당하는 모든 pep_job       */        

        public List<MulGrp> GroupList { get; set; }

        public eMultiJobWhere MultiJobWhere { get; set; }   /* MultiJob Grouping 위치 src, dst - 1 : src grouping, 2 : dst grouping */

        public DateTime DtOld { get; set; }                 /* AllDbUpdate 수행한 과거 시간                    */

        public DateTime DtCur { get; set; }                 /* 현재시간                                        */

        private VertualPep _virtualPep = new VertualPep();
        #endregion property

        private DbHandler _db = DbHandler.Inst;
        #region event
        public event EventHandler<CallCmdProcArgs1> OnCallCmdProcedure;
        public event EventHandler<SendPreTempDown> OnSendPreTempDown;
        public event EventHandler<DeleteEventArgs> OnDeleteCmd;
        #endregion event
        // Schedule() 루틴이 비정상 탈출될 때, 재시작하기 위한 flag
        public VSP()
        {
            IsStop = false;
        }

        public async Task StartVsp()
        {
            while (!IsStop)
            {
                await Schedule();
                await Task.Delay(5000);
            }
        }

        public bool NoUpdateisAssigned()
        {
            bool update = false;
            List<vehicle> vecs = Db.Vechicles.Where(p => p.isUse == 1 && p.installState == (int)VehicleInstallState.INSTALLED)
                                             .ToList();
            foreach (var x in vecs)
            {
                if (x.C_BATCHID == null && x.isAssigned == 1)
                {
                    x.isAssigned = 0;
                    update = true;
                }
            }

            if (update)
            {
                Db.DbUpdate(TableType.VEHICLE);
                Thread.Sleep(1000);
            }
            return update;
        }
        public (bool bAssigned, List<vehicle>) VehicleIsAssigned()
        {
            if (NoUpdateisAssigned())
                return (false, null);
            List<vehicle> vecs = Db.Vechicles.Where(p => (p.isAssigned == 0 || p.isAssigned == null) && p.isUse == 1 && p.installState == (int)VehicleInstallState.INSTALLED
                                                        && p.C_mode == (int)VehicleMode.AUTO 
                                                        && p.C_chargeRate > Cfg.Data.ChargeStart
                                             ).ToList();
            List<vehicle> stop_vec = new List<vehicle>();

            foreach (var x in vecs)
            {
                if (!RvSenderList[x.ID].IsStop)
                    stop_vec.Add(x);
            }

            if (stop_vec != null)
            {
                vecs.Clear();
                foreach(var z in stop_vec)
                {
                    vecs.Add(z);   
                }
            }
            bool bAssigned = vecs.Count > 0;

            return (bAssigned, vecs);
        }

        public async Task Schedule()
        {
            Logger.Inst.Write(CmdLogType.Db, $"작업 스케쥴링 시작");

            vehicle vec = new vehicle();
            List<vehicle> vecs = new List<vehicle>();
            PepsWorkType curWorkType = PepsWorkType.NONE;

            int scheduled_delay = Cfg.Data.ScheduledDelay;  // msec
            bool is_scheduled_delay = (scheduled_delay == 0) ? false : true;

            try
            {
                while (!IsStop)
                {
                    await Task.Delay(100);

                    if (DbRefresh()) continue;
                    Db.UpdateSchedule();       // ReadDb New Schedule Append
                    Db.UpdateError();
                    Db.UpdateVehicle();
                    if (!IsControllerAuto()) continue;

                    if (!PreProcess()) continue;

                    (bool bret, bool bdelay) = CyclicProcessing(ref curWorkType, ref vecs, ref scheduled_delay, ref is_scheduled_delay);
                    if (!(bret && bdelay))
                    {
                        //Logger.Inst.Write(CmdLogType.Db, "Schedule CyclicProcessing is Fail", "Debug");
                        continue;
                    }

                    SelectMultiJob(curWorkType);

                    AssingGrplst(vecs);
                }
            }
            catch (Exception ex)
            {

            }
        }


        bool IsScheduledTime(string scheduledTime, List<vehicle> veclist, List<pepschedule> select_peps)
        {
            bool bret_tray = false;
            bool bret_stack = false;
            bool bret_common = false;
            int passedtime = Cfg.Data.PassedTime;
            string dummy = string.Empty;
            var stack_job = select_peps.Where(p => p.TRANSFERTYPE == "STACK").OrderBy(p => p.REAL_TIME).ToList();
            var tray_job = select_peps.Where(p => p.TRANSFERTYPE == "TRAY").OrderBy(p => p.REAL_TIME).ToList();
            var delay_job = Db.Peps.Where(p => (p.WORKTYPE == "TI" || p.WORKTYPE == "I") && p.C_isChecked == 1).OrderByDescending(p => p.C_srcAssignTime).ToList(); // 이미 시행중인 job list
            Int32 udtTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;   // unix 타임
#if true
            if (tray_job != null && tray_job.Count() > 0 && (veclist[0].TRANSFERTYPE == "TRAY" || veclist[0].TRANSFERTYPE == "COMMON"))
            {
                bret_tray = tray_job_Check(tray_job, delay_job, udtTime, scheduledTime);

                if (bret_tray)
                    return bret_tray;
            }
            else
            {
                VehicleGoDock(veclist[0].TRANSFERTYPE);
                bret_tray = false;
            }

            if (stack_job != null && stack_job.Count() > 0 && (veclist[0].TRANSFERTYPE == "STACK" || veclist[0].TRANSFERTYPE == "COMMON"))
            {
                bret_stack = Stack_job_Check(stack_job, delay_job, udtTime, scheduledTime, ref dummy);
                if (bret_stack)
                    return bret_stack;
            }
            else
            {
                VehicleGoDock(veclist[0].TRANSFERTYPE);
                bret_stack = false;
            }

#else
            if (tray_job != null && tray_job.Count() > 0 && veclist[0].TRANSFERTYPE == "TRAY")
            {
                bret_tray = Job_Check(tray_job, udtTime, scheduledTime);
                if (bret_tray)
                    return bret_tray;
            }
            else
            {
                VehicleGoDock("TRAY");
                bret_tray = false;
            }

            if (stack_job != null && stack_job.Count() > 0)
            {
                bret_stack = Job_Check(stack_job, udtTime, scheduledTime);
                if (bret_stack)
                    return bret_stack;
            }
            else
            {
                VehicleGoDock("STACK");
                bret_stack = false;
            }
#endif
            if (!(bret_tray || bret_stack))
                Logger.Inst.Write(CmdLogType.Db, "IsScheduledTime bret_tray is Fail, bret_stack is Fail", "Debug");
            return (bret_tray || bret_stack);
        }

        bool tray_job_Check(List<pepschedule> tray_job, List<pepschedule> delay_job, Int32 udtTime, string scheduledTime)
        {
            bool bret_tray = false;
            
            // jm.choi - 191202
            // O, I, OI 작업 시 O나 OI 작업이 I 작업보다 RealTime이 빠르나 I 작업의 우선진행이 빠를때 진행이 안되는 문제로 인하여
            // I 작업 List의 기준에서 p.REALTIME == scheduledTime 삭제
            var I_type = tray_job.Where(p => p.WORKTYPE == "I" && p.C_isChecked != 1).OrderBy(p => p.REAL_TIME).ToList();
            if (I_type != null && I_type.Count() > 0)
            {
                // jm.choi - 191209
                // 191202에 수정된 I 작업 List의 기준에서 p.REALTIME == scheduledTime 삭제로 인하여
                // I 작업 우선진행이 JobList에 생성된 모든 I 작업의 Tray의 개수를 기준으로 우선진행하게되어
                // 상위에서 기준 p.REALTIME == scheduledTime 삭제 후 조건 내로 진입하여 
                // 기준에 p.REALTIME == scheduledTime 추가하여 재 선정
                I_type = I_type.Where(p => p.WORKTYPE == "I" && p.C_isChecked != 1 && p.REAL_TIME == I_type[0].REAL_TIME).ToList();
                bret_tray = ExecuteTime_check_I_Type(I_type, tray_job, delay_job, udtTime, scheduledTime);
                if (!bret_tray)
                    bret_tray = ExecuteTime_check_another(tray_job, udtTime, scheduledTime);
            }
            else
            {
                bret_tray = ExecuteTime_check_another(tray_job, udtTime, scheduledTime);
            }
            return bret_tray;
        }
        bool ExecuteTime_check_I_Type(List<pepschedule> I_type, List<pepschedule> tray_job, List<pepschedule> delay_job, Int32 udtTime, string scheduledTime)
        {
            string tray_all = I_Type_tray_all_check(I_type);

            if ((Convert.ToInt32(I_type[0].REAL_TIME) - (tray_all.Split(',').Count() * Cfg.Data.early_Time)) > udtTime)
            {
                //var peps_chk = tray_job.Where(p => p.WORKTYPE != "I" && p.C_isChecked != 1 && p.REAL_TIME == scheduledTime).OrderBy(p => p.REAL_TIME).ToList();
                var peps_chk = tray_job.Where(p => p.WORKTYPE != "I" && p.C_isChecked != 1).OrderBy(p => p.REAL_TIME).ToList();
                if (peps_chk == null || peps_chk.Count() == 0)
                {
                    if ((Convert.ToInt32(I_type[0].REAL_TIME) - Cfg.Data.PassedTime) > udtTime)
                    {
                        VehicleGoDock(I_type[0].TRANSFERTYPE);
                    }
                    return false;
                }
                else
                {
                    if (Convert.ToInt32(peps_chk[0].REAL_TIME) > udtTime)
                    {
                        if ((Convert.ToInt32(peps_chk[0].REAL_TIME) - Cfg.Data.PassedTime) > udtTime)
                        {
                            VehicleGoDock(peps_chk[0].TRANSFERTYPE);
                        }
                        return false;
                    }
                    ExecuteTime = peps_chk[0].REAL_TIME;
                    return true;
                }
            }
            
            if (I_type[0].WORKTYPE == "I")
            {
                if (delay_job.Count != 0)
                {
                    TimeSpan diff = DateTime.Now - Convert.ToDateTime(delay_job[0].C_srcAssignTime);
                    if (diff.TotalSeconds < Cfg.Data.delayTime)
                    {
                        return retray_job_chk(tray_job, delay_job, udtTime);
                    }
                }
            }
            ExecuteTime = I_type[0].REAL_TIME;
            return true;
        }

        bool retray_job_chk(List<pepschedule> tray_job, List<pepschedule> delay_job, Int32 udtTime)
        {
            var peps_chk = tray_job.Where(p => p.WORKTYPE != "I" && p.C_isChecked != 1).OrderBy(p => p.REAL_TIME).ToList();
            if (peps_chk == null || peps_chk.Count() == 0)
            {
                return false;
            }
            else
            {
                if (Convert.ToInt32(peps_chk[0].REAL_TIME) > udtTime)
                {
                    if ((Convert.ToInt32(peps_chk[0].REAL_TIME) - Cfg.Data.PassedTime) > udtTime)
                    {
                        VehicleGoDock(peps_chk[0].TRANSFERTYPE);
                    }
                    return false;
                }
                ExecuteTime = peps_chk[0].REAL_TIME;
                return true;
            }
        }
        string I_Type_tray_all_check(List<pepschedule> I_type)
        {
            string tray_all = string.Empty;
            for (int i = 0; i < I_type.Count(); i++)
            {
                if (I_type[i].WORKTYPE == "I")
                {
                    if (tray_all.Length > 0)
                        tray_all += ",";
                    tray_all += I_type[i].TRAYID;
                }
            }
            return tray_all;
        }

        int DelayTime(List<pepschedule> delay_job)
        {
            DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan diff = Convert.ToDateTime(delay_job[0].C_srcAssignTime.ToString()) - origin;
            return (Int32)Math.Floor(diff.TotalSeconds);
        }

        bool ExecuteTime_check_another(List<pepschedule> tray_job, Int32 udtTime, string scheduledTime)
        {
            //var peps_chk = tray_job.Where(p => p.WORKTYPE != "I" && p.REAL_TIME == scheduledTime && p.C_isChecked != 1).OrderBy(p => p.REAL_TIME).ToList();
            var peps_chk = tray_job.Where(p => p.C_isChecked != 1).OrderBy(p => p.REAL_TIME).ToList();
            if (peps_chk == null || peps_chk.Count() == 0)
            {
                if ((Convert.ToInt32(tray_job[0].REAL_TIME) - Cfg.Data.PassedTime) > udtTime)
                {
                    VehicleGoDock(tray_job[0].TRANSFERTYPE);
                }
                return false;
            }
            else
            {

                if (Convert.ToInt32(peps_chk[0].REAL_TIME) > udtTime)
                {
                    if ((Convert.ToInt32(peps_chk[0].REAL_TIME) - Cfg.Data.PassedTime) > udtTime)
                    {
                        VehicleGoDock(peps_chk[0].TRANSFERTYPE);
                    }
                    return false;
                }
                ExecuteTime = peps_chk[0].REAL_TIME;
                return true;
            }
        }
        bool Stack_job_Check(List<pepschedule> stack_job, List<pepschedule> delay_job, Int32 udtTime, string scheduledTime, ref string a)
        {
            //var peps_chk = stack_job.Where(p => p.REAL_TIME == scheduledTime && p.C_isChecked != 1).OrderBy(p => p.REAL_TIME).ToList();
            var peps_chk = stack_job.Where(p => p.C_isChecked != 1).OrderBy(p => p.REAL_TIME).ToList();
            string peps_chk_realtime = string.Empty;
            if (peps_chk == null || peps_chk.Count() == 0)
            {
                if ((Convert.ToInt32(stack_job[0].REAL_TIME) - Cfg.Data.PassedTime) > udtTime)
                {
                    VehicleGoDock(stack_job[0].TRANSFERTYPE);
                }
                return false;
            }
            else
            {
                if (Convert.ToInt32(peps_chk[0].REAL_TIME) > udtTime)
                {
                    if ((Convert.ToInt32(peps_chk[0].REAL_TIME) - Cfg.Data.PassedTime) > udtTime)
                    {
                        VehicleGoDock(peps_chk[0].TRANSFERTYPE);
                    }
                    return false;
                }
                peps_chk_realtime = peps_chk[0].REAL_TIME;
                a = peps_chk_realtime;
                if (stack_job[0].WORKTYPE == "TI")
                {
                    if (delay_job.Count != 0)
                    {
                        var diff = DateTime.Now - Convert.ToDateTime(delay_job[0].C_srcAssignTime);
                        if (diff.TotalSeconds < Cfg.Data.delayTime)
                        {
                            var restack_job = stack_job.Where(p => p.WORKTYPE != "TI" && p.C_isChecked != 1).OrderBy(p => p.REAL_TIME).ToList();
                            if (restack_job.Count() > 0)
                            {
                                Stack_job_Check(restack_job, delay_job, udtTime, restack_job[0].REAL_TIME, ref peps_chk_realtime);
                                return true;
                            }
                            return false;
                        }
                    }
                }
                ExecuteTime = peps_chk_realtime;
                return true;
            }
        }
        bool Job_Check(List<pepschedule> peps, Int32 udtTime, string scheduledTime)
        {
            var peps_chk = peps.Where(p => p.REAL_TIME == scheduledTime && p.C_isChecked != 1).OrderBy(p => p.REAL_TIME).ToList();
            if (peps_chk == null || peps_chk.Count() == 0)
            {
                if ((Convert.ToInt32(peps[0].REAL_TIME) - Cfg.Data.PassedTime) > udtTime)
                {
                    VehicleGoDock(peps[0].TRANSFERTYPE);
                }
                return false;
            }
            else
            {
                if (Convert.ToInt32(peps_chk[0].REAL_TIME) > udtTime)
                {
                    if ((Convert.ToInt32(peps_chk[0].REAL_TIME) - Cfg.Data.PassedTime) > udtTime)
                    {
                        VehicleGoDock(peps_chk[0].TRANSFERTYPE);
                    }
                    return false;
                }
                ExecuteTime = peps_chk[0].REAL_TIME;
                return true;
            }
        }

        void TrayVehicle_GoDock_Check()
        {
            Int32 udtTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            var tray_job = Db.Peps.Where(p => Convert.ToInt32(p.REAL_TIME) < (udtTime + Cfg.Data.PassedTime) && p.TRANSFERTYPE == "TRAY" && p.C_isChecked != 1)
                .OrderBy(p => p.REAL_TIME).ToList();
            List<vehicle> tray_vec = Db.Vechicles.Where(p => p.TRANSFERTYPE == "TRAY").ToList();

            (List<pepschedule> collect_job, int count) = Tray_Job_Collect(tray_job);

            List<vehicle> collect_vec = Tray_Vec_Collect(tray_vec);

            Tray_Vec_GoDock_Check(collect_job, collect_vec, count);
        }

        (List<pepschedule>, int) Tray_Job_Collect(List<pepschedule> tray_job)
        {
            List<pepschedule> collect_job = new List<pepschedule>();
            int count = 0;
            string real = string.Empty;
            string work = string.Empty;
            foreach (var x in tray_job)
            {
                if (x.REAL_TIME != real || x.WORKTYPE != work)
                {
                    real = x.REAL_TIME;
                    work = x.WORKTYPE;
                    count++;
                    collect_job.Add(x);
                }
            }
            return (collect_job, count);
        }

        List<vehicle> Tray_Vec_Collect(List<vehicle> tray_vec)
        {
            List<vehicle> collect_vec = new List<vehicle>();
            foreach (var z in tray_vec)
            {
                if (VehicleList[z.ID].JobAssign)
                {
                    collect_vec.Add(z);
                }
            }

            return collect_vec;
        }

        void Tray_Vec_GoDock_Check(List<pepschedule> collect_job, List<vehicle> collect_vec, int count)
        {
            if (collect_vec.Count() > 0 && count == 1)
            {
                TempNearestGoalAndVehicleStruct v = SelectVehicleNearbyToUnitS(collect_job, collect_vec);
                VehicleList[v.vec.ID].JobAssign = false;
                VehicleGoDock(collect_job[0].TRANSFERTYPE);
            }
            else if (collect_vec.Count() > 0 && count == 0)
            {
                VehicleGoDock("TRAY");
            }

        }

        void StackVehicle_GoDock_Check()
        {

            Int32 udtTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            List<pepschedule> stack_job = Db.Peps.Where(p => Convert.ToInt32(p.REAL_TIME) < (udtTime + Cfg.Data.PassedTime) && p.TRANSFERTYPE == "STACK" && p.C_isChecked != 1)
                .OrderBy(p => p.REAL_TIME).ToList();

            (List<pepschedule> collect_job, int count) = Stack_Job_Collect(stack_job);

            List<vehicle> stack_vec = Db.Vechicles.Where(p => p.TRANSFERTYPE == "STACK").ToList();

            List<vehicle> collect_vec = Stack_Vec_Collect(stack_vec);

            Stack_Vec_GoDock_Check(collect_job, collect_vec, count);
        }

        (List<pepschedule>, int) Stack_Job_Collect(List<pepschedule> stack_job)
        {
            List<pepschedule> collect_job = new List<pepschedule>();
            int count = 0;
            string real = string.Empty;
            string work = string.Empty;
            foreach (var x in stack_job)
            {
                if (x.REAL_TIME != real || x.WORKTYPE != work)
                {
                    real = x.REAL_TIME;
                    work = x.WORKTYPE;
                    count++;
                    collect_job.Add(x);
                }
            }
            return (collect_job, count);
        }

        List<vehicle> Stack_Vec_Collect(List<vehicle> stack_vec)
        {
            List<vehicle> collect_vec = new List<vehicle>();
            foreach (var z in stack_vec)
            {
                if (VehicleList[z.ID].JobAssign)
                {
                    collect_vec.Add(z);
                }
            }

            return collect_vec;
        }

        void Stack_Vec_GoDock_Check(List<pepschedule> collect_job, List<vehicle> collect_vec, int count)
        {
            if (collect_vec.Count() > 0 && count == 0)
            {
                VehicleGoDock("STACK");
            }
            // 여기에 Stack Vehicle 대기 위치 이동 명령 추가
            else if (collect_vec.Count() > 0 && count > 0)
            {

            }

            //if (collect_vec.Count() > 0 && count == 1)
            //{
            //    TempNearestGoalAndVehicleStruct v = SelectVehicleNearbyToUnitS(collect_job, collect_vec);
            //    VehicleList[v.vec.ID].JobAssign = false;
            //    VehicleGoDock(collect_job[0].TRANSFERTYPE);
            //}
            //else if (collect_vec.Count() > 0 && count == 0)
            //{
            //    VehicleGoDock("STACK");
            //}

        }



        void CommonVehicle_GoDock_Check()
        {

            Int32 udtTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            List<pepschedule> common_job = Db.Peps.Where(p => Convert.ToInt32(p.REAL_TIME) < (udtTime + Cfg.Data.PassedTime) && p.C_isChecked != 1)
                .OrderBy(p => p.REAL_TIME).ToList();

            (List<pepschedule> collect_job, int count) = Common_Job_Collect(common_job);

            List<vehicle> common_vec = Db.Vechicles.Where(p => p.TRANSFERTYPE == "COMMON").ToList();

            List<vehicle> collect_vec = Common_Vec_Collect(common_vec);

            Common_Vec_GoDock_Check(collect_job, collect_vec, count);
        }

        (List<pepschedule>, int) Common_Job_Collect(List<pepschedule> stack_job)
        {
            List<pepschedule> collect_job = new List<pepschedule>();
            int count = 0;
            string real = string.Empty;
            string work = string.Empty;
            foreach (var x in stack_job)
            {
                if (x.REAL_TIME != real || x.WORKTYPE != work)
                {
                    real = x.REAL_TIME;
                    work = x.WORKTYPE;
                    count++;
                    collect_job.Add(x);
                }
            }
            return (collect_job, count);
        }

        List<vehicle> Common_Vec_Collect(List<vehicle> stack_vec)
        {
            List<vehicle> collect_vec = new List<vehicle>();
            foreach (var z in stack_vec)
            {
                if (VehicleList[z.ID].JobAssign)
                {
                    collect_vec.Add(z);
                }
            }

            return collect_vec;
        }

        void Common_Vec_GoDock_Check(List<pepschedule> collect_job, List<vehicle> collect_vec, int count)
        {
            if (collect_vec.Count() > 0 && count == 0)
            {
                VehicleGoDock("COMMON");
            }
            // 여기에 Stack Vehicle 대기 위치 이동 명령 추가
            else if (collect_vec.Count() > 0 && count > 0)
            {

            }

            //if (collect_vec.Count() > 0 && count == 1)
            //{
            //    TempNearestGoalAndVehicleStruct v = SelectVehicleNearbyToUnitS(collect_job, collect_vec);
            //    VehicleList[v.vec.ID].JobAssign = false;
            //    VehicleGoDock(collect_job[0].TRANSFERTYPE);
            //}
            //else if (collect_vec.Count() > 0 && count == 0)
            //{
            //    VehicleGoDock("STACK");
            //}

        }

        bool IsScheduledDelay(ref int delay, ref bool is_delay)
        {
            if ((delay > 0) && is_delay)
            {
                Thread.Sleep(delay);
                is_delay = false;
                return true;
            }
            else
            {
                is_delay = true;
                return false;
            }
        }

        (bool bret, bool bdelay) CyclicProcessing(ref PepsWorkType worktype, ref List<vehicle> veclist, ref int delay, ref bool isDelay)
        {
            List<pepschedule> select_peps = new List<pepschedule>();
            (bool bv, List<vehicle> lstvecs) = VehicleIsAssigned();
            if (!bv)
            {
                Logger.Inst.Write(CmdLogType.Db, "CyclicProcessing No Vehicle", "Debug");
                return (false, false);
            }

            veclist = lstvecs;
            if (string.IsNullOrEmpty(ExecuteTime))
            {
                ExecuteTime = Db.Select_ExcuteTime(ref veclist, Cfg.Data.early_Time, ref select_peps);
                if (string.IsNullOrEmpty(ExecuteTime)) { TrayVehicle_GoDock_Check(); StackVehicle_GoDock_Check(); CommonVehicle_GoDock_Check();
                    Logger.Inst.Write(CmdLogType.Db, "CyclicProcessing ExecuteTime is Null or Empty", "Debug"); return (false, false); }
                if (!IsScheduledTime(ExecuteTime, veclist, select_peps)) { ExecuteTime = default; TrayVehicle_GoDock_Check(); StackVehicle_GoDock_Check(); CommonVehicle_GoDock_Check();
                    Logger.Inst.Write(CmdLogType.Db, "CyclicProcessing IsScheduledTime Fail", "Debug"); return (false, false); }
                if (IsScheduledDelay(ref delay, ref isDelay)) return (true, false);
            }
            else
            {
                if (!Db.Select_vehicle(ExecuteTime, ref veclist)) { ExecuteTime = default;
                    Logger.Inst.Write(CmdLogType.Db, "CyclicProcessing Db.Select_vehicle is Fail", "Debug"); return (false, false); }
                IsScheduledDelay(ref delay, ref isDelay);
            }

            if (!FetchJob(ref worktype, veclist))
            {
                Logger.Inst.Write(CmdLogType.Db, "CyclicProcessing FetchJob is Fail", "Debug");
                return (false, false);
            }

            return (true, true);
        }

        private void AssingGrplst(List<vehicle> vecs)
        {
            vehicle catchCheckvec = null;
            try
            {
                if (vecs.Count() > 0)
                {
                    TempNearestGoalAndVehicleStruct v = SelectVehicleNearbyToUnitS(Jobs, vecs);
                    catchCheckvec = v.vec;
                    int group_count = GroupList.Count();
                    foreach (var g in Jobs)
                    {
                        if (string.Compare(g.S_EQPID, v.goalName) == 0)
                        {
                            VehicleList[v.vec.ID].JobAssign = false;
                            vecs.Remove(v.vec);
                            OnCallCmdProcedure.Invoke(this, new CallCmdProcArgs1
                            {
                                where = MultiJobWhere,
                                vec = v.vec,
                                grp = GroupList[0],
                                executeTime = g.EXECUTE_TIME,
                                realTime = ExecuteTime,
                                grp_count = group_count
                            });
                            GroupList.Remove(GroupList[0]);
                            break;
                        }
                    }
                    Thread.Sleep(1000);
                    //if (Jobs[0].WORKTYPE == "I" || Jobs[0].WORKTYPE == "TI" || Jobs[0].WORKTYPE == "TO" || Jobs[0].WORKTYPE == "EI" || Jobs[0].WORKTYPE == "EO")
                        ExecuteTime = string.Empty;
                    v.vec.C_BATCHID = Jobs[0].BATCHID;
                    Jobs.Clear();
                    v.vec.isAssigned = 1;
                    Db.DbUpdate(TableType.VEHICLE);
                }
            }
            catch
            {
                Logger.Inst.Write(CmdLogType.Db, "AssingGrplst catch");
                foreach (var pep in Jobs)
                {
                    pep.C_isChecked = null;
                    pep.C_srcAssignTime = null;
                    pep.C_priority = null;
                }
                catchCheckvec.C_BATCHID = null;
                catchCheckvec.isAssigned = 0;
                ExecuteTime = string.Empty;
                Db.DbUpdate(TableType.PEPSCHEDULE);
                Db.DbUpdate(TableType.VEHICLE);
                Jobs.Clear();
            }
        }



        // jm.choi - 190418
        // Assign된 Job이 완료가 되고 다음 Job이 없거나 5분안에 없으면 Vehicle을 Dock으로 보냄
        // JobAssign은 해당 Vehicle이 JobAssign이 되었던 적이 있는지를 확인하는 Flag이며
        // Assign된 Job을 완료하면 Vehicle의 isAssigned는 0으로 변경됨
        // JobAssign == true고 isAssigned가 1이면 Job이 Assign 되었으나 완료된 Vehicle이 되며
        // 그 Vehicle을 Dock으로 보냄
        private bool VehicleGoDock(string transfer = null)
        {
            var vec_dock_count = Db.Vechicles.Where(p => p.isUse == 1 && (transfer != null) ? (p.TRANSFERTYPE == transfer) : (p.TRANSFERTYPE != transfer)).ToList();

            if (vec_dock_count == null && vec_dock_count.Count() == 0)
            {
                return false;
            }
            for (int i = 0; i < vec_dock_count.Count(); i++)
            {
                if (VehicleList[vec_dock_count[i].ID].JobAssign)
                {
                    if (vec_dock_count[i].C_state == 0)
                    {
                        VehicleList[vec_dock_count[i].ID].SendMessageToVehicle("CHARGE;");
                        VehicleList[vec_dock_count[i].ID].JobAssign = false;
                        vec_dock_count[i].isAssigned = 0;
                        Db.DbUpdate(TableType.VEHICLE);
                    }
                }
            }
            return true;
        }
#region db refresh

        private bool DbRefresh()
        {
            try
            {
                AllDbUpdate();              // update
                if (AllDbDeleteInsert())    // delete, insert
                    return true;            // refresh ok - continue
            }
            catch (Exception ex)
            {
                Logger.Inst.Write(CmdLogType.Rv, $"Exception. DbRefresh. {ex.Message}\r\n{ex.StackTrace}");
                return true;                // - continue
            }
            return false;                   // 수행 내역 없음 - next
        }

        /// <summary>
        /// 시작하자마다 _Db.DbContext.Update(); 수행되면 오류가 발생했다. 시간 딜레이를 주기 위해 한 번 스킵(Skip)한다
        /// </summary>
        /// <param name="dtCur">현재 DateTime. Caller 에 시간값을 저장</param>
        /// <param name="dtOld">이전 DateTime. 이전에 _Db.DbContext.Update 수행했던 시간값을 저장</param>
        /// <param name="bfirst">최초실행flag. </param>
        private void AllDbUpdate()
        {
            // 호출시 즉각 db 반영될 수 있게 시간 딜레이 제거
            DtCur = DateTime.Now;

            if (DtOld == default(DateTime))
                DtOld = DtCur;
            else
            {
                TimeSpan timeDiff = DtCur - DtOld;
                if (timeDiff.TotalSeconds > 1)
                {
                    Db.Update();
                    Console.WriteLine($"dbrefresh.....dtcur = {DtCur}, dtold = {DtOld}, timediff = {timeDiff.TotalSeconds}");
                    DtOld = DtCur;
                }
            }
        }

        private bool AllDbDeleteInsert()
        {
            bool brefresh = false;
            if (Db.GetCount_ObjListDeleteAll() > 0)
            {
                Db.CommitDeleteAll();
                brefresh = true;
            }
            if (Db.GetCount_ObjListDelete() > 0)
            {
                Db.CommitDelete();
                brefresh = true;
            }
            if (Db.GetCount_ObjListAll() > 0)
            {
                Db.CommitAdd();
                brefresh = true;
            }

            if (brefresh)
            {
                Db.DbUpdate(true, new TableType[] { TableType.PEPSCHEDULE });
                brefresh = false;
                return true;
            }
            return false;
        }

#endregion db refresh

        /// <summary>
        /// ExecuteTime 가 IsNullOrEmpty 일 때 ExecuteTime 을 가져오기 위해 Db에서 pepschedule 테이블을 로딩한다
        /// </summary>
        /// <returns></returns>
        private bool FetchJob(ref PepsWorkType worktype, List<vehicle> veclist)
        {
            if (string.IsNullOrEmpty(ExecuteTime)) return false;

            //Db.Update_PepsPriority(ExecuteTime);    // priority 가 할당되지 않은 항목에 대해 update

            // 동일 executetime 에 걸리는 모든 schedule 을 priority로 정렬하여 
            // 가장 우선순위가 높은 녀석의 worktype 을 변수에 할당받는다
            (PepsWorkType type, List<pepschedule> jobs) = Db.SelectAllPepsInExecuteTime(ExecuteTime, veclist);
            if (jobs == null)
            {
                ExecuteTime = string.Empty;
                Logger.Inst.Write(CmdLogType.Db, "FetchJob jobs is null");
                return false;
            }
            if (jobs.Count > 0)
            {
                if (Jobs == null || Jobs.Count == 0 || Jobs.Count() != jobs.Count() || Jobs[0].BATCHID != jobs[0].BATCHID)
                    Jobs = jobs;
                else
                {
                    var v = Jobs?.ToList();
                    // adding 작업
                    if (UpdateLocalSchedule(ref v, jobs))   // add, remove 작업이 있을 때 실제 ref 변경되었는지 비교
                        Console.WriteLine("ReferenceQeuals(Jobs, v) = {0}", Object.ReferenceEquals(Jobs, v));
                }
            }
            worktype = type;

            if (Jobs == null || Jobs.Count == 0 || jobs.Count == 0)
            {
                Logger.Inst.Write(CmdLogType.Db, "FetchJob Jobs and jobs is null or Count Zero");
                ExecuteTime = string.Empty;
                return false;
            }
            else
            {
                foreach (var x in Jobs)
                {
                    x.C_isChecked = 1;
                    x.C_srcAssignTime = DateTime.Now;
                }
                Db.DbUpdate(TableType.PEPSCHEDULE);
            }
            if (!VerifyEquipExist())
            { ExecuteTime = string.Empty;
                Logger.Inst.Write(CmdLogType.Db, "FetchJob VerifyEquipExist is Fail");
                return false;
            }

            CheckPortSlotInfo();
            if (Jobs == null || Jobs.Count == 0)
            { ExecuteTime = string.Empty;
                Logger.Inst.Write(CmdLogType.Db, "FetchJob Jobs is null or Count Zero");
                return false;
            }

            return true;
        }

        /// <summary>
        /// RealDb.pepschedule -> LocalDb.Peps -> Jobs 에 동기화하기 위한 과정
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="jobs"></param>
        private bool UpdateLocalSchedule(ref List<pepschedule> origin, List<pepschedule> jobs)
        {
            pepschedule ExistEqualsPepschedule(List<pepschedule> joblist, pepschedule comp)
            {
                for (int x = 0; x < joblist.Count; x++)
                { if (joblist[x].ID == comp.ID && joblist[x].BATCHID == comp.BATCHID)
                        return joblist[x];
                }
                return null;
            }

            bool b = false;
            if (jobs.Count() > 0)
            {
                for (int x = 0; origin != null && x < origin.Count; x++)
                {
                    if (jobs.Count > 0)
                    {
                        pepschedule job = ExistEqualsPepschedule(jobs, origin[x]);  //id, batch 가지고 비교
                        if (job != null)
                        {
                            jobs.Remove(job);
                            b = true;
                        }
                        else
                        {   // origin 에는 있는데 jobs 에 없다? db 에서 삭제되었다.
                            // origin Cancel 처리
                        }
                    }
                    else
                    {   // origin 에는 있는데 jobs 에 없다? db 에서 삭제되었다.
                        // origin Cancel 처리
                    }
                }

                // jobs 에 남아 있는게 있으면 origin 에 추가한다.
                for (int y = 0; y < jobs.Count; y++)
                {
                    origin.Add(jobs[y]);
                    b = true;
                }
                jobs.Clear();
            }
            else
            { if (origin.Count > 0)
                {   // 1. db 단에서 삭제 되었다.
                    // 모두 cancel 처리
                }
                else
                {   // 아무 action 없다
                }
            }

            return b;
        }

        private bool VerifyEquipExist()
        {
            foreach (var v in Jobs)
            {
                var isSrcUnit = Db.Units.Where(p => p.ID == ((pepschedule)v).S_EQPID).ToList();
                var isDstUnit = Db.Units.Where(p => p.ID == ((pepschedule)v).T_EQPID).ToList();
                if (isSrcUnit == null || isDstUnit == null || isSrcUnit.Count() == 0 || isDstUnit.Count() == 0)
                {
                    ((pepschedule)v).C_srcAssignTime = DateTime.Now;
                    ((pepschedule)v).C_state = (int)CmdState.EQP_NOTEXIST;

                    Db.CopyCmdToHistory(((pepschedule)v).ID.ToString());
                    Db.Delete(v);
                    Db.DbUpdate(TableType.PEPSCHEDULE);
                    Logger.Inst.Write(CmdLogType.Db, $"[VSP] EQP_NOTEXIST. srcEqp = {((pepschedule)v).S_EQPID}, dstEqp = {((pepschedule)v).T_EQPID}");
                }
            }

            return (Jobs.Count() > 0) ? true : false;
        }

        private void CheckPortSlotInfo()
        {
            string eqp_portslot = string.Empty;
            string buf_portslot = string.Empty;
            string eqp = string.Empty;
            string slot = string.Empty;
            List<unit> units = new List<unit>();
            bool b = false;
            foreach (var v in Jobs)
            {
                b = false;
                for (int i = 0; i < 2 && !b; i++)
                {
                    eqp = (i == 0) ? ((pepschedule)v).S_EQPID : ((pepschedule)v).T_EQPID;
                    slot = (i == 0) ? ((pepschedule)v).S_SLOT : ((pepschedule)v).T_SLOT;

                    units = Db.Units.Where(p => p.ID == eqp).ToList();
                    if (units.Count() == 0)
                    {
                        b = true; continue;
                    }

                    if (units[0].goaltype == (int)EqpGoalType.STK)
                    {
                        if (!(slot.ToUpper().Contains("AUTO01.LP") || slot.ToUpper().Contains("AUTO02.LP")
                              || slot.ToUpper().Contains("STACK01.LP") || slot.ToUpper().Contains("STACK02.LP")))
                        {
                            b = true; continue;
                        }
                    }
                    else if (units[0].goaltype == (int)EqpGoalType.HANDLER || units[0].goaltype == (int)EqpGoalType.HANDLER_STACK)
                    {
                        if (!(slot.ToUpper().Contains("LOADER") || slot.ToUpper().Contains("GOOD") || slot.ToUpper().Contains("FAIL")))
                        {
                            b = true; continue;
                        }
                    }
                    else if (units[0].goaltype == (int)EqpGoalType.REFLOW)
                    {
                        if (!(slot.ToUpper().Contains("AUTO01") || slot.ToUpper().Contains("AUTO02")))
                        {
                            b = true; continue;
                        }
                    }
                    else if (units[0].goaltype == (int)EqpGoalType.SYSWIN_OVEN || units[0].goaltype == (int)EqpGoalType.SYSWIN
                        || units[0].goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || units[0].goaltype == (int)EqpGoalType.BUFFER_STK)
                    {
                        string[] words = slot.Split(',');
                        for (int n = 0; n < words.Count() && !b; n++)
                        {
                            byte[] ascPort = Encoding.ASCII.GetBytes(words[n].Substring(0, 1));// - 'A';
                            byte[] ascBase = Encoding.ASCII.GetBytes("A");
                            int portNo = -1, slotNo = -1;
                            if (units[0].goaltype == (int)EqpGoalType.SYSWIN_OVEN) // A,B,C 입력을 D,E,F로 바꾸기 위해 + 4 를 한다
                            {
                                if (Convert.ToInt32(words[n].Substring(1, words[n].Length - 1)) > 6)
                                {
                                    portNo = ascPort[0] - ascBase[0] + 4;
                                    slotNo = 11 - Convert.ToInt32(words[n].Substring(1, words[n].Length - 1));
                                }
                                else
                                {
                                    portNo = ascPort[0] - ascBase[0] + 1;   // portNo 는 1 Base 다
                                    slotNo = 6 - Convert.ToInt32(words[n].Substring(1, words[n].Length - 1));
                                }

                                // err check
                                if (!((portNo <= units[0].max_col) || (slotNo <= units[0].max_row)))
                                {
                                    b = true; continue;
                                }
                            }
                            else if (units[0].goaltype == (int)EqpGoalType.SYSWIN_OVEN_t) // A,B,C 입력을 D,E,F로 바꾸기 위해 + 4 를 한다
                            {
                                if (Convert.ToInt32(words[n].Substring(1, words[n].Length - 1)) > 7)
                                {
                                    portNo = ascPort[0] - ascBase[0] + 4;
                                    slotNo = 11 - Convert.ToInt32(words[n].Substring(1, words[n].Length - 1));
                                }
                                else
                                {
                                    portNo = ascPort[0] - ascBase[0] + 1;   // portNo 는 1 Base 다
                                    slotNo = 7 - Convert.ToInt32(words[n].Substring(1, words[n].Length - 1));
                                }

                                // err check
                                if (!((portNo <= units[0].max_col) || (slotNo <= units[0].max_row)))
                                {
                                    b = true; continue;
                                }
                            }
                            else
                            {
                                portNo = ascPort[0] - ascBase[0] + 1;   // portNo 는 1 Base 다
                                try
                                {
                                    int a = Convert.ToInt32(words[n].Substring(1, words[n].Length - 1));
                                    int c = (int)units[0].max_row;
                                    slotNo = c - a;
                                }
                                catch (Exception ex)
                                {
                                    Logger.Inst.Write(CmdLogType.Db, $"Exception. CheckPortSlotInfo. {ex.Message}\r\n{ex.StackTrace}");
                                }

                                // err check
                                if (!((portNo <= units[0].max_col) || (slotNo <= units[0].max_row)))
                                {
                                    b = true; continue;
                                }
                            }
                        }
                    }
                    else
                    {
                        b = true;
                    }
                }
                if (b)
                {
                    Logger.Inst.Write(CmdLogType.Db, $"CheckPortSlotInfo. Invalid slot info");
                    try
                    {
                        ((pepschedule)v).C_srcAssignTime = DateTime.Now;
                        ((pepschedule)v).C_dstAssignTime = DateTime.Now;
                        ((pepschedule)v).C_state = (int)CmdState.INVALID_SLOT;
                        Db.DbUpdate(TableType.PEPSCHEDULE);    // FormMonitor update
                        Db.CopyCmdToHistory(((pepschedule)v).ID.ToString());
                        Db.Delete(((pepschedule)v));
                    }
                    catch (Exception ex)
                    {
                        Logger.Inst.Write(CmdLogType.Db, $"exception CheckPortSlotInfo: {ex.Message}\r\n{ex.StackTrace}");
                    }
                }
            }
        }



#region job_preprocess

        /// <summary>
        /// scheduled 시간이 되었는지 IF_FLAG 업데이트, 실행가능한 PreTempDown에 대해 수행, 수행가능잡이 있는지, Idle Vehicle이 있는지 체크
        /// </summary>
        /// <param name="bb"></param>
        /// <param name="vec"></param>
        /// <returns></returns>
        private bool PreProcess()
        {
            var tempdownjob = Db.Peps.Where(p => p.WORKTYPE == "TEMP_DOWN" && p.C_state == null).OrderBy(p => p.REAL_TIME).ToList();
            if (tempdownjob == null || tempdownjob.Count() == 0)
                return true;
            Thread.Sleep(100);

            Int32 udtTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;   // unix 타임
            
            if ((Convert.ToInt32(tempdownjob[0].REAL_TIME)) > udtTime)
                return true;
            Jobs = Db.Peps.Where(p => p.WORKTYPE == "TEMP_DOWN" && p.C_state == null).OrderBy(p => p.REAL_TIME).ToList();
            
            ((pepschedule)Jobs[0]).C_state = (int)CmdState.DST_COMPLETE;

            if (Cfg.Data.UseRv)
            {
                var isSrcUnit = Db.Units.Where(p => p.ID == ((pepschedule)Jobs[0]).S_EQPID).Single();
                var isDstUnit = Db.Units.Where(p => p.ID == ((pepschedule)Jobs[0]).T_EQPID).Single();

                var args = new SendPreTempDown()
                {
                    vec = JobProcList["PROGRAM"].Vec,
                    job = (pepschedule)Jobs[0],
                    srcUnit = isSrcUnit,
                    dstUnit = isDstUnit,
                    downtemp = ((pepschedule)Jobs[0]).DOWNTEMP
                };
                OnSendPreTempDown?.Invoke(this, args);
            }       

            // 체크 수행 후, 실제 명령을 수행하던 삭제하던 레코드의 데이터를 삭제하던 레코드 시간 데이터를 업데이트
            ((pepschedule)Jobs[0]).C_srcAssignTime = DateTime.Now;
            ((pepschedule)Jobs[0]).C_srcFinishTime = DateTime.Now;
            ((pepschedule)Jobs[0]).C_dstAssignTime = DateTime.Now;
            ((pepschedule)Jobs[0]).C_dstFinishTime = DateTime.Now;
            Db.DbUpdate(TableType.PEPSCHEDULE);    // FormMonitor update
            Db.CopyCmdToHistory(((pepschedule)Jobs[0]).ID.ToString());
            Db.Delete(Jobs[0]);

            Jobs.Remove(Jobs[0]);
            return false;

        }

#endregion job_preprocess

#region selectmulti

        /// <summary>
        /// MultiJobWhere, IsMultiJob, MultiJobs, Jobs
        /// </summary>
        /// <param name="worktype"></param>
        private void SelectMultiJob(PepsWorkType worktype)
        {
            MultiJobWhere = (int)eMultiJobWhere.NONE;
            if (Jobs.Count() <= 0)
            {
                Logger.Inst.Write(CmdLogType.Db, "SelectMultiJob Jos is Count Zero");
                return;
            }

            List<MulGrp> grplst = new List<MulGrp>();
            switch(worktype)
            {
                case PepsWorkType.EI:
                case PepsWorkType.TI:
                case PepsWorkType.I:
                case PepsWorkType.OI: MultiJobWhere = eMultiJobWhere.SRC; break;
                case PepsWorkType.EO:
                case PepsWorkType.TO:
                case PepsWorkType.O:  MultiJobWhere = eMultiJobWhere.DST; break;
                default:
                    Logger.Inst.Write(CmdLogType.Db, "SelectMultiJob PepsWorkType default"); return;
            }
            grplst = TryGrouping(MultiJobWhere, worktype);

            if (grplst.Count > 0)   // 배열첨자 인덱싱 오류 예방 체크
            {   if (grplst[0].COUNT < 2)
                {
                    if (worktype == PepsWorkType.OI)
                    {   MultiJobWhere = eMultiJobWhere.DST;
                        grplst = TryGrouping(MultiJobWhere, worktype);
                    }
                }
            }

            GroupList = grplst;
        }

        /// <summary>
        /// EXECUTE_TIME 이 WORKTYPE 에 따라 JOB 을 그룹핑하여, 여러건의 잡(JOB)을 단건으로의 조합으로 통합
        /// . 통합시 
        /// </summary>
        /// <param name="where">그룹핑 할 장비 선택 기준</param>
        /// <param name="worktype">worktype 선택</param>
        /// <returns>그룹핑된 </returns>
        //  (priority, eqpid, count)
        //  3	RO1501-1	2
        //  3	RO2114-1	3
        //  3	RR4201-1	3
        private List<MulGrp> TryGrouping(eMultiJobWhere where, PepsWorkType worktype)
        {
            List<MulGrp> itemlist =
                   Jobs.Where(p => p.REAL_TIME == ExecuteTime && p.C_priority == (int)worktype)
                       .GroupBy(p => new { PRIORITY = p.C_priority, EQPID = ((where == eMultiJobWhere.SRC) ? p.S_EQPID : p.T_EQPID) })
                       .Select(group => new MulGrp()
                       {
                           PRIORITY = group.Key.PRIORITY.Value,
                           EQPID = group.Key.EQPID,
                           COUNT = group.Count()
                       })
                       .OrderBy(x => x.PRIORITY).ThenByDescending(x => x.COUNT)
                       .ToList();
            return itemlist;
        }

#endregion selectmulti


        private struct TempNearestGoalAndVehicleStruct
        {
            public string goalName;
            public float goalDist;
            public vehicle vec;
        }

        /// <summary>
        /// Groupby된 작업목록,itemlist와 가용 가능한 vehicles 간의 거리값을 구하여, 최종 vehicle 을 선정, 배정할 것이다
        /// </summary>
        /// <param name="itemlist"></param>
        /// <param name="vecs"></param>
        /// <returns></returns>
        private TempNearestGoalAndVehicleStruct SelectVehicleNearbyToUnitS(List<pepschedule> itemlist, List<vehicle> vecs)
        {   // itemlist 의 eqpid 로 unitlist 생성
            List<unit> unitlist = new List<unit>();
            foreach (var v in itemlist)
            {
                unit unt = SelectUnitByID(v.S_EQPID);
                if (unt == null)
                    continue;
                unitlist.Add(unt);
            }

            if (unitlist.Count == 0 || vecs.Count == 0)
            {
                TempNearestGoalAndVehicleStruct ret = new TempNearestGoalAndVehicleStruct();
                return ret;
            }

            // job 목록에 해당하는 unitlist 와 가용 가능한 vecs 들의 절대 거리값을 구한다
            var resArry = new TempNearestGoalAndVehicleStruct[unitlist.Count*vecs.Count];
            Parallel.For(0, vecs.Count, i =>
            {   for (int j = 0; j < unitlist.Count; j++)
                {   resArry[i * unitlist.Count + j] = new TempNearestGoalAndVehicleStruct()
                    {   goalDist = GetDistanceBetweenPoints(new PointF((float)unitlist[j].loc_x, (float)unitlist[j].loc_y), 
                                                            new PointF((float)vecs[i].loc_x, (float)vecs[i].loc_y)),
                        goalName = unitlist[j].ID,
                        vec = vecs[i]
                    };
                }
            });

            // 최소 거리를 가지는 vehicle 을 선정, 배정할 것이다
            var min = resArry.OrderBy(p => p.goalDist).First();
            if (itemlist[0].C_VEHICLEID != null && itemlist[0].C_VEHICLEID != "" && itemlist[0].C_srcFinishTime != null)
            {
                var AssignVec = vecs.Where(p => p.ID == itemlist[0].C_VEHICLEID).SingleOrDefault();
                if (AssignVec != null)
                    min.vec = AssignVec;
            }
            return min;
        }

        private unit SelectUnitByID(string id)
        {
            unit unt = new unit();
            try
            {   unt = Db.Units.Where(p => p.ID == id).FirstOrDefault();
            }
            catch(Exception ex)
            {   Logger.Inst.Write(CmdLogType.Rv, $"Exception. SelectUnitByID. {ex.Message}\r\n{ex.StackTrace}");
                return (unit)null;
            }

            if (unt == null)
                return (unit)null;
            return unt;
        }

        private float GetDistanceBetweenPoints(PointF a, PointF b)
        {
            return (float)Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        public async void _Vsp_OnCallExecuteTime(object sender, CallExecuteTime e)
        {
            ExecuteTime = e.executeTime;
        }
    }
	
    public class SendJobToVecArgs1 : EventArgs
    {
        public string cmd;
        public string vecID = "";
        public pepschedule job;
        public vehicle vec;
        public unit    eqp;
        public string eqpslot;
        public string bufslot;
        public string sndMsg;
        public RvAssignProc rvAssign;
        public string MoveCheck_Fail_unit = "";
    }

    public class SendPreTempDown : EventArgs
    {
        public vehicle vec;
        public pepschedule job;
        public unit srcUnit;
        public unit dstUnit;
        public Nullable<int> downtemp;
    }

    public class MulGrp
    {
        public int PRIORITY;
        public string EQPID;
        public int ORDER_NUM;
        public int COUNT;
    }

    public class OrderGrp
    {
        public int ORDER_NUM;
        public int COUNT;
    }

    public class CallCmdProcArgs1 : EventArgs
    {
        public eMultiJobWhere where;
        public vehicle vec;
        public MulGrp grp;
        public string executeTime;
        public string realTime;
        public int grp_count;
    }
    public class CallSTKSrcArgs1 : EventArgs
    {
        public pepschedule pep;
        public vehicle vec;
    }
    public class CallSTKSrcArgs2 : EventArgs
    {
        public pepschedule pep;
        public vehicle vec;
    }
    public class CallSTKSrcArgs2_Flag : EventArgs
    {
        public bool STKSrc_Finish;
    }

    public class MultiSrcFinishArgs : EventArgs
    {
        public pepschedule pep;
        public vehicle vec;
    }
    public class CallExecuteTime : EventArgs
    {
        public string executeTime;
    }
}
