using MPlus.Ref;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using tech_library.Tools;

namespace MPlus.Logic
{

    public abstract class RvSender_member : Global
    {
        public class RV_EQ
        {
            public bool _berror;     // error 발생, _bwait 플래그가 의미 없어질, 없을 것이다
            public bool _bSucc;
            public bool _bWait;
            public bool _TempDownberror;     // error 발생, _bwait 플래그가 의미 없어질, 없을 것이다
            public bool _TempDownbSucc;
            public bool _TempDownbWait;
            public pepschedule job;
            public unit eqp;
            public bool _bVehicleSucc;
            public bool _bVehicleWait;
            public bool _bReflowSucc;
            public bool _bLoaderSucc;
            public bool _bReflowerror;
            public bool _bLoadererror;
            public bool _bReflowWait;
            public bool _bLoaderWait;
            public string reflowRunMode = "ONLINEREMOTE"; // REFLOW RunMode를 저장하기 위한 변수 jm.choi 추가 - 190215

            public bool _MoveCompberror;     // error 발생, _bwait 플래그가 의미 없어질, 없을 것이다
            public bool _MoveCompbSucc;
            public bool _MoveCompbWait;

            public bool tempdown_check = true;

            public bool jobcancel_check = false;
            public bool lp_tray = false;
            public bool STK_Retry = false;
            // Handler Loader 저장 변수
            public List<pepschedule> Handler_LD_peps = new List<pepschedule>();
            public List<string> Handler_LD_TrayID = new List<string>();
            public List<string> Handler_LD_LotID = new List<string>();
            public List<string> Handler_LD_StepID = new List<string>();

            public void Handler_LD_Reset()
            {
                Handler_LD_peps = new List<pepschedule>();
                Handler_LD_TrayID = new List<string>();
                Handler_LD_LotID = new List<string>();
                Handler_LD_StepID = new List<string>();
            }
            public void Reset()
            {
                _berror = false;
                _bWait = false;
                _bSucc = false;
                job = null;
                eqp = null;
            }
            public void ResetFlag()
            {
                _berror = false;
                _bWait = false;
                _bSucc = false;
            }
            public void MoveCompResetFlag()
            {
                _MoveCompberror = false;
                _MoveCompbWait = false;
                _MoveCompbSucc = false;
            }
            public void ResetReflowFlag()
            {
                _bReflowSucc = false;
                _bLoaderSucc = false;
                _bReflowerror = false;
                _bLoadererror = false;
                _bReflowWait = false;
                _bLoaderWait = false;

            }
            public void ResetvehicleFlag()
            {
                _bVehicleWait = false;
                _bVehicleSucc = false;
            }
            public void Alloc(SendJobToVecArgs1 e)
            {
                Reset();                
                reflowRunMode = string.Empty;
                job = e.job;
                eqp = e.eqp;
            }
        }

        public RV_EQ RvComm = new RV_EQ();

        private string _vehicleId;
        public string VehicleId
        {
            get { return _vehicleId; }
            set { _vehicleId = value; }
        }

        public TIBCO.Rendezvous.Transport Transport
        {
            get { return RvLsner.Transport; }
        }

        public string SndSubjct = "KDS1.LH.";

        private string _jobtype;
        public string JobType
        {
            get { return _jobtype; }
            set { _jobtype = value; }
        }

        private EqpGoalType _goalType;
        public EqpGoalType GoalType
        {
            get { return _goalType; }
            set { _goalType = value; }
        }

        private pepschedule _curJob;
        public pepschedule CurJob
        {
            get { return _curJob; }
            set { _curJob = value; }
        }

        private bool _ismulti;
        public bool IsMulti
        {
            get { return _ismulti; }
            set { _ismulti = value; }
        }

        private bool _isStop;
        public bool IsStop
        {
            get { return _isStop; }
            set { _isStop = value; }
        }

        private bool _handlerStandby;
        public bool HandlerStandby
        {
            get { return _handlerStandby; }
            set { _handlerStandby = value; }
        }
        private List<pepschedule> _multilist;
        public List<pepschedule> MultiList
        {
            get
            {
                lock (this)
                {
                    return _multilist;
                }
            }
            set
            {
                lock (this)
                {
                    _multilist = value;
                }
            }
        }

        public List<string> lstTempDown = new List<string>();
        public object syncTempDown = new object();

        public class ReflowLoaderInfoSet_val : EventArgs
        {
            public string tray01id = string.Empty;
            public string qty01id = string.Empty;
            public string lot01id = string.Empty;
            public string tray01stepid = string.Empty;
            public string au01stepid = string.Empty;

            public string tray02id = string.Empty;
            public string qty02id = string.Empty;
            public string lot02id = string.Empty;
            public string tray02stepid = string.Empty;
            public string au02stepid = string.Empty;
        }
    }
}
