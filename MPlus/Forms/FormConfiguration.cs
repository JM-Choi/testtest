using MPlus.Logic;
using MPlus.Ref;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Timers;

namespace MPlus.Forms
{
    public partial class FormConfiguration : Form
    {
        public event EventHandler<EventArgs> OnRecalculateDist;
        public event EventHandler<EventArgs> OnRecalculateDistZero;
        public event EventHandler<EventArgs> OnRecalculateDistAdd;
        public event EventHandler<EventArgs> OnRefreshGoalInfo;
        public event EventHandler<EventArgs> OnCheckGoalInfo;
        public event EventHandler<EventArgs> OnRefreshVecConnect;
        public event EventHandler<TCFakeData> OnTCFakeDataSend;

        private CfgData _CfgData;
        private DbHandler Db;

        //private List<VehicleProperty> _VehicleProperty = new List<VehicleProperty>();

        public FormConfiguration()
        {
            InitializeComponent();
            _CfgData = Configuration.Init.Data;
            Db = DbHandler.Inst;
            ViewInfoRefresh();
        }

        private void ViewInfoRefresh()
        {
            propertyGridCommonInfo.SelectedObject = _CfgData;

            dataGridViewController.DataSource = null;
            dataGridViewController.DataSource = Db.Controllers.ToList();
            dataGridViewController.Refresh();
            dataGridViewController.Parent.Refresh();


            List<unit> _unit = Db.Units.Where(p => p.idx != 0).ToList();

            foreach (var x in _unit)
            {
                cbSRCEQPID.Items.Add(x.ID);
                cbDSTEQPID.Items.Add(x.ID);
            }
            //listBoxVehicleReg.DataSource = null;
            //listBoxVehicleReg.DataSource = Db.Vechicles.Where(p => p.isUse == 1).Select(p => p.ID).ToList();
            //listBoxVehicleReg.Refresh();

            //dataGridViewUnit.DataSource = null;
            //dataGridViewUnit.DataSource = Db.Units.ToList();
            //dataGridViewUnit.Refresh();
            //dataGridViewUnit.Parent.Refresh();

            //dataGridViewDist.DataSource = Db.Distances.ToArray();
        }

        private string selVecID;
        private int selVecIndex;
        //private void listBoxVehicleReg_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    var list = sender as ListBox;
        //    if (list.SelectedItem == null)
        //    {
        //        return;
        //    }
        //    selVecID = list.SelectedItem.ToString();
        //    selVecIndex = list.SelectedIndex;
        //    propertyGridVehicleInfo.SelectedObject = Db.Vechicles.Where(p=>p.ID == selVecID && p.isUse == 1).SingleOrDefault();


        //    List<string> LstNo = new List<string>();
        //    LstNo.Clear();
        //    var lst = Db.VecParts.Where(p => p.VEHICLEID == selVecID).GroupBy(p => p.portNo).ToList();
        //    foreach (var item in lst)
        //    {
        //        LstNo.Add(item.Key.ToString());
        //    }
        //    //ss.kim-181212
        //    //lst_PartID.DataSource = LstNo;
        //    //var temp = DbData.VecParts.Where(p => p.VEHICLEID == selVecID).ToDictionary(p => p.portNo)./*Where(p => p.Value.Seq == 0).*/SingleOrDefault().Value;
        //    //propertyGridPart.SelectedObject = temp;
        //}

        //private void lst_PartID_SelectedIndexChanged(object sender, EventArgs e)
        //{
        //    int selPartIndex = lst_PartID.SelectedIndex;
        //    var temp = Db.VecParts.Where(p => p.VEHICLEID == selVecID).OrderBy(x => x.portNo).ThenBy(x=>x.slotNo).ToList()[selPartIndex];
        //    propertyGridPart.SelectedObject = temp;
        //}

        private void buttonAddVehicle_Click(object sender, EventArgs e)
        {
#if false
            string id = "";
            InputBox("차량 추가", "새로 추가 할 차량의 ID를 넣으세요.", ref id);
            string count = "4";
            int partCount = 4; ;
            InputBox("차량 파티션 추가", "새로 추가 할 차량 파티션의 개수를 넣으세요.", ref count);
            if (id.Length < 2)
            {
                MessageBox.Show("차량의 이름의 길이는 1보다 길어야 합니다.");
                return;
            }
            try
            {
                partCount = Convert.ToInt32(count);
            }
            catch 
            {
                MessageBox.Show("파티션의 개수를 정수를 입력 해 주세요.");
                return;
            }
            try
            {
                DateTime dateTime = DateTime.Now;
                var data = DbData.Vechicles;
                var tempVec = new VEHICLE(id) { IpAddress = "0.0.0.0", Port = 0, InstallState = Ref.VehicleInstallState.REMOVED, State = Ref.VehicleState.NOT_ASSIGNED, Mode = Ref.VehicleMode.MANUAL };
                DbData.Add(tempVec);

                var partData = DbData.VecParts;
                for (int i = 0; i < partCount; i++)
                {
                    var tempPart = new VEHICLE_PART($"{id}_VP{i+1}") { State = Ref.VehiclePartState.ENABLE, Status = Ref.VehiclePartStatus.EMPTY, Seq = i, VehicleID = id };
                    DbData.Add(tempPart);
                }

                ViewInfoRefresh();                
            }
            catch 
            {

            }
#endif
        }

        private void buttonDelVehicle_Click(object sender, EventArgs e)
        {
            while (true)
            {
                var partData = Db.VecParts.Where(p => p.VEHICLEID == selVecID).FirstOrDefault();
                if (partData == null)
                {
                    break;
                }
                else
                {
                    Db.Delete(partData);
                }
            }
            //var partData = DbData.VecParts.Where(p => p.VehicleID == selVecID).ToList();
            //if (partData == null)
            //{
            //    return;
            //}

            //foreach (var item in partData)
            //{
            //    DbData.Delete(item);
            //}
            //ViewInfoRefresh();

            var data = Db.Vechicles.Where(p => p.ID == selVecID && p.isUse == 1).SingleOrDefault();
            if (data == null)
            {
                return;
            }
            Db.Delete(data);
            ViewInfoRefresh();
        }

        private void FormConfiguration_FormClosing(object sender, FormClosingEventArgs e)
        {

        }
        public static DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        private void buttonUnitAdd_Click(object sender, EventArgs e)
        {
            string id = "";
            string goalName = "";
            InputBox("유닛 추가", "새로 추가 할 유닛의 ID를 넣으세요.", ref id);
            InputBox("유닛 추가", $"새로 추가 한 유닛[{id}]의 실제 골 이름을 넣으세요.", ref goalName);

            if (id.Length < 1 || goalName.Length < 1)
            {
                return;
            }

            var unitData = Db.Units;
            unit tempData = new unit()
            {
                ID = id,
                loc_x = 0,
                loc_y = 0,
                direction = "",
                GOALNAME = goalName,
            };

            Db.Add(tempData);
            ViewInfoRefresh();
        }

        //private void buttonUnitDel_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        foreach (DataGridViewRow item in dataGridViewUnit.SelectedRows)
        //        {
        //            var unitName = item.Cells[1].Value?.ToString();
        //            var sel = Db.Units.Where(p => p.ID == unitName).SingleOrDefault();
        //            if (sel != null)
        //            {
        //                Db.Delete(sel);
        //            }
        //        }
        //        ViewInfoRefresh();
        //    }
        //    catch 
        //    {

        //    }

        //}

        //private string selUnitName = "";
        //private void dataGridViewUnit_CellClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex < 0)
        //    {
        //        return;
        //    }
        //    selUnitName = dataGridViewUnit.Rows[e.RowIndex].Cells[1].Value?.ToString();
        //}

        private void buttonDistRecalcul_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("포트간의 거리를 다시 계산하시겠습니까?\r\n매우 긴 시간이 소모됩니다. 모든차량이 멈춰있는 상태에서 진행 해 주세요.", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                OnRecalculateDist?.Invoke(this, null);
                //dataGridViewDist.DataSource = DbData.Distances.ToArray();
                ViewInfoRefresh();
            }
        }

