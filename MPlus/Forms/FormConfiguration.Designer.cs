namespace MPlus.Forms
{
    partial class FormConfiguration
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.dataGridViewController = new System.Windows.Forms.DataGridView();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage4 = new System.Windows.Forms.TabPage();
            this.button1 = new System.Windows.Forms.Button();
            this.propertyGridCommonInfo = new System.Windows.Forms.PropertyGrid();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.txtLogger = new System.Windows.Forms.TextBox();
            this.txtTSTEPID = new System.Windows.Forms.TextBox();
            this.lbTSTEPID = new System.Windows.Forms.Label();
            this.txtSSTEPID = new System.Windows.Forms.TextBox();
            this.lbSSTEPID = new System.Windows.Forms.Label();
            this.txtQTY = new System.Windows.Forms.TextBox();
            this.txtLOTNO = new System.Windows.Forms.TextBox();
            this.lbLOTNO = new System.Windows.Forms.Label();
            this.lbQTY = new System.Windows.Forms.Label();
            this.txtDSTPORT = new System.Windows.Forms.TextBox();
            this.txtSRCPORT = new System.Windows.Forms.TextBox();
            this.cbDSTEQPID = new System.Windows.Forms.ComboBox();
            this.cbSRCEQPID = new System.Windows.Forms.ComboBox();
            this.btnCreate = new System.Windows.Forms.Button();
            this.cbVEHICLEID = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.cbWORKTYPE = new System.Windows.Forms.ComboBox();
            this.rbDSTSTART = new System.Windows.Forms.RadioButton();
            this.rbSRCSTART = new System.Windows.Forms.RadioButton();
            this.txtTRAYID = new System.Windows.Forms.TextBox();
            this.lbTRAYID = new System.Windows.Forms.Label();
            this.cbDSTPORT = new System.Windows.Forms.ComboBox();
            this.lbDSTPORT = new System.Windows.Forms.Label();
            this.cbSRCPORT = new System.Windows.Forms.ComboBox();
            this.lbSRCPORT = new System.Windows.Forms.Label();
            this.lbWORKTYPE = new System.Windows.Forms.Label();
            this.lbDSTEQPID = new System.Windows.Forms.Label();
            this.lbSRCEQPID = new System.Windows.Forms.Label();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.lbSelNum = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.rbDSTSel = new System.Windows.Forms.RadioButton();
            this.rbSRCSel = new System.Windows.Forms.RadioButton();
            this.txtRvLogger = new System.Windows.Forms.TextBox();
            this.btnSend = new System.Windows.Forms.Button();
            this.rbReflowLoaderInfoSet = new System.Windows.Forms.RadioButton();
            this.rbReflowRecipeSet = new System.Windows.Forms.RadioButton();
            this.rbLoadJobStandby = new System.Windows.Forms.RadioButton();
            this.rbLoadComp = new System.Windows.Forms.RadioButton();
            this.rbLoadInfo = new System.Windows.Forms.RadioButton();
            this.rbUnloadComp = new System.Windows.Forms.RadioButton();
            this.rbUnloadInfo = new System.Windows.Forms.RadioButton();
            this.rbTempDown = new System.Windows.Forms.RadioButton();
            this.rbMoveComp = new System.Windows.Forms.RadioButton();
            this.rbMoveInfo = new System.Windows.Forms.RadioButton();
            this.rbMoveCheck = new System.Windows.Forms.RadioButton();
            this.dgvPeps = new System.Windows.Forms.DataGridView();
            this.cbRvVehicleID = new System.Windows.Forms.ComboBox();
            this.label2 = new System.Windows.Forms.Label();
            this.tmrRefresh = new System.Windows.Forms.Timer(this.components);
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewController)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage4.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPeps)).BeginInit();
            this.SuspendLayout();
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.dataGridViewController);
            this.groupBox2.Location = new System.Drawing.Point(12, 15);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.groupBox2.Size = new System.Drawing.Size(605, 110);
            this.groupBox2.TabIndex = 3;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Controller ";
            // 
            // dataGridViewController
            // 
            this.dataGridViewController.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dataGridViewController.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewController.Location = new System.Drawing.Point(6, 25);
            this.dataGridViewController.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dataGridViewController.Name = "dataGridViewController";
            this.dataGridViewController.RowTemplate.Height = 23;
            this.dataGridViewController.Size = new System.Drawing.Size(646, 78);
            this.dataGridViewController.TabIndex = 0;
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPage4);
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 132);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(664, 617);
            this.tabControl1.TabIndex = 6;
            // 
            // tabPage4
            // 
            this.tabPage4.Controls.Add(this.button1);
            this.tabPage4.Controls.Add(this.propertyGridCommonInfo);
            this.tabPage4.Location = new System.Drawing.Point(4, 24);
            this.tabPage4.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPage4.Name = "tabPage4";
            this.tabPage4.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tabPage4.Size = new System.Drawing.Size(656, 589);
            this.tabPage4.TabIndex = 3;
            this.tabPage4.Text = "Common";
            this.tabPage4.UseVisualStyleBackColor = true;
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.Brown;
            this.button1.ForeColor = System.Drawing.SystemColors.Window;
            this.button1.Location = new System.Drawing.Point(558, 524);
            this.button1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(90, 61);
            this.button1.TabIndex = 1;
            this.button1.Text = "SAVE";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // propertyGridCommonInfo
            // 
            this.propertyGridCommonInfo.HelpVisible = false;
            this.propertyGridCommonInfo.Location = new System.Drawing.Point(3, 8);
            this.propertyGridCommonInfo.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.propertyGridCommonInfo.Name = "propertyGridCommonInfo";
            this.propertyGridCommonInfo.Size = new System.Drawing.Size(645, 508);
            this.propertyGridCommonInfo.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.txtLogger);
            this.tabPage1.Controls.Add(this.txtTSTEPID);
            this.tabPage1.Controls.Add(this.lbTSTEPID);
            this.tabPage1.Controls.Add(this.txtSSTEPID);
            this.tabPage1.Controls.Add(this.lbSSTEPID);
            this.tabPage1.Controls.Add(this.txtQTY);
            this.tabPage1.Controls.Add(this.txtLOTNO);
            this.tabPage1.Controls.Add(this.lbLOTNO);
            this.tabPage1.Controls.Add(this.lbQTY);
            this.tabPage1.Controls.Add(this.txtDSTPORT);
            this.tabPage1.Controls.Add(this.txtSRCPORT);
            this.tabPage1.Controls.Add(this.cbDSTEQPID);
            this.tabPage1.Controls.Add(this.cbSRCEQPID);
            this.tabPage1.Controls.Add(this.btnCreate);
            this.tabPage1.Controls.Add(this.cbVEHICLEID);
            this.tabPage1.Controls.Add(this.label1);
            this.tabPage1.Controls.Add(this.cbWORKTYPE);
            this.tabPage1.Controls.Add(this.rbDSTSTART);
            this.tabPage1.Controls.Add(this.rbSRCSTART);
            this.tabPage1.Controls.Add(this.txtTRAYID);
            this.tabPage1.Controls.Add(this.lbTRAYID);
            this.tabPage1.Controls.Add(this.cbDSTPORT);
            this.tabPage1.Controls.Add(this.lbDSTPORT);
            this.tabPage1.Controls.Add(this.cbSRCPORT);
            this.tabPage1.Controls.Add(this.lbSRCPORT);
            this.tabPage1.Controls.Add(this.lbWORKTYPE);
            this.tabPage1.Controls.Add(this.lbDSTEQPID);
            this.tabPage1.Controls.Add(this.lbSRCEQPID);
            this.tabPage1.Location = new System.Drawing.Point(4, 24);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(656, 589);
            this.tabPage1.TabIndex = 4;
            this.tabPage1.Text = "Job Create";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // txtLogger
            // 
            this.txtLogger.Location = new System.Drawing.Point(9, 541);
            this.txtLogger.Name = "txtLogger";
            this.txtLogger.Size = new System.Drawing.Size(486, 23);
            this.txtLogger.TabIndex = 31;
            // 
            // txtTSTEPID
            // 
            this.txtTSTEPID.Location = new System.Drawing.Point(482, 299);
            this.txtTSTEPID.Name = "txtTSTEPID";
            this.txtTSTEPID.Size = new System.Drawing.Size(166, 23);
            this.txtTSTEPID.TabIndex = 30;
            // 
            // lbTSTEPID
            // 
            this.lbTSTEPID.AutoSize = true;
            this.lbTSTEPID.Location = new System.Drawing.Point(413, 302);
            this.lbTSTEPID.Name = "lbTSTEPID";
            this.lbTSTEPID.Size = new System.Drawing.Size(62, 15);
            this.lbTSTEPID.TabIndex = 29;
            this.lbTSTEPID.Text = "T STEPID :";
            // 
            // txtSSTEPID
            // 
            this.txtSSTEPID.Location = new System.Drawing.Point(236, 299);
            this.txtSSTEPID.Name = "txtSSTEPID";
            this.txtSSTEPID.Size = new System.Drawing.Size(166, 23);
            this.txtSSTEPID.TabIndex = 28;
            // 
            // lbSSTEPID
            // 
            this.lbSSTEPID.AutoSize = true;
            this.lbSSTEPID.Location = new System.Drawing.Point(167, 302);
            this.lbSSTEPID.Name = "lbSSTEPID";
            this.lbSSTEPID.Size = new System.Drawing.Size(63, 15);
            this.lbSSTEPID.TabIndex = 27;
            this.lbSSTEPID.Text = "S STEPID :";
            // 
            // txtQTY
            // 
            this.txtQTY.Location = new System.Drawing.Point(48, 299);
            this.txtQTY.Name = "txtQTY";
            this.txtQTY.Size = new System.Drawing.Size(100, 23);
            this.txtQTY.TabIndex = 26;
            // 
            // txtLOTNO
            // 
            this.txtLOTNO.Location = new System.Drawing.Point(86, 244);
            this.txtLOTNO.Name = "txtLOTNO";
            this.txtLOTNO.Size = new System.Drawing.Size(562, 23);
            this.txtLOTNO.TabIndex = 25;
            // 
            // lbLOTNO
            // 
            this.lbLOTNO.AutoSize = true;
            this.lbLOTNO.Location = new System.Drawing.Point(6, 247);
            this.lbLOTNO.Name = "lbLOTNO";
            this.lbLOTNO.Size = new System.Drawing.Size(57, 15);
            this.lbLOTNO.TabIndex = 24;
            this.lbLOTNO.Text = "LOT NO :";
            // 
            // lbQTY
            // 
            this.lbQTY.AutoSize = true;
            this.lbQTY.Location = new System.Drawing.Point(6, 302);
            this.lbQTY.Name = "lbQTY";
            this.lbQTY.Size = new System.Drawing.Size(36, 15);
            this.lbQTY.TabIndex = 22;
            this.lbQTY.Text = "QTY :";
            // 
            // txtDSTPORT
            // 
            this.txtDSTPORT.Location = new System.Drawing.Point(86, 122);
            this.txtDSTPORT.Name = "txtDSTPORT";
            this.txtDSTPORT.Size = new System.Drawing.Size(562, 23);
            this.txtDSTPORT.TabIndex = 21;
            this.txtDSTPORT.Visible = false;
            // 
            // txtSRCPORT
            // 
            this.txtSRCPORT.Location = new System.Drawing.Point(86, 65);
            this.txtSRCPORT.Name = "txtSRCPORT";
            this.txtSRCPORT.Size = new System.Drawing.Size(562, 23);
            this.txtSRCPORT.TabIndex = 20;
            this.txtSRCPORT.Visible = false;
            // 
            // cbDSTEQPID
            // 
            this.cbDSTEQPID.FormattingEnabled = true;
            this.cbDSTEQPID.Location = new System.Drawing.Point(501, 17);
            this.cbDSTEQPID.Name = "cbDSTEQPID";
            this.cbDSTEQPID.Size = new System.Drawing.Size(100, 23);
            this.cbDSTEQPID.TabIndex = 19;
            this.cbDSTEQPID.SelectedIndexChanged += new System.EventHandler(this.cbDSTEQPID_SelectedIndexChanged);
            // 
            // cbSRCEQPID
            // 
            this.cbSRCEQPID.FormattingEnabled = true;
            this.cbSRCEQPID.Location = new System.Drawing.Point(298, 17);
            this.cbSRCEQPID.Name = "cbSRCEQPID";
            this.cbSRCEQPID.Size = new System.Drawing.Size(100, 23);
            this.cbSRCEQPID.TabIndex = 18;
            this.cbSRCEQPID.SelectedIndexChanged += new System.EventHandler(this.cbSRCEQPID_SelectedIndexChanged);
            // 
            // btnCreate
            // 
            this.btnCreate.BackColor = System.Drawing.Color.DimGray;
            this.btnCreate.ForeColor = System.Drawing.SystemColors.Window;
            this.btnCreate.Location = new System.Drawing.Point(511, 521);
            this.btnCreate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnCreate.Name = "btnCreate";
            this.btnCreate.Size = new System.Drawing.Size(90, 61);
            this.btnCreate.TabIndex = 17;
            this.btnCreate.Text = "CREATE";
            this.btnCreate.UseVisualStyleBackColor = false;
            this.btnCreate.Click += new System.EventHandler(this.btnCreate_Click);
            // 
            // cbVEHICLEID
            // 
            this.cbVEHICLEID.Enabled = false;
            this.cbVEHICLEID.FormattingEnabled = true;
            this.cbVEHICLEID.Items.AddRange(new object[] {
            "VEHICLE01",
            "VEHICLE02",
            "VEHICLE03",
            "VEHICLE04"});
            this.cbVEHICLEID.Location = new System.Drawing.Point(298, 356);
            this.cbVEHICLEID.Name = "cbVEHICLEID";
            this.cbVEHICLEID.Size = new System.Drawing.Size(100, 23);
            this.cbVEHICLEID.TabIndex = 16;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(216, 359);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(76, 15);
            this.label1.TabIndex = 15;
            this.label1.Text = "VEHICLE ID :";
            // 
            // cbWORKTYPE
            // 
            this.cbWORKTYPE.FormattingEnabled = true;
            this.cbWORKTYPE.Items.AddRange(new object[] {
            "O",
            "I",
            "OI",
            "TI",
            "TO",
            "EI",
            "EO"});
            this.cbWORKTYPE.Location = new System.Drawing.Point(86, 356);
            this.cbWORKTYPE.Name = "cbWORKTYPE";
            this.cbWORKTYPE.Size = new System.Drawing.Size(100, 23);
            this.cbWORKTYPE.TabIndex = 14;
            this.cbWORKTYPE.SelectedIndexChanged += new System.EventHandler(this.cbWORKTYPE_SelectedIndexChanged);
            // 
            // rbDSTSTART
            // 
            this.rbDSTSTART.AutoSize = true;
            this.rbDSTSTART.Location = new System.Drawing.Point(110, 18);
            this.rbDSTSTART.Name = "rbDSTSTART";
            this.rbDSTSTART.Size = new System.Drawing.Size(85, 19);
            this.rbDSTSTART.TabIndex = 13;
            this.rbDSTSTART.Text = "DST START";
            this.rbDSTSTART.UseVisualStyleBackColor = true;
            this.rbDSTSTART.CheckedChanged += new System.EventHandler(this.rbDSTSTART_CheckedChanged);
            // 
            // rbSRCSTART
            // 
            this.rbSRCSTART.AutoSize = true;
            this.rbSRCSTART.Checked = true;
            this.rbSRCSTART.Location = new System.Drawing.Point(9, 18);
            this.rbSRCSTART.Name = "rbSRCSTART";
            this.rbSRCSTART.Size = new System.Drawing.Size(85, 19);
            this.rbSRCSTART.TabIndex = 12;
            this.rbSRCSTART.TabStop = true;
            this.rbSRCSTART.Text = "SRC START";
            this.rbSRCSTART.UseVisualStyleBackColor = true;
            this.rbSRCSTART.CheckedChanged += new System.EventHandler(this.rbSRCSTART_CheckedChanged);
            // 
            // txtTRAYID
            // 
            this.txtTRAYID.Location = new System.Drawing.Point(86, 181);
            this.txtTRAYID.Name = "txtTRAYID";
            this.txtTRAYID.Size = new System.Drawing.Size(562, 23);
            this.txtTRAYID.TabIndex = 11;
            // 
            // lbTRAYID
            // 
            this.lbTRAYID.AutoSize = true;
            this.lbTRAYID.Location = new System.Drawing.Point(6, 184);
            this.lbTRAYID.Name = "lbTRAYID";
            this.lbTRAYID.Size = new System.Drawing.Size(58, 15);
            this.lbTRAYID.TabIndex = 10;
            this.lbTRAYID.Text = "TRAY ID :";
            // 
            // cbDSTPORT
            // 
            this.cbDSTPORT.FormattingEnabled = true;
            this.cbDSTPORT.Location = new System.Drawing.Point(86, 122);
            this.cbDSTPORT.Name = "cbDSTPORT";
            this.cbDSTPORT.Size = new System.Drawing.Size(100, 23);
            this.cbDSTPORT.TabIndex = 9;
            // 
            // lbDSTPORT
            // 
            this.lbDSTPORT.AutoSize = true;
            this.lbDSTPORT.Location = new System.Drawing.Point(6, 125);
            this.lbDSTPORT.Name = "lbDSTPORT";
            this.lbDSTPORT.Size = new System.Drawing.Size(69, 15);
            this.lbDSTPORT.TabIndex = 8;
            this.lbDSTPORT.Text = "DST PORT :";
            // 
            // cbSRCPORT
            // 
            this.cbSRCPORT.FormattingEnabled = true;
            this.cbSRCPORT.Location = new System.Drawing.Point(86, 65);
            this.cbSRCPORT.Name = "cbSRCPORT";
            this.cbSRCPORT.Size = new System.Drawing.Size(100, 23);
            this.cbSRCPORT.TabIndex = 7;
            // 
            // lbSRCPORT
            // 
            this.lbSRCPORT.AutoSize = true;
            this.lbSRCPORT.Location = new System.Drawing.Point(6, 68);
            this.lbSRCPORT.Name = "lbSRCPORT";
            this.lbSRCPORT.Size = new System.Drawing.Size(69, 15);
            this.lbSRCPORT.TabIndex = 6;
            this.lbSRCPORT.Text = "SRC PORT :";
            // 
            // lbWORKTYPE
            // 
            this.lbWORKTYPE.AutoSize = true;
            this.lbWORKTYPE.Location = new System.Drawing.Point(6, 359);
            this.lbWORKTYPE.Name = "lbWORKTYPE";
            this.lbWORKTYPE.Size = new System.Drawing.Size(74, 15);
            this.lbWORKTYPE.TabIndex = 4;
            this.lbWORKTYPE.Text = "WORKTYPE :";
            // 
            // lbDSTEQPID
            // 
            this.lbDSTEQPID.AutoSize = true;
            this.lbDSTEQPID.Location = new System.Drawing.Point(421, 20);
            this.lbDSTEQPID.Name = "lbDSTEQPID";
            this.lbDSTEQPID.Size = new System.Drawing.Size(74, 15);
            this.lbDSTEQPID.TabIndex = 2;
            this.lbDSTEQPID.Text = "DST EQPID :";
            // 
            // lbSRCEQPID
            // 
            this.lbSRCEQPID.AutoSize = true;
            this.lbSRCEQPID.Location = new System.Drawing.Point(218, 20);
            this.lbSRCEQPID.Name = "lbSRCEQPID";
            this.lbSRCEQPID.Size = new System.Drawing.Size(74, 15);
            this.lbSRCEQPID.TabIndex = 0;
            this.lbSRCEQPID.Text = "SRC EQPID :";
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.lbSelNum);
            this.tabPage2.Controls.Add(this.label3);
            this.tabPage2.Controls.Add(this.button2);
            this.tabPage2.Controls.Add(this.panel1);
            this.tabPage2.Controls.Add(this.txtRvLogger);
            this.tabPage2.Controls.Add(this.btnSend);
            this.tabPage2.Controls.Add(this.rbReflowLoaderInfoSet);
            this.tabPage2.Controls.Add(this.rbReflowRecipeSet);
            this.tabPage2.Controls.Add(this.rbLoadJobStandby);
            this.tabPage2.Controls.Add(this.rbLoadComp);
            this.tabPage2.Controls.Add(this.rbLoadInfo);
            this.tabPage2.Controls.Add(this.rbUnloadComp);
            this.tabPage2.Controls.Add(this.rbUnloadInfo);
            this.tabPage2.Controls.Add(this.rbTempDown);
            this.tabPage2.Controls.Add(this.rbMoveComp);
            this.tabPage2.Controls.Add(this.rbMoveInfo);
            this.tabPage2.Controls.Add(this.rbMoveCheck);
            this.tabPage2.Controls.Add(this.dgvPeps);
            this.tabPage2.Controls.Add(this.cbRvVehicleID);
            this.tabPage2.Controls.Add(this.label2);
            this.tabPage2.Location = new System.Drawing.Point(4, 24);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(656, 589);
            this.tabPage2.TabIndex = 5;
            this.tabPage2.Text = "RV Message Send(TC->Mplus)";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // lbSelNum
            // 
            this.lbSelNum.AutoSize = true;
            this.lbSelNum.Location = new System.Drawing.Point(299, 25);
            this.lbSelNum.Name = "lbSelNum";
            this.lbSelNum.Size = new System.Drawing.Size(0, 15);
            this.lbSelNum.TabIndex = 37;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(211, 24);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(83, 15);
            this.label3.TabIndex = 36;
            this.label3.Text = "Selecte Num :";
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.Color.DimGray;
            this.button2.ForeColor = System.Drawing.SystemColors.Window;
            this.button2.Location = new System.Drawing.Point(304, 392);
            this.button2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(90, 61);
            this.button2.TabIndex = 35;
            this.button2.Text = "SEND";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.panel1.Controls.Add(this.rbDSTSel);
            this.panel1.Controls.Add(this.rbSRCSel);
            this.panel1.Location = new System.Drawing.Point(9, 341);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(639, 44);
            this.panel1.TabIndex = 34;
            // 
            // rbDSTSel
            // 
            this.rbDSTSel.Location = new System.Drawing.Point(59, 7);
            this.rbDSTSel.Name = "rbDSTSel";
            this.rbDSTSel.Size = new System.Drawing.Size(47, 19);
            this.rbDSTSel.TabIndex = 22;
            this.rbDSTSel.Text = "DST";
            this.rbDSTSel.UseVisualStyleBackColor = true;
            // 
            // rbSRCSel
            // 
            this.rbSRCSel.AutoSize = true;
            this.rbSRCSel.Checked = true;
            this.rbSRCSel.Location = new System.Drawing.Point(6, 7);
            this.rbSRCSel.Name = "rbSRCSel";
            this.rbSRCSel.Size = new System.Drawing.Size(47, 19);
            this.rbSRCSel.TabIndex = 21;
            this.rbSRCSel.TabStop = true;
            this.rbSRCSel.Text = "SRC";
            this.rbSRCSel.UseVisualStyleBackColor = true;
            // 
            // txtRvLogger
            // 
            this.txtRvLogger.Location = new System.Drawing.Point(9, 541);
            this.txtRvLogger.Name = "txtRvLogger";
            this.txtRvLogger.Size = new System.Drawing.Size(486, 23);
            this.txtRvLogger.TabIndex = 33;
            // 
            // btnSend
            // 
            this.btnSend.BackColor = System.Drawing.Color.DimGray;
            this.btnSend.ForeColor = System.Drawing.SystemColors.Window;
            this.btnSend.Location = new System.Drawing.Point(511, 521);
            this.btnSend.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnSend.Name = "btnSend";
            this.btnSend.Size = new System.Drawing.Size(90, 61);
            this.btnSend.TabIndex = 32;
            this.btnSend.Text = "SEND";
            this.btnSend.UseVisualStyleBackColor = false;
            this.btnSend.Click += new System.EventHandler(this.btnSend_Click);
            // 
            // rbReflowLoaderInfoSet
            // 
            this.rbReflowLoaderInfoSet.AutoSize = true;
            this.rbReflowLoaderInfoSet.Location = new System.Drawing.Point(225, 302);
            this.rbReflowLoaderInfoSet.Name = "rbReflowLoaderInfoSet";
            this.rbReflowLoaderInfoSet.Size = new System.Drawing.Size(186, 19);
            this.rbReflowLoaderInfoSet.TabIndex = 30;
            this.rbReflowLoaderInfoSet.Text = "REFLOWLOADERINFOSET_REP";
            this.rbReflowLoaderInfoSet.UseVisualStyleBackColor = true;
            // 
            // rbReflowRecipeSet
            // 
            this.rbReflowRecipeSet.AutoSize = true;
            this.rbReflowRecipeSet.Location = new System.Drawing.Point(15, 302);
            this.rbReflowRecipeSet.Name = "rbReflowRecipeSet";
            this.rbReflowRecipeSet.Size = new System.Drawing.Size(151, 19);
            this.rbReflowRecipeSet.TabIndex = 29;
            this.rbReflowRecipeSet.Text = "REFLOWRECIPESET_REP";
            this.rbReflowRecipeSet.UseVisualStyleBackColor = true;
            // 
            // rbLoadJobStandby
            // 
            this.rbLoadJobStandby.AutoSize = true;
            this.rbLoadJobStandby.Location = new System.Drawing.Point(435, 267);
            this.rbLoadJobStandby.Name = "rbLoadJobStandby";
            this.rbLoadJobStandby.Size = new System.Drawing.Size(198, 19);
            this.rbLoadJobStandby.TabIndex = 28;
            this.rbLoadJobStandby.Text = "EQTRAYLOADJOBSTANDBY_REP";
            this.rbLoadJobStandby.UseVisualStyleBackColor = true;
            // 
            // rbLoadComp
            // 
            this.rbLoadComp.AutoSize = true;
            this.rbLoadComp.Location = new System.Drawing.Point(225, 267);
            this.rbLoadComp.Name = "rbLoadComp";
            this.rbLoadComp.Size = new System.Drawing.Size(159, 19);
            this.rbLoadComp.TabIndex = 27;
            this.rbLoadComp.Text = "EQTRAYLOADCOMPLETE";
            this.rbLoadComp.UseVisualStyleBackColor = true;
            // 
            // rbLoadInfo
            // 
            this.rbLoadInfo.AutoSize = true;
            this.rbLoadInfo.Location = new System.Drawing.Point(15, 267);
            this.rbLoadInfo.Name = "rbLoadInfo";
            this.rbLoadInfo.Size = new System.Drawing.Size(152, 19);
            this.rbLoadInfo.TabIndex = 26;
            this.rbLoadInfo.Text = "EQTRAYLOADINFO_REP";
            this.rbLoadInfo.UseVisualStyleBackColor = true;
            // 
            // rbUnloadComp
            // 
            this.rbUnloadComp.AutoSize = true;
            this.rbUnloadComp.Location = new System.Drawing.Point(435, 232);
            this.rbUnloadComp.Name = "rbUnloadComp";
            this.rbUnloadComp.Size = new System.Drawing.Size(176, 19);
            this.rbUnloadComp.TabIndex = 25;
            this.rbUnloadComp.Text = "EQTRAYUNLOADCOMPLETE";
            this.rbUnloadComp.UseVisualStyleBackColor = true;
            // 
            // rbUnloadInfo
            // 
            this.rbUnloadInfo.AutoSize = true;
            this.rbUnloadInfo.Location = new System.Drawing.Point(225, 232);
            this.rbUnloadInfo.Name = "rbUnloadInfo";
            this.rbUnloadInfo.Size = new System.Drawing.Size(169, 19);
            this.rbUnloadInfo.TabIndex = 24;
            this.rbUnloadInfo.Text = "EQTRAYUNLOADINFO_REP";
            this.rbUnloadInfo.UseVisualStyleBackColor = true;
            // 
            // rbTempDown
            // 
            this.rbTempDown.AutoSize = true;
            this.rbTempDown.Location = new System.Drawing.Point(15, 232);
            this.rbTempDown.Name = "rbTempDown";
            this.rbTempDown.Size = new System.Drawing.Size(155, 19);
            this.rbTempDown.TabIndex = 23;
            this.rbTempDown.Text = "EQTEMPDOWNREQ_REP";
            this.rbTempDown.UseVisualStyleBackColor = true;
            // 
            // rbMoveComp
            // 
            this.rbMoveComp.AutoSize = true;
            this.rbMoveComp.Location = new System.Drawing.Point(435, 197);
            this.rbMoveComp.Name = "rbMoveComp";
            this.rbMoveComp.Size = new System.Drawing.Size(137, 19);
            this.rbMoveComp.TabIndex = 22;
            this.rbMoveComp.Text = "EQTRAYMOVECOMP";
            this.rbMoveComp.UseVisualStyleBackColor = true;
            // 
            // rbMoveInfo
            // 
            this.rbMoveInfo.AutoSize = true;
            this.rbMoveInfo.Location = new System.Drawing.Point(225, 197);
            this.rbMoveInfo.Name = "rbMoveInfo";
            this.rbMoveInfo.Size = new System.Drawing.Size(154, 19);
            this.rbMoveInfo.TabIndex = 21;
            this.rbMoveInfo.Text = "EQTRAYMOVEINFO_REP";
            this.rbMoveInfo.UseVisualStyleBackColor = true;
            // 
            // rbMoveCheck
            // 
            this.rbMoveCheck.AutoSize = true;
            this.rbMoveCheck.Checked = true;
            this.rbMoveCheck.Location = new System.Drawing.Point(15, 197);
            this.rbMoveCheck.Name = "rbMoveCheck";
            this.rbMoveCheck.Size = new System.Drawing.Size(165, 19);
            this.rbMoveCheck.TabIndex = 20;
            this.rbMoveCheck.TabStop = true;
            this.rbMoveCheck.Text = "EQTRAYMOVECHECK_REP";
            this.rbMoveCheck.UseVisualStyleBackColor = true;
            this.rbMoveCheck.CheckedChanged += new System.EventHandler(this.rbMoveCheck_CheckedChanged);
            // 
            // dgvPeps
            // 
            this.dgvPeps.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.AllCells;
            this.dgvPeps.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvPeps.Location = new System.Drawing.Point(4, 61);
            this.dgvPeps.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dgvPeps.Name = "dgvPeps";
            this.dgvPeps.RowTemplate.Height = 23;
            this.dgvPeps.Size = new System.Drawing.Size(646, 118);
            this.dgvPeps.TabIndex = 19;
            this.dgvPeps.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvPeps_CellClick);
            // 
            // cbRvVehicleID
            // 
            this.cbRvVehicleID.FormattingEnabled = true;
            this.cbRvVehicleID.Items.AddRange(new object[] {
            "VEHICLE01",
            "VEHICLE02",
            "VEHICLE03",
            "VEHICLE04"});
            this.cbRvVehicleID.Location = new System.Drawing.Point(94, 21);
            this.cbRvVehicleID.Name = "cbRvVehicleID";
            this.cbRvVehicleID.Size = new System.Drawing.Size(100, 23);
            this.cbRvVehicleID.TabIndex = 18;
            this.cbRvVehicleID.SelectedIndexChanged += new System.EventHandler(this.cbRvVehicleID_SelectedIndexChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 24);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(76, 15);
            this.label2.TabIndex = 17;
            this.label2.Text = "VEHICLE ID :";
            // 
            // tmrRefresh
            // 
            this.tmrRefresh.Enabled = true;
            this.tmrRefresh.Tick += new System.EventHandler(this.tmrRefresh_Tick);
            // 
            // FormConfiguration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(680, 756);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.groupBox2);
            this.Font = new System.Drawing.Font("맑은 고딕", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(129)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "FormConfiguration";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Configuration";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormConfiguration_FormClosing);
            this.Load += new System.EventHandler(this.FormConfiguration_Load);
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewController)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage4.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPeps)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.DataGridView dataGridViewController;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage4;
        private System.Windows.Forms.PropertyGrid propertyGridCommonInfo;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.Button btnCreate;
        private System.Windows.Forms.ComboBox cbVEHICLEID;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbWORKTYPE;
        private System.Windows.Forms.RadioButton rbDSTSTART;
        private System.Windows.Forms.RadioButton rbSRCSTART;
        private System.Windows.Forms.TextBox txtTRAYID;
        private System.Windows.Forms.Label lbTRAYID;
        private System.Windows.Forms.ComboBox cbDSTPORT;
        private System.Windows.Forms.Label lbDSTPORT;
        private System.Windows.Forms.ComboBox cbSRCPORT;
        private System.Windows.Forms.Label lbSRCPORT;
        private System.Windows.Forms.Label lbWORKTYPE;
        private System.Windows.Forms.Label lbDSTEQPID;
        private System.Windows.Forms.Label lbSRCEQPID;
        private System.Windows.Forms.ComboBox cbDSTEQPID;
        private System.Windows.Forms.ComboBox cbSRCEQPID;
        private System.Windows.Forms.TextBox txtDSTPORT;
        private System.Windows.Forms.TextBox txtSRCPORT;
        private System.Windows.Forms.TextBox txtTSTEPID;
        private System.Windows.Forms.Label lbTSTEPID;
        private System.Windows.Forms.TextBox txtSSTEPID;
        private System.Windows.Forms.Label lbSSTEPID;
        private System.Windows.Forms.TextBox txtQTY;
        private System.Windows.Forms.TextBox txtLOTNO;
        private System.Windows.Forms.Label lbLOTNO;
        private System.Windows.Forms.Label lbQTY;
        private System.Windows.Forms.TextBox txtLogger;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.ComboBox cbRvVehicleID;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.DataGridView dgvPeps;
        private System.Windows.Forms.TextBox txtRvLogger;
        private System.Windows.Forms.Button btnSend;
        private System.Windows.Forms.RadioButton rbReflowLoaderInfoSet;
        private System.Windows.Forms.RadioButton rbReflowRecipeSet;
        private System.Windows.Forms.RadioButton rbLoadJobStandby;
        private System.Windows.Forms.RadioButton rbLoadComp;
        private System.Windows.Forms.RadioButton rbLoadInfo;
        private System.Windows.Forms.RadioButton rbUnloadComp;
        private System.Windows.Forms.RadioButton rbUnloadInfo;
        private System.Windows.Forms.RadioButton rbTempDown;
        private System.Windows.Forms.RadioButton rbMoveComp;
        private System.Windows.Forms.RadioButton rbMoveInfo;
        private System.Windows.Forms.RadioButton rbMoveCheck;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton rbDSTSel;
        private System.Windows.Forms.RadioButton rbSRCSel;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Timer tmrRefresh;
        private System.Windows.Forms.Label lbSelNum;
        private System.Windows.Forms.Label label3;
    }
}