using MPlus.Controller;
using MPlus.Ref;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MPlus.Forms
{
    public partial class FormDetailInfo : Form
    {
        public event EventHandler<ChangePartEventArgs> OnChangePartInfo;
        public event EventHandler<buttonEventArgs> OnResume;
        public event EventHandler<buttonEventArgs> OnStop;
        public event EventHandler<ReconnectedEventArgs> OnReconnect;

        public string VecID = string.Empty;
        public bool vec_stop = false;

        public FormDetailInfo(vehicle[] vec, vehicle_part[] parts, bool isstop)
        {
            InitializeComponent();
            dataGridViewVec.DataSource = vec;
            dataGridViewPart.DataSource = parts;
            VecID = vec[0].ID;
            //vec_stop = isstop;

            if (isstop)
            {
                buttonStopMag.Text = "Auto";
                buttonStopMag.BackColor = Color.Turquoise;

                for (int i = 0; i < dataGridViewVec.ColumnCount; i++)
                {
                    if (i != 2)
                        dataGridViewVec.Columns[i].ReadOnly = true;
                }
            }
            else
            {
                buttonStopMag.Text = "Stop";
                buttonStopMag.BackColor = Color.Crimson;

                dataGridViewVec.ReadOnly = true;
            }
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            Close();
        }
        int selectedVecRow = 0;
        private void dataGridViewPart_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            selectedVecRow = e.RowIndex;
        }

        private void buttonInsertMag_Click(object sender, EventArgs e)
        {
            var cells = dataGridViewPart.SelectedCells;

            if (selectedVecRow > 0)
            {
                //Logger.Inst.Write(_VecID, CmdLogType.Db, $"Tray Insert : idx[{e.PartIndex}]/TrayID[{id}]/part[{parts[e.PartIndex].VEHICLEID}]");
            }
            else
            {
                int cellnum = Convert.ToInt32(dataGridViewPart.Rows[selectedVecRow].Cells[0].Value.ToString());
                if (cells != null)
                {
                    OnChangePartInfo?.Invoke(this, new ChangePartEventArgs() { DstStatus = VehiclePartStatus.FULL, PartIndex = cellnum });
                    dataGridViewPart.Refresh();
                }
            }
        }

        private void buttonDeleteMag_Click(object sender, EventArgs e)
        {
            var cells = dataGridViewPart.SelectedCells;

            if (selectedVecRow < 0)
            {
                //Logger.Inst.Write(_VecID, CmdLogType.Db, $"Tray Insert : idx[{e.PartIndex}]/TrayID[{id}]/part[{parts[e.PartIndex].VEHICLEID}]");
            }
            else
            {
                int cellnum = Convert.ToInt32(dataGridViewPart.Rows[selectedVecRow].Cells[0].Value.ToString());
                if (cells != null)
                {
                    OnChangePartInfo?.Invoke(this, new ChangePartEventArgs() { DstStatus = VehiclePartStatus.EMPTY, PartIndex = cellnum });
                    dataGridViewPart.Refresh();
                }
            }
        }

        private void buttonResumeMag_Click(object sender, EventArgs e)
        {
            OnResume?.Invoke(this, new buttonEventArgs() { vecID = VecID });
        }

        public void ResumeEnable(bool check)
        {
            buttonResumeMag.Enabled = check;
        }

        private void buttonStopMag_Click(object sender, EventArgs e)
        {
            if (buttonStopMag.Text == "Stop")
            {
                OnStop?.Invoke(this, new buttonEventArgs() { vecID = VecID, stop = true });
                buttonStopMag.Text = "Auto";
                buttonStopMag.BackColor = Color.Turquoise;

                dataGridViewVec.ReadOnly = false;
                for (int i = 0; i < dataGridViewVec.ColumnCount; i++)
                {
                    if (i != 2)
                        dataGridViewVec.Columns[i].ReadOnly = true;
                }
            }
            else
            {
                OnStop?.Invoke(this, new buttonEventArgs() { vecID = VecID, stop = false });
                buttonStopMag.Text = "Stop";
                buttonStopMag.BackColor = Color.Crimson;

                dataGridViewVec.ReadOnly = true;
            }
        }
        private void buttonReconnect_Click(object sender, EventArgs e)
        {
            OnReconnect?.Invoke(this, new ReconnectedEventArgs() { vecID = VecID });
        }
    }

    public class ChangePartEventArgs : EventArgs
    {
        public int PartIndex = 0;
        public VehiclePartStatus DstStatus = VehiclePartStatus.EMPTY;
    }
    public class buttonEventArgs : EventArgs
    {
        public string vecID = "";
        public bool stop = false;
    }
    public class ReconnectedEventArgs : EventArgs
    {
        public string vecID = "";
    }
}
