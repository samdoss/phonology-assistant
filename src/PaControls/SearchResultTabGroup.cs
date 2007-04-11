using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using SIL.SpeechTools.Utils;
using SIL.Pa.FFSearchEngine;
using SIL.FieldWorks.Common.UIAdapters;
using XCore;

namespace SIL.Pa.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SearchResultTabGroup : Panel, IxCoreColleague
	{
		// This is the number of pixels of height to add to the tab panel height
		// in addition to the height of the tab's font height.
		private const int kTabPanelHeightPadding = 19;

		private bool m_closingTabInProcess = false;
		private bool m_isCurrentTabGroup = false;
		private List<SearchResultTab> m_tabs;
		private SearchResultTab m_currTab;
		private Panel m_pnlHdrBand;
		private Panel m_pnlTabs;
		private Panel m_pnlClose;
		private Panel m_pnlScroll;
		private XButton m_btnClose;
		private XButton m_btnLeft;
		private XButton m_btnRight;
		private SearchResultsViewManager m_rsltVwMngr;
		internal ToolTip m_tooltip;

		private static SearchResultTab s_lastTabRightClickedOn;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The panel m_pnlHdrBand owns both the m_pnlTabs and the m_pnlUndock panels.
		/// m_pnlUndock contains the close buttons and the arrow buttons that allow the user
		/// to scroll all the tabs left and right. m_pnlTabs contains all the tabs and is the
		/// panel that moves left and right (i.e. scrolls) when the number of tabs in the
		/// group exceeds the available space in which to display them all.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SearchResultTabGroup(SearchResultsViewManager rsltVwMngr)
		{
			Visible = true;
			DoubleBuffered = true;
			AllowDrop = true;

			m_tooltip = new ToolTip();

			TextFormatFlags flags = TextFormatFlags.VerticalCenter |
				TextFormatFlags.SingleLine | TextFormatFlags.LeftAndRightPadding;

			// Create the panel that holds everything that will be displayed
			// above a result view (i.e. tabs, close button and scroll buttons).
			m_pnlHdrBand = new Panel();
			m_pnlHdrBand.Dock = DockStyle.Top;
			m_pnlHdrBand.Padding = new Padding(0, 0, 0, 5);
			m_pnlHdrBand.Paint += new PaintEventHandler(HandlePanelPaint);
			m_pnlHdrBand.Resize += new EventHandler(m_pnlHdrBand_Resize);
			m_pnlHdrBand.Click += new EventHandler(HandleClick);
			using (Graphics g = CreateGraphics())
			{
				m_pnlHdrBand.Height = TextRenderer.MeasureText(g, "X",
					FontHelper.PhoneticFont, Size.Empty, flags).Height + kTabPanelHeightPadding;
			}

			Controls.Add(m_pnlHdrBand);

			// Create the panel that holds all the tabs. 
			m_pnlTabs = new Panel();
			m_pnlTabs.Visible = true;
			m_pnlTabs.Anchor = AnchorStyles.Top | AnchorStyles.Left;
			m_pnlTabs.Location = new Point(0, 0);
			m_pnlTabs.Height = m_pnlHdrBand.Height - 5;
			m_pnlTabs.Click += new EventHandler(HandleClick);
			m_pnlHdrBand.Controls.Add(m_pnlTabs);

			// Create the panel that will hold the close button
			m_pnlClose = new Panel();
			m_pnlClose.Width = 22;
			m_pnlClose.Visible = true;
			m_pnlClose.Dock = DockStyle.Right;
			m_pnlClose.Paint += new PaintEventHandler(HandleCloseScrollPanelPaint);
			m_pnlHdrBand.Controls.Add(m_pnlClose);

			// Create a button that will close a tab.
			m_btnClose = new XButton();
			m_btnClose.Anchor = AnchorStyles.Right | AnchorStyles.Top;
			m_btnClose.Click += new EventHandler(m_btnClose_Click);
			m_btnClose.Location = new Point(m_pnlClose.Width - m_btnClose.Width,
				(m_pnlHdrBand.Height - m_btnClose.Height) / 2 - 3);
			m_tooltip.SetToolTip(m_btnClose,
				Properties.Resources.kstidCloseActiveTabButtonToolTip);
			
			m_pnlClose.Controls.Add(m_btnClose);
			m_pnlClose.BringToFront();

			SetupScrollPanel();

			m_tabs = new List<SearchResultTab>();
			m_rsltVwMngr = rsltVwMngr;
			PaApp.AddMediatorColleague(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			m_btnClose.Dispose();
			m_btnLeft.Dispose();
			m_btnRight.Dispose();
			m_pnlHdrBand.Dispose();
			m_pnlTabs.Dispose();
			m_pnlClose.Dispose();
			m_pnlScroll.Dispose();
			m_tooltip.Dispose();

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupScrollPanel()
		{
			// Create the panel that will hold the close button
			m_pnlScroll = new Panel();
			m_pnlScroll.Width = 40;
			m_pnlScroll.Visible = true;
			m_pnlScroll.Dock = DockStyle.Right;
			m_pnlScroll.Paint += new PaintEventHandler(HandleCloseScrollPanelPaint);
			m_pnlHdrBand.Controls.Add(m_pnlScroll);
			m_pnlScroll.Visible = false;
			m_pnlScroll.BringToFront();

			// Create a left scrolling button.
			m_btnLeft = new XButton();
			m_btnLeft.DrawLeftArrowButton = true;
			m_btnLeft.Size = new Size(18, 18);
			m_btnLeft.Anchor = AnchorStyles.Right | AnchorStyles.Top;
			m_btnLeft.Click += new EventHandler(m_btnLeft_Click);
			m_btnLeft.Location = new Point(4, (m_pnlHdrBand.Height - m_btnLeft.Height) / 2 - 3);
			m_pnlScroll.Controls.Add(m_btnLeft);

			// Create a right scrolling button.
			m_btnRight = new XButton();
			m_btnRight.DrawRightArrowButton = true;
			m_btnRight.Size = new Size(18, 18);
			m_btnRight.Anchor = AnchorStyles.Right | AnchorStyles.Top;
			m_btnRight.Click += new EventHandler(m_btnRight_Click);
			m_btnRight.Location = new Point(22, (m_pnlHdrBand.Height - m_btnRight.Height) / 2 - 3);
			m_pnlScroll.Controls.Add(m_btnRight);

			m_tooltip.SetToolTip(m_btnLeft, Properties.Resources.kstidScrollTabsLeftToolTip);
			m_tooltip.SetToolTip(m_btnRight, Properties.Resources.kstidScrollTabsRightToolTip);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleDestroyed(EventArgs e)
		{
			base.OnHandleDestroyed(e);
			PaApp.RemoveMediatorColleague(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display a message informing the user what to do. This always gets displayed but
		/// is only visible when the current tab is empty. Otherwise, the tab's result view
		/// covers the message.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			base.OnPaint(e);

			TextFormatFlags flags = TextFormatFlags.WordBreak | TextFormatFlags.NoPadding |
				TextFormatFlags.EndEllipsis | TextFormatFlags.HorizontalCenter |
				TextFormatFlags.VerticalCenter;

			Rectangle rc = ClientRectangle;
			rc.Y = m_pnlHdrBand.Bottom;
			rc.Height -= m_pnlHdrBand.Height;
			Color clr = (m_isCurrentTabGroup ? SystemColors.ControlText : SystemColors.GrayText);

			using (Font fnt = new Font(FontHelper.UIFont, FontStyle.Bold))
			{
				TextRenderer.DrawText(e.Graphics,
					Properties.Resources.kstidEmtpyTabInfoText, fnt, rc, clr, flags);
			}

			PaApp.DrawWatermarkImage("kimidSearchWatermark", e.Graphics, ClientRectangle);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClick(EventArgs e)
		{
			base.OnClick(e);
			HandleClick(null, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleClick(object sender, EventArgs e)
		{
			if (m_currTab != null)
				tab_Click(m_currTab, EventArgs.Empty);
		}

		#region Tab managment methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an empty tab to the tab group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SearchResultTab AddTab()
		{
			SearchResultTab tab = new SearchResultTab(this);
			tab.Text = Properties.Resources.kstidEmptySrchResultTabText;
			AddTab(tab);
			return tab;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SearchResultTab AddTab(SearchResultView resultView)
		{
			if (m_pnlTabs.Left > 0)
				m_pnlTabs.Left = 0;

			SearchResultTab tab = new SearchResultTab(this);
			tab.ResultView = resultView;
			AddTab(tab);
			return tab;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds an empty tab to the tab group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddTab(SearchResultTab tab)
		{
			tab.Dock = DockStyle.Left;
			tab.Click += new EventHandler(tab_Click);
			tab.MouseDown += new MouseEventHandler(tab_MouseDown);

			InitializeTab(tab, tab.ResultView, false);
			m_pnlTabs.Controls.Add(tab);
			tab.BringToFront();
			m_tabs.Add(tab);
			AdjustTabContainerWidth();

			UseWaitCursor = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the specified tab with the specified text and result view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitializeTab(SearchResultTab tab, SearchResultView resultView,
			bool removePreviousResults)
		{
			if (tab == null)
				return;

			bool viewWasInCIEView = tab.m_btnCIEOptions.Visible;

			// Make sure that if tab already has a result view, it gets removed.
			if (removePreviousResults)
				tab.RemoveResultView();

			// If there is no tab text, then get it from the result view's search query.
			if (string.IsNullOrEmpty(tab.Text) && resultView != null && resultView.SearchQuery != null)
				tab.Text = resultView.SearchQuery.ToString();

			tab.AdjustWidth();
			tab.OwningTabGroup = this;

			if (resultView != null)
			{
				tab.ResultView = resultView;
				tab.ResultView.Size = new Size(Width, Height - m_pnlHdrBand.Height);
				tab.ResultView.Click += new EventHandler(HandleClick);
				Controls.Add(resultView);
				AdjustTabContainerWidth();
				resultView.BringToFront();

				if (viewWasInCIEView)
					tab.CIEViewRefresh();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void AdjustTabContainerWidth()
		{
			int totalWidth = 0;
			foreach (SearchResultTab tab in m_tabs)
				totalWidth += tab.Width;

			m_pnlTabs.SuspendLayout();
			m_pnlTabs.Width = totalWidth;
			RefreshScrollButtonPanel();
			m_pnlTabs.ResumeLayout(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the name assigned to the current tab's search query.
		/// </summary>
		/// <param name="newName"></param>
		/// ------------------------------------------------------------------------------------
		public void UpdateCurrentTabsQueryName(string newName)
		{
			if (CurrentTab != null && CurrentTab.SearchQuery != null)
			{
				CurrentTab.SearchQuery.Name = newName;
				CurrentTab.Text = newName;
				CurrentTab.AdjustWidth();
				AdjustTabContainerWidth();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the specified tab from the group's collection of tabs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveTab(SearchResultTab tab, bool disposeOfTab)
		{
			// If the tab being removed is selected and owned by a tab group, then make
			// sure we select an adjacent tab before removing the tab because a tab group
			// always has to have a selected tab.
			if (tab.Selected && tab.OwningTabGroup != null)
			{
				SearchResultTabGroup tabGroup = tab.OwningTabGroup;
				SearchResultTab newTabInSendingGroup = null;
				int i = tabGroup.m_pnlTabs.Controls.IndexOf(tab);

				if (i - 1 >= 0)
					newTabInSendingGroup = tabGroup.m_pnlTabs.Controls[i - 1] as SearchResultTab;
				else if (i + 1 < tabGroup.m_pnlTabs.Controls.Count)
					newTabInSendingGroup = tabGroup.m_pnlTabs.Controls[i + 1] as SearchResultTab;

				if (newTabInSendingGroup != null)
					tabGroup.SelectTab(newTabInSendingGroup, true);
			}

			if (m_pnlTabs.Controls.Contains(tab))
			{
				tab.Click -= tab_Click;
				tab.MouseDown -= tab_MouseDown;

				if (tab.ResultView != null)
					tab.ResultView.Click -= HandleClick;

				if (Controls.Contains(tab.ResultView))
					Controls.Remove(tab.ResultView);

				m_pnlTabs.Controls.Remove(tab);
				m_tabs.Remove(tab);

				if (disposeOfTab)
					tab.Dispose();

				AdjustTabContainerWidth();
			}

			// If the last tab was removed from the group, then close the tab group by
			// removing ourselves from our parent's control collection.
			if (m_tabs.Count == 0 && Parent != null)
			{
				Controls.Clear();
				Parent.Controls.Remove(this);
				Dispose();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SelectTab(SearchResultTab newSelectedTab, bool makeTabCurrent)
		{
			if (newSelectedTab == null)
				return;

			if (makeTabCurrent)
			{
				m_rsltVwMngr.SearchResultTabGroupChanged(this);

				// This is used to inform other tab groups in the same view tabs manager.
				PaApp.MsgMediator.SendMessage("SearchResultTabGroupChanged", this);
			}

			newSelectedTab.Selected = true;
			m_currTab = newSelectedTab;

			foreach (SearchResultTab tab in m_tabs)
			{
				if (tab != newSelectedTab)
					tab.Selected = false;
			}

			if (makeTabCurrent)
			{
				EnsureTabVisible(m_currTab);

				// Make sure the tab's grid has focus.
				if (m_currTab.ResultView != null && m_currTab.ResultView.Grid != null)
					m_currTab.ResultView.Grid.Focus();

				m_rsltVwMngr.CurrentSearchResultTabChanged(m_currTab);

				// Sometimes selecting an empty tab causes a chain reaction caused by hiding
				// the former selected tab's grid. Doing that forces the .Net framework to
				// look for the next visible control in line that can be focused. Sometimes
				// that means a grid on another tab will get focus and ultimately cause that
				// grid's tab to become selected, thus negating the fact that we just got
				// through setting this tab group as current. Therefore, force the issue
				// again.
				if (!m_isCurrentTabGroup)
				{
					m_rsltVwMngr.SearchResultTabGroupChanged(this);
					PaApp.MsgMediator.SendMessage("SearchResultTabGroupChanged", this);
					m_rsltVwMngr.CurrentSearchResultTabChanged(m_currTab);
				}

				if (m_currTab.ResultView != null && m_currTab.ResultView.Grid != null)
					m_currTab.ResultView.Grid.IsCurrentPlaybackGrid = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void EnsureTabVisible(SearchResultTab tab)
		{
			// Make sure the tab isn't wider than the available width.
			// Just leave if there's no hope of making the tab fully visible.
			int availableWidth = m_pnlHdrBand.Width - (m_pnlClose.Width +
				(m_pnlScroll.Visible ? m_pnlScroll.Width : 0));

			if (tab.Width > availableWidth)
				return;

			int maxRight = (m_pnlScroll.Visible ? m_pnlScroll.Left : m_pnlClose.Left);

			// Get the tab's left and right edge relative to the header panel.	
			int left = tab.Left + m_pnlTabs.Left;
			int right = left + tab.Width;

			// Check if it's already fully visible.
			if (left >= 0 && right < maxRight)
				return;

			// Slide the panel in the proper direction to make it visible.
			int dx = (left < 0 ? left : right - maxRight);
			SlideTabs(m_pnlTabs.Left - dx);
			RefreshScrollButtonPanel();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void tab_Click(object sender, EventArgs e)
		{
			SelectTab(sender as SearchResultTab, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save the tab the mouse was right-clicked on so this tab group will know what tab
		/// the user clicked on when one of the tab's context menu message handlers is called.
		/// Also make sure the tab becomes the current tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void tab_MouseDown(object sender, MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Right)
			{
				s_lastTabRightClickedOn = sender as SearchResultTab;
				tab_Click(sender, null);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the selected tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_btnClose_Click(object sender, EventArgs e)
		{
			// m_closingTabInProcess prevents reentrancy
			if (m_currTab != null && !m_closingTabInProcess)
			{
				m_closingTabInProcess = true;
				RemoveTab(m_currTab, true);
				m_closingTabInProcess = false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This message is received when the current tab should be moved to a new
		/// side-by-side tab group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnMoveToNewSideBySideTabGroup(object args)
		{
			if (s_lastTabRightClickedOn != null)
			{
				PaApp.MsgMediator.SendMessage("ReflectMoveToNewSideBySideTabGroup",
					s_lastTabRightClickedOn);

				s_lastTabRightClickedOn = null;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This message is received when the current tab should be moved to a new
		/// stacked tab group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnMoveToNewStackedTabGroup(object args)
		{
			if (s_lastTabRightClickedOn != null)
			{
				PaApp.MsgMediator.SendMessage("ReflectMoveToNewStackedTabGroup",
					s_lastTabRightClickedOn);

				s_lastTabRightClickedOn = null;
			}

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnCloseTab(object args)
		{
			if (s_lastTabRightClickedOn != null && s_lastTabRightClickedOn.OwningTabGroup == this)
			{
				m_btnClose_Click(null, null);
				s_lastTabRightClickedOn = null;
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnCloseTabGroup(object args)
		{
			if (s_lastTabRightClickedOn == null || s_lastTabRightClickedOn.OwningTabGroup != this)
				return false;

			Close();
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the tab group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Close()
		{
			while (m_tabs.Count > 0)
			{
				s_lastTabRightClickedOn = m_tabs[0];
				m_btnClose_Click(null, null);
			}

			PaApp.RemoveMediatorColleague(this);
			s_lastTabRightClickedOn = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't allow this menu item to be chosen when there's only one tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateMoveToNewSideBySideTabGroup(object args)
		{
			// If we're not the tab group that owns the tab that was
			// clicked on, then we don't want to handle the message.
			if (!m_tabs.Contains(s_lastTabRightClickedOn))
				return false;

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null)
				return false;

			itemProps.Visible = true;
			itemProps.Enabled = (m_tabs.Count > 1);
			itemProps.Update = true;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Don't allow this menu item to be chosen when there's only one tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnUpdateMoveToNewStackedTabGroup(object args)
		{
			// If we're not the tab group that owns the tab that was
			// clicked on, then we don't want to handle the message.
			if (!m_tabs.Contains(s_lastTabRightClickedOn))
				return false;

			TMItemProperties itemProps = args as TMItemProperties;
			if (itemProps == null)
				return false;

			itemProps.Visible = true;
			itemProps.Enabled = (m_tabs.Count > 1);
			itemProps.Update = true;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This message is received when the current tab group changes. We catch it here
		/// for every group in order to invalidate the tabs contained in the group. They're
		/// invalidated to force painting consistent with their selected status.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnSearchResultTabGroupChanged(object args)
		{
			SearchResultTabGroup group = args as SearchResultTabGroup;
			if (group != null && group.m_rsltVwMngr == m_rsltVwMngr)
			{
				m_isCurrentTabGroup = (group == this);

				foreach (SearchResultTab tab in m_tabs)
				{
					tab.Invalidate();
					if (tab.ResultView != null && tab.ResultView.Grid != null)
						tab.ResultView.Grid.IsCurrentPlaybackGrid = false;
				}

				// Force the text in empty views to be redrawn. This is only necessary
				// when there is more than one tab group and the current tab in any one
				// of those groups is empty. (The text is drawn disabled looking when
				// the tab's owning tab group isn't current).
				if (m_currTab != null && m_currTab.ResultView == null)
					Invalidate();
			}

			// There's a strange problem in which a tab group's wait cursor gets turned on
			// and I can't find where. It's not explicitly so it must be implicitly.
			UseWaitCursor = false;

			return false;
		}

		#endregion

		#region Methods for managing scrolling of the tabs
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_pnlHdrBand_Resize(object sender, EventArgs e)
		{
			RefreshScrollButtonPanel();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void RefreshScrollButtonPanel()
		{
			if (m_pnlTabs == null || m_pnlHdrBand == null || m_pnlClose == null)
				return;

			// Determine whether or not the scroll button panel should
			// be visible and set its visible state accordingly.
			bool shouldBeVisible = (m_pnlTabs.Width > m_pnlHdrBand.Width - m_pnlClose.Width);
			if (m_pnlScroll.Visible != shouldBeVisible)
				m_pnlScroll.Visible = shouldBeVisible;

			// Determine whether or not the tabs are scrolled to either left or right
			// extreme. If so, then the appropriate scroll buttons needs to be disabled.
			m_btnLeft.Enabled = (m_pnlTabs.Left < 0);
			m_btnRight.Enabled = (m_pnlTabs.Right > m_pnlClose.Left ||
				(shouldBeVisible && m_pnlTabs.Right > m_pnlScroll.Left));

			// If the scroll buttons are hidden and the tab panel is
			// not all visible, then move it so all the tabs are visible.
			if (!shouldBeVisible && m_pnlTabs.Left < 0)
				m_pnlTabs.Left = 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the tabs to the right (i.e. move the tab's panel to the right) so user is
		/// able to see tabs obscured on the left side.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_btnLeft_Click(object sender, EventArgs e)
		{
			int left = m_pnlTabs.Left;

			// Find the furthest right tab that is partially
			// obscurred and needs to be scrolled into view.
			foreach (SearchResultTab tab in m_tabs)
			{
				if (left < 0 && left + tab.Width >= 0)
				{
					SlideTabs(m_pnlTabs.Left + Math.Abs(left));
					break;
				}

				left += tab.Width;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the tabs to the left (i.e. move the tab's panel to the left) so user is
		/// able to see tabs obscured on the right side.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_btnRight_Click(object sender, EventArgs e)
		{
			int left = m_pnlTabs.Left;

			// Find the furthest left tab that is partially
			// obscurred and needs to be scrolled into view.
			foreach (SearchResultTab tab in m_tabs)
			{
				if (left <= m_pnlScroll.Left && left + tab.Width > m_pnlScroll.Left)
				{
					int dx = (left + tab.Width) - m_pnlScroll.Left;
					SlideTabs(m_pnlTabs.Left - dx);
					break;
				}

				left += tab.Width;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Slides the container for all the tab controls to the specified new left value. 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SlideTabs(int newLeft)
		{
			float dx = Math.Abs(m_pnlTabs.Left - newLeft);
			int pixelsPerIncrement = (int)Math.Ceiling(dx / 75f);
			bool slidingLeft = (newLeft < m_pnlTabs.Left);

			while (m_pnlTabs.Left != newLeft)
			{
				if (slidingLeft)
				{
					if (m_pnlTabs.Left - pixelsPerIncrement < newLeft)
						m_pnlTabs.Left = newLeft;
					else
						m_pnlTabs.Left -= pixelsPerIncrement;
				}
				else
				{
					if (m_pnlTabs.Left + pixelsPerIncrement > newLeft)
						m_pnlTabs.Left = newLeft;
					else
						m_pnlTabs.Left += pixelsPerIncrement;
				}

				Application.DoEvents();
			}

			RefreshScrollButtonPanel();
		}

		#endregion

		#region Painting methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandlePanelPaint(object sender, PaintEventArgs e)
		{
			Panel pnl = sender as Panel;

			if (pnl != null)
			{
				int y = pnl.ClientRectangle.Bottom - 6;
				e.Graphics.DrawLine(SystemPens.ControlDark, 0, y, pnl.Right, y);

				using (SolidBrush br = new SolidBrush(Color.White))
					e.Graphics.FillRectangle(br, 0, y + 1, pnl.Right, pnl.Bottom);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draw a line that's the continuation of the line drawn on the owner of m_pnlUndock.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleCloseScrollPanelPaint(object sender, PaintEventArgs e)
		{
			Panel pnl = sender as Panel;
			int y = pnl.ClientRectangle.Bottom - 1;
			e.Graphics.DrawLine(SystemPens.ControlDark, 0, y, pnl.Right, y);
		}

		#endregion

		#region Drag and drop methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides an accessor for a tab to call it's owning tab group's drag over event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void InternalDragOver(DragEventArgs e)
		{
			OnDragOver(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides an accessor for a tab to call it's owning tab group's drag drop event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void InternalDragDrop(DragEventArgs e)
		{
			OnDragDrop(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnDragOver(DragEventArgs e)
		{
			base.OnDragOver(e);
			SearchResultTab tab = e.Data.GetData(typeof(SearchResultTab)) as SearchResultTab;
			SearchQuery query = e.Data.GetData(typeof(SearchQuery)) as SearchQuery;

			// Dropping a query anywhere on the tab group is allowed.
			if (query != null && !query.PatternOnly)
			{
				e.Effect = e.AllowedEffect;
				return;
			}

			// Don't allow a tab to be dragged over (or dropped as the
			// case may be) the tab group that already owns it.
			if (tab != null && tab.OwningTabGroup != this)
			{
				e.Effect = DragDropEffects.Move;
				return;
			}

			e.Effect = DragDropEffects.None;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnDragDrop(DragEventArgs e)
		{
			base.OnDragDrop(e);
			SearchResultTab tab = e.Data.GetData(typeof(SearchResultTab)) as SearchResultTab;
			SearchQuery query = e.Data.GetData(typeof(SearchQuery)) as SearchQuery;

			// Is what was dropped appropriate to be dropped in a search pattern?
			if (tab != null && tab.OwningTabGroup != this)
			{
				// Remove the tab from it's owning group.
				tab.OwningTabGroup.RemoveTab(tab, false);
				AddTab(tab);
				tab_Click(tab, null);
			}
			else if (query != null && !query.PatternOnly)
			{
				SelectTab(m_currTab, true);
				PaApp.MsgMediator.SendMessage("PatternDroppedOnTabGroup", query);
			}
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current tab in the group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SearchResultTab CurrentTab
		{
			get { return m_currTab; }
			set { m_currTab = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the tab control is the tab control with
		/// the focused child grid or record view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsCurrent
		{
			get { return m_isCurrentTabGroup; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tab group's record view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public RawRecordView RawRecordView
		{
			get { return m_rsltVwMngr.RawRecordView; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tab group's toolbar/menu adapter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITMAdapter TMAdapter
		{
			get { return m_rsltVwMngr.TMAdapter; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public List<SearchResultTab> Tabs
		{
			get { return m_tabs; }
			set { m_tabs = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the tab group's wait cursor state.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public new bool UseWaitCursor
		{
			get { return base.UseWaitCursor; }
			set
			{
				base.UseWaitCursor = value;

				// Cascade the setting to each of the tab group's tabs. 
				foreach (SearchResultTab tab in m_tabs)
				{
					tab.UseWaitCursor = value;
					if (tab.ResultView != null && tab.ResultView.Grid != null)
						tab.ResultView.Grid.UseWaitCursor = value;
				}
			}
		}

		#endregion

		#region Minimal pair (i.e. CIE) options drop-down handling methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void ShowCIEOptions(Control ctrl)
		{
			if (m_currTab == null || m_currTab.ResultView == null || m_currTab.ResultView.Grid == null)
				return;

			if (m_currTab.CieOptionsDropDown == null)
				m_currTab.CieOptionsDropDown = new CIEOptionsDropDown();

			if (m_currTab.CieOptionsDropDownContainer == null)
			{
				m_currTab.CieOptionsDropDownContainer = new CustomDropDown();
				m_currTab.CieOptionsDropDownContainer.AddControl(m_currTab.CieOptionsDropDown);
				m_currTab.CieOptionsDropDownContainer.Closed +=
					new ToolStripDropDownClosedEventHandler(m_cieOptionsDropDownContainer_Closed);
			}

			Point pt = ctrl.PointToScreen(new Point(0, ctrl.Height));
			m_currTab.CieOptionsDropDownContainer.Show(pt);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_cieOptionsDropDownContainer_Closed(object sender, ToolStripDropDownClosedEventArgs e)
		{
			// Make sure the drop-down completely goes away before proceeding.
			Application.DoEvents();

			if (m_currTab.CieOptionsDropDown.OptionsChanged)
			{
				// Save the options as the new defaults for the project.
				PaApp.Project.CIEOptions = m_currTab.CieOptionsDropDown.CIEOptions;
				PaApp.Project.Save();
				m_currTab.ResultView.Grid.CIEOptions = m_currTab.CieOptionsDropDown.CIEOptions;
				m_currTab.CIEViewRefresh();
			}
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
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
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
			return (new IxCoreColleague[] { this });
		}

		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class SearchResultTab : Panel, IxCoreColleague
	{
		// The combined left and right margins of the image. 
		private const int kleftImgMargin = 6;

		private Point m_mouseDownLocation = Point.Empty;
		private bool m_mouseOver = false;
		private bool m_selected = false;
		private SearchResultTabGroup m_owningTabGroup;
		private SearchResultView m_resultView;
		private SearchQuery m_query;
		private bool m_tabTextClipped = false;
		private Image m_image;
		internal XButton m_btnCIEOptions;
		private CustomDropDown m_cieOptionsDropDownContainer;
		private CIEOptionsDropDown m_cieOptionsDropDown;
		private Color m_activeTabInactiveGroupBack1;
		private Color m_activeTabInactiveGroupBack2;
		private Color m_activeTabInactiveGroupFore;
		private Color m_activeTabFore;
		private Color m_activeTabBack;
		private Color m_inactiveTabFore;
		private Color m_inactiveTabBack;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SearchResultTab(SearchResultTabGroup owningTabControl)
		{
			DoubleBuffered = true;
			AutoSize = false;
			AllowDrop = true;
			Font = FontHelper.PhoneticFont;
			m_owningTabGroup = owningTabControl;
			m_query = new SearchQuery();
			PaApp.AddMediatorColleague(this);

			if (m_owningTabGroup.TMAdapter != null)
				m_owningTabGroup.TMAdapter.SetContextMenuForControl(this, "cmnuSearchResultTab");

			Disposed += new EventHandler(SearchResultTab_Disposed);

			// Prepare the tab's minimal pair options button.
			Image img = Properties.Resources.kimidMinimalPairOptions;
			m_btnCIEOptions = new XButton();
			m_btnCIEOptions.Image = img;
			m_btnCIEOptions.Size = new Size(img.Width + 4, img.Height + 4);
			m_btnCIEOptions.BackColor = Color.Transparent;
			m_btnCIEOptions.Visible = false;
			m_btnCIEOptions.Left = kleftImgMargin;
			m_btnCIEOptions.Click += new EventHandler(m_btnCIEOptions_Click);
			Controls.Add(m_btnCIEOptions);

			m_owningTabGroup.m_tooltip.SetToolTip(m_btnCIEOptions,
				Properties.Resources.kstidCIEOptionsButtonToolTip);

			GetTabColors();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void GetTabColors()
		{
			m_activeTabInactiveGroupBack1 = PaApp.SettingsHandler.GetColorSettingsValue(
				"srchresulttabs", "activeininactivegroup1", Color.White);

			m_activeTabInactiveGroupBack2 = PaApp.SettingsHandler.GetColorSettingsValue(
				"srchresulttabs", "activeininactivegroup1", 0xFFD7D1C4);

			m_activeTabInactiveGroupFore = PaApp.SettingsHandler.GetColorSettingsValue(
				"srchresulttabs", "activeininactivegroupfore", Color.Black);

			m_activeTabBack = PaApp.SettingsHandler.GetColorSettingsValue(
				"srchresulttabs", "activetabback", Color.White);

			m_activeTabFore = PaApp.SettingsHandler.GetColorSettingsValue(
				"srchresulttabs", "activetabfore", Color.Black);

			m_inactiveTabBack = PaApp.SettingsHandler.GetColorSettingsValue(
				"srchresulttabs", "inactivetabback", SystemColors.Control);

			m_inactiveTabFore = PaApp.SettingsHandler.GetColorSettingsValue(
				"srchresulttabs", "inactivetabfore", SystemColors.ControlText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// For some reason, this is safer to do in a Disposed delegate than in an override
		/// of the Dispose method. Putting this in an override of Dispose sometimes throws
		/// a "Parameter is not valid" exception.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SearchResultTab_Disposed(object sender, EventArgs e)
		{
			Disposed -= SearchResultTab_Disposed;

			if (m_resultView != null)
			{
				m_resultView.Dispose();
				m_resultView = null;
			}

			if (m_query != null)
				m_query = null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up a little.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			PaApp.RemoveMediatorColleague(this);
			m_btnCIEOptions.Dispose();

			if (m_image != null)
				m_image.Dispose();

			base.Dispose(disposing);
		}

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get & Set the CieOptionsDropDownContainer.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CustomDropDown CieOptionsDropDownContainer
		{
			get { return m_cieOptionsDropDownContainer; }
			set { m_cieOptionsDropDownContainer = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get & Set the CieOptionsDropDown.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CIEOptionsDropDown CieOptionsDropDown
		{
			get { return m_cieOptionsDropDown; }
			set { m_cieOptionsDropDown = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public Image Image
		{
			get { return m_image; }
			set
			{
				if (m_image != value)
				{
					m_image = value;
					Invalidate();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the tab contains any results.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsEmpty
		{
			get { return m_resultView == null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Selected
		{
			get { return m_selected; }
			set
			{
				if (m_selected != value)
				{
					m_selected = value;
					Invalidate();
					Application.DoEvents();

					if (m_resultView != null)
					{
						if (m_resultView.Grid != null)
							m_resultView.Grid.IsCurrentPlaybackGrid = value;

						m_resultView.Visible = value;
						if (value)
						{
							m_resultView.BringToFront();
							if (m_resultView.Grid != null)
							{
								m_resultView.Grid.Focus();
								FindInfo.Grid = m_resultView.Grid;
							}
						}
					}
	
					HandleResultViewRowEnter(null, null);
				}
				else if (m_owningTabGroup.IsCurrent && m_resultView != null &&
					m_resultView.Grid != null && !m_resultView.Grid.Focused)
				{
					m_resultView.Grid.Focus();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the tab's result view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SearchResultView ResultView
		{
			get {return m_resultView;}
			set
			{
				if (m_resultView == value)
					return;

				if (value == null)
					Clear();

				m_resultView = value;
				if (m_resultView != null)
				{
					m_query = m_resultView.SearchQuery;
					m_resultView.Dock = DockStyle.Fill;
					SubscribeToGridEvents();
					HandleResultViewRowEnter(null, null);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the tab's search query.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SearchQuery SearchQuery
		{
			get { return m_query; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the tab's owning group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SearchResultTabGroup OwningTabGroup
		{
			get { return m_owningTabGroup; }
			set { m_owningTabGroup = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the text in a tab was clipped (i.e. was
		/// too long so it is displayed with ellipses).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool TabTextClipped
		{
			get { return m_tabTextClipped; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Color ForeColor
		{
			get
			{
				if (!m_selected)
					return m_inactiveTabFore;

				return (m_owningTabGroup.IsCurrent ?
					m_activeTabFore : m_activeTabInactiveGroupFore);
			}
			set {}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override Color BackColor
		{
			get
			{
				if (!m_selected)
					return m_inactiveTabBack;

				return (m_owningTabGroup.IsCurrent ? m_activeTabBack : SystemColors.Control);
			}
			set
			{
			}
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjust the tab's width based on it's text and font.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void AdjustWidth()
		{
			TextFormatFlags flags = TextFormatFlags.VerticalCenter |
				TextFormatFlags.SingleLine | TextFormatFlags.LeftAndRightPadding;

			int width;

			// Get the text's width.
			using (Graphics g = CreateGraphics())
				width = TextRenderer.MeasureText(g, Text, Font,	Size.Empty, flags).Width;

			// Add a little for good measure.
			width += 6;

			if (m_image != null)
				width += (kleftImgMargin + m_image.Width);

			if (m_btnCIEOptions.Visible)
				width += (kleftImgMargin + m_btnCIEOptions.Width);

			// Don't allow the width of a tab to be any
			// wider than 3/4 of it's owning group's width.
			Width = Math.Min(width, (int)((float)m_owningTabGroup.Width * 0.75));

			m_tabTextClipped = (Width < width);
			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the search results on the tab and sets the tab to an empty tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Clear()
		{
			RemoveResultView();

			if (m_owningTabGroup.RawRecordView != null)
				m_owningTabGroup.RawRecordView.Rtf = null;

			m_query = new SearchQuery();
			Text = Properties.Resources.kstidEmptySrchResultTabText;
			AdjustWidth();
			m_owningTabGroup.AdjustTabContainerWidth();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the tab's result view.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RemoveResultView()
		{
			if (m_resultView != null)
			{
				UnsubscribeToGridEvents();
				if (m_owningTabGroup != null && m_owningTabGroup.Controls.Contains(m_resultView))
					m_owningTabGroup.Controls.Remove(m_resultView);

				m_resultView.Dispose();
				m_resultView = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the search results as a result of the project's underlying data sources
		/// changing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnDataSourcesModified(object args)
		{
			if (m_resultView != null)
			{
				UnsubscribeToGridEvents();
				m_resultView.RefreshResults();
				SubscribeToGridEvents();
				CIEViewRefresh();
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SubscribeToGridEvents()
		{
			if (m_resultView != null && m_resultView.Grid != null)
			{
				m_resultView.Grid.AllowDrop = true;
				m_resultView.Grid.DragOver += new DragEventHandler(HandleResultViewDragOver);
				m_resultView.Grid.DragDrop += new DragEventHandler(HandleResultViewDragDrop);
				m_resultView.Grid.RowEnter += new DataGridViewCellEventHandler(HandleResultViewRowEnter);
				m_resultView.Grid.Enter += new EventHandler(HandleResultViewEnter);
				m_resultView.Grid.AllowDrop = true;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void UnsubscribeToGridEvents()
		{
			if (m_resultView != null && m_resultView.Grid != null)
			{
				m_resultView.Grid.AllowDrop = false;
				m_resultView.Grid.DragOver -= HandleResultViewDragOver;
				m_resultView.Grid.DragDrop -= HandleResultViewDragDrop;
				m_resultView.Grid.RowEnter -= HandleResultViewRowEnter;
				m_resultView.Grid.Enter -= HandleResultViewEnter;
			}
		}

		#region Overridden methods and event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the current tab is selected when its grid get's focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleResultViewEnter(object sender, EventArgs e)
		{
			if (!m_selected || !m_owningTabGroup.IsCurrent)
				m_owningTabGroup.SelectTab(this, true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the record pane with the raw record query for the current row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleResultViewRowEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (!m_selected)
				return;

			if (m_owningTabGroup.RawRecordView == null || m_resultView == null ||
				!m_owningTabGroup.IsCurrent || m_resultView.Grid == null)
			{
				m_owningTabGroup.RawRecordView.UpdateRecord(null);
			}
			else
			{
				RecordCacheEntry entry = (e == null ? m_resultView.Grid.GetRecord() :
					m_resultView.Grid.GetRecord(e.RowIndex));
				
				m_owningTabGroup.RawRecordView.UpdateRecord(entry);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Updates the record view after a sort has taken place since the grid's RowEnter
		/// event doesn't seem to take care of it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnWordListGridSorted(object args)
		{
			HandleResultViewRowEnter(null, null);
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method gets called when the CV syllables get changed in the options dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnCVSyllablesChanged(object args)
		{
			return OnRecordViewOptionsChanged(args);
		}
		
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Update the record view when the user changed the order or visibility of fields.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected bool OnRecordViewOptionsChanged(object args)
		{
			if (m_selected && m_owningTabGroup.IsCurrent &&
				m_owningTabGroup.RawRecordView != null &&
			 	m_resultView != null && m_resultView.Grid != null)
			{
				m_owningTabGroup.RawRecordView.UpdateRecord(
					m_resultView.Grid.GetRecord(), true);
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleResultViewEnterAndLeave(object sender, EventArgs e)
		{
			Invalidate();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Treat dragging on a result view grid just like dragging on the tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleResultViewDragOver(object sender, DragEventArgs e)
		{
			m_owningTabGroup.InternalDragOver(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Treat dropping on a result view grid just like dropping on the tab.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandleResultViewDragDrop(object sender, DragEventArgs e)
		{
			m_owningTabGroup.InternalDragDrop(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reflects drag over events to the tab's owning group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnDragOver(DragEventArgs e)
		{
			base.OnDragOver(e);
			m_owningTabGroup.InternalDragOver(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Reflects drag drop events to the tab's owning group.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnDragDrop(DragEventArgs e)
		{
			base.OnDragDrop(e);
			m_owningTabGroup.InternalDragDrop(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseDown(MouseEventArgs e)
		{
			if (e.Button == MouseButtons.Left)
				m_mouseDownLocation = e.Location;
			else
			{
				Form frm = FindForm();
				if (!PaApp.IsFormActive(frm))
					frm.Focus();
			}

			base.OnMouseDown(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseUp(MouseEventArgs e)
		{
			m_mouseDownLocation = Point.Empty; 
			base.OnMouseUp(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);

			// This will be empty when the mouse button is not down.
			if (m_mouseDownLocation.IsEmpty)
				return;
			
			// Begin draging a tab when the mouse is held down
			// and has moved 4 or more pixels in any direction.
			int dx = Math.Abs(m_mouseDownLocation.X - e.X);
			int dy = Math.Abs(m_mouseDownLocation.Y - e.Y);
			if (dx >= 4 || dy >= 4)
			{
				m_mouseDownLocation = Point.Empty;
				DoDragDrop(this, DragDropEffects.Move);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseEnter(EventArgs e)
		{
			m_mouseOver = true;
			Invalidate();
			base.OnMouseEnter(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnMouseLeave(EventArgs e)
		{
			m_mouseOver = false;
			Invalidate();
			base.OnMouseLeave(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnSizeChanged(EventArgs e)
		{
			base.OnSizeChanged(e);
			m_btnCIEOptions.Top = (Height - m_btnCIEOptions.Height) / 2 + 1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnPaint(PaintEventArgs e)
		{
			Rectangle rc = ClientRectangle;
			e.Graphics.FillRectangle(SystemBrushes.Control, rc);

			int topMargin = (m_selected ? 0 : 2);

			// Establish the points that outline the region for the tab outline (which
			// also marks off it's interior).
			Point[] pts = new Point[] {
				new Point(0, rc.Bottom), new Point(0, rc.Top + topMargin + 3),
				new Point(3, topMargin), new Point(rc.Right - 4, topMargin),
				new Point(rc.Right - 1, rc.Top + topMargin + 3),
				new Point(rc.Right - 1, rc.Bottom)};

			// First, clear the decks with an all white background.
			using (SolidBrush br = new SolidBrush(Color.White))
				e.Graphics.FillPolygon(br, pts);

			if (!m_selected || m_owningTabGroup.IsCurrent)
			{
				using (SolidBrush br = new SolidBrush(BackColor))
					e.Graphics.FillPolygon(br, pts);
			}
			else
			{
				// The tab is the current tab but is not in the current
				// tab group so paint with a gradient background.
				//Color clr1 = Color.FromArgb(120, SystemColors.ControlDark);
				//Color clr2 = Color.FromArgb(150, SystemColors.Control);
				using (LinearGradientBrush br = new LinearGradientBrush(rc,
					m_activeTabInactiveGroupBack1, m_activeTabInactiveGroupBack2, 70))
				{
					e.Graphics.FillPolygon(br, pts);
				}
			}

			e.Graphics.DrawLines(SystemPens.ControlDark, pts);
			
			if (!m_selected)
			{
				// The tab is not the selected tab, so draw a
				// line across the bottom of the tab.
				e.Graphics.DrawLine(SystemPens.ControlDark,
					0, rc.Bottom - 1, rc.Right, rc.Bottom - 1);
			}

			if (!m_btnCIEOptions.Visible)
				DrawImage(e.Graphics, ref rc);
			else
			{
				rc.X += (kleftImgMargin + m_btnCIEOptions.Width);
				rc.Width -= (kleftImgMargin + m_btnCIEOptions.Width);
			}

			if (!m_selected)
			{
				rc.Y += topMargin;
				rc.Height -= topMargin;
			}

			DrawText(e.Graphics, ref rc);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the tab's image.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawImage(Graphics g, ref Rectangle rc)
		{
			if (m_image != null)
			{
				Rectangle rcImage = new Rectangle();
				rcImage.Size = m_image.Size;
				rcImage.X = rc.Left + kleftImgMargin;
				rcImage.Y = rc.Top + (rc.Height - rcImage.Height) / 2;
				g.DrawImage(m_image, rcImage);
				rc.X += (kleftImgMargin + rcImage.Width);
				rc.Width -= (kleftImgMargin + rcImage.Width);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Draws the tab's text
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DrawText(Graphics g, ref Rectangle rc)
		{
			TextFormatFlags flags = TextFormatFlags.VerticalCenter |
				TextFormatFlags.WordEllipsis | TextFormatFlags.SingleLine |
				TextFormatFlags.NoPadding | TextFormatFlags.LeftAndRightPadding;

			if (m_image == null)
				flags |= TextFormatFlags.HorizontalCenter;

			rc.Height -= 3;
			TextRenderer.DrawText(g, Text, Font, rc, ForeColor, flags);

			if (m_mouseOver)
			{
				// Draw the lines that only show when the mouse is over the tab.
				using (Pen pen = new Pen(Color.DarkOrange))
				{
					int topLine = (m_selected ? 1 : 3);
					g.DrawLine(pen, 3, topLine, rc.Right - 4, topLine);
					g.DrawLine(pen, 2, topLine + 1, rc.Right - 3, topLine + 1);
				}
			}
		}

		#endregion

		#region Minimal pair (i.e. CIE) options drop-down related methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ShowCIEOptions()
		{
			if (m_btnCIEOptions.Visible)
				m_owningTabGroup.ShowCIEOptions(m_btnCIEOptions);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void m_btnCIEOptions_Click(object sender, EventArgs e)
		{
			if (!m_selected)
				m_owningTabGroup.SelectTab(this, true);

			ShowCIEOptions();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ToggleCIEView()
		{
			if (m_resultView != null && m_resultView.Grid != null && m_resultView.Grid.Cache != null)
			{
				if (m_resultView.Grid.Cache.IsCIEList)
					m_resultView.Grid.CIEViewOff();
				else
					m_resultView.Grid.CIEViewOn();

				// Force users to restart Find when toggling the CIEView
				FindInfo.CanFindAgain = false;

				m_btnCIEOptions.Visible = m_resultView.Grid.Cache.IsCIEList;
				AdjustWidth();
				m_owningTabGroup.AdjustTabContainerWidth();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void CIEViewRefresh()
		{
			if (!m_resultView.Grid.CIEViewRefresh())
			{
				m_btnCIEOptions.Visible = false;
				AdjustWidth();
				m_owningTabGroup.AdjustTabContainerWidth();
			}
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
		public void Init(Mediator mediator, System.Xml.XmlNode configurationParameters)
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
			return new IxCoreColleague[] { this };
		}

		#endregion
	}
}
