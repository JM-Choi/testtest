using MPlus.Ref;
using MPlus.Vehicles;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MPlus.Logic
{
    public partial class JobProccess
    {
        // CallCmdProcedure Method Start  
#region CallCmdProcedure Method
        private void Job_Select(ref List<pepschedule> Jobs, ref bool IsMultiJob, CallCmdProcArgs1 e)
        {
            if (e.grp_count > 1)
            {
                IsMultiJob = true;
                Jobs = SelectMultiList_OI(true, e.where, e.grp.PRIORITY, e.realTime);
                e.where = eMultiJobWhere.DST;
            }
            else
            {
                if (e.grp.COUNT > 0)
                {
                    if (e.grp.COUNT > 1)
                        IsMultiJob = true;
                    else
                        IsMultiJob = false;

                    Jobs = SelectMultiList(IsMultiJob, e.where, e.grp.PRIORITY, e.grp.EQPID, e.realTime);
                }
            }
        }

        private void Job_ordering(ref List<pepschedule> Jobs, int I_type_remakcount, ref int remakecount, CallCmdProcArgs1 e)
        {
            // Worktype이 O가 아니고 Grouping된 설비가 1대면
            if (Jobs[0].WORKTYPE != "O" && e.grp_count == 1)
            {
                // Jobs 순서 Reverse
                Jobs.Reverse();
                // Remake된 Job의 수가 1개 초과 일 때
                if (I_type_remakcount > 1)
                {
                    // I 작업의 TrayOver로 인한 Remake 시
                    // Jobs를 Reverse 하게되면 맨 처음 Job이 Tray 수가 적은 Job이 오게된다.
                    // Tray 수가 적은 Job이 우선 진행하게되면 Tack Time과 Robot의 설비 앞 대기 시간이 길어져
                    // Tray수가 적은 Job과 Tray가 10개인 Job의 위치를 바꾸어준다.                

                    // Remake된 Job 수 만큼 배열 생성
                    pepschedule[] trans_jobs = new pepschedule[I_type_remakcount];

                    // Remake된 Job을 저장
                    for (int i = 0; i < I_type_remakcount; i++)
                    {
                        trans_jobs[i] = Jobs[i];
                    }

                    // Remake된 Job을 반대로 저장
                    for (int i = 0; i < I_type_remakcount; i++)
                    {
                        Jobs[i] = trans_jobs[I_type_remakcount - (i + 1)];
                    }
                }

            }
            if (Jobs[0].WORKTYPE == "OI" && e.grp_count > 1)
            {
                //Jobs = Jobs.OrderBy(p => p.BATCHID).ToList();
                // OI Job의 경우 Src와 Dst가 제각각으로 묶일수있으므로
                // remake된 Job의 수가 아닌 설비의 수를 저장하여 사용
                remakecount = e.grp_count;
            }

            // O 작업 일 때는 Job Data중 Order의 순서대로 진행하도록 Ordering 한다.
            // 20201106 - jm.choi
            // ProcessSrcJob 내부에서 Job을 ORDER 순으로 Ordering하고 진행하므로 제외
            //if (Jobs[0].WORKTYPE == "O" && e.grp_count == 1)
            //    Jobs = Jobs.OrderBy(p => p.ORDER).ToList();
        }
        private bool SrcMultiJobDeleteCheck_MI(ref List<pepschedule> Jobs, CallCmdProcArgs1 e, ref int I_type_remakeJobcount, ref int I_type_foreachcount, ref int I_type_remakcount)
        {
            MultiSrcFinishArgs arglist = new MultiSrcFinishArgs();

            // I 작업의 Remake된 Job의 수가 1을 초과하면
            if (I_type_remakeJobcount > 1)
                // I_type_foreachcount 순서의 Job을 저장
                arglist.pep = Jobs[I_type_foreachcount];
            // I 작업의 Remake된 Job의 수가 1이면
            else
                // 맨위의 Job을 저장
                arglist.pep = Jobs[0];
            // Robot 정보 저장
            arglist.vec = e.vec;
            // Remake로 진행된 Job에 저장된 Data를 원본 Job에 저장
            OnMultiSrcFinish(arglist, Jobs, I_type_remakcount);

            I_type_remakcount--;
            I_type_foreachcount++;
            // Remake된 Job만큼 반복하여 0이 되었으면
            if (I_type_remakcount == 0)
            {
                // Jobs의 개수만큼 반복
                for (int j = 0; j < Jobs.Count(); j++)
                {
                    // Job의 BatchID가 MSRC를 포함하면
                    if (Jobs[I_type_remakeJobcount - 1].BATCHID.Contains("MSRC"))
                    {
                        // DB에서 Job을 History로 복사하고
                        Db.CopyCmdToHistory(Jobs[I_type_remakeJobcount - 1]);
                        // Job을 DB에서 삭제
                        Db.Delete(Jobs[I_type_remakeJobcount - 1]);
                        // Jobs에서 해당 Job을 삭제
                        Jobs.Remove(Jobs[I_type_remakeJobcount - 1]);

                        I_type_remakeJobcount--;
                        // Remake된 Job만큼 반복하여 0이 되면 break
                        if (I_type_remakeJobcount == 0)
                            break;
                    }
                }
                return true;
            }
            return false;
        }
        private void SrcMultiJobDeleteCheck_MOI_MEO_MTO(ref List<pepschedule> Jobs, CallCmdProcArgs1 e)
        {
            MultiSrcFinishArgs arglist = new MultiSrcFinishArgs();
            arglist.pep = Jobs[0];
            arglist.vec = e.vec;
            // Remake로 진행된 Job에 저장된 Data를 원본 Job에 저장
            OnMultiSrcFinish(arglist, Jobs);
        }
        private bool SrcMultiJobDeleteCheck_MOI(ref List<pepschedule> Jobs, CallCmdProcArgs1 e, pepschedule nowJob, ref int remakeJobcount, ref int foreachcount, ref int remakecount)
        {
            // OI Job 중 Multi 로 묶인 Job이 아닐 경우 return  
            // Multi로 묶인 Job이 하나라도 있으면 break
            bool multichk = false;
            foreach (var x in Jobs)
            {
                if (x.BATCHID.Contains("MDST"))
                {
                    multichk = false;
                    break;
                }
                else
                    multichk = true;
            }


            if (multichk)
                return false;

            MultiSrcFinishArgs arglist = new MultiSrcFinishArgs();
            //arglist.pep = Jobs[foreachcount];
            arglist.pep = Jobs.Where(p => p.BATCHID == nowJob.BATCHID).Single();
            arglist.vec = e.vec;
            // Remake로 진행된 Job에 저장된 Data를 원본 Job에 저장
            OnMultiSrcFinish(arglist, Jobs);
            remakecount--;
            foreachcount++;

            // Remake된 Job의 수만큼 반복하여 0이 되었으면
            if (remakecount == 0)
            {
                var mdstjob = Jobs.Where(p => p.BATCHID.Contains("MDST")).ToList();
                
                foreach (var val in mdstjob)
                {
                    // DB에서 Job을 History로 복사
                    Db.CopyCmdToHistory(Jobs.Where(p => p.BATCHID == val.BATCHID).Single());
                    // Job을 DB에서 삭제
                    Db.Delete(Jobs.Where(p => p.BATCHID == val.BATCHID).Single());
                    // Jobs에서 해당 Job 삭제
                    Jobs.Remove(Jobs.Where(p => p.BATCHID == val.BATCHID).Single());
                }
                remakeJobcount = 0;

                //// Jobs의 수만큼 반복
                //for (int j = 0; j < Jobs.Count(); j++)
                //{
                //    // Job의 BatchID가 MDST를 포함하고 있으면
                //    if (Jobs[remakeJobcount - 1].BATCHID.Contains("MDST"))
                //    {
                //        // DB에서 Job을 History로 복사
                //        Db.CopyCmdToHistory(Jobs[remakeJobcount - 1]);
                //        // Job을 DB에서 삭제
                //        Db.Delete(Jobs[remakeJobcount - 1]);
                //        // Jobs에서 해당 Job 삭제
                //        Jobs.Remove(Jobs[remakeJobcount - 1]);
                //        remakeJobcount--;
                //        // Remake된 Jobd 수만큼 반복하여 0이 되었으면 break
                //        if (remakeJobcount == 0)
                //            break;
                //    }
                //}
                return true;
            }
            return false;
        }
        #endregion
        // CallCmdProcedure Method End

        // OnMultiSrcFinish Method Start
#region OnMultiSrcFinish
        private void multiJob_Delete(ref List<pepschedule> Jobs, int I_type_remakcount)
        {
            if (Jobs != null)
            {
                for (int i = 0; i < Jobs.Count(); i++)
                {
                    if (Jobs[i].WORKTYPE != "O" && Jobs[i].BATCHID.Contains("MSRC") && I_type_remakcount == 0)
                    {
                        Db.CopyCmdToHistory(Jobs[i]);
                        Db.Delete(Jobs[i]);
                        Jobs.Remove(Jobs[i]);
                    }
                }
            }
        }
        private string[] stack_tray_check(List<pepschedule> Jobs)
        {
            string[] job_tray_num = new string[Jobs.Count()];
            for (int i = 0; i < Jobs.Count(); i++)
            {
                job_tray_num[i] = Jobs[i].TRAYID;
            }
            return job_tray_num;
        }
        private string batchJob_tray_data(string[] _virPep, string[] trayid, string[] word)
        {
            string bufslot = string.Empty;
            for (int i = 0; i < trayid.Count(); i++)
            {
                if (i != 0 && bufslot != null && bufslot != "")
                    bufslot += ",";

                for (int j = 0; j < _virPep.Length; j++)
                {
                    if (trayid[i] == _virPep[j])
                    {
                        bufslot += word[(j * 2)] + ",";
                        bufslot += word[(j * 2) + 1];
                        break;
                    }
                }
            }
            return bufslot;
        }
        private string batchJob_stack_data(string[] _virPep, string[] trayid, string[] word, string[] job_tray_num)
        {
            string bufslot = string.Empty;
            int portarray = 0;
            for (int i = 0; i < trayid.Count(); i++)
            {
                if (bufslot.Length > 0)
                    break;

                for (int j = 0; j < _virPep.Length; j++)
                {
                    if (trayid[i] == _virPep[j])
                    {
                        for (int k = 0; k < job_tray_num.Count(); k++)
                        {
                            if (job_tray_num[k].Contains(trayid[i]))
                            {
                                portarray = k;
                                break;
                            }
                        }
                        bufslot += word[(portarray * 2)] + ",";
                        bufslot += word[(portarray * 2) + 1];
                        break;
                    }
                }
            }
            return bufslot;
        }
        private int tray_empty_check(pepschedule v)
        {
            int result = 0;
            for (int i = 0; i < v.C_bufSlot.Split(',').Count(); i++)
            {
                if (v.C_bufSlot.Split(',')[i] == "")
                {
                    result = i;
                    break;
                }
            }
            return result;
        }
        private string batchJob_tray_empty_select(pepschedule v, string[] c_bufslot, int emptyslotchk)
        {
            string bufslot = string.Empty;
            for (int i = 0; i < v.TRAYID.Split(',').Count(); i++)
            {
                for (int j = 0; j < _virtualPep.TRAYID.Split(',').Length; j++)
                {
                    if (v.TRAYID.Split(',')[i] == _virtualPep.TRAYID.Split(',')[j])
                    {
                        c_bufslot[emptyslotchk] += _virtualPep.C_bufSlot.Split(',')[(j * 2)] + ",";
                        c_bufslot[emptyslotchk] += _virtualPep.C_bufSlot.Split(',')[(j * 2) + 1];
                        emptyslotchk++;
                        break;
                    }
                }
            }

            for (int i = 0; i < c_bufslot.Count(); i++)
            {
                if (i != 0)
                    bufslot += ",";
                bufslot += c_bufslot[i];
            }
            return bufslot;
        }
        #endregion
        // OnMultiSrcFinish Method End

        // ProcessSrcJob Method Start
#region ProcessSrcJob
        private bool SRCJobwhereSRCProcces(ref List<pepschedule> Jobs, List<MulGrp> mulgrplist, CallCmdProcArgs1 e, ref int remakecount, ref int I_type_remakcount)
        {
            if (Jobs[0].WORKTYPE == "I")
                SortMultiJobListToEqp(ref mulgrplist, Jobs[0].S_EQPID);
            else
                SortMultiJobListToVehicle(ref mulgrplist, e.vec.ID);
            if (Jobs[0].WORKTYPE == "OI")
                e.grp_count = mulgrplist.Count();
            int job_count = 0;
            List<pepschedule> tempJobs = new List<pepschedule>();
            foreach (var w in mulgrplist)
            {
                if (Jobs[0].WORKTYPE == "OI")
                {
                    if (!SRCremakejob_SRC_OI(Jobs, ref tempJobs, e, Jobs[0].MULTIID, ref remakecount, w, job_count))
                        return false;
                    job_count += w.COUNT;
                }
                else if (Jobs[0].WORKTYPE != "EI" && Jobs[0].WORKTYPE != "TI")
                {
                    if (!SRCremakejob_SRC_other(ref Jobs, ref tempJobs, e, Jobs[0].MULTIID, ref remakecount, ref I_type_remakcount, true, w))
                        return false;
                }
            }

            if (tempJobs.Count() != 0)
            {
                Jobs.Clear();
                foreach (var z in tempJobs)
                {
                    Jobs.Add(z);
                }
            }

            if (Jobs[0].WORKTYPE == "EI" || Jobs[0].WORKTYPE == "TI")
            {
                if (!SRCremakejob_SRC_EI_TI(ref Jobs, e, Jobs[0].MULTIID, ref remakecount, ref I_type_remakcount))
                    return false;
            }
            else //if (Jobs[0].WORKTYPE != "OI")
            {
                if (!SRCremakejob_SRC_other(ref Jobs, ref tempJobs, e, Jobs[0].MULTIID, ref remakecount, ref I_type_remakcount, false))
                    return false;
            }
            return true;
        }
        //private bool SRCJobwhereSRCProcces(ref List<pepschedule> Jobs, List<MulGrp> mulgrplist, CallCmdProcArgs1 e, ref int remakecount, ref int I_type_remakcount)
        //{
        //    SortMultiJobList1(ref mulgrplist, Jobs[0].S_EQPID);

        //    List<pepschedule> tempJobs = new List<pepschedule>();
        //    foreach (var w in mulgrplist)
        //    {
        //        if (Jobs[0].WORKTYPE == "OI")
        //        {
        //            if (!SRCremakejob_SRC_OI(Jobs, ref tempJobs, e, Jobs[0].MULTIID, ref remakecount, w))
        //                return false;
        //        }
        //        else if (Jobs[0].WORKTYPE != "EI" && Jobs[0].WORKTYPE != "TI")
        //        {
        //            if (!SRCremakejob_SRC_other(ref Jobs, ref tempJobs, e, Jobs[0].MULTIID, ref remakecount, ref I_type_remakcount, true, w))
        //                return false;
        //        }
        //    }

        //    if (tempJobs.Count() != 0)
        //    {
        //        Jobs.Clear();
        //        foreach (var z in tempJobs)
        //        {
        //            Jobs.Add(z);
        //        }
        //    }

        //    if (Jobs[0].WORKTYPE == "EI" || Jobs[0].WORKTYPE == "TI")
        //    {
        //        if (!SRCremakejob_SRC_EI_TI(ref Jobs, e, Jobs[0].MULTIID, ref remakecount, ref I_type_remakcount))
        //            return false;
        //    }
        //    else //if (Jobs[0].WORKTYPE != "OI")
        //    {
        //        if (!SRCremakejob_SRC_other(ref Jobs, ref tempJobs, e, Jobs[0].MULTIID, ref remakecount, ref I_type_remakcount, false))
        //            return false;
        //    }
        //    return true;
        //}

        private bool SRCJobwhereDSTProcces(ref List<pepschedule> Jobs, List<MulGrp> mulgrplist, CallCmdProcArgs1 e, ref int remakecount, ref int I_type_remakcount)
        {
            SortMultiJobListToVehicle(ref mulgrplist, e.vec.ID);

            List<pepschedule> tempJobs = new List<pepschedule>();
            if (Jobs[0].WORKTYPE != "EO" && Jobs[0].WORKTYPE != "TO")
            {
                foreach (var w in mulgrplist)
                {

                    SRCremakejob_DST_other(ref Jobs, ref tempJobs, e, Jobs[0].MULTIID, ref remakecount, w);

                }
                Jobs.Clear();
                foreach (var z in tempJobs)
                {
                    Jobs.Add(z);
                }
            }
            // Job의 Worktype이 EO 또는 TO 일 경우
            if (Jobs[0].WORKTYPE == "EO" || Jobs[0].WORKTYPE == "TO")
            {
                SRCremakejob_DST_EO_TO(ref Jobs, e, Jobs[0].MULTIID, ref remakecount);
            }
            return true;
        }

        private bool SRCremakejob_SRC_OI(List<pepschedule> Jobs, ref List<pepschedule> tempJobs, CallCmdProcArgs1 e, string multiId, ref int mulgrplist_count, MulGrp w, int job_count)
        {
            List<pepschedule> jobs = null;

            if (e.grp_count > 1)
                jobs = FindSubMultiJobList(Jobs, w, eMultiJobWhere.SRC, e.executeTime);
            else
                jobs = FindSubMultiJobList(Jobs, w, eMultiJobWhere.DST, e.executeTime);

            if (w.COUNT > 1)
            {
                if (e.grp_count > 1)
                {
                    foreach (var v in jobs)
                    {
                        tempJobs.Add(v);
                    }

                    if (!RemakeMultiJobs(ref jobs, (int)eMultiJobWhere.DST, ref mulgrplist_count, false))
                        return false;
                }
                else
                {
                    if (!RemakeMultiJobs(ref jobs, (int)eMultiJobWhere.DST, ref mulgrplist_count, true))
                        return false;
                }
                mulgrplist_count++;
            }

            foreach (var v in jobs)
            {
                if (v.BATCHID.Contains("MDST"))
                {
                    int index = job_count == 0 ? 0 : job_count - 1;
                    tempJobs.Insert(job_count, v);
                }
                else
                    tempJobs.Add(v);
            }

            return true;
        }

        private bool SRCremakejob_SRC_EI_TI(ref List<pepschedule> Jobs, CallCmdProcArgs1 e, string multiId, ref int mulgrplist_count, ref int I_type_remakcount, bool isSingle = false)
        {
            List<pepschedule> itemlist = Jobs;

            int tray_over = Jobs[0].TRAYID.Split(',').Count();

            if (tray_over > 10)
            {
                if (!RemakeMultiJobs(ref itemlist, (int)eMultiJobWhere.SRC, ref mulgrplist_count, isSingle, true))
                    return false;

                for (int i = 0; i < itemlist.Count(); i++)
                {
                    itemlist[i].C_isChecked = 1;
                }
                if (isSingle)
                    Jobs.Clear();
                foreach (var z in itemlist)
                {
                    Jobs.Add(z);
                }
                I_type_remakcount = itemlist.Count();
            }


            return true;
        }

        private bool SRCremakejob_SRC_other(ref List<pepschedule> Jobs, ref List<pepschedule> tempJobs, CallCmdProcArgs1 e, string multiId, ref int mulgrplist_count,
            ref int I_type_remakcount, bool foreach_chk = true, MulGrp w = null)
        {
            if (foreach_chk)
            {
                List<pepschedule> jobs = null;

                jobs = FindSubMultiJobList(Jobs, w, eMultiJobWhere.DST, e.executeTime);

                if (w.COUNT > 1)
                {
                    if (!RemakeMultiJobs(ref jobs, (int)eMultiJobWhere.DST, ref mulgrplist_count, true))
                        return false;

                    mulgrplist_count++;
                }

                foreach (var z in jobs)
                {
                    tempJobs.Add(z);
                }
            }
            else
            {
                int i_tray_over = Jobs[0].TRAYID.Split(',').Count();
                List<pepschedule> itemlist = Jobs;
                if (e.grp_count == 1 && Jobs.Count() > 1)
                {
                    if (!RemakeMultiJobs(ref itemlist, (int)eMultiJobWhere.SRC, ref mulgrplist_count, false))
                        return false;

                    for (int i = 0; i < itemlist.Count(); i++)
                    {
                        itemlist[i].C_isChecked = 1;
                    }
                    foreach (var z in itemlist)
                    {
                        Jobs.Add(z);
                    }
                    I_type_remakcount = itemlist.Count();
                }
                else if (Jobs[0].WORKTYPE == "I" && i_tray_over > 10)
                {
                    if (!RemakeMultiJobs(ref itemlist, (int)eMultiJobWhere.SRC, ref mulgrplist_count, false, true))
                        return false;

                    for (int i = 0; i < itemlist.Count(); i++)
                    {
                        itemlist[i].C_isChecked = 1;
                    }
                    foreach (var z in itemlist)
                    {
                        Jobs.Add(z);
                    }
                    I_type_remakcount = itemlist.Count();
                }
            }

            return true;
        }

        //private bool SRCremakejob_SRC_other(ref List<pepschedule> Jobs, ref List<pepschedule> tempJobs, CallCmdProcArgs1 e, string multiId, ref int mulgrplist_count,
        //    ref int I_type_remakcount, bool foreach_chk = true, MulGrp w = null)
        //{
        //    if (foreach_chk)
        //    {
        //        List<pepschedule> jobs = null;

        //        jobs = FindSubMultiJobList(Jobs, w, eMultiJobWhere.DST, e.executeTime);

        //        if (w.COUNT > 1)
        //        {
        //            if (!RemakeMultiJobs(ref jobs, (int)eMultiJobWhere.DST, ref mulgrplist_count, true))
        //                return false;

        //            mulgrplist_count++;
        //        }

        //        foreach (var z in jobs)
        //        {
        //            tempJobs.Add(z);
        //        }
        //    }
        //    else
        //    {
        //        int i_tray_over = Jobs[0].TRAYID.Split(',').Count();
        //        List<pepschedule> itemlist = Jobs;
        //        if (e.grp_count == 1 && Jobs.Count() > 1)
        //        {
        //            if (!RemakeMultiJobs(ref itemlist, (int)eMultiJobWhere.SRC, ref mulgrplist_count, false))
        //                return false;

        //            for (int i = 0; i < itemlist.Count(); i++)
        //            {
        //                itemlist[i].C_isChecked = 1;
        //            }
        //            foreach (var z in itemlist)
        //            {
        //                Jobs.Add(z);
        //            }
        //            I_type_remakcount = itemlist.Count();
        //        }
        //        else if (Jobs[0].WORKTYPE == "I" && i_tray_over > 10)
        //        {
        //            if (!RemakeMultiJobs(ref itemlist, (int)eMultiJobWhere.SRC, ref mulgrplist_count, false, true))
        //                return false;

        //            for (int i = 0; i < itemlist.Count(); i++)
        //            {
        //                itemlist[i].C_isChecked = 1;
        //            }
        //            foreach (var z in itemlist)
        //            {
        //                Jobs.Add(z);
        //            }
        //            I_type_remakcount = itemlist.Count();
        //        }
        //    }

        //    return true;
        //}

        private bool SRCremakejob_DST_EO_TO(ref List<pepschedule> Jobs, CallCmdProcArgs1 e, string multiId, ref int mulgrplist_count)
        {
            List<pepschedule> itemlist = Jobs;
            unit unit_chk = Db.Units.Where(p => p.GOALNAME == itemlist[0].S_EQPID).SingleOrDefault();

            if (Jobs.Count() > 1 && unit_chk.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                if (!RemakeMultiJobs(ref itemlist, (int)eMultiJobWhere.SRC, ref mulgrplist_count, false))
                    return false;

                for (int i = 0; i < itemlist.Count(); i++)
                {
                    itemlist[i].C_isChecked = 1;
                }
                foreach (var z in itemlist)
                {
                    Jobs.Add(z);
                }
            }


            return true;
        }

        private bool SRCremakejob_DST_other(ref List<pepschedule> Jobs, ref List<pepschedule> tempJobs, CallCmdProcArgs1 e, string multiId, ref int mulgrplist_count, MulGrp w = null)
        {
            List<pepschedule> jobs = null;

            jobs = FindSubMultiJobList(Jobs, w, eMultiJobWhere.SRC, e.executeTime);

            if (w.COUNT > 1)
            {
                if (!RemakeMultiJobs(ref jobs, (int)eMultiJobWhere.SRC, ref mulgrplist_count, true))
                    return false;

                mulgrplist_count++;
            }

            foreach (var z in jobs)
            {
                tempJobs.Add(z);
            }

            return true;
        }
        #endregion
        // ProcessSrcJob Method End

        // ProcessDstJob Method Start
#region ProcessDstJob
        private bool DSTJobwhereSRCProcces(ref List<pepschedule> Jobs, List<MulGrp> mulgrplist, CallCmdProcArgs1 e, ref int mulgrplist_count)
        {
            List<pepschedule> itemlist = Jobs;
            if (Jobs.Count() > 1)
            {
                if (Jobs[0].WORKTYPE == "OI")
                {
                    DSTremakejob_DST_OI(ref Jobs, e, ref mulgrplist_count, mulgrplist);
                }
                else if (Jobs[0].WORKTYPE != "EO" && Jobs[0].WORKTYPE != "TO")
                {
                    DSTremakejob_DST_other(ref Jobs, e, ref mulgrplist_count);
                }
            }
            else
            {
                DSTremakejob_DST_O(ref Jobs, e, ref mulgrplist_count);
            }
            return true;
        }
        private bool DSTJobwhereDSTProcces(ref List<pepschedule> Jobs, List<MulGrp> mulgrplist, CallCmdProcArgs1 e, ref int mulgrplist_count)
        {
            List<MulGrp> itemlist = FindSubMultiJob(Jobs, eMultiJobWhere.DST);

            SortMultiJobListToVehicle(ref itemlist, e.vec.ID);
            List<pepschedule> tempJobs = new List<pepschedule>();
            foreach (var w in itemlist)
            {
                Thread.Sleep(10);
                List<pepschedule> jobs = FindSubMultiJobList(Jobs, w, eMultiJobWhere.DST, e.executeTime);
                foreach (var v in jobs)
                {
                    tempJobs.Add(v);
                }
            }

            unit unit_chk = Db.Units.Where(p => p.GOALNAME == tempJobs[0].T_EQPID).SingleOrDefault();
            if (Jobs.Count() > 1 && (Jobs[0].WORKTYPE == "EI" || Jobs[0].WORKTYPE == "TI") && e.grp_count == 1 && unit_chk.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                Jobs.Clear();
                foreach (var z in tempJobs)
                {
                    Jobs.Add(z);
                }

                tempJobs.Reverse();
                if (!RemakeMultiJobs(ref tempJobs, (int)eMultiJobWhere.SRC, ref mulgrplist_count, false))
                    return false;
                for (int i = 0; i < tempJobs.Count(); i++)
                {
                    tempJobs[i].C_isChecked = 1;
                }
                Jobs.Clear();
                foreach (var z in tempJobs)
                {
                    Jobs.Add(z);
                }

            }
            return true;
        }

        private bool DSTremakejob_DST_OI(ref List<pepschedule> Jobs, CallCmdProcArgs1 e, ref int mulgrplist_count, List<MulGrp> mulgrplist = null)
        {
            List<pepschedule> tempJobs = new List<pepschedule>();
            if (e.grp_count > 1)
            {
                // 현시점 x,y 에서 각 장비들의 위치를 따져서 path를 구하고 eqpGrp 를 ordering 한다
                SortMultiJobListToEqp(ref mulgrplist, e.vec.C_lastArrivedUnit);

                string multiId = string.Format("M{0}_{1}", Jobs[0].WORKTYPE.ToString(), DateTime.Now.ToString("ddHHmmssfff"));

                foreach (var w in mulgrplist)
                {
                    List<pepschedule> jobs = null;
                    jobs = FindSubMultiJobList(Jobs, w, eMultiJobWhere.DST, e.executeTime);

                    if (w.COUNT > 1)
                    {
                        if (!RemakeMultiJobs(ref jobs, (int)eMultiJobWhere.DST, ref mulgrplist_count, true))
                            return false;

                        mulgrplist_count++;
                    }

                    foreach (var v in jobs)
                    {
                        tempJobs.Add(v);
                    }
                }

                Jobs.Clear();
                foreach (var z in tempJobs)
                {
                    Jobs.Add(z);
                }
            }
            else
            {
                tempJobs = Jobs;
                if (!RemakeMultiJobs(ref tempJobs, (int)eMultiJobWhere.DST, ref mulgrplist_count, true))
                    return false;

                Jobs.Clear();
                foreach (var z in tempJobs)
                {
                    Jobs.Add(z);
                }
            }

            return true;
        }

        private bool DSTremakejob_DST_O(ref List<pepschedule> Jobs, CallCmdProcArgs1 e, ref int mulgrplist_count)
        {
            List<pepschedule> tempJobs = new List<pepschedule>();
            tempJobs = Jobs;
            int tray_count = Jobs[0].TRAYID.Split(',').Count();
            if (tray_count > 10)
            {
                if (!RemakeMultiJobs(ref tempJobs, (int)eMultiJobWhere.DST, ref mulgrplist_count, true, true))
                    return false;

                Jobs.Clear();
                foreach (var z in tempJobs)
                {
                    Jobs.Add(z);
                }
            }

            return true;
        }

        private bool DSTremakejob_DST_other(ref List<pepschedule> Jobs, CallCmdProcArgs1 e, ref int mulgrplist_count)
        {
            List<pepschedule> tempJobs = new List<pepschedule>();
            tempJobs = Jobs;
            if (!RemakeMultiJobs(ref tempJobs, (int)eMultiJobWhere.DST, ref mulgrplist_count, true))
                return false;

            Jobs.Clear();
            foreach (var z in tempJobs)
            {
                Jobs.Add(z);
            }

            return true;
        }
        #endregion
        // ProcessDstJob Method End

        // MakePortSlot Method Start
#region MakePortSlot
        private void portslot_error_msg(string eqp_portslot, unit units, string slot, string vecID)
        {
            if (string.IsNullOrEmpty(eqp_portslot))
            {
                Logger.Inst.Write(vecID, CmdLogType.All, $"{((EqpGoalType)units.goaltype).ToString()}. Invalid reflow Slot = {slot}");
            }

        }
        #endregion
        // MakePortSlot Method End

        // makeportslot_stk/syswin/handler/reflow Method Start
#region makeportslot_stk/syswin/handler/reflow
        private string portslot_stk_check(string slot)
        {
            string tslot = string.Empty;
            if (slot.Contains("AUTO01"))
                tslot = "1,0";
            else if (slot.Contains("AUTO02"))
                tslot = "2,0";
            else if (slot.Contains("STACK01"))
                tslot = "3,0";
            else if (slot.Contains("STACK02"))
                tslot = "4,0";
            else
                tslot = string.Empty;

            return tslot;
        }
        private string eqp_portslot_value(int traycount, string tslot)
        {
            string eqp_portslot = string.Empty;
            for (int i = 0; i < traycount; i++)
            {
                if (i != 0)
                    eqp_portslot += ",";
                eqp_portslot += tslot;
            }
            return eqp_portslot;
        }
        private (int, int) makeportNo_slotNo(string words, unit unti)
        {
            byte[] ascPort = Encoding.ASCII.GetBytes(words.Substring(0, 1));// - 'A';
            byte[] ascBase = Encoding.ASCII.GetBytes("A");
            int portNo = -1;
            int slotNo = -1;
            if (unti.goaltype == (int)EqpGoalType.SYSWIN_OVEN)
            {
                (portNo, slotNo) = portslot_syswinoven_check(6, words, ascPort[0], ascBase[0]);
            }
            else if (unti.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t)
            {
                (portNo, slotNo) = portslot_syswinoven_check(7, words, ascPort[0], ascBase[0]);
            }
            else
            {
                portNo = ascPort[0] - ascBase[0] + 1;   // portNo 는 1 Base 다
                try
                {
                    int a = Convert.ToInt32(words.Substring(1, words.Length - 1));
                    int b = (int)unti.max_row;
                    slotNo = b - a;
                }
                catch (Exception ex)
                {
                    Logger.Inst.Write(VecId, CmdLogType.Rv, $"Exception. makeportslot_syswin. {ex.Message}\r\n{ex.StackTrace}");
                    return (-1, -1);
                }
            }
            return (portNo, slotNo);
        }
        private (int, int) portslot_syswinoven_check(int val, string words, byte ascPort, byte ascBase)
        {
            int portNo, slotNo = 0;
            if (Convert.ToInt32(words.Substring(1, words.Length - 1)) > val)
            {
                portNo = ascPort - ascBase + 4;
                slotNo = 11 - Convert.ToInt32(words.Substring(1, words.Length - 1));
            }
            else
            {
                portNo = ascPort - ascBase + 1;  
                slotNo = val - Convert.ToInt32(words.Substring(1, words.Length - 1));
            }
            return (portNo, slotNo);
        }
        private void make_eqpportslot(int n, ref string eqp_portslot, int portNo, int slotNo)
        {
            if (n != 0)
                eqp_portslot += ",";
            eqp_portslot += portNo.ToString();
            eqp_portslot += ",";
            eqp_portslot += slotNo.ToString();
        }
        private string portslot_handler_check(int goaltype, string slot)
        {
            string tslot = string.Empty;

            if (goaltype == (int)EqpGoalType.HANDLER)
            {
                if (slot.Contains("LOADER1"))
                    tslot = "1,0";
                else if (slot.Contains("FAIL"))
                    tslot = "2,0";
                else
                    tslot = string.Empty;
            }
            if (goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                string[] slot_split = slot.Split(',');

                foreach (var x in slot_split)
                {
                    if (x.Contains("GOOD"))
                        tslot += "3,0";
                    else if (x.Contains("FAIL"))
                        tslot += "4,0";
                    else
                        tslot += string.Format($"{x.Substring(6, 1)},0");
                }
            }
            return tslot;
        }
        #endregion
        // makeportslot_stk/syswin/handler/reflow Method End

        // MakeBuffSlot Method Start
#region MakeBuffSlot
        private string bufslot_data(ref string buf_portslot, IEnumerable<vehicle_part> vecParts)
        {
            int loop = 0;
            foreach (var part in vecParts)
            {
                if (loop != 0)
                    buf_portslot += ",";
                buf_portslot += part.portNo.ToString();
                buf_portslot += ",";
                buf_portslot += ((int)part.slotNo).ToString("D3");
                loop++;
            }
            return buf_portslot;
        }
        #endregion
        // MakeBuffSlot Method End

        // RemakeMultiJobs Method Start
#region RemakeMultiJobs
        private List<pepschedule> Job_remake(int where, ref int mulgrplist_count, bool tray_over, pepschedule pep, ref List<pepschedule> multijobs, bool bDelete, JobRemakeWords jobrewords)
        {
            int order_num = (int)multijobs[0].ORDER;
            List<pepschedule> newmultijobs = new List<pepschedule>();
            int buflimit, for_count = 0;

            StringDataSum(ref jobrewords, multijobs, bDelete);

            if (jobType != "DST")
                jobrewords.bufslot = pep.C_bufSlot;

            unit unt = Db.Units.Where(p => p.ID == ((where == (int)eMultiJobWhere.SRC) ? pep.S_EQPID : pep.T_EQPID)).Single();

            (buflimit, for_count) = lotation_check(where, pep, jobrewords.wordTrayIds);

            for (int i = 0; i < for_count; i++)
            {
                jobrewords.trayIds = string.Empty;
                jobrewords.lotNos = string.Empty;
                jobrewords.Qtys = string.Empty;
                jobrewords.teqpid = string.Empty;

                mulgrplist_count_check(ref mulgrplist_count, i, where, tray_over, pep);

                remakejob_wording(i, buflimit, where, pep, ref jobrewords, mulgrplist_count);

                if ((where == (int)eMultiJobWhere.SRC && pep.WORKTYPE == "I") || (where == (int)eMultiJobWhere.DST && pep.WORKTYPE == "O" && tray_over == false) ||
                        (where == (int)eMultiJobWhere.SRC && pep.WORKTYPE == "OI"))
                {
                    rewordStepID_I_OandtrayoverX(ref jobrewords, multijobs);                    
                }

                if ((where == (int)eMultiJobWhere.DST && pep.WORKTYPE == "O") || (where == (int)eMultiJobWhere.SRC
                    && (pep.WORKTYPE == "EO" || pep.WORKTYPE == "TO" || pep.WORKTYPE == "EI" || pep.WORKTYPE == "TI")))
                {
                    jobrewords.teqpid = pep.T_EQPID;
                    if (where == (int)eMultiJobWhere.SRC
                    && (pep.WORKTYPE == "EO" || pep.WORKTYPE == "TO" || pep.WORKTYPE == "EI" || pep.WORKTYPE == "TI") && tray_over)
                    {
                        jobrewords.stepids = pep.STEPID;
                        jobrewords.sstepids = pep.S_STEPID;
                        jobrewords.tstepids = pep.T_STEPID;
                        if (i == 0)
                            jobrewords.tslot = "AUTO01";
                        else if (i == 1)
                            jobrewords.tslot = "AUTO02";
                    }
                }

                if (tray_over)
                {
                    if (jobType == "DST")
                    {
                        DSTJobrewordStepID(ref jobrewords, multijobs);                        
                    }
                }
                remakePeps(pep, jobrewords, order_num, ref newmultijobs);   
                
                
            }

            if (bDelete)
            {
                foreach (var v in multijobs)
                {
                    v.MULTIID = pep.MULTIID;
                    if (jobType == "SRC")
                        v.C_srcArrivingTime = Vsp.DtCur;
                    else
                        v.C_dstArrivingTime = Vsp.DtCur;
                    Db.CopyCmdToHistory(v);
                    Db.Delete(v);
                    Logger.Inst.Write(VecId, CmdLogType.Rv, $"RemakeMultiJobs. OnDeleteCmd({pep.MULTIID})");
                }
            }
            Thread.Sleep(1000);

            return newmultijobs;
        }
        private void StringDataSum(ref JobRemakeWords jobrewords, List<pepschedule> multijobs, bool bDelete)
        {
            string totalTrayIds = string.Empty;
            string totalLotNos = string.Empty;
            string totalQtys = string.Empty;
            string totalSSlot = string.Empty;
            string totalTSlot = string.Empty;
            string totalStepIds = string.Empty;
            string totalSStepIds = string.Empty;
            string totalTStepIds = string.Empty;
            string totalbuffslot = string.Empty;
            string totalTEQPID = string.Empty;

            foreach (var v in multijobs)
            {
                StringAdd(ref totalTrayIds, v.TRAYID, ',');
                StringAdd(ref totalLotNos, v.LOT_NO, ',');
                StringAdd(ref totalQtys, v.QTY, ',');
                StringAdd(ref totalStepIds, v.STEPID, ',');
                StringAdd(ref totalSStepIds, v.S_STEPID, ',');
                StringAdd(ref totalTStepIds, v.T_STEPID, ',');

                if (bDelete == false)
                {
                    StringAdd(ref totalTEQPID, v.T_EQPID, ',');
                }

                jobrewords.chkunit_src = Db.Units.Where(p => p.GOALNAME == ((pepschedule)v).S_EQPID).Single();
                if (jobrewords.chkunit_src != null && (jobrewords.chkunit_src.goaltype == (int)EqpGoalType.SYSWIN || jobrewords.chkunit_src.goaltype == (int)EqpGoalType.SYSWIN_OVEN
                     || jobrewords.chkunit_src.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || jobrewords.chkunit_src.goaltype == (int)EqpGoalType.BUFFER_STK)
                     || jobrewords.chkunit_src.goaltype == (int)EqpGoalType.HANDLER_STACK)
				{
                    StringAdd(ref totalSSlot, v.S_SLOT, ',');
                }

                jobrewords.chkunit_dst = Db.Units.Where(p => p.GOALNAME == ((pepschedule)v).T_EQPID).Single();
                if (jobrewords.chkunit_dst != null && (jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.SYSWIN || jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.SYSWIN_OVEN
                     || jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.BUFFER_STK) 
                     || jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.HANDLER_STACK)
                {
                    StringAdd(ref totalTSlot, v.T_SLOT, ',');
                }

                if (jobType == "DST")
                {
                    StringAdd(ref totalbuffslot, v.C_bufSlot, ',');
                }
            }

            if (totalSSlot == null || totalSSlot == "")
                totalSSlot = multijobs[0].S_SLOT;

            if (totalTSlot == null || totalTSlot == "")
                totalTSlot = multijobs[0].T_SLOT;

            if (totalTEQPID == null || totalTEQPID == "")
                totalTEQPID = multijobs[0].T_EQPID;

            jobrewords.wordTrayIds = totalTrayIds.Split(',');
            jobrewords.wordLotNos = totalLotNos.Split(',');
            jobrewords.wordQtys = totalQtys.Split(',');
            jobrewords.wordSSlot = totalSSlot.Split(',');
            jobrewords.wordTSlot = totalTSlot.Split(',');
            jobrewords.wordStepIds = totalStepIds.Split(',');
            jobrewords.wordSStepIds = totalSStepIds.Split(',');
            jobrewords.wordTStepIds = totalTStepIds.Split(',');
            jobrewords.wordbuffslot = totalbuffslot.Split(',');
            jobrewords.wordteqpid = totalTEQPID.Split(',');
        }
        private (int, int) lotation_check(int where, pepschedule pep, string[] wordTrayIds)
        {
            unit unt = Db.Units.Where(p => p.ID == ((where == (int)eMultiJobWhere.SRC) ? pep.S_EQPID : pep.T_EQPID)).Single();
            int buflimit = (unt.goaltype == 1) ? (int)unt.max_row : (int)unt.max_row * (int)unt.max_col;

            if (where == (int)eMultiJobWhere.SRC && (pep.WORKTYPE == "TI" || pep.WORKTYPE == "EI") && unt.goaltype == (int)EqpGoalType.STK)
            {
                unt = Db.Units.Where(p => p.ID == pep.T_EQPID).Single();
                if (unt.goaltype == (int)EqpGoalType.REFLOW)
                    buflimit = 10;
                else if (unt.goaltype == (int)EqpGoalType.HANDLER_STACK && jobType == "DST")
                    buflimit = 20;
            }
            else if (where == (int)eMultiJobWhere.SRC && (pep.WORKTYPE == "TO" || pep.WORKTYPE == "EO") && unt.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                if (jobType == "SRC")
                    buflimit = 20;
            }

            int for_count = wordTrayIds.Count() / buflimit;
            int count_chk = wordTrayIds.Count() % buflimit;
            if (count_chk != 0)
                for_count += 1;

            return (buflimit, for_count);
        }
        private void mulgrplist_count_check(ref int mulgrplist_count, int i, int where, bool tray_over, pepschedule pep)
        {
            if (where == (int)eMultiJobWhere.SRC && i != 0)
                mulgrplist_count++;
            if (tray_over && i != 0)
                mulgrplist_count++;
            if (where == (int)eMultiJobWhere.DST && pep.WORKTYPE == "O" && i != 0)
                mulgrplist_count++;
        }
        private void remakejob_wording(int i, int buflimit, int where, pepschedule pep, ref JobRemakeWords jobrewords, int mulgrplist_count)
        {
            for (int j = i * buflimit; ((j < buflimit + (i * buflimit)) && (j < jobrewords.wordTrayIds.Count())); j++)
            {
                if (j == i * buflimit)
                {
                    remakeFirstStep(j, ref jobrewords, where, pep, mulgrplist_count);
                }
                else
                {
                    remakeNextStep(j, ref jobrewords, where, pep, mulgrplist_count);
                }
            }
        }
        private void remakeFirstStep(int j, ref JobRemakeWords jobrewords, int where, pepschedule pep, int mulgrplist_count)
        {
            const char delimeter = ',';
            string submultiId = string.Empty;

            if (pep.MULTIID != null)
                submultiId = pep.MULTIID.Split('_')[1];
            else
                submultiId = DateTime.Now.ToString("ddHHmmssfff");

            if ((pep.WORKTYPE == "EI" || pep.WORKTYPE == "TI") && jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.REFLOW)
                jobrewords.submultiId = string.Format($"{pep.BATCHID}_{mulgrplist_count}");
            else
                jobrewords.submultiId = string.Format("M{0}_{1}_{2}", (where == (int)eMultiJobWhere.SRC) ? "SRC" : "DST", submultiId, mulgrplist_count);

            jobrewords.trayIds = (jobrewords.wordTrayIds.Count() > j) ? jobrewords.wordTrayIds[j] : "";
            jobrewords.lotNos = (jobrewords.wordLotNos.Count() > j) ? jobrewords.wordLotNos[j] : "";
            jobrewords.Qtys = (jobrewords.wordQtys.Count() > j) ? jobrewords.wordQtys[j] : "";

            if (jobrewords.chkunit_src.goaltype == (int)EqpGoalType.SYSWIN || jobrewords.chkunit_src.goaltype == (int)EqpGoalType.SYSWIN_OVEN
                 || jobrewords.chkunit_src.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || jobrewords.chkunit_src.goaltype == (int)EqpGoalType.BUFFER_STK
                 || jobrewords.chkunit_src.goaltype == (int)EqpGoalType.HANDLER_STACK)
                jobrewords.sslot = (jobrewords.wordSSlot.Count() > j) ? jobrewords.wordSSlot[j] : "";
            else
                jobrewords.sslot = (jobrewords.wordSSlot.Count() > 0) ? jobrewords.wordSSlot[0] : "";
            if (jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.SYSWIN || jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.SYSWIN_OVEN
                 || jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.BUFFER_STK 
                 || jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.HANDLER_STACK)
                jobrewords.tslot = (jobrewords.wordTSlot.Count() > j) ? jobrewords.wordTSlot[j] : "";
            else
                jobrewords.tslot = (jobrewords.wordTSlot.Count() > 0) ? jobrewords.wordTSlot[0] : "";
            jobrewords.stepids = (jobrewords.wordStepIds.Count() > j) ? jobrewords.wordStepIds[j] : "";
            jobrewords.sstepids = (jobrewords.wordSStepIds.Count() > j) ? jobrewords.wordSStepIds[j] : ""; 
            jobrewords.tstepids = (jobrewords.wordTStepIds.Count() > j) ? jobrewords.wordTStepIds[j] : ""; 
                                                                                                          
            if (where == (int)eMultiJobWhere.DST)
            {
                jobrewords.teqpid = (jobrewords.wordteqpid.Count() > j) ? jobrewords.wordteqpid[j] : ""; 
            }
            if (pep.WORKTYPE == "O")
            {
                jobrewords.teqpid = (jobrewords.wordteqpid.Count() > j) ? jobrewords.wordteqpid[j] : ""; 
            }

            if (jobType == "DST")
            {
                jobrewords.bufslot = (jobrewords.wordbuffslot.Count() > (j * 2)) ? jobrewords.wordbuffslot[(j * 2)] : "";
                jobrewords.bufslot += string.Format("{0}{1}", delimeter, (jobrewords.wordbuffslot.Count() > (j * 2) + 1) ? jobrewords.wordbuffslot[(j * 2) + 1] : "");
            }
        }

        private void remakeNextStep(int j, ref JobRemakeWords jobrewords, int where, pepschedule pep, int mulgrplist_count)
        {
            const char delimeter = ',';
            jobrewords.trayIds += string.Format("{0}{1}", delimeter, (jobrewords.wordTrayIds.Count() > j) ? jobrewords.wordTrayIds[j] : "");
            jobrewords.lotNos += string.Format("{0}{1}", delimeter, (jobrewords.wordLotNos.Count() > j) ? jobrewords.wordLotNos[j] : "");
            jobrewords.Qtys += string.Format("{0}{1}", delimeter, (jobrewords.wordQtys.Count() > j) ? jobrewords.wordQtys[j] : "");

            if (jobrewords.chkunit_src.goaltype == (int)EqpGoalType.SYSWIN || jobrewords.chkunit_src.goaltype == (int)EqpGoalType.SYSWIN_OVEN
                || jobrewords.chkunit_src.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || jobrewords.chkunit_src.goaltype == (int)EqpGoalType.BUFFER_STK)
                jobrewords.sslot += string.Format("{0}{1}", delimeter, (jobrewords.wordSSlot.Count() > j) ? jobrewords.wordSSlot[j] : "");
            else if (jobrewords.chkunit_src.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                if (jobrewords.wordSSlot.Count() > j && !jobrewords.sslot.Contains(jobrewords.wordSSlot[j]))
                {
                    jobrewords.sslot += ',';
                    jobrewords.sslot += jobrewords.wordSSlot[j];
                }
            }
            if (jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.SYSWIN || jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.SYSWIN_OVEN
                || jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.BUFFER_STK)
                jobrewords.tslot += string.Format("{0}{1}", delimeter, (jobrewords.wordTSlot.Count() > j) ? jobrewords.wordTSlot[j] : "");
            else if (jobrewords.chkunit_dst.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                if (jobrewords.wordTSlot.Count() > j && !jobrewords.tslot.Contains(jobrewords.wordTSlot[j]))
                {
                    jobrewords.tslot += ',';
                    jobrewords.tslot += jobrewords.wordTSlot[j];
                }
            }
            if (jobrewords.wordStepIds.Count() > j)
                jobrewords.stepids += string.Format("{0}{1}", delimeter, (jobrewords.wordStepIds.Count() > j) ? jobrewords.wordStepIds[j] : "");

            if (jobrewords.wordSStepIds.Count() > j)
                jobrewords.sstepids += string.Format("{0}{1}", delimeter, (jobrewords.wordSStepIds.Count() > j) ? jobrewords.wordSStepIds[j] : "");
            if (jobrewords.wordTStepIds.Count() > j)
                jobrewords.tstepids += string.Format("{0}{1}", delimeter, (jobrewords.wordTStepIds.Count() > j) ? jobrewords.wordTStepIds[j] : "");
           
            if (where == (int)eMultiJobWhere.DST)
            {
                if (jobrewords.wordteqpid.Count() > j)
                    jobrewords.teqpid += string.Format("{0}{1}", delimeter, (jobrewords.wordteqpid.Count() > j) ? jobrewords.wordteqpid[j] : "");
            }
            if (pep.WORKTYPE == "O")
            {
                if (jobrewords.wordteqpid.Count() > j)
                    jobrewords.teqpid += string.Format("{0}{1}", delimeter, (jobrewords.wordteqpid.Count() > j) ? jobrewords.wordteqpid[j] : "");
            }
            if (jobType == "DST")
            {
                if (pep.TRANSFERTYPE == "STACK")
                {
                    if (jobrewords.wordbuffslot.Count() > j * 2)
                    {
                        if (j != 0)
                            jobrewords.bufslot += ',';

                        jobrewords.bufslot += jobrewords.wordbuffslot[(j * 2)];
                        jobrewords.bufslot += ',';
                        jobrewords.bufslot += jobrewords.wordbuffslot[(j * 2) + 1];
                    }
                }
                else
                {
                    jobrewords.bufslot += string.Format("{0}{1}", delimeter, (jobrewords.wordbuffslot.Count() > (j * 2)) ? jobrewords.wordbuffslot[(j * 2)] : "");
                    jobrewords.bufslot += string.Format("{0}{1}", delimeter, (jobrewords.wordbuffslot.Count() > (j * 2) + 1) ? jobrewords.wordbuffslot[(j * 2) + 1] : "");
                }
            }
        }
        private void rewordStepID_I_OandtrayoverX(ref JobRemakeWords jobrewords, List<pepschedule> multijobs)
        {
            jobrewords.teqpid = string.Empty;
            jobrewords.stepids = string.Empty;
            jobrewords.sstepids = string.Empty;
            jobrewords.tstepids = string.Empty;
            string[] trayIds_count = jobrewords.trayIds.Split(',');
            bool teqpid_add = false;
            foreach (var l in multijobs)
            {
                string[] org_tray = l.TRAYID.Split(',');
                teqpid_add = false;
                foreach (var k in org_tray)
                {
                    foreach (var j in trayIds_count)
                    {
                        if (k == j)
                        {
                            if (jobrewords.teqpid != "")
                            {
                                jobrewords.teqpid += ",";
                                jobrewords.stepids += ",";
                                jobrewords.sstepids += ",";
                                jobrewords.tstepids += ",";
                            }
                            jobrewords.teqpid += l.T_EQPID;
                            jobrewords.stepids += l.STEPID;
                            jobrewords.sstepids += l.S_STEPID;
                            jobrewords.tstepids += l.T_STEPID;
                            teqpid_add = true;
                            break;
                        }
                    }
                    if (teqpid_add)
                        break;
                }
            }
        }
        private void DSTJobrewordStepID(ref JobRemakeWords jobrewords, List<pepschedule> multijobs)
        {
            jobrewords.stepids = string.Empty;
            jobrewords.sstepids = string.Empty;
            jobrewords.tstepids = string.Empty;
            string[] trayIds_count = jobrewords.trayIds.Split(',');
            bool stepids_add = false;

            foreach (var l in multijobs)
            {
                List<pepschedule_history> pepshis = Db.PepsHisto.Where(p => p.EXECUTE_TIME == l.EXECUTE_TIME && p.WORKTYPE == l.WORKTYPE).ToList();
                foreach (var u in pepshis)
                {
                    string[] org_tray = u.TRAYID.Split(',');
                    stepids_add = false;
                    foreach (var k in org_tray)
                    {
                        foreach (var j in trayIds_count)
                        {
                            if (k == j)
                            {
                                if (jobrewords.stepids != "")
                                {
                                    jobrewords.stepids += ",";
                                    jobrewords.sstepids += ",";
                                    jobrewords.tstepids += ",";
                                }
                                jobrewords.stepids += u.STEPID;
                                jobrewords.sstepids += u.S_STEPID;
                                jobrewords.tstepids += u.T_STEPID;
                                stepids_add = true;
                                break;
                            }
                        }
                        if (stepids_add)
                            break;
                    }
                }
            }
        }

        private void remakePeps(pepschedule pep, JobRemakeWords jobrewords, int order_num, ref List<pepschedule> newmultijobs)
        {
            pepschedule addjob = new pepschedule()
            {
                MULTIID = pep.MULTIID,
                BATCHID = jobrewords.submultiId,
                S_EQPID = pep.S_EQPID,
                S_PORT = pep.S_PORT,
                S_SLOT = jobrewords.sslot,
                T_EQPID = jobrewords.teqpid,
                T_PORT = pep.T_PORT,
                T_SLOT = jobrewords.tslot,
                TRAYID = jobrewords.trayIds,
                WORKTYPE = pep.WORKTYPE,
                TRANSFERTYPE = pep.TRANSFERTYPE,
                WINDOW_TIME = pep.WINDOW_TIME,
                EXECUTE_TIME = pep.EXECUTE_TIME,
                REAL_TIME = pep.REAL_TIME,
                STATUS = pep.STATUS,
                LOT_NO = jobrewords.lotNos,
                QTY = jobrewords.Qtys,
                STEPID = jobrewords.stepids,
                S_STEPID = jobrewords.sstepids,
                T_STEPID = jobrewords.tstepids,
                URGENCY = pep.URGENCY,
                FLOW_STATUS = pep.FLOW_STATUS,
                C_VEHICLEID = pep.C_VEHICLEID,
                C_bufSlot = jobrewords.bufslot,
                C_state = pep.C_state,
                C_srcAssignTime = pep.C_srcAssignTime,
                C_srcArrivingTime = pep.C_srcArrivingTime,
                C_srcStartTime = pep.C_srcStartTime,
                C_srcFinishTime = pep.C_srcFinishTime,
                C_dstAssignTime = pep.C_dstAssignTime,
                C_dstArrivingTime = pep.C_dstArrivingTime,
                C_dstStartTime = pep.C_dstStartTime,
                C_dstFinishTime = pep.C_dstFinishTime,
                C_isChecked = pep.C_isChecked,
                C_priority = pep.C_priority,
                DOWNTEMP = pep.DOWNTEMP,
                EVENT_DATE = pep.EVENT_DATE,
                ORDER = order_num
            };
            Db.Add(addjob);
            Logger.Inst.Write(VecId, CmdLogType.Rv, $"RemakeMultiJobs. AddCmd({pep.MULTIID})");

            newmultijobs.Add(addjob);
        }
#endregion
        // RemakeMultiJobs Method End

    }
}
