using MPlus.Ref;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
//using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace MPlus.Vehicles
{
    public class VecStatus
    {
        public int posX;
        public int posY;
        public int angle;
        public int charge;
        public VehicleMode mode;
        public VehicleState state;

        public static VecStatus Parse(string input)
        {
            VecStatus data = new VecStatus();

            string[] words = input.ToUpper().Split(';');

            data.posX   = Convert.ToInt32((words[1].Length == 0) ? "0" : words[1]);
            data.posY   = Convert.ToInt32((words[2].Length == 0) ? "0" : words[2]);
            data.angle  = Convert.ToInt32((words[3].Length == 0) ? "0" : words[3]);
            data.state  = (VehicleState)Enum.Parse(typeof(VehicleState), words[4]);
            data.mode   = (VehicleMode)Enum.Parse(typeof(VehicleMode), words[5]);
            data.charge = (int)Convert.ToDouble((words[6].Length == 0) ? "99" : words[6]);

            return data;
        }
        public VecStatus()
        {
        }
        public VecStatus(VecStatus vec)
        {   this.posX   = vec.posX;
            this.posY   = vec.posY;
            this.angle  = vec.angle;
            this.charge = vec.charge;
            this.mode   = vec.mode;
            this.state  = vec.state;
        }
    }
    public class VecErrStatus
    {
        public int state;
        public int ErrCode;
        public static VecErrStatus Parse(string input)
        {
            VecErrStatus data = new VecErrStatus();

            string[] words  = input.ToUpper().Split(';');

            data.state      = Convert.ToInt32(words[1]);
            data.ErrCode    = Convert.ToInt32(words[2]);
            
            return data;
        }
    }
    public class VecJobStatus
    {
        public string batchID;
        public VehicleCmdState state;
        public int port;
        public int slot;
        public int port_dst; // jm.choi 추가 - 190305
        public int slot_dst; // jm.choi 추가 - 190305
        public string trayid;
        public string all;
        public static VecJobStatus Parse(string input)
        {
            string[] words = input.ToUpper().Split(';');
            VecJobStatus data = new VecJobStatus();
            try
            {
                data.batchID = words[1];
                data.state = (VehicleCmdState)Enum.Parse(typeof(VehicleCmdState), words[2]);
                if (data.state == VehicleCmdState.TRANS_END)
                {
                    data.port = Convert.ToInt32((words[3].Length == 0) ? "-1" : words[3]);
                    data.slot = Convert.ToInt32((words[4].Length == 0) ? "-1" : words[4]);
                    data.trayid = words[5];

                    // jm.choi 추가 - 190305
                    // TransEnd에 추가된 Port/Slot 의 정보 저장
                    data.port_dst = Convert.ToInt32((words[3].Length == 0) ? "-1" : words[6]);
                    data.slot_dst = Convert.ToInt32((words[4].Length == 0) ? "-1" : words[7]);
                }
                else if (data.state == VehicleCmdState.TRANS_COMPLETE)
                {
                    for (int i = 3; i < words.Count(); i++)
                    {
                        data.all += words[i];
                        data.all += ";";
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"exception VecJobStatus: {ex.Message}\r\n{ex.StackTrace}");
            }
            return data;
        }
    }
    public class VecScanArgs
    {
        public string vehicleid;
        public int[] port = new int[4];
        public string[,] trayid = new string[4,10];
        public static VecScanArgs Parse(string input)
        {
            string[] words = input.ToUpper().Split(';');
            VecScanArgs data = new VecScanArgs();
            data.vehicleid = words[1];
            for(int i = 0, j = 2; i<4; i++,j++)
            {
                data.port[i] = Convert.ToInt32((words[j].Length == 0) ? "0" : words[j]);
                for(int x = 0; x < 10; x++)
                {
                    j++;
                    data.trayid[data.port[i], x] = words[j];
                }
            }
            return data;
        }
    }
    public class VecGoal
    {
        public string goal;
        public static VecGoal Parse(string input)
        {
            string[] words = input.ToUpper().Split(';');
            VecGoal data = new VecGoal();
            data.goal = words[0];
            return data;
        }
    }
    public class RecvMsgArgs : EventArgs
    {
        public string recvMsg;
        public Cmd4Vehicle Cmd;
        public VehicleEvent reportEvent;
        public VecStatus Status;
        public VecJobStatus JobState;
        public VecGoal Goal;
        public VecErrStatus ErrState;
        public VecRemoteEvent Remote;
        public VecScanArgs Scan;
    }
    public class StandbyMsgArgs : EventArgs
    {
        public string destGoal = string.Empty;
        public string vecId = string.Empty;
    }
    public class VecRemoteEvent
    {
        public string Rcmd = "";
        public static VecRemoteEvent Parse(string input)
        {
            string[] words = input.ToUpper().Split(';');

            VecRemoteEvent data = new VecRemoteEvent() {
                Rcmd = words[1]
            };
            return data;
        }
    }
    public class MonitorArgs : EventArgs
    {
        public double charge;
        public double posX;
        public double posY;
        public int angle;
        public VehicleMode mode;
        public VehicleCmdState state;
    }
    public partial class VehicleEntity : IDisposable
    {
        public event EventHandler<RecvMsgArgs> OnRecvMsg;
        //public event EventHandler<VecStatus> OnRecvMonitor;
        public event EventHandler<ChangeConnectedArgs> OnChageConnected;
        //public event EventHandler<StandbyMsgArgs> OnGotoStandby;
        //public event EventHandler<StandbyMsgArgs> OnStopIdle;

        private Timer tmrConnect = new Timer();
        private Timer tmrSpoolling = new Timer();
        private Timer tmrState = new Timer();
        //private Queue<string> spoolResp = new Queue<string>();

        //StartConnection 요청시 ip,port,id 값 할당
        private string _id;               
        public string Id
        {   get { return _id; }
            set { _id = value; }
        }
        private string _ip;
        public string Ip
        {   get { return _ip; }
            set { _ip = value; }
        }
        private int _port;
        public int Port
        {   get { return _port; }
            set { _port = value; }
        }
        private string _remoteIp;
        public string RemoteIp
        {   get { return _remoteIp; }
            set { _remoteIp = value; }
        }
        private int _remotePort;
        public int RemotePort
        {   get { return _remotePort; }
            set { _remotePort = value; }
        }
        private bool _jobassign;
        public bool JobAssign
        {
            get { return _jobassign; }
            set { _jobassign = value; }
        }
        private ChargeStandby _csby;
        public ChargeStandby Csby
        {   get { return _csby; }
            set { _csby = value; }
        }

        private AsyncClintSock sock;            // m6x 와 연결에 사용할 socket
        public bool IsConnected = false;

        private string _MsgBuf = "";            // socket 에서 사용할 버퍼
        private readonly char _Token = '\n';    // 메시지 끝을 찾을 구분자
        private readonly char _SubToken = '\r'; // 그 다움 구분자

        public ControllerState controllerState = ControllerState.INIT;

        private VehicleMode _curVecMode;
        public VehicleMode CurVecMode
        {   get { return _curVecMode; }
            set { _curVecMode = value; }
        }

        public VehicleEntity()
        {
            _csby = new ChargeStandby(this);
        }

        public void SetInfo(string ip, int port, string id, string remoteIP, int? remotePort)
        {
            Ip = ip;
            Port = port;
            Id = id;
            RemoteIp = remoteIP;
            RemotePort = (remotePort == null) ? 0 : (int)remotePort;
        }

        private void InitSock(string vecID)
        {
            sock = new AsyncClintSock(true, vecID);
            sock.OnRcvMsg += Sock_OnRcvMsg;
            sock.OnChangeConnected += Sock_OnChangeConnected;
        }

        private void Sock_OnChangeConnected(object sender, ChangeConnectedArgs e)
        {
            if (e.connected)
            {
                IsConnected = true;
                Logger.Inst.Write(this.Id, CmdLogType.Comm, $"{Ip} : O (Client 접속 성공 이벤트)");
                StillConnected();
            }
            else
            {
                IsConnected = false;
                Logger.Inst.Write(this.Id, CmdLogType.Comm, $"{Ip} : X (Client 접속 종료 이벤트)");
            }
        }

        public void SetupTimer(string vecID)
        {
            InitSock(vecID);

            tmrConnect.Interval = 5000;
            tmrConnect.Elapsed += tmrConnect_Elapsed;
            tmrConnect.Enabled = true;
        }

        public void StateCheck()
        {
            tmrState.Interval = 1000;
            tmrState.Elapsed += tmrState_Elapsed;
            tmrState.Enabled = true;
        }
        public async Task<bool> SendMessageToVehicle(string msg)
        {
            if (msg.Contains("CONNECT") || msg.Contains("GOAL_LD"))
            {
                // jm.choi 추가 - 190304
                // CONNECT, GOAL_LD 시 Message Send를 하지않아 Send하는 부분 추가
                if (sock.Connected)
                {
                    try
                    {
                        await Task.Delay(1);
                        sock.SendMessage(msg + "\r\n");
                        Logger.Inst.Write(this.Id, CmdLogType.Comm, $"S = [{msg}]");
                        return true;
                    }
                    catch (Exception e)
                    {
                        //spoolResp.Enqueue(msg);
                        Logger.Inst.Write(this.Id, CmdLogType.Comm, $"{Id}:예외 {e.ToString()} : S = [{msg}]");
                        return false;
                    }
                }
                else
                {
                    //spoolResp.Enqueue(msg);
                    Logger.Inst.Write(this.Id, CmdLogType.Comm, $"{Id}:연결이 되지 않아 보내지 못했습니다. : S = [{msg}]");
                    return false;
                }
            }
            else
            {
                if (controllerState == ControllerState.AUTO)
                {
                    if (CurVecMode == VehicleMode.TEACHING && CurVecMode == VehicleMode.ERROR
                        || CurVecMode == VehicleMode.INIT && CurVecMode == VehicleMode.AUTO)
                    {
                        Logger.Inst.Write(this.Id, CmdLogType.Comm, $"Ignore~~~~~~ VehicleMode is {CurVecMode.ToString()}");
                        return false;
                    }
                    else
                    {
                        //spoolResp 사용에 대해 고민하자.
                        if (sock.Connected)
                        {
                            try
                            {
                                await Task.Delay(1);
                                sock.SendMessage(msg + "\r\n");
                                Logger.Inst.Write(this.Id, CmdLogType.Comm, $"S = [{msg}]");
                                return true;
                            }
                            catch (Exception e)
                            {
                                //spoolResp.Enqueue(msg);
                                Logger.Inst.Write(this.Id, CmdLogType.Comm, $"{Id}:예외 {e.ToString()} : S = [{msg}]");
                                return false;
                            }
                        }
                        else
                        {
                            //spoolResp.Enqueue(msg);
                            Logger.Inst.Write(this.Id, CmdLogType.Comm, $"{Id}:연결이 되지 않아 보내지 못했습니다. : S = [{msg}]");
                            return false;
                        }
                    }
                }
                else
                {
                    Logger.Inst.Write(this.Id, CmdLogType.Comm, $"{Id} controllerState is No Auto. controllerState : {controllerState.ToString()}");
                    return false;
                }
            }
            return false;
        }

        public void SendPauseCmd()
        {
            controllerState = ControllerState.PAUSED;
            SendMessageToVehicle("REMOTE;PAUSE;");
        }

        public void SendResumeCmd()
        {
            controllerState = ControllerState.AUTO;
            SendMessageToVehicle("REMOTE;RESUME;");
        }
        public void SendStopCmd()
        {
            SendMessageToVehicle("STOP;");
        }

        public void SendCancelCmd()
        {
            //SendMessageToVehicle("REMOTE;ABORT;");
            SendMessageToVehicle("REMOTE;CANCEL;");
        }

        public void SendJobResumeCmd()
        {
            SendMessageToVehicle("JOB_RESUME;");
        }

        public void Reconnected()
        {
            sock.StopClient();
            IsConnected = false;
            OnChageConnected?.Invoke(this, new ChangeConnectedArgs() { connected = false });
            Logger.Inst.Write(this.Id, CmdLogType.All, $"VEHICLE Socket Close");

        }

        private bool _IsEndDistanceCalc = false;
        public bool IsEndDistanceCalc { get { return _IsEndDistanceCalc; } }

        public void SendCalcDistance(string goalName)
        {
            _IsEndDistanceCalc = false;
            SendMessageToVehicle($"DISTANCE;{goalName};");
        }
        private int _CalcDistResult = -1;
        public async Task<int> SendCalcDistanceBtw(string goalName1, string goalName2)
        {
            _IsEndDistanceCalc = false;
            SendMessageToVehicle($"DISTANCEBTW;{goalName1};{goalName2};");
            int cnt = 0;
            while (!_IsEndDistanceCalc)
            {
                await Task.Delay(10);
                if (cnt++ >= 500)
                {
                    _CalcDistResult = -1;
                    break;
                }
            }
            return _CalcDistResult;
        }

        public bool jobResponse = false;
        public bool goResponse = false;
        public bool goReSend = false;
        public bool goRetry_Fail = false;
        public int goRetry_count = 0;

        public void goReset()
        {
            goResponse = false;
            goReSend = false;
            goRetry_Fail = false;
            goRetry_count = 0;
        }
        private async void MsgPars(string msg)
        {
            await Task.Delay(1);
            Logger.Inst.Write(this.Id, CmdLogType.Comm, $"R = [{msg}]");
            RecvMsgArgs sendArg = new RecvMsgArgs() { recvMsg = msg };
            tmrState.Enabled = true;
            State_wait_count = 0;

            sendArg.Cmd = GetVehicleCmd(msg);
            switch (sendArg.Cmd)
            {
                case Cmd4Vehicle.STATUS:
                    VecStatus CurrStatus = VecStatus.Parse(sendArg.recvMsg);
                    sendArg.Status = VecStatus.Parse(sendArg.recvMsg);
                    sendArg.reportEvent = VehicleEvent.ReportStatus;
                    {
                        Csby.propertyCurrVecStatus = new VecStatus(CurrStatus);
                        CurVecMode = CurrStatus.mode;
                    }
                    break;
                case Cmd4Vehicle.ERROR:
                    sendArg.ErrState = VecErrStatus.Parse(sendArg.recvMsg);
                    break;
                case Cmd4Vehicle.JOB:
                    sendArg.JobState = VecJobStatus.Parse(sendArg.recvMsg);
                    break;
                case Cmd4Vehicle.GOAL_LD:
                case Cmd4Vehicle.GOAL_UL:
                    sendArg.Goal = VecGoal.Parse(sendArg.recvMsg);
                    sendArg.reportEvent = VehicleEvent.RequestGoalList;
                    break;
                case Cmd4Vehicle.SCAN:
                    sendArg.Scan = VecScanArgs.Parse(sendArg.recvMsg);
                    break;
                case Cmd4Vehicle.RESP:
                    Logger.Inst.Write(this.Id, CmdLogType.Comm, $"메세지 응답 수신 [{msg}]");
                    break;
                case Cmd4Vehicle.GOAL_FAIL:
                    Logger.Inst.Write(this.Id, CmdLogType.Comm, $"[{msg}]");
                    break;
                default:
                    Logger.Inst.Write(this.Id, CmdLogType.Comm, $"잘못된 명령을 수신 [{msg}]");
                    return;
            }
            
            OnRecvMsg?.Invoke(this, sendArg);
            if (sendArg.Cmd == Cmd4Vehicle.STATUS)
            {
                //OnRecvMonitor?.Invoke(this, CurrStatus);    // 자동충전 시나리오 한다
            }

            if (sendArg.Cmd == Cmd4Vehicle.GOAL_LD || sendArg.Cmd == Cmd4Vehicle.GOAL_UL)
            {
            }
        }

        private VehicleMode GetVehicleModeType(string type)
        {
            return (VehicleMode)Enum.Parse(typeof(VehicleMode), type);
        }

        public void Dispose()
        {
            Csby.propertyStop = true;
            tmrConnect.Stop();
            tmrConnect.Dispose();
            //sock.Dispose();
            sock.StopClient();
            sock = null;
        }

#region 확인완료
        /// <summary>
        /// 수신 메시지의 가장 앞단을 잘라낸다. 명령어 부분
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private Cmd4Vehicle GetVehicleCmd(string msg)
        {
            Cmd4Vehicle rtn = Cmd4Vehicle.None;
            char tok = ';';
            var split = msg.Split(tok);

            try
            {
                rtn = (Cmd4Vehicle)Enum.Parse(typeof(Cmd4Vehicle), split[0]);
            }
            catch (Exception)
            {
                Logger.Inst.Write(CmdLogType.Comm, $"String to Enum(Cmd4Vehicle) 변환에 실패하였습니다.{split[0]}");
            }
            return rtn;
        }
        private void Sock_OnRcvMsg(object sender, string rcvStr)
        {
            _MsgBuf += rcvStr;
            sock.Connected_chk = false;
            while (_MsgBuf.IndexOf(_Token) != -1)
            {
                var split = _MsgBuf.Split(_Token);
                foreach (var item in split)
                {
                    try
                    {
                        if (item.IndexOf(_SubToken) != -1)
                        {
                            //'\r'을 삭제
                            string tempStr = item.Remove(item.Length - 1);
                            MsgPars(tempStr);
                        }
                    }
                    catch
                    {
                        Logger.Inst.Write(CmdLogType.Comm, $"차량으로부터 받은 메세지를 분석 할 수 없습니다. [{item}]");
                        continue;
                    }
                }
                _MsgBuf = _MsgBuf.Remove(0, _MsgBuf.LastIndexOf(_Token) + 1);
            }
            sock.Connected_chk = true;
        }
        /// <summary>
        /// 1초의 delay 를 주고 "CONNECT" 메시지를 Vehicle에 전송한다.
        /// </summary>
        private async void StillConnected()
        {
            int connectDelay = 0;
            while (sock.Connected)
            {
                await Task.Delay(100);
                connectDelay++;

                if (connectDelay >= 10)
                {
                    SendMessageToVehicle("CONNECT");
                    break;
                }
            }
        }
        /// <summary>
        /// timer 에 의해 connection 모니터링
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmrConnect_Elapsed(object sender, ElapsedEventArgs e)
        {
            tmrConnect.Enabled = false;
            if (IsConnected)
            {
                if (sock.Connected)
                {
                    OnChageConnected?.Invoke(this, new ChangeConnectedArgs() { connected = true });
                    Debug.WriteLine("connected");
                }
                else
                {
                    sock.StopClient();
                    IsConnected = false;
                    OnChageConnected?.Invoke(this, new ChangeConnectedArgs() { connected = false });
                    Debug.WriteLine("disconnected");
                }
            }
            else
            {
                if (!sock.Connected)
                {
                    sock.StopClient();
                    OnChageConnected?.Invoke(this, new ChangeConnectedArgs() { connected = false });
                    Debug.WriteLine("disconnected");
                }
                
#if true
                sock.ConnectToServer(Ip, (ushort)Port, RemoteIp, (ushort)RemotePort, Id);
#else
                sock.StopClient();
                InitSock();
                sock.ConnectToServer(Ip, (ushort)Port, RemoteIp, (ushort)RemotePort, Id);
#endif
            }

            try
            {
                tmrConnect.Enabled = true;
            }
            catch (Exception) { }                
        }
        #endregion

        int State_wait_count = 0;
        /// <summary>
        /// timer 에 의해 connection 모니터링
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void tmrState_Elapsed(object sender, ElapsedEventArgs e)
        {
            tmrState.Enabled = false;
            if (State_wait_count >= 30)
            {
                Reconnected();
            }
            else
            {
                State_wait_count++;
                tmrState.Enabled = true;
            }
        }
    }

}
