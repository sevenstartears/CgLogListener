namespace CgLogListener
{
    partial class FormMain
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.notifyIconContextMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolOpen = new System.Windows.Forms.ToolStripMenuItem();
            this.toolMinsize = new System.Windows.Forms.ToolStripMenuItem();
            this.toolSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolExit = new System.Windows.Forms.ToolStripMenuItem();
            this.panelHeader = new System.Windows.Forms.Panel();
            this.lblHeaderSubtitle = new System.Windows.Forms.Label();
            this.lblHeaderTitle = new System.Windows.Forms.Label();
            this.panelToolbar = new System.Windows.Forms.Panel();
            this.btnClearLogs = new System.Windows.Forms.Button();
            this.btnOpenSettings = new System.Windows.Forms.Button();
            this.lblDedupValue = new System.Windows.Forms.Label();
            this.lblDedupLabel = new System.Windows.Forms.Label();
            this.lblStatusValue = new System.Windows.Forms.Label();
            this.lblStatusLabel = new System.Windows.Forms.Label();
            this.txtCgLogPath = new System.Windows.Forms.TextBox();
            this.lblLogPath = new System.Windows.Forms.Label();
            this.panelLogHost = new System.Windows.Forms.Panel();
            this.lblEmptyState = new System.Windows.Forms.Label();
            this.flowLogs = new CgLogListener.BufferedFlowLayoutPanel();
            this.notifyIconContextMenu.SuspendLayout();
            this.panelHeader.SuspendLayout();
            this.panelToolbar.SuspendLayout();
            this.panelLogHost.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon
            // 
            this.notifyIcon.BalloonTipTitle = "BlueCG ログビューア";
            this.notifyIcon.ContextMenuStrip = this.notifyIconContextMenu;
            this.notifyIcon.Text = "BlueCG ログビューア";
            this.notifyIcon.Visible = true;
            this.notifyIcon.DoubleClick += new System.EventHandler(this.NotifyIcon_DoubleClick);
            // 
            // notifyIconContextMenu
            // 
            this.notifyIconContextMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.notifyIconContextMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolOpen,
            this.toolMinsize,
            this.toolSep1,
            this.toolExit});
            this.notifyIconContextMenu.Name = "notifyIconContextMenu";
            this.notifyIconContextMenu.ShowImageMargin = false;
            this.notifyIconContextMenu.Size = new System.Drawing.Size(141, 76);
            // 
            // toolOpen
            // 
            this.toolOpen.Name = "toolOpen";
            this.toolOpen.Size = new System.Drawing.Size(140, 22);
            this.toolOpen.Text = "表示";
            this.toolOpen.Click += new System.EventHandler(this.ToolOpen_Click);
            // 
            // toolMinsize
            // 
            this.toolMinsize.Name = "toolMinsize";
            this.toolMinsize.Size = new System.Drawing.Size(140, 22);
            this.toolMinsize.Text = "最小化";
            this.toolMinsize.Click += new System.EventHandler(this.ToolMinsize_Click);
            // 
            // toolSep1
            // 
            this.toolSep1.Name = "toolSep1";
            this.toolSep1.Size = new System.Drawing.Size(137, 6);
            // 
            // toolExit
            // 
            this.toolExit.Name = "toolExit";
            this.toolExit.Size = new System.Drawing.Size(140, 22);
            this.toolExit.Text = "終了";
            this.toolExit.Click += new System.EventHandler(this.ToolExit_Click);
            // 
            // panelHeader
            // 
            this.panelHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(18)))), ((int)(((byte)(93)))), ((int)(((byte)(156)))));
            this.panelHeader.Controls.Add(this.lblHeaderSubtitle);
            this.panelHeader.Controls.Add(this.lblHeaderTitle);
            this.panelHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelHeader.Location = new System.Drawing.Point(0, 0);
            this.panelHeader.Name = "panelHeader";
            this.panelHeader.Padding = new System.Windows.Forms.Padding(24, 20, 24, 14);
            this.panelHeader.Size = new System.Drawing.Size(1084, 112);
            this.panelHeader.TabIndex = 0;
            // 
            // lblHeaderSubtitle
            // 
            this.lblHeaderSubtitle.AutoSize = true;
            this.lblHeaderSubtitle.Font = new System.Drawing.Font("Yu Gothic UI", 10F);
            this.lblHeaderSubtitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(220)))), ((int)(((byte)(238)))), ((int)(((byte)(255)))));
            this.lblHeaderSubtitle.Location = new System.Drawing.Point(28, 64);
            this.lblHeaderSubtitle.Name = "lblHeaderSubtitle";
            this.lblHeaderSubtitle.Size = new System.Drawing.Size(471, 19);
            this.lblHeaderSubtitle.TabIndex = 1;
            this.lblHeaderSubtitle.Text = "リアルタイム表示をメインにしつつ、通知や外部送信はサブ機能として扱います。";
            // 
            // lblHeaderTitle
            // 
            this.lblHeaderTitle.AutoSize = true;
            this.lblHeaderTitle.Font = new System.Drawing.Font("Yu Gothic UI Semibold", 20F, System.Drawing.FontStyle.Bold);
            this.lblHeaderTitle.ForeColor = System.Drawing.Color.White;
            this.lblHeaderTitle.Location = new System.Drawing.Point(24, 20);
            this.lblHeaderTitle.Name = "lblHeaderTitle";
            this.lblHeaderTitle.Size = new System.Drawing.Size(246, 37);
            this.lblHeaderTitle.TabIndex = 0;
            this.lblHeaderTitle.Text = "BlueCG ログビューア";
            // 
            // panelToolbar
            // 
            this.panelToolbar.BackColor = System.Drawing.Color.White;
            this.panelToolbar.Controls.Add(this.btnClearLogs);
            this.panelToolbar.Controls.Add(this.btnOpenSettings);
            this.panelToolbar.Controls.Add(this.lblDedupValue);
            this.panelToolbar.Controls.Add(this.lblDedupLabel);
            this.panelToolbar.Controls.Add(this.lblStatusValue);
            this.panelToolbar.Controls.Add(this.lblStatusLabel);
            this.panelToolbar.Controls.Add(this.txtCgLogPath);
            this.panelToolbar.Controls.Add(this.lblLogPath);
            this.panelToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelToolbar.Location = new System.Drawing.Point(0, 112);
            this.panelToolbar.Name = "panelToolbar";
            this.panelToolbar.Padding = new System.Windows.Forms.Padding(24, 18, 24, 14);
            this.panelToolbar.Size = new System.Drawing.Size(1084, 122);
            this.panelToolbar.TabIndex = 1;
            // 
            // btnClearLogs
            // 
            this.btnClearLogs.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClearLogs.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClearLogs.Location = new System.Drawing.Point(900, 65);
            this.btnClearLogs.Name = "btnClearLogs";
            this.btnClearLogs.Size = new System.Drawing.Size(160, 34);
            this.btnClearLogs.TabIndex = 7;
            this.btnClearLogs.Text = "表示ログをクリア";
            this.btnClearLogs.UseVisualStyleBackColor = true;
            this.btnClearLogs.Click += new System.EventHandler(this.BtnClearLogs_Click);
            // 
            // btnOpenSettings
            // 
            this.btnOpenSettings.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOpenSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnOpenSettings.Location = new System.Drawing.Point(900, 24);
            this.btnOpenSettings.Name = "btnOpenSettings";
            this.btnOpenSettings.Size = new System.Drawing.Size(160, 34);
            this.btnOpenSettings.TabIndex = 6;
            this.btnOpenSettings.Text = "表示と通知の設定";
            this.btnOpenSettings.UseVisualStyleBackColor = true;
            this.btnOpenSettings.Click += new System.EventHandler(this.BtnOpenSettings_Click);
            // 
            // lblDedupValue
            // 
            this.lblDedupValue.AutoSize = true;
            this.lblDedupValue.Font = new System.Drawing.Font("Yu Gothic UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblDedupValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(84)))), ((int)(((byte)(140)))));
            this.lblDedupValue.Location = new System.Drawing.Point(673, 32);
            this.lblDedupValue.Name = "lblDedupValue";
            this.lblDedupValue.Size = new System.Drawing.Size(34, 15);
            this.lblDedupValue.TabIndex = 5;
            this.lblDedupValue.Text = "2 秒";
            // 
            // lblDedupLabel
            // 
            this.lblDedupLabel.AutoSize = true;
            this.lblDedupLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(102)))), ((int)(((byte)(114)))));
            this.lblDedupLabel.Location = new System.Drawing.Point(571, 32);
            this.lblDedupLabel.Name = "lblDedupLabel";
            this.lblDedupLabel.Size = new System.Drawing.Size(79, 15);
            this.lblDedupLabel.TabIndex = 4;
            this.lblDedupLabel.Text = "まとめ秒数:";
            // 
            // lblStatusValue
            // 
            this.lblStatusValue.AutoSize = true;
            this.lblStatusValue.Font = new System.Drawing.Font("Yu Gothic UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblStatusValue.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(84)))), ((int)(((byte)(140)))));
            this.lblStatusValue.Location = new System.Drawing.Point(99, 32);
            this.lblStatusValue.Name = "lblStatusValue";
            this.lblStatusValue.Size = new System.Drawing.Size(91, 15);
            this.lblStatusValue.TabIndex = 3;
            this.lblStatusValue.Text = "監視フォルダ未設定";
            // 
            // lblStatusLabel
            // 
            this.lblStatusLabel.AutoSize = true;
            this.lblStatusLabel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(102)))), ((int)(((byte)(114)))));
            this.lblStatusLabel.Location = new System.Drawing.Point(28, 32);
            this.lblStatusLabel.Name = "lblStatusLabel";
            this.lblStatusLabel.Size = new System.Drawing.Size(43, 15);
            this.lblStatusLabel.TabIndex = 2;
            this.lblStatusLabel.Text = "状態:";
            // 
            // txtCgLogPath
            // 
            this.txtCgLogPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCgLogPath.BackColor = System.Drawing.Color.White;
            this.txtCgLogPath.Location = new System.Drawing.Point(102, 69);
            this.txtCgLogPath.Name = "txtCgLogPath";
            this.txtCgLogPath.ReadOnly = true;
            this.txtCgLogPath.Size = new System.Drawing.Size(786, 23);
            this.txtCgLogPath.TabIndex = 1;
            // 
            // lblLogPath
            // 
            this.lblLogPath.AutoSize = true;
            this.lblLogPath.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(102)))), ((int)(((byte)(114)))));
            this.lblLogPath.Location = new System.Drawing.Point(28, 73);
            this.lblLogPath.Name = "lblLogPath";
            this.lblLogPath.Size = new System.Drawing.Size(67, 15);
            this.lblLogPath.TabIndex = 0;
            this.lblLogPath.Text = "ゲームフォルダ:";
            // 
            // panelLogHost
            // 
            this.panelLogHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(246)))), ((int)(((byte)(251)))));
            this.panelLogHost.Controls.Add(this.lblEmptyState);
            this.panelLogHost.Controls.Add(this.flowLogs);
            this.panelLogHost.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelLogHost.Location = new System.Drawing.Point(0, 234);
            this.panelLogHost.Name = "panelLogHost";
            this.panelLogHost.Padding = new System.Windows.Forms.Padding(24, 18, 24, 18);
            this.panelLogHost.Size = new System.Drawing.Size(1084, 487);
            this.panelLogHost.TabIndex = 2;
            // 
            // lblEmptyState
            // 
            this.lblEmptyState.AutoSize = true;
            this.lblEmptyState.BackColor = System.Drawing.Color.Transparent;
            this.lblEmptyState.Font = new System.Drawing.Font("Yu Gothic UI", 12F);
            this.lblEmptyState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(91)))), ((int)(((byte)(102)))), ((int)(((byte)(114)))));
            this.lblEmptyState.Location = new System.Drawing.Point(28, 22);
            this.lblEmptyState.Name = "lblEmptyState";
            this.lblEmptyState.Size = new System.Drawing.Size(523, 21);
            this.lblEmptyState.TabIndex = 1;
            this.lblEmptyState.Text = "ログ表示を開始するには、右上の「表示と通知の設定」からゲームフォルダを指定してください。";
            // 
            // flowLogs
            // 
            this.flowLogs.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLogs.Location = new System.Drawing.Point(24, 18);
            this.flowLogs.Name = "flowLogs";
            this.flowLogs.Padding = new System.Windows.Forms.Padding(0, 36, 0, 0);
            this.flowLogs.Size = new System.Drawing.Size(1036, 467);
            this.flowLogs.TabIndex = 0;
            this.flowLogs.SizeChanged += new System.EventHandler(this.FlowLogs_SizeChanged);
            // 
            // FormMain
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(246)))), ((int)(((byte)(251)))));
            this.ClientSize = new System.Drawing.Size(1084, 721);
            this.Controls.Add(this.panelLogHost);
            this.Controls.Add(this.panelToolbar);
            this.Controls.Add(this.panelHeader);
            this.Font = new System.Drawing.Font("Yu Gothic UI", 9F);
            this.MinimumSize = new System.Drawing.Size(980, 680);
            this.Name = "FormMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "BlueCG ログビューア";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.Resize += new System.EventHandler(this.FormMain_Resize);
            this.notifyIconContextMenu.ResumeLayout(false);
            this.panelHeader.ResumeLayout(false);
            this.panelHeader.PerformLayout();
            this.panelToolbar.ResumeLayout(false);
            this.panelToolbar.PerformLayout();
            this.panelLogHost.ResumeLayout(false);
            this.panelLogHost.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.ContextMenuStrip notifyIconContextMenu;
        private System.Windows.Forms.ToolStripMenuItem toolOpen;
        private System.Windows.Forms.ToolStripMenuItem toolMinsize;
        private System.Windows.Forms.ToolStripSeparator toolSep1;
        private System.Windows.Forms.ToolStripMenuItem toolExit;
        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblHeaderSubtitle;
        private System.Windows.Forms.Label lblHeaderTitle;
        private System.Windows.Forms.Panel panelToolbar;
        private System.Windows.Forms.Button btnClearLogs;
        private System.Windows.Forms.Button btnOpenSettings;
        private System.Windows.Forms.Label lblDedupValue;
        private System.Windows.Forms.Label lblDedupLabel;
        private System.Windows.Forms.Label lblStatusValue;
        private System.Windows.Forms.Label lblStatusLabel;
        private System.Windows.Forms.TextBox txtCgLogPath;
        private System.Windows.Forms.Label lblLogPath;
        private System.Windows.Forms.Panel panelLogHost;
        private System.Windows.Forms.Label lblEmptyState;
        private BufferedFlowLayoutPanel flowLogs;
    }
}
