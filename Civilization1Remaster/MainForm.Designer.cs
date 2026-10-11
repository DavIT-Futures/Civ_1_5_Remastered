namespace Civilization1Remaster
{
	partial class MainForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool releasing)
		{
			if (releasing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(releasing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager assets = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
			this.clkRenew = new System.Windows.Forms.Timer(this.components);
			this.toolbarPrimary = new System.Windows.Forms.ToolStrip();
			this.btnHold = new System.Windows.Forms.ToolStripButton();
			this.btnOperate = new System.Windows.Forms.ToolStripButton();
			this.utilityBandDivider1 = new System.Windows.Forms.ToolStripSeparator();
			this.captionDisplays = new System.Windows.Forms.ToolStripLabel();
			this.toolbarDisplays = new System.Windows.Forms.ToolStripSplitButton();
			this.toolbarPrimary.SuspendLayout();
			this.SuspendLayout();
			// 
			// tmrRefresh
			// 
			this.clkRenew.Enabled = true;
			this.clkRenew.Interval = 16;
			this.clkRenew.Tick += new System.EventHandler(this.OnRefreshTick);
			// 
			// tsMain
			// 
			this.toolbarPrimary.BackColor = System.Drawing.SystemColors.Control;
			this.toolbarPrimary.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
			this.toolbarPrimary.ImageScalingSize = new System.Drawing.Size(20, 20);
			this.toolbarPrimary.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnHold,
            this.btnOperate,
            this.utilityBandDivider1,
            this.captionDisplays,
            this.toolbarDisplays});
			this.toolbarPrimary.Location = new System.Drawing.Point(0, 0);
			this.toolbarPrimary.Name = "tsMain";
			this.toolbarPrimary.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
			this.toolbarPrimary.Size = new System.Drawing.Size(580, 27);
			this.toolbarPrimary.TabIndex = 0;
			this.toolbarPrimary.Text = "toolStrip1";
			// 
			// cmdPause
			// 
			this.btnHold.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnHold.Enabled = false;
			this.btnHold.Image = ((System.Drawing.Image)(assets.GetObject("cmdPause.Image")));
			this.btnHold.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnHold.Name = "cmdPause";
			this.btnHold.Size = new System.Drawing.Size(24, 24);
			this.btnHold.Text = "Pause";
			this.btnHold.Click += new System.EventHandler(this.PauseButtonPressed);
			// 
			// cmdRun
			// 
			this.btnOperate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.btnOperate.Enabled = false;
			this.btnOperate.Image = ((System.Drawing.Image)(assets.GetObject("cmdRun.Image")));
			this.btnOperate.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.btnOperate.Name = "cmdRun";
			this.btnOperate.Size = new System.Drawing.Size(24, 24);
			this.btnOperate.Text = "Run";
			this.btnOperate.Click += new System.EventHandler(this.RunButtonPressed);
			// 
			// toolStripSeparator1
			// 
			this.utilityBandDivider1.Name = "toolStripSeparator1";
			this.utilityBandDivider1.Size = new System.Drawing.Size(6, 27);
			// 
			// lblScreens
			// 
			this.captionDisplays.BackColor = System.Drawing.SystemColors.Control;
			this.captionDisplays.Name = "lblScreens";
			this.captionDisplays.Size = new System.Drawing.Size(47, 24);
			this.captionDisplays.Text = "Screens";
			// 
			// tsScreens
			// 
			this.toolbarDisplays.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.toolbarDisplays.Enabled = false;
			this.toolbarDisplays.Image = ((System.Drawing.Image)(assets.GetObject("tsScreens.Image")));
			this.toolbarDisplays.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.toolbarDisplays.Name = "tsScreens";
			this.toolbarDisplays.Size = new System.Drawing.Size(36, 24);
			this.toolbarDisplays.Text = "Plane list";
			// 
			// MainForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.BackColor = System.Drawing.Color.Black;
			this.ClientSize = new System.Drawing.Size(580, 340);
			this.Controls.Add(this.toolbarPrimary);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.KeyPreview = true;
			this.MaximizeBox = false;
			this.Name = "MainForm";
			this.ShowIcon = false;
			this.Text = "Civilization1Remaster";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.OnWindowClosing);
			this.Shown += new System.EventHandler(this.OnWindowShown);
			this.Paint += new System.Windows.Forms.PaintEventHandler(this.OnFormRepaint);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.OnKeyPressedDown);
			this.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.OnCharTyped);
			this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.OnMouseButtonDown);
			this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.OnMouseMoved);
			this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.OnMouseButtonUp);
			this.toolbarPrimary.ResumeLayout(false);
			this.toolbarPrimary.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Timer clkRenew;
		private System.Windows.Forms.ToolStrip toolbarPrimary;
		private System.Windows.Forms.ToolStripButton btnHold;
		private System.Windows.Forms.ToolStripButton btnOperate;
		private System.Windows.Forms.ToolStripSeparator utilityBandDivider1;
		private System.Windows.Forms.ToolStripLabel captionDisplays;
		private System.Windows.Forms.ToolStripSplitButton toolbarDisplays;
	}
}