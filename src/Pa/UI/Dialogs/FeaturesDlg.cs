﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.Localization;
using SIL.Pa.Model;
using SIL.Pa.Properties;
using SIL.Pa.UI.Controls;
using SilUtils;

namespace SIL.Pa.UI.Dialogs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FeaturesDlg : OKCancelDlgBase
	{
		private FeatureListView m_lvAFeatures;
		private FeatureListView m_lvBFeatures;
		private ToolTip m_phoneToolTip;
		private List<IPhoneInfo> m_phones;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FeaturesDlg()
		{
			InitializeComponent();

			if (DesignMode)
				return;

			DoubleBuffered = true;
			tabFeatures.Font = FontHelper.UIFont;
			lblAFeatures.Font = new Font(FontHelper.UIFont, FontStyle.Bold);
			m_phoneToolTip = new ToolTip();

			SetupFeatureLists();
			BuildPhoneGrid();
			LoadPhoneGrid();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				if (m_phoneToolTip != null)
				{
					m_phoneToolTip.Dispose();
					m_phoneToolTip = null;
				}

				if (m_lvAFeatures != null && !m_lvAFeatures.IsDisposed)
				{
					m_lvAFeatures.Dispose();
					m_lvAFeatures = null;
				}

				if (m_lvBFeatures != null && !m_lvBFeatures.IsDisposed)
				{
					m_lvBFeatures.Dispose();
					m_lvBFeatures = null;
				}

				components.Dispose();
			}

			base.Dispose(disposing);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupFeatureLists()
		{
			m_lvAFeatures = new FeatureListView(App.FeatureType.Articulatory);
			m_lvAFeatures.Dock = DockStyle.Fill;
			m_lvAFeatures.Load();
			tpgAFeatures.Controls.Add(m_lvAFeatures);
			m_lvAFeatures.BringToFront();

			m_lvAFeatures.FeatureChanged += HandleArticulatoryFeatureCheckChanged;

			m_lvBFeatures = new FeatureListView(App.FeatureType.Binary);
			m_lvBFeatures.Dock = DockStyle.Fill;
			m_lvBFeatures.Load();
			tpgBFeatures.Controls.Add(m_lvBFeatures);
			m_lvBFeatures.BringToFront();
		
			//if (!PaintingHelper.CanPaintVisualStyle())
			{
				m_lvAFeatures.BorderStyle = BorderStyle.None;
				m_lvBFeatures.BorderStyle = BorderStyle.None;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void BuildPhoneGrid()
		{
			gridPhones.Name = Name + "PhoneGrid";
			gridPhones.AutoGenerateColumns = false;
			gridPhones.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Raised;
			gridPhones.Font = FontHelper.UIFont;
			gridPhones.VirtualMode = true;
			gridPhones.CellValueNeeded += HandlePhoneGridCellValueNeeded;

			DataGridViewColumn col = SilGrid.CreateTextBoxColumn("phone");
			col.ReadOnly = true;
			col.Width = 55;
			col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
			col.DefaultCellStyle.Font = FontHelper.PhoneticFont;
			col.CellTemplate.Style.Font = FontHelper.PhoneticFont;
			col.HeaderText = LocalizationManager.LocalizeString(
				Name + ".PhoneListPhoneHeadingText", "Phone", App.kLocalizationGroupDialogs);
			
			gridPhones.Columns.Add(col);

			col = SilGrid.CreateTextBoxColumn("count");
			col.ReadOnly = true;
			col.Width = 55;
			col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
			col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			col.HeaderText = LocalizationManager.LocalizeString(
				Name + ".PhoneListCountHeadingText", "Count", App.kLocalizationGroupDialogs);

			gridPhones.Columns.Add(col);

			if (Settings.Default.FeaturesDlgPhoneGrid != null)
				Settings.Default.FeaturesDlgPhoneGrid.InitializeGrid(gridPhones);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void LoadPhoneGrid()
		{
			gridPhones.Rows.Clear();

			m_phones = (from x in App.PhoneCache.Values
						orderby x.POAKey
						select x.Clone()).ToList();

			gridPhones.RowCount = m_phones.Count;

			if (m_phones.Count > 0)
				gridPhones.CurrentCell = gridPhones[0, 0];

			AdjustGridRows(gridPhones, Settings.Default.FeaturesDlgGridExtraRowHeight);
			gridPhones.IsDirty = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adjusts the rows in the specified grid by letting the grid calculate the row
		/// heights automatically, then adds the extra amount specified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void AdjustGridRows(DataGridView grid, int extraAmount)
		{
			try
			{
				// Sometimes (or maybe always) this throws an exception when
				// the first row is the only row and is the NewRowIndex row.
				grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
			}
			catch { }

			grid.AutoResizeRows();

			if (extraAmount > 0)
			{
				foreach (DataGridViewRow row in grid.Rows)
					row.Height += extraAmount;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			int savedLoc = Settings.Default.FeaturesDlgSplitLoc;

			if (savedLoc > 0 && savedLoc >= splitFeatures.Panel1MinSize)
				splitFeatures.SplitterDistance = savedLoc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether or not the user has changed any of the features
		/// for the phones in the phone inventory list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private bool DidPhoneFeaturesChanged
		{
			get
			{
				foreach (var pi in m_phones)
				{
					PhoneInfo phoneInfo = pi as PhoneInfo;
					if (phoneInfo == null)
						continue;

					IPhoneInfo origPhoneInfo = App.PhoneCache[phoneInfo.Phone];

					if (origPhoneInfo == null ||
						phoneInfo.AMask != origPhoneInfo.AMask ||
						phoneInfo.BMask != origPhoneInfo.BMask)
					{
						return true;
					}
				}

				return false;
			}
		}

		#region Overridden methods of base class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool IsDirty
		{
			get { return base.IsDirty || DidPhoneFeaturesChanged; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void SaveSettings()
		{
			Settings.Default.FeaturesDlgPhoneGrid = GridSettings.Create(gridPhones);
			Settings.Default.FeaturesDlgSplitLoc = splitFeatures.SplitterDistance;
			base.SaveSettings();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool SaveChanges()
		{
			base.SaveChanges();
			
			FeatureOverrides featureOverrideList = new FeatureOverrides();

			foreach (var pi in m_phones)
			{
				// Get the phone from the grid.
				PhoneInfo phoneInfo = pi as PhoneInfo;
				if (phoneInfo == null)
					continue;

				// Find the grid phone's entry in the application's phone cache. If the
				// features in the grid phone are different from those in the phone
				// cache entry, then add the phone from the grid to our temporary list
				// of phone features to override.
				IPhoneInfo origPhoneInfo = App.PhoneCache[phoneInfo.Phone];
				if (origPhoneInfo == null)
					continue;

				phoneInfo.AFeaturesAreOverridden = (origPhoneInfo.AMask != phoneInfo.AMask);
				phoneInfo.BFeaturesAreOverridden = (origPhoneInfo.BMask != phoneInfo.BMask);

				if (phoneInfo.AFeaturesAreOverridden || phoneInfo.BFeaturesAreOverridden)
					featureOverrideList[phoneInfo.Phone] = phoneInfo;
			}

			App.MsgMediator.SendMessage("BeforePhoneFeatureOverridesSaved", featureOverrideList);
			featureOverrideList.Save(App.Project.ProjectPathFilePrefix);
			App.MsgMediator.SendMessage("AfterPhoneFeatureOverridesSaved", featureOverrideList);
			App.Project.ReloadDataSources();
			return true;
		}

		#endregion

		#region Grid event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void HandlePhoneGridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			int i = e.RowIndex;
			if (m_phones == null || i < 0 || i >= m_phones.Count)
			{
				e.Value = null;
				return;
			}

			if (e.ColumnIndex == 0)
				e.Value = m_phones[i].Phone;
			else
				e.Value = m_phones[i].TotalCount + m_phones[i].CountAsPrimaryUncertainty;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandlePhoneGridCellMouseEnter(object sender, DataGridViewCellEventArgs e)
		{
			try
			{
				if (e.ColumnIndex != 0)
					return;

				PhoneInfo phoneInfo = m_phones[e.RowIndex] as PhoneInfo;
				if (phoneInfo == null || phoneInfo.Phone.Trim().Length == 0)
					return;

				var bldr = new StringBuilder();
				foreach (char c in phoneInfo.Phone)
					bldr.AppendFormat("U+{0:X4}, ", (int)c);

				var fmt = LocalizationManager.LocalizeString(Name + ".PhonesGridInfoFormat",
					"Unicode Values:\n{0}", App.kLocalizationGroupDialogs);

				string tip = bldr.ToString();
				tip = string.Format(fmt, tip.Substring(0, tip.Length - 2));

				tip = Utils.ConvertLiteralNewLines(tip);

				var rc = gridPhones.GetCellDisplayRectangle(0, e.RowIndex, true);
				var pt = gridPhones.PointToScreen(new Point(rc.Right - 5, rc.Bottom - 4));
				pt = PointToClient(pt);
				m_phoneToolTip.Tag = rc;
				m_phoneToolTip.Show(tip, this, pt, 5000);
			}
			catch { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandlePhoneGridCellMouseLeave(object sender, DataGridViewCellEventArgs e)
		{
			try
			{
				// Sometimes this event is fired because the mouse is over the tooltip, even
				// though it's tip's point is still within the bounds of the cell. It is only
				// when the mouse location leaves the cell do we want to hide the tooltip.
				Rectangle rc = gridPhones.GetCellDisplayRectangle(0, e.RowIndex, true);
				Point pt = gridPhones.PointToClient(MousePosition);
				if (!rc.Contains(pt))
					m_phoneToolTip.Hide(this);
			}
			catch { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandlePhoneGridRowEnter(object sender, DataGridViewCellEventArgs e)
		{
			var phoneInfo = m_phones[e.RowIndex];
			m_lvAFeatures.CurrentMask = phoneInfo.AMask;
			m_lvBFeatures.CurrentMask = phoneInfo.BMask;
			lblAFeatures.Text = m_lvAFeatures.FormattedFeaturesString;
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleArticulatoryFeatureCheckChanged(object sender, FeatureMask newMask)
		{
			lblAFeatures.Text = m_lvAFeatures.FormattedFeaturesString;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleTableLayoutPaint(object sender, PaintEventArgs e)
		{
			var rc = ((Control)sender).ClientRectangle;
			e.Graphics.DrawLine(SystemPens.GrayText, rc.X, rc.Bottom - 6,
				rc.Right - 1, rc.Bottom - 6);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void tabFeatures_SizeChanged(object sender, EventArgs e)
		{
			// There's a painting bug that manifests itself when tab control's change sizes.
			tabFeatures.Invalidate();
		}
	}
}