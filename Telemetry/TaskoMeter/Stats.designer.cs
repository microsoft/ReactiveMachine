namespace ReactiveMachine.Tools.Taskometer
{
    internal partial class Stats
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
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.Task = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Count = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Average = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Sum = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Start = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.End = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Display = new ReactiveMachine.Tools.Taskometer.Stats.DataGridViewCustomColumn();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.hScrollBar1 = new System.Windows.Forms.HScrollBar();
            this.trackBar1 = new System.Windows.Forms.TrackBar();
            this.button1 = new System.Windows.Forms.Button();
            this.comboBoxFolder = new System.Windows.Forms.ComboBox();
            this.comboBoxAccount = new System.Windows.Forms.ComboBox();
            this.label1 = new System.Windows.Forms.Label();
            this.fldr = new System.Windows.Forms.Label();
            this.lblDeployment = new System.Windows.Forms.Label();
            this.txtDeployment = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).BeginInit();
            this.SuspendLayout();
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToOrderColumns = true;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.ColumnHeadersHeight = 30;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Task,
            this.Count,
            this.Average,
            this.Sum,
            this.Start,
            this.End,
            this.Display});
            this.dataGridView1.Location = new System.Drawing.Point(1, 96);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.Size = new System.Drawing.Size(1060, 517);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.CellMouseDown += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.dataGridView1_CellMouseDown);
            this.dataGridView1.UserDeletingRow += new System.Windows.Forms.DataGridViewRowCancelEventHandler(this.dataGridView1_UserDeletingRow);
            this.dataGridView1.SizeChanged += new System.EventHandler(this.dataGridView1_SizeChanged);
            // 
            // Task
            // 
            this.Task.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.Task.HeaderText = "Name";
            this.Task.Name = "Task";
            this.Task.ReadOnly = true;
            this.Task.Width = 60;
            // 
            // Count
            // 
            this.Count.HeaderText = "Count";
            this.Count.Name = "Count";
            this.Count.ReadOnly = true;
            this.Count.Width = 40;
            // 
            // Average
            // 
            this.Average.HeaderText = "Average";
            this.Average.Name = "Average";
            this.Average.ReadOnly = true;
            this.Average.Width = 50;
            // 
            // Sum
            // 
            this.Sum.HeaderText = "Sum";
            this.Sum.Name = "Sum";
            this.Sum.ReadOnly = true;
            this.Sum.Width = 50;
            // 
            // Start
            // 
            this.Start.HeaderText = "Start";
            this.Start.Name = "Start";
            this.Start.ReadOnly = true;
            this.Start.Width = 50;
            // 
            // End
            // 
            this.End.HeaderText = "End";
            this.End.Name = "End";
            this.End.ReadOnly = true;
            this.End.Width = 50;
            // 
            // Display
            // 
            this.Display.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.Display.HeaderText = "";
            this.Display.Name = "Display";
            this.Display.ReadOnly = true;
            // 
            // textBox2
            // 
            this.textBox2.AcceptsTab = true;
            this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.textBox2.Location = new System.Drawing.Point(1000, 6);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(47, 20);
            this.textBox2.TabIndex = 5;
            this.textBox2.Text = "100";
            this.textBox2.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.textBox2.TextChanged += new System.EventHandler(this.textBox2_TextChanged_1);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(900, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(94, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Display Width [ms]";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // hScrollBar1
            // 
            this.hScrollBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.hScrollBar1.LargeChange = 20000;
            this.hScrollBar1.Location = new System.Drawing.Point(322, 33);
            this.hScrollBar1.Maximum = 100000;
            this.hScrollBar1.Name = "hScrollBar1";
            this.hScrollBar1.Size = new System.Drawing.Size(739, 20);
            this.hScrollBar1.SmallChange = 5000;
            this.hScrollBar1.TabIndex = 6;
            this.hScrollBar1.Value = 80000;
            this.hScrollBar1.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hScrollBar1_Scroll);
            // 
            // trackBar1
            // 
            this.trackBar1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.trackBar1.LargeChange = 10000;
            this.trackBar1.Location = new System.Drawing.Point(322, 5);
            this.trackBar1.Maximum = 100000;
            this.trackBar1.Name = "trackBar1";
            this.trackBar1.Size = new System.Drawing.Size(573, 45);
            this.trackBar1.SmallChange = 1000;
            this.trackBar1.TabIndex = 6;
            this.trackBar1.TabStop = false;
            this.trackBar1.TickStyle = System.Windows.Forms.TickStyle.None;
            this.trackBar1.Value = 100000;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            // 
            // button1
            // 
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.Location = new System.Drawing.Point(260, 5);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(56, 44);
            this.button1.TabIndex = 9;
            this.button1.Text = "Load";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // comboBoxFolder
            // 
            this.comboBoxFolder.FormattingEnabled = true;
            this.comboBoxFolder.Location = new System.Drawing.Point(46, 28);
            this.comboBoxFolder.Name = "comboBoxFolder";
            this.comboBoxFolder.Size = new System.Drawing.Size(208, 21);
            this.comboBoxFolder.TabIndex = 10;
            this.comboBoxFolder.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // comboBoxAccount
            // 
            this.comboBoxAccount.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboBoxAccount.FormattingEnabled = true;
            this.comboBoxAccount.Location = new System.Drawing.Point(64, 6);
            this.comboBoxAccount.Name = "comboBoxAccount";
            this.comboBoxAccount.Size = new System.Drawing.Size(190, 21);
            this.comboBoxAccount.TabIndex = 11;
            this.comboBoxAccount.SelectedIndexChanged += new System.EventHandler(this.comboBox2_SelectedIndexChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(51, 13);
            this.label1.TabIndex = 12;
            this.label1.Text = "container";
            this.label1.Click += new System.EventHandler(this.label1_Click);
            // 
            // fldr
            // 
            this.fldr.AutoSize = true;
            this.fldr.Location = new System.Drawing.Point(7, 31);
            this.fldr.Name = "fldr";
            this.fldr.Size = new System.Drawing.Size(33, 13);
            this.fldr.TabIndex = 13;
            this.fldr.Text = "folder";
            // 
            // lblDeployment
            // 
            this.lblDeployment.AutoSize = true;
            this.lblDeployment.Location = new System.Drawing.Point(7, 60);
            this.lblDeployment.Name = "lblDeployment";
            this.lblDeployment.Size = new System.Drawing.Size(130, 13);
            this.lblDeployment.TabIndex = 14;
            this.lblDeployment.Text = "Process Id (can be empty)";
            // 
            // txtDeployment
            // 
            this.txtDeployment.Location = new System.Drawing.Point(161, 55);
            this.txtDeployment.Name = "txtDeployment";
            this.txtDeployment.Size = new System.Drawing.Size(100, 20);
            this.txtDeployment.TabIndex = 15;
            // 
            // Stats
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1059, 614);
            this.Controls.Add(this.txtDeployment);
            this.Controls.Add(this.lblDeployment);
            this.Controls.Add(this.fldr);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.comboBoxAccount);
            this.Controls.Add(this.comboBoxFolder);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.hScrollBar1);
            this.Controls.Add(this.trackBar1);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.dataGridView1);
            this.Name = "Stats";
            this.Text = "TaskoMeter";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.HScrollBar hScrollBar1;
        private System.Windows.Forms.TrackBar trackBar1;
        private System.Windows.Forms.DataGridViewTextBoxColumn Task;
        private System.Windows.Forms.DataGridViewTextBoxColumn Count;
        private System.Windows.Forms.DataGridViewTextBoxColumn Average;
        private System.Windows.Forms.DataGridViewTextBoxColumn Sum;
        private System.Windows.Forms.DataGridViewTextBoxColumn Start;
        private System.Windows.Forms.DataGridViewTextBoxColumn End;
        private ReactiveMachine.Tools.Taskometer.Stats.DataGridViewCustomColumn Display;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.ComboBox comboBoxFolder;
        private System.Windows.Forms.ComboBox comboBoxAccount;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label fldr;
        private System.Windows.Forms.Label lblDeployment;
        private System.Windows.Forms.TextBox txtDeployment;

    }
}