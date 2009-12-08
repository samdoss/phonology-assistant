// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.   
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.   
//    
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright> 
#endregion
// 
// File: PaMainWnd.cs
// Responsibility: DavidO
// 
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.Pa.Data;
using SIL.Pa.UI.Views;
using SilUtils;

namespace SIL.Pa.UI
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for PaMainWnd.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class PaMainWnd : Form, IxCoreColleague
	{
		private ITMAdapter m_tmAdapter;
		private bool m_shuttingDown;

		#region Construction and Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PaMainWnd"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PaMainWnd()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public PaMainWnd(bool showSplashScreen) : this()
		{
			if (showSplashScreen)
				PaApp.ShowSplashScreen();

			sblblMain.Text = string.Empty;
			PaApp.MainForm = this;
			PaApp.StatusBarLabel = sblblMain;
			PaApp.ProgressBar = sbProgress;
			PaApp.ProgressBarLabel = sblblProgress;
			PaApp.AddMediatorColleague(this);
			DataUtils.MainWindow = this;
			sbProgress.Visible = false;
			sblblProgress.Visible = false;

			base.MinimumSize = PaApp.MinimumViewWindowSize;
			LoadToolbarsAndMenus();
			SetUIFont();
			Show();

			if (PaApp.SplashScreen != null && PaApp.SplashScreen.StillAlive)
				PaApp.SplashScreen.Activate();

			Application.DoEvents();

			// Unpack training projects if it's never been done before.
			TrainingProjectsHelper tph = new TrainingProjectsHelper();
			tph.Setup();

			EnableOptionsMenus(false);
			EnableUndockMenu(false);

			// If there's a project specified on the command line, then load that.
			// Otherwise, load the last loaded project whose name is in the settings file.
			string projArg = (from args in Environment.GetCommandLineArgs()
						where args.StartsWith("/o:") || args.StartsWith("-o:")
						select args).FirstOrDefault();

			if (projArg != null)
				LoadProject(projArg.Substring(3));
			else
				LoadProject(PaApp.SettingsHandler.LastProject);

			PaApp.CloseSplashScreen();
			
			if (PaApp.Project != null)
				OnDataSourcesModified(PaApp.Project.ProjectName);

			PaApp.MsgMediator.SendMessage("MainViewOpened", this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If the user knows enough to add an entry to the settings file to override the
		/// default UI font, then read it and use it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void SetUIFont()
		{
			const string kFontEntry = "UIFont";

			string name = PaApp.SettingsHandler.GetStringSettingsValue(kFontEntry, "name", null);
			if (name == null)
				return;

			float size = PaApp.SettingsHandler.GetFloatSettingsValue(kFontEntry, "size",
				SystemInformation.MenuFont.SizeInPoints);

			FontStyle style = FontStyle.Regular;

			if (PaApp.SettingsHandler.GetBoolSettingsValue(
				kFontEntry, "bold", SystemInformation.MenuFont.Bold))
			{
				style |= FontStyle.Bold;
			}

			if (PaApp.SettingsHandler.GetBoolSettingsValue(kFontEntry, "italic",
				SystemInformation.MenuFont.Italic))
			{
				style |= FontStyle.Italic;
			}

			FontHelper.UIFont = FontHelper.MakeFont(name, size, style);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable/disable "mnuOptionsMain".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void EnableOptionsMenus(bool enable)
		{
			TMItemProperties itemProps = m_tmAdapter.GetItemProperties("mnuOptionsMain");
			if (itemProps != null)
			{
				itemProps.Visible = true;
				itemProps.Enabled = enable;
				itemProps.Update = true;
				m_tmAdapter.SetItemProperties("mnuOptionsMain", itemProps);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable/disable "mnuUnDockView".
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EnableUndockMenu(bool enable)
		{
			TMItemProperties itemProps = m_tmAdapter.GetItemProperties("mnuUnDockView");
			if (itemProps != null)
			{
				itemProps.Visible = true;
				itemProps.Enabled = enable;
				itemProps.Update = true;
				m_tmAdapter.SetItemProperties("mnuUnDockView", itemProps);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadProject(string projectFileName)
		{
			if (string.IsNullOrEmpty(projectFileName))
				return;

			if (PaApp.Project != null)
				PaApp.Project.EnsureSortOptionsSaved();

			PaApp.ProjectLoadInProcess = true;
			Utils.WaitCursors(true);
			PaProject project = PaProject.Load(projectFileName, this);

			if (project != null)
			{
				vwTabGroup.CloseAllViews();

				if (PaApp.Project != null)
					PaApp.Project.Dispose();

				PaApp.Project = project;
				PaApp.SettingsHandler.LastProject = projectFileName;

				Text = string.Format(Properties.Resources.kstidMainWindowCaption,
					project.ProjectName, Application.ProductName);

				// When there are already tabs it means there was a project loaded before
				// the one just loaded. Therefore, save the current view so it may be
				// restored after the tabs are loaded for the new project.
				if (vwTabGroup.CurrentTab != null)
				{
					PaApp.SettingsHandler.SaveSettingsValue(Name, "currentview",
						vwTabGroup.CurrentTab.ViewType.ToString());
				}

				LoadViewTabs();

				// Make the last tab that was current the current one now.
				Type type = Type.GetType(PaApp.SettingsHandler.GetStringSettingsValue(
					Name, "currentview", typeof(DataCorpusVw).FullName));

				vwTabGroup.ActivateView(type ?? typeof(DataCorpusVw));

				PaApp.AddProjectToRecentlyUsedProjectsList(projectFileName);
				
				EnableOptionsMenus(true);
				EnableUndockMenu(true);
			}

			BackColor = vwTabGroup.BackColor;
			PaApp.ProjectLoadInProcess = false;
			Utils.WaitCursors(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadViewTabs()
		{
			if (vwTabGroup.Visible)
			{
				if (vwTabGroup.CurrentTab != null)
					vwTabGroup.CurrentTab.RefreshView();

				return;
			}

			TMItemProperties itemProps = m_tmAdapter.GetItemProperties("mnuDataCorpus");
			string text = (itemProps == null ? "Error!" : itemProps.Text);
			string tooltip = Properties.Resources.kstidDataCorpusViewToolTip;
			string helptooltip = Properties.Resources.kstidDataCorpusViewHelpToolTip;
			Image img = (itemProps == null ? null : itemProps.Image);
			vwTabGroup.AddTab(text, tooltip, helptooltip, "hidDataCorpusView", img, typeof(DataCorpusVw));

			itemProps = m_tmAdapter.GetItemProperties("mnuFindPhones");
			text = (itemProps == null ? "Error!" : itemProps.Text);
			tooltip = Properties.Resources.kstidSearchViewToolTip;
			helptooltip = Properties.Resources.kstidSearchViewHelpToolTip;
			img = (itemProps == null ? null : itemProps.Image);
			vwTabGroup.AddTab(text, tooltip, helptooltip, "hidSearchView", img, typeof(SearchVw));

			itemProps = m_tmAdapter.GetItemProperties("mnuConsonantChart");
			text = (itemProps == null ? "Error!" : itemProps.Text);
			tooltip = Properties.Resources.kstidConsonantChartViewToolTip;
			helptooltip = Properties.Resources.kstidConsonantChartViewHelpToolTip;
			img = (itemProps == null ? null : itemProps.Image);
			vwTabGroup.AddTab(text, tooltip, helptooltip, "hidConsonantChartView", img, typeof(ConsonantChartVw));

			itemProps = m_tmAdapter.GetItemProperties("mnuVowelChart");
			text = (itemProps == null ? "Error!" : itemProps.Text);
			tooltip = Properties.Resources.kstidVowelChartViewToolTip;
			helptooltip = Properties.Resources.kstidVowelChartViewHelpToolTip;
			img = (itemProps == null ? null : itemProps.Image);
			vwTabGroup.AddTab(text, tooltip, helptooltip, "hidVowelChartView", img, typeof(VowelChartVw));

			itemProps = m_tmAdapter.GetItemProperties("mnuXYChart");
			text = (itemProps == null ? "Error!" : itemProps.Text);
			tooltip = Properties.Resources.kstidXYChartsViewToolTip;
			helptooltip = Properties.Resources.kstidXYChartsViewHelpToolTip;
			img = (itemProps == null ? null : itemProps.Image);
			vwTabGroup.AddTab(text, tooltip, helptooltip, "hidXYChartsView", img, typeof(XYChartVw));

			itemProps = m_tmAdapter.GetItemProperties("mnuPhoneInventory");
			text = (itemProps == null ? "Error!" : itemProps.Text);
			tooltip = Properties.Resources.kstidPhoneInventoryViewToolTip;
			helptooltip = Properties.Resources.kstidPhoneInventoryViewHelpToolTip;
			img = (itemProps == null ? null : itemProps.Image);
			vwTabGroup.AddTab(text, tooltip, helptooltip, "hidPhoneInventoryView", img, typeof(PhoneInventoryVw));
			
			vwTabGroup.Visible = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadToolbarsAndMenus()
		{
			m_tmAdapter = PaApp.LoadDefaultMenu(this);
			PaApp.TMAdapter = m_tmAdapter;

			// This item is only visible for the main PA window (i.e. this one).
			TMItemProperties itemProps = m_tmAdapter.GetItemProperties("mnuUnDockView");
			if (itemProps != null)
			{
				itemProps.Visible = true;
				itemProps.Update = true;
				m_tmAdapter.SetItemProperties("mnuUnDockView", itemProps);
			}

			// This item is only visible for undocked views, but not this window.
			itemProps = m_tmAdapter.GetItemProperties("mnuDockView");
			if (itemProps != null)
			{
				itemProps.Visible = false;
				itemProps.Update = true;
				m_tmAdapter.SetItemProperties("mnuDockView", itemProps);
			}
		}

		#endregion

		#region Overridden Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fix for PA-62.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnActivated(EventArgs e)
		{
			Utils.UpdateWindow(Handle);
			base.OnActivated(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			PaApp.SettingsHandler.LoadFormProperties(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosing(CancelEventArgs e)
		{
			// Closing isn't allowed in the middle of loading a project.
			if (PaApp.ProjectLoadInProcess)
			{
				e.Cancel = true;
				return;
			}

			if (PaApp.MsgMediator.SendMessage("PaShuttingDown", e))
			{
				e.Cancel = true;
				return;
			}

			if (PaApp.Project != null)
				PaApp.Project.EnsureSortOptionsSaved();

			PaApp.SettingsHandler.SaveFormProperties(this);

			if (vwTabGroup.CurrentTab != null)
			{
				PaApp.SettingsHandler.SaveSettingsValue(Name, "currentview",
					vwTabGroup.CurrentTab.ViewType.ToString());
			}

			// Close all the instances of SA that we started, if there are any.
			DataSourceEditor.CloseSAInstances();
			
			TempRecordCache.Dispose();
			vwTabGroup.CloseAllViews();
			m_shuttingDown = false;
			base.OnClosing(e);
			
			// This shouldn't be necessary but is in order to fix PA-431, which is
			// a little disconcerting. I have no clue how PA could get into a state
			// where it can get this far without the app. window going away and
			// PA being unloaded from memory.
			Dispose();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws a gradient fill in the application workspace when there is no project open.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaintBackground(PaintEventArgs e)
		{
			if (PaApp.Project != null)
			{
				base.OnPaintBackground(e);
				return;
			}

			Color clr1 = ColorHelper.CalculateColor(Color.White,
				SystemColors.AppWorkspace, 200);

			using (LinearGradientBrush br = new LinearGradientBrush(ClientRectangle,
				clr1, SystemColors.AppWorkspace, 45))
			{
				e.Graphics.FillRectangle(br, ClientRectangle);
			}

			// Draw the PA logo at the bottom right corner of the application workspace.
			Image img = Properties.Resources.kimidPaLogo;
			Rectangle rc = new Rectangle(0, 0, img.Width, img.Height);
			rc.X = ClientRectangle.Right - img.Width - 20;
			rc.Y = ClientRectangle.Bottom - img.Height - 20 - statusStrip.Height;
			e.Graphics.DrawImageUnscaledAndClipped(img, rc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When there is no project open, this forces the gradient background to be repainted
		/// on the application workspace.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			if (PaApp.Project == null)
				Invalidate();
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// True if the application is shutting down.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsShuttingDown
		{
			get {return m_shuttingDown;}
		}

		#endregion

		#region Message mediator message handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This gets called whenever playback is about to begin. Use the message to enable
		/// the menu item for stopping playback. That way, the shortcut key for stopping will
		/// be enabled. Normally this gets done in the menu item's update handler but that
		/// only gets called when the menu pops up. We need to enable the stop option now in
		/// order for the application to respond to it's shortcut key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnPlaybackBeginning(object args)
		{
			TMItemProperties itemProps = m_tmAdapter.GetItemProperties("mnuStopPlayback");
			if (itemProps != null)
			{
				itemProps.Visible = true;
				itemProps.Enabled = true;
				itemProps.Update = true;
				m_tmAdapter.SetItemProperties("mnuStopPlayback", itemProps);
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This gets called whenever playback has just ended. Use the message to disable
		/// the menu item for stopping playback. Normally the update handler for the stop
		/// menu item would take care of this, but that only gets called when the menu
		/// pops up. We need to disable the options before that.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnPlaybackEnded(object args)
		{
			TMItemProperties itemProps = m_tmAdapter.GetItemProperties("mnuStopPlayback");
			if (itemProps != null)
			{
				itemProps.Visible = true;
				itemProps.Enabled = false;
				itemProps.Update = true;
				m_tmAdapter.SetItemProperties("mnuStopPlayback", itemProps);
			}

			return false;
		}

		#endregion
	}
}