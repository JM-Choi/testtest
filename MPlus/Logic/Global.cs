using MPlus.Ref;
using MPlus.Vehicles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPlus.Logic
{
    public class DeleteEventArgs : EventArgs
    {
        public string cmdID = "";
    }

    public class Global
    {
        protected static MainHandler _MainHandler;
        public static MapDrawer _MapDraw = new MapDrawer();
        private static Configuration _cfg = Configuration.Init;
        private static DbHandler _db = DbHandler.Inst;
        private static VSP _vsp = VSP.Init;
        private static VehicleEventParser _vep = VehicleEventParser.Init;
        private static RvListener _rvListener = RvListener.Init;
        private static Dictionary<string/*VehicleName*/, VehicleEntity> _VehicleList = new Dictionary<string, VehicleEntity>();
        private static Dictionary<string/*VehicleName*/, RvSender> _RvSenderList = new Dictionary<string, RvSender>();
        private static Dictionary<string/*VehicleName*/, JobProccess> _JobProcList = new Dictionary<string, JobProccess>();
        private static Proc_Atom _poa = Proc_Atom.Init;
        private static Dictionary<string/*VehicleName*/, string> _DoubleMoveJobs = new Dictionary<string, string>();

        public Configuration Cfg
        {   get { return _cfg; }
            set { _cfg = value; }
        }
        public DbHandler Db
        {
            get { return _db; }
            set { _db = value; }
        }
        public VSP Vsp
        {
            get { return _vsp; }
            set { _vsp = value; }
        }
        public VehicleEventParser VEP
        {
            get { return _vep; }
            set { _vep = value; }
        }
        public Proc_Atom POA
        {
            get { return _poa; }
            set { _poa = value; }
        }
        public RvListener RvLsner
        {
            get { return _rvListener; }
            set { _rvListener = value; }
        }
        public Dictionary<string, VehicleEntity> VehicleList
        {
            get { return _VehicleList; }
            set { _VehicleList = value; }
        }
        public Dictionary<string, RvSender> RvSenderList
        {
            get { return _RvSenderList; }
            set { _RvSenderList = value; }
        }
        public Dictionary<string, JobProccess> JobProcList
        {
            get { return _JobProcList; }
            set { _JobProcList = value; }
        }
        public Dictionary<string, string> DoubleMoveJob
        {
            get { return _DoubleMoveJobs; }
            set { _DoubleMoveJobs = value; }
        }

        // controller 관련 중복 오류 메시지 발생 방지 Flag
        private bool _bAuto;
        public bool bAuto
        {   get { return _bAuto; }
            set { _bAuto = value; }
        }

        private bool _bPause;
        public bool bPause
        {
            get { return _bPause; }
            set { _bPause = value; }
        }

        public Global()
        {
            Db.OnChangeTableData += _Db_OnChangeTableData;
        }

        virtual protected void _Db_OnChangeTableData(object sender, TableUpdateArgs e)
        {
        }

#region controller status

        /// <summary>
        /// M+ controller 의 상태가 auto 이면 true, 아니면 false
        /// </summary>
        /// <param name="form"></param>
        /// <param name="b">동일상태의 중복메시지가 발생되지 않도록 flag 처리</param>
        /// <returns></returns>
        protected bool IsControllerAuto()
        {
            var controller = Db.Controllers.SingleOrDefault();
            if (controller == null)
            {
                if (bAuto == true)
                {
                    bAuto = false;
                    Logger.Inst.Write(CmdLogType.Db, $"Not found Controller Infomation");

                    AlphaMessageForm.FormMessage form = new AlphaMessageForm.FormMessage(string.Empty, AlphaMessageForm.MsgType.Error, AlphaMessageForm.BtnType.Ok);
                    form.SetMsg("Not found Controller Infomation");
                    form.ShowDialog();
                }
                return false;
            }

            if (!bAuto)
                bAuto = true;

            if (controller.C_state != (int)ControllerState.AUTO)
                return false;

            return true;
        }
        #endregion controller status

        #region db search
        protected vehicle SelectVehicleByID(string id)
        {
            vehicle vec = new vehicle();
            try
            {   vec = Db.Vechicles.Where(p => p.ID == id).FirstOrDefault();
            }
            catch(Exception ex)
            {   Logger.Inst.Write(CmdLogType.Rv, $"Exception. SelectVehicleByID. {ex.Message}\r\n{ex.StackTrace}");
                return (vehicle)null;
            }

            if (vec == null)
                return (vehicle)null;
            return vec;
        }

        protected void UpdateVehicleByID(string vecid, string batchid)
        {
            try
            {   vehicle vec = Db.Vechicles.Where(p => p.ID == vecid).FirstOrDefault();
                if (vec != null)
                {   vec.C_BATCHID = batchid;
                    Db.DbUpdate(TableType.VEHICLE);
                }
                pepschedule pep = Db.Peps.Where(p => p.BATCHID == batchid).FirstOrDefault();
                if (pep != null)
                {
                    pep.C_VEHICLEID = vecid;
                    Db.DbUpdate(TableType.PEPSCHEDULE);
                }
            }
            catch(Exception ex)
            {   Logger.Inst.Write(vecid, CmdLogType.Rv, $"Exception. SelectVehicleByID. {ex.Message}\r\n{ex.StackTrace}");
                return;
            }
        }

        #endregion //db search

    }
}
