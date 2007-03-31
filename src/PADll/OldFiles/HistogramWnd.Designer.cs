namespace SIL.Pa
{
	partial class HistogramWnd
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HistogramWnd));
			this.rbAllCons = new System.Windows.Forms.RadioButton();
			this.pnlScroller = new System.Windows.Forms.Panel();
			this.pnlIPAChars = new System.Windows.Forms.Panel();
			this.lblBarValue = new System.Windows.Forms.Label();
			this.pnlCharChoices = new System.Windows.Forms.Panel();
			this.rbAllVows = new System.Windows.Forms.RadioButton();
			this.m_histToolTip = new System.Windows.Forms.ToolTip(this.components);
			this.pnlFixedBorder = new SIL.Pa.Controls.PaPanel();
			this.pnlBars = new SIL.Pa.Controls.PaPanel();
			this.pnlYaxis = new SIL.Pa.Controls.PaPanel();
			this.pnlMasterOuter = new System.Windows.Forms.Panel();
			this.pnlScroller.SuspendLayout();
			this.pnlCharChoices.SuspendLayout();
			this.pnlFixedBorder.SuspendLayout();
			this.pnlMasterOuter.SuspendLayout();
			this.SuspendLayout();
			// 
			// rbAllCons
			// 
			this.rbAllCons.AutoEllipsis = true;
			resources.ApplyResources(this.rbAllCons, "rbAllCons");
			this.rbAllCons.Name = "rbAllCons";
			this.m_histToolTip.SetToolTip(this.rbAllCons, resources.GetString("rbAllCons.ToolTip"));
			this.rbAllCons.UseVisualStyleBackColor = true;
			this.rbAllCons.Click += new System.EventHandler(this.rb_Clicked);
			// 
			// pnlScroller
			// 
			resources.ApplyResources(this.pnlScroller, "pnlScroller");
			this.pnlScroller.Controls.Add(this.pnlIPAChars);
			this.pnlScroller.Name = "pnlScroller";
			this.pnlScroller.Scroll += new System.Windows.Forms.ScrollEventHandler(this.pnlScroller_Scroll);
			// 
			// pnlPhones
			// 
			resources.ApplyResources(this.pnlIPAChars, "pnlIPAChars");
			this.pnlIPAChars.BackColor = System.Drawing.SystemColors.InactiveCaption;
			this.pnlIPAChars.Name = "pnlIPAChars";
			// 
			// lblBarValue
			// 
			resources.ApplyResources(this.lblBarValue, "lblBarValue");
			this.lblBarValue.BackColor = System.Drawing.SystemColors.Info;
			this.lblBarValue.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lblBarValue.Name = "lblBarValue";
			// 
			// pnlCharChoices
			// 
			this.pnlCharChoices.Controls.Add(this.rbAllVows);
			this.pnlCharChoices.Controls.Add(this.rbAllCons);
			resources.ApplyResources(this.pnlCharChoices, "pnlCharChoices");
			this.pnlCharChoices.Name = "pnlCharChoices";
			// 
			// rbAllVows
			// 
			this.rbAllVows.AutoEllipsis = true;
			resources.ApplyResources(this.rbAllVows, "rbAllVows");
			this.rbAllVows.Name = "rbAllVows";
			this.m_histToolTip.SetToolTip(this.rbAllVows, resources.GetString("rbAllVows.ToolTip"));
			this.rbAllVows.UseVisualStyleBackColor = true;
			this.rbAllVows.Click += new System.EventHandler(this.rb_Clicked);
			// 
			// m_phoneToolTip
			// 
			resources.ApplyResources(this.m_histToolTip, "m_histToolTip");
			this.m_histToolTip.Popup += new System.Windows.Forms.PopupEventHandler(this.histToolTip_Popup);
			this.m_histToolTip.Draw += new System.Windows.Forms.DrawToolTipEventHandler(this.histToolTip_Draw);
			// 
			// pnlFixedBorder
			// 
			this.pnlFixedBorder.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlFixedBorder.Controls.Add(this.pnlBars);
			this.pnlFixedBorder.DoubleBuffered = false;
			resources.ApplyResources(this.pnlFixedBorder, "pnlFixedBorder");
			this.pnlFixedBorder.Name = "pnlFixedBorder";
			this.pnlFixedBorder.Resize += new System.EventHandler(this.pnlFixedBorder_Resize);
			// 
			// pnlBars
			// 
			resources.ApplyResources(this.pnlBars, "pnlBars");
			this.pnlBars.BackColor = System.Drawing.SystemColors.Window;
			this.pnlBars.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.pnlBars.DoubleBuffered = true;
			this.pnlBars.Name = "pnlBars";
			this.pnlBars.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlBars_Paint);
			// 
			// pnlYaxis
			// 
			this.pnlYaxis.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			resources.ApplyResources(this.pnlYaxis, "pnlYaxis");
			this.pnlYaxis.DoubleBuffered = true;
			this.pnlYaxis.Name = "pnlYaxis";
			this.pnlYaxis.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlYaxis_Paint);
			// 
			// pnlMasterOuter
			// 
			this.pnlMasterOuter.Controls.Add(this.pnlScroller);
			this.pnlMasterOuter.Controls.Add(this.pnlFixedBorder);
			this.pnlMasterOuter.Controls.Add(this.pnlCharChoices);
			this.pnlMasterOuter.Controls.Add(this.lblBarValue);
			this.pnlMasterOuter.Controls.Add(this.pnlYaxis);
			resources.ApplyResources(this.pnlMasterOuter, "pnlMasterOuter");
			this.pnlMasterOuter.Name = "pnlMasterOuter";
			// 
			// HistogramWnd
			// 
			resources.ApplyResources(this, "$this");
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.pnlMasterOuter);
			this.DoubleBuffered = true;
			this.Name = "HistogramWnd";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Show;
			this.pnlScroller.ResumeLayout(false);
			this.pnlCharChoices.ResumeLayout(false);
			this.pnlFixedBorder.ResumeLayout(false);
			this.pnlMasterOuter.ResumeLayout(false);
			this.pnlMasterOuter.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.RadioButton rbAllCons;
		private System.Windows.Forms.Panel pnlScroller;
		private System.Windows.Forms.Panel pnlIPAChars;
		private System.Windows.Forms.Panel pnlCharChoices;
		private System.Windows.Forms.Label lblBarValue;
		private System.Windows.Forms.RadioButton rbAllVows;
		private System.Windows.Forms.ToolTip m_histToolTip;
		private SIL.Pa.Controls.PaPanel pnlFixedBorder;
		private SIL.Pa.Controls.PaPanel pnlBars;
		private SIL.Pa.Controls.PaPanel pnlYaxis;
		private System.Windows.Forms.Panel pnlMasterOuter;
	}
}