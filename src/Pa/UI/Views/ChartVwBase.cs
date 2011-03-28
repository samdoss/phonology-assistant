using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using Palaso.IO;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.Pa.Model;
using SIL.Pa.PhoneticSearching;
using SIL.Pa.Processing;
using SIL.Pa.Properties;
using SIL.Pa.UI.Controls;
using SilTools;
using SilTools.Controls;

namespace SIL.Pa.UI.Views
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provides a base window class for vowel, consonant, diacritic and suprasegmental
	/// character charts.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ChartVwBase : UserControl, IxCoreColleague, ITabView
	{
		protected List<CharGridCell> m_phoneList;
		protected ITMAdapter m_tmAdapter;
		protected ChartOptionsDropDown m_chartOptionsDropDown;

		protected CVChartGrid m_chartGrid;
		protected SilPanel m_pnlGrid;
		protected WebBrowser m_htmlVw;
		
		private string m_persistedInfoFilename;
		private bool m_histogramOn = true;
		private bool m_initialDock = true;
		private bool m_activeView;

		/// ------------------------------------------------------------------------------------
		public ChartVwBase()
		{
			InitializeComponent();

			if (LicenseManager.UsageMode == LicenseUsageMode.Designtime)
				return;

			Utils.WaitCursors(true);
			base.DoubleBuffered = true;
			
			LoadToolbarAndContextMenus();
			m_chrGrid.OwningViewType = GetType();

			m_chartGrid = new CVChartGrid(m_tmAdapter);
			m_chartGrid.Dock = DockStyle.Fill;
			m_chartGrid.GridColor = ChartGridColor;
			m_pnlGrid = new SilPanel();
			m_pnlGrid.Dock = DockStyle.Fill;
			m_pnlGrid.Controls.Add(m_chartGrid);

			m_htmlVw = new WebBrowser();
			m_htmlVw.Dock = DockStyle.Fill;
			m_htmlVw.Visible = false;
			m_htmlVw.AllowWebBrowserDrop = false;
			m_pnlGrid.Controls.Add(m_htmlVw);
			
			m_chrGrid.Visible = false;
			splitOuter.Panel1.Controls.Add(m_pnlGrid);
			Utils.WaitCursors(false);
		}

		/// ------------------------------------------------------------------------------------
		public virtual CVChartType ChartType
		{
			get { throw new NotImplementedException("The property must be overridden in derived class."); }
		}

		/// ------------------------------------------------------------------------------------
		protected virtual Color ChartGridColor
		{
			get { return m_chartGrid.GridColor; }
		}

		/// ------------------------------------------------------------------------------------
		protected virtual int ColumnHeaderHeight
		{
			get { throw new NotImplementedException("The property must be overridden in derived class."); }
		}

		/// ------------------------------------------------------------------------------------
		protected virtual int RowHeaderWidth
		{
			get { throw new NotImplementedException("The property must be overridden in derived class."); }
		}

		/// ------------------------------------------------------------------------------------
		protected virtual string DefaultHTMLOutputFile
		{
			get { throw new NotImplementedException("The property must be overridden in derived class."); }
		}

		/// ------------------------------------------------------------------------------------
		protected virtual string DefaultWordXmlOutputFile
		{
			get { throw new NotImplementedException("The property must be overridden in derived class."); }
		}

		/// ------------------------------------------------------------------------------------
		protected virtual string DefaultXLingPaperOutputFile
		{
			get { throw new NotImplementedException("The property must be overridden in derived class."); }
		}

		/// ------------------------------------------------------------------------------------
		private void LoadOldChart()
		{
			var bldr = new CharGridBuilder(m_chrGrid, CharacterType);
			m_phoneList = bldr.Build();
			m_persistedInfoFilename = bldr.PersistedInfoFilename;

			// This should only be null when something has gone wrong...
			// which should never happen. :o)
			if (m_phoneList == null)
				return;

			// Create a list of phones for a histogram based on the order of the
			// phones as they appear in the grid (from left to right, top to bottom).
			List<CharGridCell> histogramPhones = new List<CharGridCell>();
			for (int iCol = 0; iCol < m_chrGrid.Grid.Columns.Count; iCol++)
			{
				for (int iRow = 0; iRow < m_chrGrid.Grid.Rows.Count; iRow++)
				{
					CharGridCell cgc = m_chrGrid.Grid[iCol, iRow].Value as CharGridCell;
					if (cgc != null)
						histogramPhones.Add(cgc);
				}
			}

			m_histogram.LoadPhones(histogramPhones);
			App.MsgMediator.PostMessage("LayoutHistogram", Name);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the newer version of the C or V chart (i.e. the one built purely from phone
		/// features and which cannot be customized except by changing phone features).
		/// This version of the chart is displayed by default and the old one may eventually
		/// go away, leaving this one only (in which case, it will no longer be called "new").
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadChart()
		{
			m_chartGrid.ClearAll();

			var cgp = XmlSerializationHelper.DeserializeFromFile<CharGridPersistence>(LayoutFile);

			foreach (var col in cgp.ColHeadings)
				m_chartGrid.AddColumnGroup(col.HeadingText);

			foreach (var row in cgp.RowHeadings)
				m_chartGrid.AddRowGroup(row.HeadingText, row.SubHeadingCount);

			foreach (var phone in cgp.Phones)
				m_chartGrid[phone.Column, phone.Row].Value = phone.Phone;

			if (ColumnHeaderHeight > 0)
				m_chartGrid.ColumnHeadersHeight = ColumnHeaderHeight;

			if (RowHeaderWidth > 0)
				m_chartGrid.RowHeadersWidth = RowHeaderWidth;

			m_chartGrid.AdjustCellSizes();

			// Do this to make sure the message mediator is hooked up for
			// the toolbar/menu items.
			if (!m_chrGrid.IsHandleCreated)
				m_chrGrid.CreateControl();
		}

		/// ------------------------------------------------------------------------------------
		private void LoadHtmlChart()
		{
			var outputFile = CreateHtmlViewFile();
			m_htmlVw.Url = new Uri(File.Exists(outputFile) ? outputFile : "about:blank");
		}
		
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Flip between the old chart and the new using Ctrl+Alt+Left or Ctrl+Alt+Right.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool ProcessDialogKey(Keys keyData)
		{
			if ((keyData & Keys.Alt) == Keys.Alt && (keyData & Keys.Control) == Keys.Control &&
				(keyData & Keys.Left) == Keys.Left || (keyData & Keys.Right) == Keys.Right)
			{
				m_pnlGrid.Visible = !m_pnlGrid.Visible;
				m_chrGrid.Visible = !m_chrGrid.Visible;

				if (m_chrGrid.Visible)
					m_chrGrid.Focus();
				else
					m_chartGrid.Focus();
				return true;
			}

			return base.ProcessDialogKey(keyData);
		}

		/// ------------------------------------------------------------------------------------
		protected virtual bool ShowHtmlChartWhenViewLoaded
		{
			get { throw new NotImplementedException("The property must be overridden in derived class."); }
		}

		/// ------------------------------------------------------------------------------------
		protected virtual string CreateHtmlViewFile()
		{
			throw new NotImplementedException("The method must be overridden in derived class.");
		}

		/// ------------------------------------------------------------------------------------
		protected virtual string LayoutFile
		{
			get { throw new NotImplementedException("The property must be overridden in derived class."); }
		}

		/// ------------------------------------------------------------------------------------
		private void ShowHtmlChart(bool show)
		{
			m_chrGrid.Visible = false;
			m_pnlGrid.Visible = true;

			m_htmlVw.Visible = show;
			m_chartGrid.Visible = !show;

			if (m_htmlVw.Visible)
				m_htmlVw.Focus();
			else
				m_chartGrid.Focus();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sometimes the histogram is initially shown without its horizontal scroll bar.
		/// This will force it to be displayed if it needs to be and this is done after all
		/// loading and layout is done -- which is the only place I've found when this will
		/// work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnLayoutHistogram(object args)
		{
			if (m_histogramOn && (args as string) == Name)
				m_histogram.ForceLayout();

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reloads a chart.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnChartPhoneMoved(object args)
		{
			try
			{
				var argArray = args as object[];
				if (argArray[0] == m_chrGrid)
				{
					CharGridPersistence.Save(m_chrGrid, m_phoneList, m_persistedInfoFilename);
					App.MsgMediator.SendMessage("PhoneChartArrangementChanged", CharacterType);
				}
			}
			catch { }

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reloads a chart.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ReloadChart(bool restoreDefault)
		{
			LoadChart();
			LoadHtmlChart();

			if (restoreDefault)
				File.Delete(m_persistedInfoFilename);
			else
				CharGridPersistence.Save(m_chrGrid, m_phoneList, m_persistedInfoFilename);
			
			m_chrGrid.Reset();
			LoadOldChart();
			m_chrGrid.ForceCurrentCellUpdate();
			CharGridPersistence.Save(m_chrGrid, m_phoneList, m_persistedInfoFilename);
			App.MsgMediator.SendMessage("PhoneChartArrangementChanged", CharacterType);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadToolbarAndContextMenus()
		{
			if (App.DesignMode)
				return;

			if (m_tmAdapter != null)
			{
				App.UnPrepareAdapterForLocalizationSupport(m_tmAdapter);
				m_tmAdapter.Dispose();
			}

			m_tmAdapter = AdapterHelper.CreateTMAdapter();
			m_chrGrid.TMAdapter = m_tmAdapter;

			if (m_tmAdapter != null)
			{
				App.PrepareAdapterForLocalizationSupport(m_tmAdapter);
				m_tmAdapter.LoadControlContainerItem += m_tmAdapter_LoadControlContainerItem;
				var defs = new[] { FileLocator.GetFileDistributedWithApplication(App.ConfigFolderName,
					"CVChartsTMDefinition.xml") };

				m_tmAdapter.Initialize(this, App.MsgMediator, App.ApplicationRegKeyPath, defs);
				m_tmAdapter.AllowUpdates = true;
			}

			// Give the chart Phone search toolbar button a default image.
			var childItemProps = m_tmAdapter.GetItemProperties("tbbChartPhoneSearchAnywhere");
			var parentItemProps = m_tmAdapter.GetItemProperties("tbbChartPhoneSearch");
			if (parentItemProps != null && childItemProps != null)
			{
				parentItemProps.Image = childItemProps.Image;
				parentItemProps.Visible = true;
				parentItemProps.Update = true;
				m_tmAdapter.SetItemProperties("tbbChartPhoneSearch", parentItemProps);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Give the adapter the chars. to ignore drop-down control. We know that's the only
		/// control the adapter will request for this form. So there's no need to check the
		/// name passed to us.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private Control m_tmAdapter_LoadControlContainerItem(string name)
		{
			m_chartOptionsDropDown = new ChartOptionsDropDown(m_chrGrid.SupraSegsToIgnore);
			m_chartOptionsDropDown.lnkRefresh.Click += HandleRefreshChartClick;
			return m_chartOptionsDropDown;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void BuildDefaultChart()
		{
			throw new NotImplementedException("The method must be overridden in derived class.");
		}

		#region ITabView Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ActiveView
		{
			get { return m_activeView; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SetViewActive(bool makeActive, bool isDocked)
		{
			m_activeView = makeActive;

			if (m_activeView && isDocked && m_chrGrid != null && m_chrGrid.Grid != null)
			{
				if (m_chrGrid.Visible)
					m_chrGrid.Grid.Focus();
				else
					m_chartGrid.Focus();
			}
		}

		/// ------------------------------------------------------------------------------------
		public Form OwningForm
		{
			get { return FindForm(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the view's toolbar/menu adapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITMAdapter TMAdapter
		{
			get { return m_tmAdapter; }
		}

		/// ------------------------------------------------------------------------------------
		protected virtual float SplitterRatioSetting
		{
			get { throw new NotImplementedException("This property must be overridden"); }
			set { throw new NotImplementedException("This property must be overridden"); }
		}

		/// ------------------------------------------------------------------------------------
		protected virtual bool HistogramVisibleSetting
		{
			get { throw new NotImplementedException("This property must be overridden"); }
			set { throw new NotImplementedException("This property must be overridden"); }
		}

		/// ------------------------------------------------------------------------------------
		public void SaveSettings()
		{
			CharGridPersistence.Save(m_chrGrid, m_phoneList, m_persistedInfoFilename);
			SplitterRatioSetting = splitOuter.SplitterDistance / (float)splitOuter.Height;
			HistogramVisibleSetting = HistogramOn;
		}

		/// ------------------------------------------------------------------------------------
		protected bool OnBeginViewClosing(object args)
		{
			if (args == this)
				SaveSettings();

			return false;
		}

		/// ------------------------------------------------------------------------------------
		protected bool OnBeginViewUnDocking(object args)
		{
			if (args == this)
			{
				m_chrGrid.Grid.SetDoubleBuffering(false);
				SaveSettings();
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		protected bool OnViewUndocked(object args)
		{
			if (args == this)
				m_chrGrid.Grid.SetDoubleBuffering(true);

			return false;
		}

		/// ------------------------------------------------------------------------------------
		protected bool OnBeginViewDocking(object args)
		{
			if (args == this && IsHandleCreated)
			{
				SaveSettings();
				m_chrGrid.Grid.SetDoubleBuffering(false);
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		protected bool OnViewDocked(object args)
		{
			if (args == this)
			{
				try
				{
					// These are in a try/catch because sometimes they might throw an exception
					// in rare cases. The exception has to do with a condition in the underlying
					// .Net framework that I haven't been able to make sense of. Anyway, if an
					// exception is thrown, no big deal, the splitter distances will just be set
					// to their default values.
					splitOuter.SplitterDistance = (int)(splitOuter.Height * SplitterRatioSetting);
				}
				catch { }

				m_chrGrid.Grid.SetDoubleBuffering(true);

				// Don't need to load the tool bar or menus if this is the first time
				// the view was docked since that all gets done during construction.
				if (m_initialDock)
					m_initialDock = false;
				else
				{
					// The toolbar has to be recreated each time the view is removed from it's
					// (undocked) form and docked back into the main form. The reason has to
					// do with tooltips. They seem to form an attachment, somehow, with the
					// form that owns the controls the tooltip is extending. When that form
					// gets pulled out from under the tooltips, sometimes the program will crash.
					LoadToolbarAndContextMenus();
				}

				m_histogram.RefreshLayout();
			}

			return false;
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		public bool HistogramOn
		{
			get { return m_histogramOn; }
			set
			{
				if (m_histogramOn != value)
				{
					m_histogramOn = value;
					splitOuter.Panel2Collapsed = !value;
					var padding = splitOuter.Panel1.Padding;
					padding = new Padding(padding.Left, padding.Top, padding.Right,
						(value ? 0 : splitOuter.Panel2.Padding.Bottom));
					splitOuter.Panel1.Padding = padding;

					if (value)
						m_histogram.ForceLayout();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the form's settings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);

			if (App.DesignMode)
				return;

			HistogramOn = HistogramVisibleSetting;
			m_chrGrid.Reset();
			LoadOldChart();
			LoadChart();
			LoadHtmlChart();

			OnViewDocked(this);
			m_initialDock = true;
			App.UninitializeProgressBar();

			if (ShowHtmlChartWhenViewLoaded)
				ShowHtmlChart(true);
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Handle Moving a row up.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//private void btnMoveRowUp_Click(object sender, EventArgs e)
		//{
		//    PaApp.MsgMediator.SendMessage("MoveCharChartRowUp", null);
		//}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Handle moving a row down.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//private void btnMoveRowDown_Click(object sender, EventArgs e)
		//{
		//    PaApp.MsgMediator.SendMessage("MoveCharChartRowDown", null);
		//}

		#region Phone searching methods and searching command message/update handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnChartPhoneSearchAnywhere(object args)
		{
			if (!m_activeView)
				return false;

			PerformSearch("*_*", "tbbChartPhoneSearchAnywhere");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnChartPhoneSearchInitial(object args)
		{
			if (!m_activeView)
				return false;

			PerformSearch("#_+", "tbbChartPhoneSearchInitial");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnChartPhoneSearchMedial(object args)
		{
			if (!m_activeView)
				return false;

			PerformSearch("+_+", "tbbChartPhoneSearchMedial");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnChartPhoneSearchFinal(object args)
		{
			if (!m_activeView)
				return false;

			PerformSearch("+_#", "tbbChartPhoneSearchFinal");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnChartPhoneSearchAlone(object args)
		{
			if (!m_activeView)
				return false;
				
			PerformSearch("#_#", "tbbChartPhoneSearchAlone");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void PerformSearch(string environment, string toolbarItemName)
		{
			string[] srchPhones = (m_chrGrid == null || !m_chrGrid.Visible ?
				null : m_chrGrid.SelectedPhones);

			if (srchPhones == null)
			{
				srchPhones = (m_pnlGrid == null || !m_pnlGrid.Visible ?
					null : m_chartGrid.SelectedPhones);
			}

			if (srchPhones == null)
				return;

			var queries = new List<SearchQuery>();
			foreach (string phone in srchPhones)
			{
				var query = new SearchQuery();
				query.Pattern = phone + "/" + environment;
				query.IgnoreDiacritics = false;

				// Check if the phone only exists as an uncertain phone. If so,
				// then set the flag in the query to include searching words
				// made using all uncertain uncertain derivations.
				var phoneInfo = App.Project.PhoneCache[phone];
				if (phoneInfo != null && phoneInfo.TotalCount == 0)
					query.IncludeAllUncertainPossibilities = true;
				
				queries.Add(query);
			}

			App.MsgMediator.SendMessage("ViewSearch", queries);

			// Now set the image of the search button to the image associated
			// with the last search environment chosen by the user.
			TMItemProperties childItemProps = m_tmAdapter.GetItemProperties(toolbarItemName);
			TMItemProperties parentItemProps = m_tmAdapter.GetItemProperties("tbbChartPhoneSearch");
			if (parentItemProps != null && childItemProps != null)
			{
				parentItemProps.Image = childItemProps.Image;
				parentItemProps.Visible = true;
				parentItemProps.Update = true;
				parentItemProps.Tag = new[] {environment, toolbarItemName};
				m_tmAdapter.SetItemProperties("tbbChartPhoneSearch", parentItemProps);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnChartPhoneSearch(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || !m_activeView)
				return false;

			// When the tag is nothing then perform a default search of the Phone anywhere.
			if (itemProps.Tag == null)
				OnChartPhoneSearchAnywhere(null);
			else
			{
				string[] srchArgs = itemProps.Tag as string[];
				if (srchArgs != null && srchArgs.Length == 2)
					PerformSearch(srchArgs[0], srchArgs[1]);
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateChartPhoneSearch(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null || !m_activeView)
				return false;

			bool enable = false;

			enable = ((m_chrGrid != null && m_chrGrid.Visible && m_chrGrid.SelectedPhones != null) ||
				(m_pnlGrid.Visible && m_chartGrid != null && m_chartGrid.Visible &&
				m_chartGrid.SelectedPhones != null));

			if (itemProps.Enabled != enable)
			{
				itemProps.Visible = true;
				itemProps.Enabled = enable;
				itemProps.Update = true;
			}
			
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateCharChartSearchContextMenu(object args)
		{
			return OnUpdateChartPhoneSearch(args);
		}

		#endregion

		#region Messages for ignore characters drop down
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateChartTBMenuIgnoredCharsParent(object args)
		{
			var itemProps = args as TMItemProperties;
			if (itemProps == null || !m_activeView)
				return false;

			// TODO: Get ignored suprasegmentals working in new CV grid.
			itemProps.Update = true;
			itemProps.Visible = true;
			itemProps.Enabled = m_chrGrid.Visible;
			return true;
		}
		
		/// ------------------------------------------------------------------------------------
		protected bool OnDropDownChooseIgnoredCharactersTBMenu(object args)
		{
			var itemProps = args as TMItemProperties;
			if (itemProps == null || !m_activeView)
				return false;

			if (itemProps.Control != null && itemProps.Control == m_chartOptionsDropDown)
				m_chartOptionsDropDown.SetIgnoredChars(m_chrGrid.SupraSegsToIgnore);

			// This is a kludge and I really don't like to do it. But I don't know how
			// else to automatically get the custom drop-down to act like it has "focus".
			SendKeys.Send("{RIGHT}");
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This gets fired when the user clicks on the "Refresh Chart" on the drop-down
		/// showing suprasegmentals to ignore.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleRefreshChartClick(object sender, EventArgs e)
		{
			// Only refresh when the list changed.
			if (m_chrGrid.SupraSegsToIgnore != m_chartOptionsDropDown.GetIgnoredChars())
			{
				m_tmAdapter.HideBarItemsPopup("tbbView");
				Application.DoEvents();
				m_chrGrid.SupraSegsToIgnore = m_chartOptionsDropDown.GetIgnoredChars();
				ReloadChart(false);
			}
		}

		#endregion

		#region Misc. Other Message handlers
		/// ------------------------------------------------------------------------------------
		protected bool OnExportAsHTML(object args)
		{
			var outputFileName = GetExportFileName(DefaultHTMLOutputFile, App.kstidFileTypeHTML, "html");
				
			if (string.IsNullOrEmpty(outputFileName))
				return false;
			
			CVChartExporter.ToHtml(App.Project, ChartType, outputFileName, m_chartGrid,
				Settings.Default.OpenHtmlCVChartAfterExport);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		private string GetExportFileName(string fmtFileName, string fileTypeFilter, string defaultFileType)
		{
			if (!m_activeView)
				return null;

			string defaultOutputFileName = string.Format(fmtFileName, App.Project.LanguageName);

			var fileTypes = fileTypeFilter + "|" + App.kstidFileTypeAllFiles;

			int filterIndex = 0;

			return App.SaveFileDialog(defaultFileType, fileTypes, ref filterIndex,
				App.kstidSaveFileDialogGenericCaption, defaultOutputFileName, App.Project.Folder);
		}

		/// ------------------------------------------------------------------------------------
		protected bool OnExportAsWordXml(object args)
		{
			var outputFileName = GetExportFileName(DefaultWordXmlOutputFile,
				App.kstidFileTypeWordXml, "xml");

			if (string.IsNullOrEmpty(outputFileName))
				return false;

			CVChartExporter.ToWordXml(App.Project, ChartType, outputFileName, m_chartGrid,
				Settings.Default.OpenWordXmlCVChartAfterExport);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		protected bool OnExportAsXLingPaper(object args)
		{
			var outputFileName = GetExportFileName(DefaultXLingPaperOutputFile,
				App.kstidFileTypeXLingPaper, "xml");

			if (string.IsNullOrEmpty(outputFileName))
				return false;

			CVChartExporter.ToXLingPaper(App.Project, ChartType, outputFileName, m_chartGrid,
				Settings.Default.OpenXLingPaperCVChartAfterExport);

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateExportAsRTF(object args)
		{
			return App.DetermineMenuStateBasedOnViewType(args as TMItemProperties, GetType());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateExportAsHTML(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (!m_activeView || itemProps == null)
				return false;

			itemProps.Visible = true;
			itemProps.Enabled = m_activeView;
			itemProps.Update = true;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateExportAsWordXml(object args)
		{
			return OnUpdateExportAsHTML(args);
		}

		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateExportAsXLingPaper(object args)
		{
			return OnUpdateExportAsHTML(args);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnRestoreDefaultLayoutTBMenu(object args)
		{
			if (!m_activeView)
				return false;

			ReloadChart(true);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateRestoreDefaultLayoutTBMenu(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (!m_activeView || itemProps == null)
				return false;

			itemProps.Visible = true;
			itemProps.Enabled = m_chrGrid.Visible;
			itemProps.Update = true;

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnShowHistogram(object args)
		{
			if (!m_activeView)
				return false;

			HistogramOn = !HistogramOn;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnShowHtmlChart(object args)
		{
			if (!m_activeView)
				return false;

			ShowHtmlChart(!m_htmlVw.Visible);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateShowHtmlChart(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (!m_activeView || itemProps == null)
				return false;

			bool shouldBechecked = m_htmlVw.Visible;

			if (itemProps.Checked != shouldBechecked)
			{
				itemProps.Visible = true;
				itemProps.Checked = shouldBechecked;
				itemProps.Update = true;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateShowHistogram(object args)
		{
			TMItemProperties itemProps = args as TMItemProperties;
			if (!m_activeView || itemProps == null)
				return false;

			bool shouldBechecked = !splitOuter.Panel2Collapsed;

			if (itemProps.Checked != shouldBechecked)
			{
				itemProps.Visible = true;
				itemProps.Checked = shouldBechecked;
				itemProps.Update = true;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		protected bool OnDataSourcesModified(object args)
		{
			ReloadChart(false);
			return false;
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Update enabled state of the move row up button.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//protected bool OnUpdateMoveCharChartRowUp(object args)
		//{
		//    if (!m_activeView || args.GetType() != typeof(bool))
		//        return false;

		//    btnMoveRowUp.Enabled = (bool)args;
		//    return true;
		//}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Update enabled state of the move row down button.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//protected bool OnUpdateMoveCharChartRowDown(object args)
		//{
		//    if (!m_activeView || args.GetType() != typeof(bool))
		//        return false;

		//    btnMoveRowDown.Enabled = (bool)args;
		//    return true;
		//}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Derived classes must override this.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual IPASymbolType CharacterType
		{
			get { return IPASymbolType.Unknown; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Paint a single line at the top of the panel (which will be just above the
		/// histogram).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
		{
			//Color clr = (PaintingHelper.CanPaintVisualStyle() ?
			//    VisualStyleInformation.TextControlBorder : SystemColors.ControlDark);

			//using (Pen pen = new Pen(clr))
			//{
			//    e.Graphics.DrawLine(pen, splitContainer1.Panel2.Padding.Left, 0,
			//        splitContainer1.Panel2.ClientSize.Width -
			//        (splitContainer1.Panel2.Padding.Right + 1), 0);
			//}
		}

		#region Update handlers for menus that shouldn't be enabled when this view is current
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdatePlayback(object args)
		{
			return App.DetermineMenuStateBasedOnViewType(args as TMItemProperties, GetType());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdatePlaybackRepeatedly(object args)
		{
			return App.DetermineMenuStateBasedOnViewType(args as TMItemProperties, GetType());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateStopPlayback(object args)
		{
			return App.DetermineMenuStateBasedOnViewType(args as TMItemProperties, GetType());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateEditSourceRecord(object args)
		{
			return App.DetermineMenuStateBasedOnViewType(args as TMItemProperties, GetType());
		}
		
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateShowCIEResults(object args)
		{
			return App.DetermineMenuStateBasedOnViewType(args as TMItemProperties, GetType());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateGroupBySortedField(object args)
		{
			return App.DetermineMenuStateBasedOnViewType(args as TMItemProperties, GetType());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateExpandAllGroups(object args)
		{
			return App.DetermineMenuStateBasedOnViewType(args as TMItemProperties, GetType());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateCollapseAllGroups(object args)
		{
			return App.DetermineMenuStateBasedOnViewType(args as TMItemProperties, GetType());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateShowRecordPane(object args)
		{
			return App.DetermineMenuStateBasedOnViewType(args as TMItemProperties, GetType());
		}

		#endregion

		#region IxCoreColleague Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Never used in PA.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="configurationParameters"></param>
		/// ------------------------------------------------------------------------------------
		public void Init(Mediator mediator, XmlNode configurationParameters)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the message target.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IxCoreColleague[] GetMessageTargets()
		{
			return new IxCoreColleague[] {this};
		}

		#endregion
	}
}