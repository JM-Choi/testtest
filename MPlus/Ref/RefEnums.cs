using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPlus.Ref
{
    public enum eMultiJobWhere
    {
        NONE = 0,
        SRC,
        DST
    }
    public enum RvState
    {
        disconnected,
        connected,
    }
    /*반송 JOB 을 수행하기 전에 EQ 및 RV 상태를 체크한다
     * From 을 먼저 체크하는 것으로 한다
     * */
    public enum RvStatusChk
    {
        eChkStepFrom,
        eChkStepTo,
    }

    /*현재 TrayStoker, SysWin(Chamber), Handler 총 3종
     * Reflow 는 아직 개발 전. 추후 추가 예정
     * */
    public enum EqpGoalType
    {
        STK = 1,
        SYSWIN = 3,
        HANDLER = 4,
        REFLOW = 5,
        SYSWIN_OVEN = 6,
        SYSWIN_OVEN_t = 7,  // 새로운 OVEN Type 추가 jm.choi - 190509
        HANDLER_STACK = 8,  // Stack형 Handler Type 추가 jm.choi - 190423
        BUFFER_STK = 14,
        rozze_Default = 99
    }

    public enum RvAssignProc
    {
        None,
        From_STK,
        From_SYSWIN,
        From_HANDLER,
        From_REFLOW,    // jm.choi 추가 - 190215
        to_STK,
        to_SYSWIN,
        to_HANDLER,
        to_REFLOW,      // jm.choi 추가 - 190215
    }
    /// <summary>
    /// VEHICLE_PORT 의 state 에서 사용
    /// </summary>
    public enum VehiclePartState
    {
        DISABLE = 0,
        ENABLE = 1,
    }
    /// <summary>
    /// PepsSchedule 쿼리는 동일시간(EXCUTETIME) 내에서 INOUT > TESTER 우선
    /// PepsSchedule 쿼리는 동일시간(EXCUTETIME) 내에서 INOUT : OUT > OUT/IN > IN
    /// PepsSchedule 쿼리는 동일시간(EXCUTETIME) 내에서 TESTER : EMPTY OUT > EMPTY IN > TESTER OUT > TESTER IN
    /// 실제입력은
    /// </summary>
    public enum PepsWorkType
    {
        NONE = 0,       // start mark       0
        OI = 1,         // OUT/IN           1
        O = 3,          // OUT              2
        I = 2,          // IN               4
        TEMP_DOWN = 4,  // TEMPDOWN         8

        EO = 5,         // EMPTY OUT       16
        EI = 6,         // EMPTY IN        32
        TO = 7,         // TESTER_OUT      64
        TI = 8,         // TESTER_IN      128
        LAST = 9,       //                256
    }
    /// <summary>
    /// Vehicle 이 가용 가능한지 설정
    /// </summary>
    public enum VehicleInstallState
    {
        REMOVED = 0,
        INSTALLED = 1,
        NOTUSED = 2,
    }
    /// <summary>
    /// Vehicle Mode
    /// </summary>
    public enum VehicleMode
    {
        TEACHING = 0,
        MANUAL = 1,
        AUTO = 2,
        ERROR = 3,
        INIT = 4
    }
    /// <summary>
    /// Vehicle State
    /// </summary>
    public enum VehicleState
    {
        NOT_ASSIGN,
        PARKED,
        ENROUTE,
        ACQUIRING,
        DEPOSITING,
        CHARGING,
    }
    public enum VehicleEvent
    {
        Assigned,
        Unassigned,
        Enroute,
        Parked,

        TransferInitiated,
        VehicleAssigned,
        VehicleArrivedSrc,
        VehicleArrivedDst,
        Transferring,
        VehicleAcquireStarted,
        CarrierInstalled,
        VehicleAcquireCompleted,
        VehicleDeparted,
        VehicleDepositStarted,
        CarrierRemoved,
        VehicleDepositCompleted,
        TransferCompleted,
        VehicleUnassiged,

        Charging,
        ChargEnd,


        RcmdResp,
        CancelComp,
        AbortComp,

        AlarmSet,
        AlarmClr,

        CalcDistanceCost,

        RequestGoalList,

        ReportStatus,
    }

    public enum ControlState
    {
        OFFLINE,
        REMOTE,
    }

    public enum TableType
    {
        ALARM,
        ALARM_DEFINE,
        PEPSCHEDULE,
        CONTROLLER,
        UNIT,
        VEHICLE,
        VEHICLE_PART,
        ZONE,
    }

    public enum GoalType
    {
        None
        , Pickup
        , Dropoff
    }

    public enum CmdState
    {
        SRC_NOT_ASSIGN,
        PRE_ASSIGN,     // 스케쥴 입력 인식

        ASSIGN,         //차량에 할당된 상태. VechileID에는 무엇인가 들어 있다. 차량으로부터 응답을 기다리는 상태. 응답 없으면 다시 QUEUE. **멀티작업**은 여기서 다음 작업을 기다린다.
        SRC_ENROUTE,    //주행중
        SRC_ARRIVED,    //소스 도착. 
        SRC_START,      //실물 이적재 작업을 시작한다는 의미. 여기서는 PIO 작업의 시작을 의미한다.   
        SRC_BEGIN,      //UR 동작 시작
        SRC_END,        //케리어의 이동이 완료되었다. ACQ_COMP까지 한방에 처리
        SRC_COMPLETE,   //소스작업을 완료하고 데스트 작업 등록 상태.

        DEPARTED,       //차량에 데스트 작업이 할당된 상태. 차량으로 부터 응답을 기다린다. 안오면 다시 ACQ_COMP. **멀티작업**은 여기서 다음작업을 기다린다.
        DST_ENROUTE,
        DST_ARRIVED,    //데스트 도착.
        DST_START,      //UR 동작
        DST_BEGIN,
        DST_END,        
        DST_COMPLETE,   //

        CANCEL_INIT,
        ABORT_INIT,
        SRC_FAIL,
        DST_FAIL,
        CANCEL,
        ABORT,
        EQP_NOTEXIST,
        BATCHID_NOTFOUND,
        PRETEMPDOWN_SUCC,
        PRETEMPDOWN_FAIL,
        INVALID_SLOT,
        EXECUTETIME_OVER,
        UR_ERROR,
        DST_NOT_ASSIGN,
    }
    public enum CollectionEvent
    {
        Offline = 1101,
        Local = 1102,
        Remote = 1103,
        AgvcAutoInitiated = 1201,
        AgvcAutoComplete = 1202,
        AgvcPauseInitiated = 1203,
        AgvcPauseCompleted = 1204,
        AgvcPaused = 1205,
        AlarmSet = 1301,
        AlarmClear = 1302,
        UnitAlarmSet = 1303,
        UnitAlarmClear = 1304,
        TransferInitiated = 3101,
        TransferCompleted = 3102,
        TransferPaused = 3103,
        TransferResumed = 3104,
        Transferring = 3105,
        TransferAbortInitiated = 3201,
        TransferAbortCompleted = 3202,
        TransferAbortFailed = 3203,
        TransferCancelInitiated = 3301,
        TransferCancelCompleted = 3302,
        TransferCancelFailed = 3303,
        VehicleArrived = 4101,
        VehicleAcquireStarted = 4102,
        VehicleAcquireCompleted = 4103,
        VehicleDeparted = 4104,
        VehicleDepositStarted = 4105,
        VehicleDepositCompleted = 4106,
        VehicleInstalled = 4201,
        VehicleRemoved = 4202,
        VehicleAssigned = 4203,
        VehicleUnassiged = 4204,
        VehicleStateChanged = 4205,
        VehicleMoving = 4206,
        CarrierInstalled = 5101,
        CarrierRemoved = 5102,
        OperatorInitAction = 6101,

        VehicleArrived_src,// = 4101,
        VehicleArrived_dst,
    }

    public enum CmdType
    {
        Job,
        Charge,
        Manual_Move,
        Manual_Transfer,
        Manual_Transfering,
        Batch_Fail,
    }

    public enum ReportId
    {
        None = 0,
        TSC_State,
        Alarm,
        Vehicle_State,
        Transfer,
        Transfer_Complete,
        Vehicle_12,
        Vehicle_22,
        Carrier,
        Operator_Action,
        Unit_Alarm,
    }


#if true
    public enum VehicleCmdState
    {
        None
        , ASSIGN
        , ENROUTE
        , ARRIVED
        , TRANS_START
        , TRANS_PLAY
        , TRANS_ERROR
        , TRANS_BEGIN
        , TRANS_END
        , TRANS_COMPLETE
        , TRANS_FAIL
        //, USER_STOPPED
        //, USER_CANCEL
        , GO_END
        , PIO_START
    }
#else
    public enum VehicleCmdState
    {
        None
        , ASSIGN
        , ENROUTE
        , ARRIVED
        , TRANSFERRING
        , TRANS_START
        , CARRIER_CHANGED
        //, TRANS_COMP
        , USER_STOPPED
    }
#endif

    public enum VehicleType
    {
        None
        , TRAY
        , STACK
        , COMMON
    }
    public enum Cmd4MPlus
    {
        None
        , SRC
        , DST
        , REMOTE
        , DISTANCEBTW
    }

    public enum Cmd4Vehicle
    {
        None,
        SCAN,
        GOAL_LD,
        GOAL_UL,
        JOB,
        STATUS,
        ERROR,

        RESP,
        GOAL_FAIL,
    }

  
    


    public enum VehiclePartStatus
    {
        FULL,
        EMPTY,
    }

    public enum CmdByWho
    {
        ACS,
        UI,
        HOST,
        VEHICLE,
    }

    public enum ResultCode
    {
        SUCCESSFUL,
        PICKUPPIOERROR,
        DEPOSITPIOERROR,
        ABNORMALERROR,
    }

    public enum RemoteCommandType
    {
        TRANSFER,
        CANCEL,
        ABORT,
    }

    public enum ControllerState
    {
        INIT = 1,
        PAUSED,
        AUTO,
        PAUSING,
        RESUME,
        STOP,
    }

    public enum ControllerOnlineState
    {
#if true
        OFFLINE = 1,
        ONLINE = 2,
#else
        OFFLINE_EQP = 1,
        OFFLINE_WILL,
        OFFLINE_HOST,
        ONLINE_LOCAL,
        ONLINE_REMOTE,
#endif
    }


    public enum OperAction
    {
        TRANSFER,
        CANCEL,
        ABORT,
    }

    public enum LOGCMD
    {
        Job
        , Vehicle
        , Etc
    }

    public enum EqpGoalActionType
    {
        Loading,
        Unloading,
    }

    public enum MagSizeType
    {
        Error,
        A,
        B,
        C,
        D,
        E,
        F,
    }

    public enum StandbyGoalStatus
    {
        Empty,
        Full,
    }

    public enum M6xError
    {
        NO_ERROR = 0,
        LD_FAILED_TO_START_GOAL,
        LD_FAILED_TO_REACH_GOAL,
        LD_FALED_TO_MOVE,
        LD_FAILED_TO_DOCK,

        LASER_UNKNOWN_VALUE_RECEIVED = 100,
        LASER_FAILED_TO_ALIGN,
        LASER_FAILED_TO_RECEIVE_DATA,

        QR_FAILED_TO_FOCUS = 200,
        QR_FAILED_TO_READ,
        QR_FAILED_TO_ALIGN,

        UR_FAIL_TO_PLAY_PROGRAM = 300,
        UR_FAIL_TO_LOAD_PROGRAM,
        UR_FAIL_TO_GET_LOADED_PROGRAM,
        UR_FAIL_TO_EXECUTE_SCRIPT,
        UR_FAIL_TO_ASSIGN_PARTITION,
        UR_FAIL_TO_ASSIGN_MGZ_SIZE_PARTITION,
        UR_FAIL_TO_TRG_AND_ALIGN_CAMERA,
        UR_FAIL_TO_COMPLETE_PROGRAM,
        UR_SAFETY_MODE,
        UR_GRIIPER_FAILED,
        UR_FAILED_TO_MOVE,

        PLC_PIO_ERROR = 400,
    }

    public class RefEnums
    {


    }
}