        private void buttonDistZeroCalc_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("포트간의 거리를 이어서 계산하시겠습니까?\r\n매우 긴 시간이 소모됩니다. 모든차량이 멈춰있는 상태에서 진행 해 주세요.", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                OnRecalculateDistZero?.Invoke(this, null);
                //dataGridViewDist.DataSource = DbData.Distances.ToArray();
                ViewInfoRefresh();
            }
        }

        private void buttonDistAddCalc_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("포트간의 거리를 추가로 계산하시겠습니까?\r\n매우 긴 시간이 소모됩니다. 모든차량이 멈춰있는 상태에서 진행 해 주세요.", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                OnRecalculateDistAdd.Invoke(this, null);
                //dataGridViewDist.DataSource = DbData.Distances.ToArray();
                ViewInfoRefresh();
            }
        }

        //private void buttonSearchDist_Click(object sender, EventArgs e)
        //{
        //    SearchPortId(textBoxDistSearch.Text);
        //}

        //private void textBoxDistSearch_KeyPress(object sender, KeyPressEventArgs e)
        //{
        //    if (e.KeyChar == (char)Keys.Enter)
        //    {
        //        SearchPortId(textBoxDistSearch.Text);
        //    }
        //}

        //private void SearchPortId(string searchText)
        //{
        //    if (searchText.Length > 0)
        //    {
        //        string target = textBoxDistSearch.Text.ToUpper();
        //        dataGridViewDist.DataSource = Db.Distances.Where(p => p.UNITID_start.ToUpper().IndexOf(target) >= 0 || p.UNITID_end.ToUpper().IndexOf(target) >= 0).ToList();
        //    }
        //    else
        //    {
        //        dataGridViewDist.DataSource = Db.Distances.ToList();
        //    }
        //}

        private void buttonGetGoalList_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Map파일로 부터 모든 Goal 정보를 얻어오시겠습니까?\r\n기존 데이터가 모두 삭제 됩니다.", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                OnRefreshGoalInfo?.Invoke(this, null);
                ViewInfoRefresh();
            }
        }

        private void buttonReconnectVec_Click(object sender, EventArgs e)
        {
            OnRefreshVecConnect?.Invoke(this, null);
        }

        private void FormConfiguration_Load(object sender, EventArgs e)
        {

        }

        private void dataGridViewUnit_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            //             DataGridViewRow sel = dataGridViewUnit.SelectedRows[0];
            //             string name = sel.Cells[4].Value.ToString();
            //           GoalEditor zone = new GoalEditor(selUnitName);
            //           zone.ShowDialog();
        }

        //private void dataGridViewUnit_SelectionChanged(object sender, EventArgs e)
        //{
        //    List<unit> arry = new List<unit>();
        //    try
        //    {
        //        foreach (DataGridViewRow item in dataGridViewUnit.SelectedRows)
        //        {
        //            var res = item.Cells["idx"].Value;
        //            arry.Add(Db.Units.Where(p => p.idx == (int)res).FirstOrDefault());
        //        }

        //        propertyGridUnitEdit.SelectedObjects = arry.ToArray();
        //    }
        //    catch 
        //    {
        //    }

        //}

        //private void dataGridViewUnit_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        //{
        //    Color[] colorArry =
        //        {
        //            Color.FromArgb(255, 255, 150, 150), //r
        //            Color.FromArgb(255, 150, 200, 150),
        //            Color.FromArgb(255, 255, 255, 150),
        //            Color.FromArgb(255, 200, 255, 100),
        //            Color.FromArgb(255, 150, 255, 255), //g
        //            Color.FromArgb(255, 100, 255, 255),
        //            Color.FromArgb(255, 100, 200, 255),
        //            Color.FromArgb(255, 100, 100, 255), //b
        //            Color.FromArgb(255, 200, 100, 200),
        //            Color.FromArgb(255, 250, 150, 200),
        //            Color.FromArgb(255, 50, 50, 50),
        //            Color.FromArgb(255, 0, 0, 0),
        //         };

        //    if (dataGridViewUnit.RowCount > 1)
        //    {
        //        var text = dataGridViewUnit.Rows[e.RowIndex].Cells["GoalType"].Value.ToString();

        //        dataGridViewUnit.Rows[e.RowIndex].DefaultCellStyle.BackColor = colorArry[(int)Enum.Parse(typeof(Ref.EqpGoalType), text)%10];
        //    }
        //}

        //private void propertyGridUnitEdit_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        //{
        //    dataGridViewUnit.Refresh();
        //}

        //private void dataGridViewUnit_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        //{
        //    DataGridView grid = (DataGridView)sender;
        //    SortOrder so = SortOrder.None;
        //    if (grid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection == SortOrder.None ||
        //        grid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection == SortOrder.Ascending)
        //    {
        //        so = SortOrder.Descending;
        //    }
        //    else
        //    {
        //        so = SortOrder.Ascending;
        //    }
        //    if (SortUnits(grid.Columns[e.ColumnIndex].HeaderText, so))
        //    {
        //        grid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = so;
        //    }
        //}

        //private bool SortUnits(string columnName, SortOrder order)
        //{
        //    //dataGridViewUnit.DataSource = null;
        //    bool retVal = true;
        //    if (order == SortOrder.Ascending)
        //    {
        //        switch (columnName)
        //        {
        //            case "Index":
        //                dataGridViewUnit.DataSource = Db.Units.OrderBy(p => p.idx).ToList();
        //                break;
        //            case "ID":
        //                dataGridViewUnit.DataSource = Db.Units.OrderBy(p => p.ID).ToList();
        //                break;
        //            case "GoalName":
        //                dataGridViewUnit.DataSource = Db.Units.OrderBy(p => p.GOALNAME).ToList();
        //                break;
        //            case "GoalType":
        //                dataGridViewUnit.DataSource = Db.Units.OrderBy(p => p.goaltype).ToList();
        //                break;
        //            default:
        //                retVal = false;
        //                break;
        //        }
        //    }
        //    else
        //    {
        //        switch (columnName)
        //        {
        //            case "Index":
        //                dataGridViewUnit.DataSource = Db.Units.OrderByDescending(p => p.idx).ToList();
        //                break;
        //            case "ID":
        //                dataGridViewUnit.DataSource = Db.Units.OrderByDescending(p => p.ID).ToList();
        //                break;
        //            case "GoalName":
        //                dataGridViewUnit.DataSource = Db.Units.OrderByDescending(p => p.GOALNAME).ToList();
        //                break;
        //            case "GoalType":
        //                dataGridViewUnit.DataSource = Db.Units.OrderByDescending(p => p.goaltype).ToList();
        //                break;
        //            default:
        //                retVal = false;
        //                break;
        //        }
        //    }
        //    return retVal;
        //}

        //private void dataGridViewDist_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        //{
        //    DataGridView grid = (DataGridView)sender;
        //    SortOrder so = SortOrder.None;
        //    if (grid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection == SortOrder.None ||
        //        grid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection == SortOrder.Ascending)
        //    {
        //        so = SortOrder.Descending;
        //    }
        //    else
        //    {
        //        so = SortOrder.Ascending;
        //    }
        //    if (SortDist(grid.Columns[e.ColumnIndex].HeaderText, so))
        //    {
        //        grid.Columns[e.ColumnIndex].HeaderCell.SortGlyphDirection = so;
        //    }
        //}

        //private bool SortDist(string columnName, SortOrder order)
        //{
        //    //dataGridViewUnit.DataSource = null;
        //    bool retVal = true;
        //    if (order == SortOrder.Ascending)
        //    {
        //        switch (columnName)
        //        {
        //            case "ID":
        //                dataGridViewDist.DataSource = Db.Distances.OrderBy(p => p.idx).ToList();
        //                break;
        //            case "Distance1":
        //                dataGridViewDist.DataSource = Db.Distances.OrderBy(p => p.distance1).ToList();
        //                break;
        //            case "UnitLeft":
        //                dataGridViewDist.DataSource = Db.Distances.OrderBy(p => p.UNITID_start).ToList();
        //                break;
        //            case "UnitRight":
        //                dataGridViewDist.DataSource = Db.Distances.OrderBy(p => p.UNITID_end).ToList();
        //                break;
        //            default:
        //                retVal = false;
        //                break;
        //        }
        //    }
        //    else
        //    {
        //        switch (columnName)
        //        {
        //            case "ID":
        //                dataGridViewDist.DataSource = Db.Distances.OrderByDescending(p => p.idx).ToList();
        //                break;
        //            case "Distance1":
        //                dataGridViewDist.DataSource = Db.Distances.OrderByDescending(p => p.distance1).ToList();
        //                break;
        //            case "UnitLeft":
        //                dataGridViewDist.DataSource = Db.Distances.OrderByDescending(p => p.UNITID_start).ToList();
        //                break;
        //            case "UnitRight":
        //                dataGridViewDist.DataSource = Db.Distances.OrderByDescending(p => p.UNITID_end).ToList();
        //                break;
        //            default:
        //                retVal = false;
        //                break;
        //        }
        //    }
        //    return retVal;
        //}

        private void buttonCheckGoalList_Click(object sender, EventArgs e)
        {
            OnCheckGoalInfo?.Invoke(this, null);
            ViewInfoRefresh();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Configuration.Init.SaveConfiguration(_CfgData);
                MessageBox.Show("저장되었습니다");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"설정저장오류:{ex.Message}");
            }
        }

        private void rbSRCSTART_CheckedChanged(object sender, EventArgs e)
        {
            if (rbSRCSTART.Checked)
            {
                cbVEHICLEID.Enabled = false;
            }
        }

        private void rbDSTSTART_CheckedChanged(object sender, EventArgs e)
        {
            if (rbDSTSTART.Checked)
            {
                cbVEHICLEID.Enabled = true;
            }
        }

        string[] stkportname = { "AUTO01.LP", "AUTO02.LP", "STACK01.LP", "STACK02.LP" };
        string[] handlerSRCportname = { "LOADER1", "LOADER2" };
        string[] handlerDSTportname = { "GOOD", "FAIL" };
        string[] reflowportname = { "AUTO01", "AUTO02" };
        private void cbSRCEQPID_SelectedIndexChanged(object sender, EventArgs e)
        {
            unit _unit = Db.Units.Where(p => p.GOALNAME == cbSRCEQPID.Text).SingleOrDefault();
            txtSRCPORT.Visible = false;
            cbSRCPORT.Visible = true;
            txtSRCPORT.Text = string.Empty;
            cbSRCPORT.Text = string.Empty;
            cbSRCPORT.Items.Clear();
            if (_unit != null && _unit.goaltype == (int)EqpGoalType.STK)
            {
                foreach (var x in stkportname)
                {
                    cbSRCPORT.Items.Add(x);
                }
            }
            else if (_unit != null && (_unit.goaltype == (int)EqpGoalType.HANDLER || _unit.goaltype == (int)EqpGoalType.HANDLER_STACK))
            {
                foreach (var x in handlerSRCportname)
                {
                    cbSRCPORT.Items.Add(x);
                }
            }
            else if (_unit != null && _unit.goaltype == (int)EqpGoalType.REFLOW)
            {
                foreach (var x in reflowportname)
                {
                    cbSRCPORT.Items.Add(x);
                }
            }
            else
            {
                txtSRCPORT.Visible = true;
                cbSRCPORT.Visible = false;
            }
        }

        private void cbDSTEQPID_SelectedIndexChanged(object sender, EventArgs e)
        {
            unit _unit = Db.Units.Where(p => p.GOALNAME == cbDSTEQPID.Text).SingleOrDefault();
            txtDSTPORT.Visible = false;
            cbDSTPORT.Visible = true;
            txtDSTPORT.Text = string.Empty;
            cbDSTPORT.Text = string.Empty;
            cbDSTPORT.Items.Clear();
            if (_unit != null && _unit.goaltype == (int)EqpGoalType.STK)
            {
                foreach (var x in stkportname)
                {
                    cbDSTPORT.Items.Add(x);
                }
            }
            else if (_unit != null && (_unit.goaltype == (int)EqpGoalType.HANDLER || _unit.goaltype == (int)EqpGoalType.HANDLER_STACK))
            {
                foreach (var x in handlerDSTportname)
                {
                    cbDSTPORT.Items.Add(x);
                }
            }
            else if (_unit != null && _unit.goaltype == (int)EqpGoalType.REFLOW)
            {
                foreach (var x in reflowportname)
                {
                    cbDSTPORT.Items.Add(x);
                }
            }
            else
            {
                txtDSTPORT.Visible = true;
                cbDSTPORT.Visible = false;
            }
        }

        private void btnCreate_Click(object sender, EventArgs e)
        {
            txtLogger.ForeColor = Color.Red;

            string batchid = string.Empty;
            string s_port = string.Empty;
            string t_port = string.Empty;
            string transfertype = string.Empty;
            DateTime dt = DateTime.Now;
            DateTime realdt = DateTime.UtcNow;
            string windowtime = string.Empty;
            string realtime = string.Empty;
            string vehicleid = string.Empty;
            string bufslot = string.Empty;
            string lotno = string.Empty;
            string qty = string.Empty;
            string sstepid = string.Empty;
            string tstepid = string.Empty;

            Nullable<int> state = null;
            Nullable<DateTime> srctime = new DateTime();


            if (cbSRCEQPID.Text == "")
            {
                txtLogger.Text = "SRC EQPID not Selete.";
                return;
            }

            if (cbDSTEQPID.Text == "")
            {
                txtLogger.Text = "DST EQPID not Selete.";
                return;
            }

            if (txtTRAYID.Text == "")
            {
                txtLogger.Text = "TRAYID is null.";
                return;
            }

            string[] val = txtTRAYID.Text.Split(',');

            if (cbWORKTYPE.Text == "")
            {
                txtLogger.Text = "WORKTYPE is null.";
                return;
            }
            else if (cbWORKTYPE.Text == "I" || cbWORKTYPE.Text == "O" || cbWORKTYPE.Text == "OI")
            {
                batchid = "P" + cbWORKTYPE.Text + "_" + dt.ToString("ddHHmmssfff");
                transfertype = "TRAY";
            }
            else
            {
                batchid = cbWORKTYPE.Text + "_" + dt.ToString("ddHHmmssfff");
                transfertype = "STACK";
            }


            if (cbSRCPORT.Visible)
            {
                if (cbSRCPORT.Text == "")
                {
                    txtLogger.Text = "SRC PORT is null.";
                    return;
                }
                else
                    s_port = cbSRCPORT.Text;
            }
            else
            {
                if (cbWORKTYPE.Text == "I" || cbWORKTYPE.Text == "O" || cbWORKTYPE.Text == "OI")
                {
                    int portcount = txtSRCPORT.Text.Split(',').Count();

                    if (val.Count() != portcount)
                    {
                        txtLogger.Text = "The number of SRC Port and TRAY is different.";
                        return;
                    }
                    else
                        s_port = txtSRCPORT.Text;
                }
            }

            if (cbDSTPORT.Visible)
            {
                if (cbDSTPORT.Text == "")
                {
                    txtLogger.Text = "DST PORT is null.";
                    return;
                }
                else
                    t_port = cbDSTPORT.Text;
            }
            else
            {
                if (cbWORKTYPE.Text == "I" || cbWORKTYPE.Text == "O" || cbWORKTYPE.Text == "OI")
                {
                    int portcount = txtDSTPORT.Text.Split(',').Count();

                    if (val.Count() != portcount)
                    {
                        txtLogger.Text = "The number of DST Port and TRAY is different.";
                        return;
                    }
                    else
                        t_port = txtDSTPORT.Text;
                }
            }

            windowtime = dt.ToString("yyyy-MM-dd HH:mm:ss");
            realtime = ((Int32)(realdt.Subtract(new DateTime(1970, 1, 1)).TotalSeconds)).ToString();

            if (cbVEHICLEID.Enabled)
            {
                if (cbVEHICLEID.Text != "")
                    vehicleid = cbVEHICLEID.Text;
                else
                    vehicleid = null;
            }
            else
                vehicleid = null;

            if (rbDSTSTART.Checked)
            {
                state = 8;
                srctime = dt;

                int port = 4;
                if (cbWORKTYPE.Text == "I" || cbWORKTYPE.Text == "O" || cbWORKTYPE.Text == "OI")
                {
                    for (int i = 0; i < val.Count(); i++)
                    {
                        if (i / 10 == 1)
                            port = 2;
                        else if (i / 10 == 2)
                            port = 3;
                        else if (i / 10 == 3)
                            port = 1;

                        if (i != 0)
                            bufslot += ",";

                        bufslot += port.ToString();
                        bufslot += ",";
                        bufslot += (i % 10).ToString("D3");

                    }
                }
                else
                {
                    int count = val.Count() / 10;
                    if (val.Count() % 10 > 0)
                        count += 1;
                    for (int i = 0; i < count; i++)
                    {
                        if (i / 3 == 1)
                            port = 2;
                        else if (i / 3 == 2)
                            port = 3;
                        else if (i / 3 == 3)
                            port = 1;

                        if (i != 0)
                            bufslot += ",";

                        bufslot += port.ToString();
                        bufslot += ",";
                        bufslot += (i % 3).ToString("D3");
                    }
                }
            }
            else
            {
                bufslot = null;
                srctime = null;
            }

            if (txtLOTNO.Text != "")
            {
                int lotnocount = txtLOTNO.Text.Split(',').Count();
                if (val.Count() != lotnocount)
                {
                    txtLogger.Text = "The number of Lot No and TRAY is different.";
                    return;
                }
                else
                    lotno = txtLOTNO.Text;
            }
            else
                lotno = null;

            if (txtQTY.Text != "")
            {
                int qtycount = txtQTY.Text.Split(',').Count();
                if (val.Count() != qtycount)
                {
                    txtLogger.Text = "The number of QTY and TRAY is different.";
                    return;
                }
                else
                    qty = txtQTY.Text;
            }
            else
                qty = null;

            if (txtSSTEPID.Text != "")
                sstepid = txtSSTEPID.Text;
            else
                sstepid = null;

            if (txtTSTEPID.Text != "")
                tstepid = txtTSTEPID.Text;
            else
                tstepid = null;

            pepschedule addjob = new pepschedule()
            {
                MULTIID = null,
                BATCHID = batchid,  // key, not null
                S_EQPID = cbSRCEQPID.Text,
                S_PORT = null,
                S_SLOT = s_port,
                T_EQPID = cbDSTEQPID.Text,             // jm.choi - 190514
                T_PORT = null,
                T_SLOT = t_port,
                TRAYID = txtTRAYID.Text,
                WORKTYPE = cbWORKTYPE.Text,
                TRANSFERTYPE = transfertype,
                WINDOW_TIME = windowtime,
                EXECUTE_TIME = realtime,
                REAL_TIME = realtime,
                STATUS = null,
                LOT_NO = lotno,
                QTY = qty,
                STEPID = sstepid,
                S_STEPID = sstepid,                      // jm.choi - 190424
                T_STEPID = tstepid,                      // jm.choi - 190424
                URGENCY = "NORMAL",
                FLOW_STATUS = "IDLE",
                C_VEHICLEID = vehicleid,
                C_bufSlot = bufslot,                      // jm.choi - 190415  
                C_state = state,
                C_srcAssignTime = srctime,
                C_srcArrivingTime = srctime,
                C_srcStartTime = srctime,
                C_srcFinishTime = srctime,
                C_dstAssignTime = null,
                C_dstArrivingTime = null,
                C_dstStartTime = null,
                C_dstFinishTime = null,
                C_isChecked = null,
                C_priority = null,
                DOWNTEMP = null,
                EVENT_DATE = dt,
                ORDER = 1
            };
            Db.Add(addjob);
            txtLogger.ForeColor = Color.Blue;
            txtLogger.Text = string.Format($"Job Create Success. BatchID = {batchid}");
        }

        string[] trayVehicle = { "VEHICLE01", "VEHICLE02" };
        string[] stackVehicle = { "VEHICLE03", "VEHICLE04" };
        private void cbWORKTYPE_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cbWORKTYPE.Text == "I" || cbWORKTYPE.Text == "O" || cbWORKTYPE.Text == "OI")
            {
                cbVEHICLEID.Text = string.Empty;
                cbVEHICLEID.Items.Clear();
                foreach (var x in trayVehicle)
                {
                    cbVEHICLEID.Items.Add(x);
                }
            }
            else
            {
                cbVEHICLEID.Text = string.Empty;
                cbVEHICLEID.Items.Clear();
                foreach (var x in stackVehicle)
                {
                    cbVEHICLEID.Items.Add(x);
                }
            }
        }

        public void peps_load(string vecID)
        {
            dgvPeps.DataSource = null;
            var cmdData = DbHandler.Inst.Peps.Where(p => p.C_VEHICLEID == vecID && p.C_isChecked == 1).Select(p => new { p.WORKTYPE, p.ORDER, p.REAL_TIME, p.EXECUTE_TIME, p.WINDOW_TIME, p.C_state, p.TRAYID, p.S_EQPID, p.T_EQPID, p.C_VEHICLEID, p.S_SLOT, p.T_SLOT, p.BATCHID, p.C_priority }).OrderBy(p => p.ORDER).ThenBy(p => p.EXECUTE_TIME).ThenBy(p => p.C_priority).ToList();

            if (cmdData.Count() > 0)
            {
                dgvPeps.DataSource = cmdData;
            }
            else
            {
                dgvPeps.DataSource = new List<pepschedule>().Select(p => new { p.WORKTYPE, p.ORDER, p.REAL_TIME, p.EXECUTE_TIME, p.WINDOW_TIME, p.C_state, p.TRAYID, p.S_EQPID, p.T_EQPID, p.C_VEHICLEID, p.S_SLOT, p.T_SLOT, p.BATCHID, p.C_priority });
            }

            //dgvPeps.Refresh();
            //dgvPeps.ClearSelection();
        }

        private void cbRvVehicleID_SelectedIndexChanged(object sender, EventArgs e)
        {
            peps_load(cbRvVehicleID.Text);
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            if (selectedCmdRow >= 0 && dgvPeps.RowCount - 1 >= selectedCmdRow)
            {
                string sndMsg = string.Empty;
                var pep = Db.Peps.Where(p => p.BATCHID == dgvPeps.Rows[selectedCmdRow].Cells[12].Value.ToString()).SingleOrDefault();

                if (pep != null)
                {
                    if (rbMoveCheck.Checked == true)
                    {
                        sndMsg = MoveCheck(pep);
                    }
                    else if (rbMoveInfo.Checked == true)
                    {
                        sndMsg = MoveInfo(pep);
                    }
                    else if (rbMoveComp.Checked == true)
                    {
                        sndMsg = MoveComp(pep);
                    }
                    else if (rbTempDown.Checked == true)
                    {
                        sndMsg = TempDown(pep);
                    }
                    else if (rbUnloadInfo.Checked == true)
                    {
                        sndMsg = UnloadInfo(pep);
                    }
                    else if (rbUnloadComp.Checked == true)
                    {
                        sndMsg = UnloadComp(pep);
                    }
                    else if (rbLoadInfo.Checked == true)
                    {
                        sndMsg = LoadInfo(pep);
                    }
                    else if (rbLoadComp.Checked == true)
                    {
                        sndMsg = LoadComp_pep(pep);
                        selectedCmdRow = -1;
                    }
                    else if (rbLoadJobStandby.Checked == true)
                    {
                        sndMsg = LoadJobStandBy(pep);
                    }
                    else if (rbReflowRecipeSet.Checked == true)
                    {
                        sndMsg = ReflowRecipSet(pep);
                    }
                    else if (rbReflowLoaderInfoSet.Checked == true)
                    {
                        sndMsg = ReflowLoaderInfoSet(pep);
                    }
                    else
                    {
                        sndMsg = string.Empty;
                    }

                    if (sndMsg != "")
                    {
                        TCFakeData tcf = new TCFakeData { sndMsg = sndMsg, vecID = pep.C_VEHICLEID };
                        OnTCFakeDataSend?.Invoke(this, tcf);
                        txtRvLogger.ForeColor = Color.Black;
                        txtRvLogger.Text = string.Format($"{sndMsg}");
                    }
                    else
                    {
                        txtRvLogger.ForeColor = Color.Red;
                        txtRvLogger.Text = "No Message";
                    }
                }
                else
                {
                    if (rbLoadComp.Checked == true)
                    {
                        var pep_hi = Db.PepsHisto.Where(p => p.BATCHID == dgvPeps.Rows[selectedCmdRow].Cells[12].Value.ToString()).SingleOrDefault();
                        sndMsg = LoadComp_pep_hi(pep_hi);
                        selectedCmdRow = -1;

                        if (sndMsg != "")
                        {
                            TCFakeData tcf = new TCFakeData { sndMsg = sndMsg, vecID = pep_hi.C_VEHICLEID };
                            OnTCFakeDataSend?.Invoke(this, tcf);
                            txtRvLogger.ForeColor = Color.Black;
                            txtRvLogger.Text = string.Format($"{sndMsg}");
                        }
                        else
                        {
                            txtRvLogger.ForeColor = Color.Red;
                            txtRvLogger.Text = "No Message";
                        }
                    }
                }
            }
            else
            {
                txtRvLogger.ForeColor = Color.Red;
                txtRvLogger.Text = "Job No Selete";
            }
            lbSelNum.Text = selectedCmdRow.ToString();
        }

        int selectedCmdRow = -1;
        private void dgvPeps_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            selectedCmdRow = e.RowIndex;
            lbSelNum.Text = selectedCmdRow.ToString();
        }

        bool movechk_reflow = false;
        int movechk_teqpcount = 0;
        private string MoveCheck(pepschedule pep)
        {
            string sndMsg = string.Empty;
            string eqpid = (rbSRCSel.Checked == true) ? pep.S_EQPID.Split('-')[0] : pep.T_EQPID.Split('-')[0];
            string subeqpid = (rbSRCSel.Checked == true) ? pep.S_EQPID : pep.T_EQPID.Split(',')[movechk_teqpcount++];
            string jobtype = (rbSRCSel.Checked == true) ? "UNLOAD" : "LOAD";
            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            sndMsg = string.Format($"EQTRAYMOVECHECK_REP HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) STATUS=PASS EQPID={eqpid} ");

            if (eqpid == "RO9902")
                sndMsg += string.Format($"SUBEQPID={subeqpid} ");

            if (un.goaltype == (int)EqpGoalType.STK)
            {
                sndMsg += string.Format($"TRAYID={pep.TRAYID} STKSTATUS=IDLE AUTO01=IDLE AUTO02=IDLE STACK01=IDLE STACK02=IDLE AUTO01LP_SLOTINFO=(1:,2:,3:,4:,5:,6:,7:,8:,9:,10:) AUTO02LP_SLOTINFO=(1:,2:,3:,4:,5:,6:,7:,8:,9:,10:) STACK01LP_SLOTINFO=(1:,2:,3:,4:,5:,6:,7:,8:,9:,10:) STACK02LP_SLOTINFO=(1:,2:,3:,4:,5:,6:,7:,8:,9:,10:) JOBTYPE={jobtype} MRNO={pep.C_VEHICLEID} ERRORCODE= ERROMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.SYSWIN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || un.goaltype == (int)EqpGoalType.BUFFER_STK)
            {
                sndMsg += string.Format($"SUBEQPID={subeqpid} TRAYID={pep.TRAYID} JOBTYPE={jobtype} EQSTATUS=IDLE PORTSTATUS=AUTO RECIPE=8 SVTEMP=45 TEMP=45 HUMI=0 PRESSURE=0 DOOR=CLOSE MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");

            }
            else if (un.goaltype == (int)EqpGoalType.HANDLER || un.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                Dictionary<string, string> loadertray = new Dictionary<string, string>();
                Dictionary<string, string> loader = new Dictionary<string, string>();
                Dictionary<string, string> ldstate = new Dictionary<string, string>();

                if (jobtype == "UNLOAD")
                {
                    List<pepschedule> pep1 = new List<pepschedule>();
                    var pep2 = Db.Peps.Where(p => p.EXECUTE_TIME == pep.EXECUTE_TIME && p.MULTIID == pep.MULTIID && p.BATCHID != pep.BATCHID && p.S_SLOT.Contains("LOADER")).ToList();
                    pep2.Insert(0, pep);
                    for (int i = 0; i < pep2.Count(); i++)
                    {
                        pep1.Add(pep2[i]);
                        for (int k = 0; k < un.max_col; k++)
                        {
                            if (pep1[i].S_SLOT == string.Format($"LOADER{k + 1}"))
                            {
                                if (pep1[i].TRAYID.Split(',').Count() > 1)
                                    loadertray.Add(string.Format($"LOADER{k + 1}"), string.Format($"({pep1[i].TRAYID})"));
                                else
                                    loadertray.Add(string.Format($"LOADER{k + 1}"), pep1[i].TRAYID);
                                loader.Add(string.Format($"LOADER{k + 1}"), pep1[i].TRAYID);
                                ldstate.Add(string.Format($"LOADER{k + 1}"), "COMP");
                            }
                            else
                            {
                                loadertray.Add(string.Format($"LOADER{k + 1}"), "");
                                loader.Add(string.Format($"LOADER{k + 1}"), "");
                                ldstate.Add(string.Format($"LOADER{k + 1}"), "");
                            }
                        }
                    }
                }
                else
                {
                    List<pepschedule> pep1 = new List<pepschedule>();
                    var pep2 = Db.Peps.Where(p => p.EXECUTE_TIME == pep.EXECUTE_TIME && p.REAL_TIME == pep.REAL_TIME && p.S_EQPID == pep.S_EQPID && p.WORKTYPE == pep.WORKTYPE
                                            && p.T_SLOT != pep.T_SLOT).ToList();
                    pep2.Insert(0, pep);
                    for (int i = 0; i < pep2.Count(); i++)
                    {
                        pep1.Add(pep2[i]);
                        for (int k = 0; k < un.max_col; k++)
                        {
                            if (pep1[i].T_SLOT == string.Format($"LOADER{k + 1}"))
                            {
                                if (pep1[i].TRAYID.Split(',').Count() > 1)
                                    loadertray.Add(string.Format($"LOADER{k + 1}"), string.Format($"({pep1[i].TRAYID})"));
                                else
                                    loadertray.Add(string.Format($"LOADER{k + 1}"), pep1[i].TRAYID);
                                loader.Add(string.Format($"LOADER{k + 1}"), pep1[i].TRAYID);
                                ldstate.Add(string.Format($"LOADER{k + 1}"), "IDLE");
                            }
                            else
                            {
                                loadertray.Add(string.Format($"LOADER{k + 1}"), "");
                                loader.Add(string.Format($"LOADER{k + 1}"), "");
                                ldstate.Add(string.Format($"LOADER{k + 1}"), "");
                            }
                        }
                    }
                }

                for (int i = 0; i < un.max_col; i++)
                {
                    sndMsg += string.Format($"LD{i + 1}TRAYID={loadertray[string.Format($"LOADER{i + 1}")]} ");
                }
                for (int i = 0; i < un.max_col; i++)
                {
                    sndMsg += string.Format($"LOADER{i + 1}={loader[string.Format($"LOADER{i + 1}")]} ");
                }
                sndMsg += "EMPTY=() EMPTYTRAY=0 TESTSTATUS=IDLE ";
                for (int i = 0; i < un.max_col; i++)
                {
                    sndMsg += string.Format($"LD{i + 1}STATUS={ldstate[string.Format($"LOADER{i + 1}")]} ");
                }
                sndMsg += string.Format($"COKID=200-FBGA-10.x15.0-1.0-16P TEMP=25.0 JOBTYPE={jobtype} MRNO={pep.C_VEHICLEID} ERRORCODE= ERROMSG=");

            }
            else if (un.goaltype == (int)EqpGoalType.REFLOW)
            {
                if (movechk_reflow)
                {
                    sndMsg = string.Format($"EQTRAYMOVECHECK_REP HDR=(KDS1.LH.MPLUS,LH.{subeqpid},GARA,TEMP) STATUS=PASS EQPID={subeqpid} ");
                    sndMsg += string.Format($"TRAYID={pep.TRAYID} RUNMODE=ONLINEREMOTE LOADERSTATUS=IDLE AUTO01=IDLE AUTO02=IDLE AUTO01_SLOTINFO=(1:,2:,3:,4:,5:,6:,7:,8:,9:,10:) AUTO02_SLOTINFO=(1:,2:,3:,4:,5:,6:,7:,8:,9:,10:) JOBTYPE={jobtype} MRNO={pep.C_VEHICLEID} ERRORCODE= ERROMSG=");
                    movechk_reflow = false;
                }
                else
                {
                    sndMsg += string.Format($"TRAYID={pep.TRAYID} RECIPECHECK=PASS JOBTYPE={jobtype} MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG= ");
                    movechk_reflow = true;
                }
            }
            else
            {
                sndMsg = string.Empty;
            }

            if (movechk_teqpcount == pep.T_EQPID.Split(',').Count())
                movechk_teqpcount = 0;
            return sndMsg;
        }

        private string MoveInfo(pepschedule pep)
        {
            string sndMsg = string.Empty;
            string eqpid = pep.S_EQPID.Split('-')[0];
            string subeqpid = pep.S_EQPID;
            string jobtype = "UNLOAD";
            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            sndMsg = string.Format($"EQTRAYMOVEINFO_REP HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) STATUS=PASS EQPID={eqpid} TRAYID={pep.TRAYID} ");
            sndMsg += string.Format($"SOURCEPORT=SHELF DESTPORT={pep.S_SLOT} JOBTYPE={jobtype} MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");

            return sndMsg;
        }

        private string MoveComp(pepschedule pep)
        {
            string sndMsg = string.Empty;
            string eqpid = pep.S_EQPID.Split('-')[0];
            string subeqpid = pep.S_EQPID;
            string slotinfo = Make_attribute_slotinfo(pep.TRAYID);
            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            sndMsg = string.Format($"EQTRAYMOVECOMP HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) EQPID={eqpid} TRAYID=({pep.TRAYID}) SOURCEPORT=SHELF ");
            sndMsg += string.Format($"DESTPORT={pep.S_SLOT} SLOTINFO=({slotinfo}) MRNO={pep.C_VEHICLEID}");

            return sndMsg;
        }
        const int slotinfo_base = 1;
        const int slotinfo_limit = 10;
        const int array_base = 0;
        string Make_attribute_slotinfo(string trayid)
        {
            string[] trays = trayid.Split(',');
            string slotinfo = string.Empty;
            int limit = trays.Count();
            for (int x = slotinfo_base, y = array_base; x <= slotinfo_limit; x++, y++)
            {
                if (y < limit)
                {
                    slotinfo += string.Format($"{x}:{trays[y]}");
                    if (x != slotinfo_limit)
                        slotinfo += ",";
                }
                else
                {
                    slotinfo += string.Format($"{x}:");
                    if (x != slotinfo_limit)
                        slotinfo += ",";
                }
            }
            return slotinfo;
        }

        private string TempDown(pepschedule pep)
        {
            string sndMsg = string.Empty;
            string eqpid = (pep.C_state < 8) ? pep.S_EQPID.Split('-')[0] : pep.T_EQPID.Split('-')[0];
            string subeqpid = (pep.C_state < 8) ? pep.S_EQPID : pep.T_EQPID;
            string jobtype = (pep.C_state < 8) ? "UNLOAD" : "LOAD";
            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            sndMsg = string.Format($"EQTEMPDOWNREQ_REP HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) STATUS=PASS EQPID={eqpid} SUBEQPID={subeqpid} ");
            sndMsg += string.Format($"JOBTYPE={jobtype} SETTEMP=25 TEMP=25 MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");

            return sndMsg;
        }

        private string UnloadInfo(pepschedule pep)
        {
            string sndMsg = string.Empty;
            string eqpid = pep.S_EQPID.Split('-')[0];
            string subeqpid = pep.S_EQPID;
            string srcslot = TrayLoadInfo_sndMsg_slot(pep.S_PORT);

            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            sndMsg = string.Format($"EQTRAYUNLOADINFO_REP HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) STATUS=PASS EQPID={eqpid} ");

            if (un.goaltype == (int)EqpGoalType.STK)
            {
                sndMsg += string.Format($"TRAYID=({pep.TRAYID}) BATCHJOBID={pep.BATCHID} MULTIJOBID= SOURCEPORT={pep.S_SLOT} SOURCESLOTNO=({srcslot}) DESTPORT={pep.T_EQPID.Split('-')[0]} DESTSLOTNO=({pep.T_SLOT}) EXECUTETIME={pep.EXECUTE_TIME} JOBTYPE=UNLOAD MRNO={pep.C_VEHICLEID} ERRORCODE= ERROMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.SYSWIN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || un.goaltype == (int)EqpGoalType.BUFFER_STK)
            {
                sndMsg += string.Format($"SUBEQPID={subeqpid} TRAYID=({pep.TRAYID}) LOTID={pep.LOT_NO} JOBTYPE=UNLOAD RECIPE= SLOTID=({pep.S_SLOT}) STEPID={pep.S_STEPID} EXECUTETIME={pep.EXECUTE_TIME} MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.HANDLER || un.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                var pep1 = Db.Peps.Where(p => p.EXECUTE_TIME == pep.EXECUTE_TIME && p.REAL_TIME == pep.REAL_TIME && p.S_EQPID == pep.S_EQPID && p.WORKTYPE == pep.WORKTYPE).ToList();
                pep1.RemoveAt(pep1.FindIndex(x => x == pep));
                pep1.Insert(0, pep);

                for (int i = 0; i < un.max_col; i++)
                {
                    for (int j = 0; j < pep1.Count(); j++)
                    {
                        if (pep1[j].S_SLOT == string.Format($"LOADER{i + 1}"))
                        {
                            sndMsg += string.Format($"LD{i + 1}TRAYID={pep1[j].TRAYID} ");
                            break;
                        }
                        if (j == pep1.Count() - 1)
                        {
                            sndMsg += string.Format($"LD{i + 1}TRAYID= ");
                        }
                    }
                }
                sndMsg += string.Format($"JOBTYPE=UNLOAD MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.REFLOW)
            {
                string reflow_srcslot = TrayLoadInfo_sndMsg_slot(pep.S_PORT);
                sndMsg += string.Format($"TRAYID=({pep.TRAYID}) BATCHJOBID={pep.BATCHID} MULTIJOBID= SOURCEPORT={pep.S_SLOT} SOURCESLOTNO=({reflow_srcslot}) DESTPORT={pep.T_EQPID.Split('-')[0]} DESTSLOTNO=({pep.T_SLOT}) JOBTYPE=UNLOAD EXECUTETIME={pep.EXECUTE_TIME} STEPID={pep.STEPID} RUNMODE=ONLINEREMOTE MRNO={pep.C_VEHICLEID} ERRORCODE= ERROMSG=");
            }
            else
            {
                sndMsg = string.Empty;
            }

            return sndMsg;
        }
        private string TrayLoadInfo_sndMsg_slot(string port)
        {
            string[] slot_split = (port).Split(',');
            string slot = string.Empty;
            for (int i = 0; i < slot_split.Count() / 2; i++)
            {
                if (i > 0)
                    slot += ",";
                slot += slot_split[(i * 2) + 1];
            }
            return slot;
        }

        private string UnloadComp(pepschedule pep)
        {
            string sndMsg = string.Empty;
            string eqpid = pep.S_EQPID.Split('-')[0];
            string subeqpid = pep.S_EQPID;
            string jobtype = "UNLOAD";
            string srcslot = TrayLoadInfo_sndMsg_slot(pep.S_PORT);
            string slotinfo = string.Empty;

            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            sndMsg = string.Format($"EQTRAYUNLOADCOMPLETE HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) STATUS=PASS EQPID={eqpid} ");

            if (un.goaltype == (int)EqpGoalType.STK)
            {
                sndMsg += string.Format($"PORT={pep.S_SLOT} MRNO={pep.C_VEHICLEID} ERRORCODE= ERROMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.SYSWIN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || un.goaltype == (int)EqpGoalType.REFLOW || un.goaltype == (int)EqpGoalType.BUFFER_STK)
            {
                slotinfo = Make_attribute_slotinfo(pep.TRAYID);
                sndMsg += string.Format($"SUBEQPID={subeqpid} TRAYID={pep.TRAYID} LOTID={pep.LOT_NO} JOBTYPE={jobtype} RECIPE= SLOTID={slotinfo} STEPID={pep.STEPID} EXECUTETIME={pep.EXECUTE_TIME} MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.HANDLER || un.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                var pep1 = Db.Peps.Where(p => p.EXECUTE_TIME == pep.EXECUTE_TIME && p.REAL_TIME == pep.REAL_TIME && p.S_EQPID == pep.S_EQPID && p.WORKTYPE == pep.WORKTYPE && p.S_SLOT.Contains("LOADER")).ToList();
                pep1.RemoveAt(pep1.FindIndex(x => x == pep));
                pep1.Insert(0, pep);

                for (int i = 0; i < un.max_col; i++)
                {
                    for (int j = 0; j < pep1.Count(); j++)
                    {
                        if (pep1[j].S_SLOT == string.Format($"LOADER{i + 1}"))
                        {
                            sndMsg += string.Format($"LD{i + 1}TRAYID={pep1[j].TRAYID} ");
                            break;
                        }
                        if (j == pep1.Count() - 1)
                        {
                            sndMsg += string.Format($"LD{i + 1}TRAYID= ");
                        }
                    }
                }
                sndMsg += string.Format($"JOBTYPE=UNLOAD MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");
            }
            else
            {
                sndMsg = string.Empty;
            }

            return sndMsg;
        }

        private string LoadInfo(pepschedule pep)
        {
            string sndMsg = string.Empty;
            string eqpid = pep.T_EQPID.Split('-')[0];
            string subeqpid = pep.T_EQPID;

            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            sndMsg = string.Format($"EQTRAYLOADINFO_REP HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) STATUS=PASS EQPID={eqpid} ");

            if (un.goaltype == (int)EqpGoalType.STK)
            {
                string dstlot = TrayLoadInfo_sndMsg_slot(pep.T_PORT);
                sndMsg += string.Format($"TRAYID=({pep.TRAYID}) BATCHJOBID={pep.BATCHID} MULTIJOBID= SOURCEPORT={pep.S_EQPID.Split('-')[0]} SOURCESLOTNO=({pep.S_SLOT}) DESTPORT={pep.T_SLOT} DESTSLOTNO=({dstlot}) EXECUTETIME={pep.EXECUTE_TIME} JOBTYPE=LOAD MRNO={pep.C_VEHICLEID} ERRORCODE= ERROMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.SYSWIN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || un.goaltype == (int)EqpGoalType.BUFFER_STK)
            {
                sndMsg += string.Format($"SUBEQPID={subeqpid} TRAYID=({pep.TRAYID}) LOTID={pep.LOT_NO} JOBTYPE=LOAD SLOTID=({pep.T_SLOT}) STEPID={pep.T_STEPID} EXECUTETIME={pep.EXECUTE_TIME} MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.HANDLER || un.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                var pep1 = Db.Peps.Where(p => p.EXECUTE_TIME == pep.EXECUTE_TIME && p.REAL_TIME == pep.REAL_TIME && p.S_EQPID == pep.S_EQPID && p.WORKTYPE == pep.WORKTYPE).ToList();
                pep1.RemoveAt(pep1.FindIndex(x => x == pep));
                pep1.Insert(0, pep);
                string traytype = string.Empty;

                for (int i = 0; i < un.max_col; i++)
                {
                    for (int j = 0; j < pep1.Count(); j++)
                    {
                        if (pep1[j].S_SLOT == string.Format($"LOADER{i + 1}"))
                        {
                            traytype = TrayLoadInfo_sndMsg_Handler_traytype(pep1[j]);
                            sndMsg += string.Format($"LD{i + 1}LOTID={pep1[j].LOT_NO} LD{i + 1}TRAYID={pep1[j].TRAYID} LD{i + 1}STEPID={pep1[j].T_STEPID} LD{1 + 1}TRAYTYPE={traytype} ");
                            break;
                        }
                        if (j == pep1.Count() - 1)
                        {
                            sndMsg += string.Format($"LD{i + 1}LOTID= LD{i + 1}TRAYID= LD{i + 1}STEPID= LD{1 + 1}TRAYTYPE= ");
                        }
                    }
                }
                sndMsg += string.Format($"JOBTYPE=UNLOAD MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.REFLOW)
            {
                string reflow_dstslot = TrayLoadInfo_sndMsg_slot(pep.T_PORT);
                sndMsg += string.Format($"TRAYID=({pep.TRAYID}) BATCHJOBID={pep.BATCHID} MULTIJOBID= SOURCEPORT={pep.S_EQPID.Split('-')[0]} SOURCESLOTNO=({pep.S_SLOT}) DESTPORT={pep.T_SLOT} DESTSLOTNO=({reflow_dstslot}) JOBTYPE=LOAD EXECUTETIME={pep.EXECUTE_TIME} STEPID={pep.STEPID} RUNMODE=ONLINEREMOTE MRNO={pep.C_VEHICLEID} ERRORCODE= ERROMSG=");
            }
            else
            {
                sndMsg = string.Empty;
            }

            return sndMsg;
        }
        private string TrayLoadInfo_sndMsg_Handler_traytype(pepschedule pep)
        {
            string traytype = string.Empty;
            if (pep.WORKTYPE == "EI" || pep.WORKTYPE == "EO")
            {
                traytype = "EMPTY";
            }
            else if (pep.WORKTYPE == "TI" || pep.WORKTYPE == "TO")
            {
                traytype = "NORMAL";
            }
            else
            {
                traytype = "UNKNOWN TYPE";
            }
            return traytype;
        }

        private string LoadComp_pep(pepschedule pep)
        {
            string sndMsg = string.Empty;
            string eqpid = pep.T_EQPID.Split('-')[0];
            string subeqpid = pep.T_EQPID;
            string jobtype = "LOAD";
            string slotinfo = string.Empty;

            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            sndMsg = string.Format($"EQTRAYLOADCOMPLETE HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) STATUS=PASS EQPID={eqpid} ");

            if (un.goaltype == (int)EqpGoalType.STK)
            {
                sndMsg += string.Format($"PORT={pep.T_SLOT} JOBTYPE={jobtype} MRNO={pep.C_VEHICLEID} ERRORCODE= ERROMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.SYSWIN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || un.goaltype == (int)EqpGoalType.REFLOW || un.goaltype == (int)EqpGoalType.BUFFER_STK)
            {
                slotinfo = Make_attribute_slotinfo(pep.TRAYID);
                sndMsg += string.Format($"SUBEQPID={subeqpid} TRAYID={pep.TRAYID} LOTID={pep.LOT_NO} JOBTYPE={jobtype} RECIPE= SLOTID={slotinfo} STEPID={pep.STEPID} EXECUTETIME={pep.EXECUTE_TIME} MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.HANDLER || un.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                var pep1 = Db.Peps.Where(p => p.EXECUTE_TIME == pep.EXECUTE_TIME && p.REAL_TIME == pep.REAL_TIME && p.T_EQPID == pep.T_EQPID && p.WORKTYPE == pep.WORKTYPE && p.T_SLOT.Contains("LOADER")).ToList();
                pep1.RemoveAt(pep1.FindIndex(x => x == pep));
                pep1.Insert(0, pep);

                for (int i = 0; i < un.max_col; i++)
                {
                    for (int j = 0; j < pep1.Count(); j++)
                    {
                        if (pep1[j].T_SLOT == string.Format($"LOADER{i + 1}"))
                        {
                            sndMsg += string.Format($"LD{i + 1}TRAYID={pep1[j].TRAYID} ");
                            break;
                        }
                        if (j == pep1.Count() - 1)
                        {
                            sndMsg += string.Format($"LD{i + 1}TRAYID= ");
                        }
                    }
                }
                sndMsg += string.Format($"JOBTYPE={jobtype} MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");
            }
            else
            {
                sndMsg = string.Empty;
            }

            return sndMsg;
        }
        private string LoadComp_pep_hi(pepschedule_history pep)
        {
            string sndMsg = string.Empty;
            string eqpid = pep.T_EQPID.Split('-')[0];
            string subeqpid = pep.T_EQPID;
            string jobtype = "LOAD";
            string slotinfo = string.Empty;

            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            sndMsg = string.Format($"EQTRAYLOADCOMPLETE HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) STATUS=PASS EQPID={eqpid} ");

            if (un.goaltype == (int)EqpGoalType.STK)
            {
                sndMsg += string.Format($"PORT={pep.T_SLOT} JOBTYPE={jobtype} MRNO={pep.C_VEHICLEID} ERRORCODE= ERROMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.SYSWIN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN || un.goaltype == (int)EqpGoalType.SYSWIN_OVEN_t || un.goaltype == (int)EqpGoalType.REFLOW || un.goaltype == (int)EqpGoalType.BUFFER_STK)
            {
                slotinfo = Make_attribute_slotinfo(pep.TRAYID);
                sndMsg += string.Format($"SUBEQPID={subeqpid} TRAYID={pep.TRAYID} LOTID={pep.LOT_NO} JOBTYPE={jobtype} RECIPE= SLOTID={slotinfo} STEPID={pep.STEPID} EXECUTETIME={pep.EXECUTE_TIME} MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");
            }
            else if (un.goaltype == (int)EqpGoalType.HANDLER || un.goaltype == (int)EqpGoalType.HANDLER_STACK)
            {
                var pep1 = Db.Peps.Where(p => p.EXECUTE_TIME == pep.EXECUTE_TIME && p.REAL_TIME == pep.REAL_TIME && p.T_EQPID == pep.T_EQPID && p.WORKTYPE == pep.WORKTYPE).ToList();
                for (int i = 0; i < un.max_col; i++)
                {
                    for (int j = 0; j < pep1.Count() + 1; j++)
                    {
                        if (j == 0)
                        {
                            if (pep.T_SLOT == string.Format($"LOADER{i + 1}"))
                            {
                                sndMsg += string.Format($"LD{i + 1}TRAYID={pep.TRAYID} ");
                                break;
                            }
                        }
                        else
                        {
                            if (pep1[j - 1].T_SLOT == string.Format($"LOADER{i + 1}"))
                            {
                                sndMsg += string.Format($"LD{i + 1}TRAYID={pep1[j - 1].TRAYID} ");
                                break;
                            }
                            if (j == pep1.Count() - 1)
                            {
                                sndMsg += string.Format($"LD{i + 1}TRAYID= ");
                            }
                        }
                    }
                }
                sndMsg += string.Format($"JOBTYPE={jobtype} MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");
            }
            else
            {
                sndMsg = string.Empty;
            }

            return sndMsg;
        }

        private string LoadJobStandBy(pepschedule pep)
        {
            string sndMsg = string.Empty;
            string eqpid = pep.T_EQPID.Split('-')[0];
            string subeqpid = pep.T_EQPID;
            string jobtype = "LOAD";
            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            string loader1 = string.Empty;
            string loader2 = string.Empty;

            var pep2 = Db.Peps.Where(p => p.EXECUTE_TIME == pep.EXECUTE_TIME && p.REAL_TIME == pep.REAL_TIME && p.S_EQPID == pep.S_EQPID && p.WORKTYPE == pep.WORKTYPE
                                                 && p.BATCHID != pep.BATCHID).SingleOrDefault();

            if (pep.T_SLOT == "LOADER1")
            {
                loader1 = pep.TRAYID;

                if (pep2 != null)
                    loader2 = pep2.TRAYID;
            }
            else
            {
                loader2 = pep.TRAYID;

                if (pep2 != null)
                    loader1 = pep2.TRAYID;

            }

            sndMsg = string.Format($"EQTRAYLOADJOBSTANDBY_REP HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) STATUS=PASS EQPID={eqpid} LD1TRAYID={loader1} LD2TRAYID={loader2} ");
            sndMsg += string.Format($"JOBTYPE={jobtype} MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");

            return sndMsg;
        }

        private string ReflowRecipSet(pepschedule pep)
        {
            string sndMsg = string.Empty;
            string eqpid = pep.T_EQPID.Split('-')[0];
            string subeqpid = pep.T_EQPID;
            string jobtype = "LOAD";
            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            sndMsg = string.Format($"REFLOWRECIPESET_REP HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) STATUS=PASS EQPID={eqpid} TRAYID={pep.TRAYID} BATCHJOBID={pep.BATCHID} ");
            sndMsg += string.Format($"MULTIJOBID= JOBTYPE={jobtype} STEPID={pep.STEPID} RUNMODE=ONLINEREMOTE MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG=");

            return sndMsg;
        }

        private string ReflowLoaderInfoSet(pepschedule pep)
        {
            string sndMsg = string.Empty;
            string eqpid = pep.T_EQPID.Split('-')[0];
            string subeqpid = pep.T_EQPID;
            string jobtype = "LOAD";
            string lotid = string.Empty;
            string trayid = string.Empty;
            string qty = string.Empty;

            unit un = Db.Units.Where(p => p.GOALNAME == subeqpid).Single();

            var pep2 = Db.Peps.Where(p => p.EXECUTE_TIME == pep.EXECUTE_TIME && p.REAL_TIME == pep.REAL_TIME && p.S_EQPID == pep.S_EQPID && p.WORKTYPE == pep.WORKTYPE
                                                 && p.BATCHID != pep.BATCHID).SingleOrDefault();

            if (pep.T_SLOT == "AUTO01")
            {
                if (pep2 != null)
                {
                    lotid = string.Format($"{pep.LOT_NO},{pep2.LOT_NO}");
                    trayid = string.Format($"{pep.TRAYID},{pep2.TRAYID}");
                    qty = string.Format($"{pep.QTY},{pep2.QTY}");
                }
                else
                {
                    lotid = pep.LOT_NO;
                    trayid = pep.TRAYID;
                    qty = pep.QTY;
                }
            }
            else
            {
                if (pep2 != null)
                {
                    lotid = string.Format($"{pep2.LOT_NO},{pep.LOT_NO}");
                    trayid = string.Format($"{pep2.TRAYID},{pep.TRAYID}");
                    qty = string.Format($"{pep2.QTY},{pep.QTY}");
                }
                else
                {
                    lotid = pep.LOT_NO;
                    trayid = pep.TRAYID;
                    qty = pep.QTY;
                }
            }

            sndMsg = string.Format($"REFLOWLOADERINFOSET_REP HDR=(KDS1.LH.MPLUS,LH.{eqpid},GARA,TEMP) STATUS=PASS EQPID={eqpid} LOTID={lotid} TRAYID={trayid} TRAYQTY={qty} ");
            sndMsg += string.Format($"BATCHJOBID={pep.BATCHID} MULTIJOBID= JOBTYPE={jobtype} STEPID={pep.STEPID} RUNMODE=ONLINEREMOTE ");

            if (pep.T_SLOT == "AUTO01")
            {
                sndMsg += string.Format($"AUTO01LOTID={pep.LOT_NO} AUTO01TRAY={pep.TRAYID} AUTO01QTY={pep.QTY} AU01STEPID={pep.STEPID} ");

                if (pep2 != null)
                    sndMsg += string.Format($"AUTO02LOTID={pep2.LOT_NO} AUTO02TRAY={pep2.TRAYID} AUTO02QTY={pep2.QTY} AU02STEPID={pep2.STEPID} ");
                else
                    sndMsg += string.Format($"AUTO02LOTID= AUTO02TRAY= AUTO02QTY= AU02STEPID= ");
            }
            else
            {
                if (pep2 != null)
                    sndMsg += string.Format($"AUTO01LOTID={pep2.LOT_NO} AUTO01TRAY={pep2.TRAYID} AUTO01QTY={pep2.QTY} AU01STEPID={pep2.STEPID} ");
                else
                    sndMsg += string.Format($"AUTO01LOTID= AUTO01TRAY= AUTO01QTY= AU01STEPID= ");

                sndMsg += string.Format($"AUTO02LOTID={pep.LOT_NO} AUTO02TRAY={pep.TRAYID} AUTO02QTY={pep.QTY} AU02STEPID={pep.STEPID} ");
            }

            sndMsg += string.Format($"MRNO={pep.C_VEHICLEID} ERRORCODE= ERRORMSG= ");

            return sndMsg;
        }

        private void rbMoveCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (rbMoveCheck.Checked == true)
                panel1.Visible = true;
            else
                panel1.Visible = false;
        }

        private void tmrRefresh_Tick(object sender, EventArgs e)
        {
            tmrRefresh.Enabled = false;

            if (cbRvVehicleID.Text != "" && cbRvVehicleID.Text != null)
                peps_load(cbRvVehicleID.Text);

            try
            {
                tmrRefresh.Enabled = true;
            }
            catch (Exception) { }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (txtRvLogger.Text != "")
            {
                TCFakeData tcf = new TCFakeData { sndMsg = txtRvLogger.Text, vecID = "VEHICLE01" };
                OnTCFakeDataSend?.Invoke(this, tcf);
            }
        }
    }


    public class TCFakeData : EventArgs
    {
        public string sndMsg;
        public string vecID;
    }
}
