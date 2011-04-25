using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SIL.Pa.Model;
using SIL.Pa.Properties;
using SilTools;

namespace SIL.Pa.UI.Dialogs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class UndefinedPhoneticCharactersDlg : Form
	{
		private int m_defaultRowHeight;
		private UndefinedPhoneticCharactersInfoList m_currUdpcil;
		private readonly Dictionary<char, UndefinedPhoneticCharactersInfoList> m_udpciList;
		private readonly string m_infoFmt;
		private readonly string m_codepointColFmt =
			App.GetString("UndefinedPhoneticCharactersDlg.CharacterColumnHeadingFormat", "U+{0:X4}",
			"Format string for buiding the group heading text for the grid in the undefined phonetic characters dialog. The first parameter is the character and the second is the unicode value.");

		private readonly string m_codepointHdgFmt;
		private readonly Color m_defaultSelectedRowForeColor;
		private readonly Color m_defaultSelectedRowBackColor;
		private readonly PaProject m_project;

		/// ------------------------------------------------------------------------------------
		public static void Show(PaProject project)
		{
			Show(project, false);
		}

		/// ------------------------------------------------------------------------------------
		public static void Show(PaProject project, bool forceShow)
		{
			if (!forceShow && (project != null && !project.ShowUndefinedCharsDlg))
				return;

			if (App.IPASymbolCache.UndefinedCharacters == null || App.IPASymbolCache.UndefinedCharacters.Count <= 0)
				return;
			
			using (var dlg = new UndefinedPhoneticCharactersDlg(project, App.IPASymbolCache.UndefinedCharacters))
			{
				if (App.MainForm != null)
					App.MainForm.AddOwnedForm(dlg);

				dlg.ShowDialog();

				if (App.MainForm != null)
					App.MainForm.RemoveOwnedForm(dlg);
			}
		}

		/// ------------------------------------------------------------------------------------
		public UndefinedPhoneticCharactersDlg()
		{
			InitializeComponent();

			m_codepointHdgFmt = pgpWhere.Text;

			pgpChars.TextFormatFlags &= ~TextFormatFlags.HidePrefix;
			pgpWhere.TextFormatFlags &= ~TextFormatFlags.HidePrefix;
			pgpChars.DrawOnlyBottomBorder = true;
			pgpWhere.DrawOnlyBottomBorder = true;

			m_udpciList = new Dictionary<char, UndefinedPhoneticCharactersInfoList>();
			m_infoFmt = lblInfo.Text;
			lblInfo.Font = FontHelper.UIFont;
			chkShowUndefinedCharDlg.Font = FontHelper.UIFont;
			chkIgnoreInSearches.Font = FontHelper.UIFont;
			pgpChars.Font = FontHelper.UIFont;
			pgpWhere.Font = FontHelper.UIFont;
			pgpChars.BorderStyle = BorderStyle.None;
			pgpWhere.BorderStyle = BorderStyle.None;

			AddColumnsToCharsGrid();
			AddColumnsToWhereGrid();
			CalcWhereGridRowHeight();

			m_defaultSelectedRowForeColor = m_gridChars.RowsDefaultCellStyle.ForeColor;
			m_defaultSelectedRowBackColor = m_gridChars.RowsDefaultCellStyle.BackColor;

			HandleGridEnter(m_gridChars, null);
			HandleGridLeave(m_gridWhere, null);
		}
		
		/// ------------------------------------------------------------------------------------
		public UndefinedPhoneticCharactersDlg(PaProject project,
			UndefinedPhoneticCharactersInfoList list) : this()
		{
			m_project = project;

			if (project != null)
			{
				chkShowUndefinedCharDlg.Checked = m_project.ShowUndefinedCharsDlg;
				chkIgnoreInSearches.Checked = m_project.IgnoreUndefinedCharsInSearches;
			}
			
			list.Sort(CompareUndefinedCharValues);
			var projectName = (project == null ? string.Empty : project.Name);
			lblInfo.Text = string.Format(m_infoFmt, projectName, Application.ProductName);
			LoadCharGrid(list);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add columns to fonts grid
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddColumnsToCharsGrid()
		{
			// Add the Unicode number column.
			DataGridViewColumn col = SilGrid.CreateTextBoxColumn("codepoint");
			col.SortMode = DataGridViewColumnSortMode.Automatic;
			m_gridChars.Columns.Add(col);
			App.RegisterForLocalization(m_gridChars.Columns["codepoint"],
				"UndefinedPhoneticCharacterDlg.UnicodeNumberColumnHeadingText",
				"Unicode\nNumber", "Heading for column in undefined phonetic chars. dialog");

			// Add the sample column.
			col = SilGrid.CreateTextBoxColumn("char");
			col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
			col.DefaultCellStyle.Font = App.PhoneticFont;
			col.CellTemplate.Style.Font = App.PhoneticFont;
			m_gridChars.Columns.Add(col);
			App.RegisterForLocalization(m_gridChars.Columns["char"],
				"UndefinedPhoneticCharacterDlg.CharacterColumnHeadingText",
				"Character", "Heading for column in undefined phonetic chars. dialog");

			// Add the count number column.
			col = SilGrid.CreateTextBoxColumn("count");
			col.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
			col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
			m_gridChars.Columns.Add(col);
			App.RegisterForLocalization(m_gridChars.Columns["count"],
				"UndefinedPhoneticCharacterDlg.CountColumnHeadingText",
				"Count", "Heading for column in undefined phonetic chars. dialog");

			m_gridChars.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
			m_gridChars.AutoResizeColumnHeadersHeight();
			m_gridChars.Name = Name + "CharsGrid";
			AppColor.SetGridSelectionColors(m_gridChars, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add columns to fonts grid
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void AddColumnsToWhereGrid()
		{
			m_gridWhere.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;

			DataGridViewColumn col = SilGrid.CreateTextBoxColumn("word");
			col.DefaultCellStyle.Font = App.PhoneticFont;
			col.CellTemplate.Style.Font = App.PhoneticFont;
			m_gridWhere.Columns.Add(col);
			App.RegisterForLocalization(m_gridWhere.Columns["word"],
				"UndefinedPhoneticCharacterDlg.WordColumnHeadingText", "Transcription",
				"Heading for column in undefined phonetic chars. dialog");

			// Add the reference column.
			col = SilGrid.CreateTextBoxColumn("reference");
			m_gridWhere.Columns.Add(col);
			App.RegisterForLocalization(m_gridWhere.Columns["reference"],
				"UndefinedPhoneticCharacterDlg.ReferenceColumnHeadingText", "Reference",
				"Heading for column in undefined phonetic chars. dialog");

			// Add the data source column.
			col = SilGrid.CreateTextBoxColumn("datasource");
			m_gridWhere.Columns.Add(col);
			App.RegisterForLocalization(m_gridWhere.Columns["datasource"],
				"UndefinedPhoneticCharacterDlg.DataSourceColumnHeadingText", "Data Source",
				"Heading for column in undefined phonetic chars. dialog");

			m_gridWhere.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			m_gridWhere.AutoResizeColumnHeadersHeight();
			m_gridWhere.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
			m_gridWhere.Name = Name + "WhereGrid";
			AppColor.SetGridSelectionColors(m_gridWhere, false);
		}

		/// ------------------------------------------------------------------------------------
		private void CalcWhereGridRowHeight()
		{
			m_defaultRowHeight = 0;

			foreach (DataGridViewColumn col in m_gridWhere.Columns)
			{
				m_defaultRowHeight = Math.Max(m_defaultRowHeight,
					(col.DefaultCellStyle.Font ?? FontHelper.UIFont).Height);
			}

			// Add a little vertical padding.
			m_defaultRowHeight += 4;
		}

		/// ------------------------------------------------------------------------------------
		private void LoadCharGrid(UndefinedPhoneticCharactersInfoList list)
		{
			Utils.WaitCursors(true);
			m_gridChars.Rows.Clear();
			
			DataGridViewRow prevRow = null;
			int count = 0;
			char prevChar = (char)0;

			for (int i = list.Count - 1; i >= 0; i--)
			{
				if (prevChar != list[i].Character)
				{
					if (prevRow != null)
						prevRow.Cells["count"].Value = count;

					count = 0;
					prevChar = list[i].Character;
					m_udpciList[prevChar] = new UndefinedPhoneticCharactersInfoList();
					m_gridChars.Rows.Insert(0, new object[] {
						string.Format(m_codepointColFmt, (int)prevChar), prevChar, 0});

					prevRow = m_gridChars.Rows[0];
				}

				count++;
				m_udpciList[prevChar].Add(list[i]);
			}

			if (prevRow != null)
				prevRow.Cells["count"].Value = count;

			m_gridChars.AutoResizeColumns();
			m_gridChars.CurrentCell = m_gridChars[0, 0];
			Utils.WaitCursors(false);
		}

		/// ------------------------------------------------------------------------------------
		private static int CompareUndefinedCharValues(UndefinedPhoneticCharactersInfo x,
			UndefinedPhoneticCharactersInfo y)
		{
			if (x == null && y == null)
				return 0;

			if (x == null)
				return -1;

			if (y == null)
				return 1;

			return (x.Character.CompareTo(y.Character));
		}

		#region Event handlers and overrides
		/// ------------------------------------------------------------------------------------
		protected override void OnLoad(EventArgs e)
		{
			Settings.Default.UndefinedPhoneticCharactersDlg =
				App.InitializeForm(this, Settings.Default.UndefinedPhoneticCharactersDlg);
			
			base.OnLoad(e);
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			if (Settings.Default.UndefinedPhoneticCharactersDlgSplitLoc > 0)
				splitContainer1.SplitterDistance = Settings.Default.UndefinedPhoneticCharactersDlgSplitLoc;

			if (Settings.Default.UndefinedPhoneticCharactersDlgCharsGrid != null)
				Settings.Default.UndefinedPhoneticCharactersDlgCharsGrid.InitializeGrid(m_gridChars);

			if (Settings.Default.UndefinedPhoneticCharactersDlgWhereGrid != null)
				Settings.Default.UndefinedPhoneticCharactersDlgWhereGrid.InitializeGrid(m_gridWhere);
		}
		
		/// ------------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			App.MsgMediator.SendMessage(Name + "HandleCreated", this);
		}

		/// ------------------------------------------------------------------------------------
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			Settings.Default.UndefinedPhoneticCharactersDlgCharsGrid = GridSettings.Create(m_gridChars);
			Settings.Default.UndefinedPhoneticCharactersDlgWhereGrid = GridSettings.Create(m_gridWhere);
			Settings.Default.UndefinedPhoneticCharactersDlgSplitLoc = splitContainer1.SplitterDistance;	

			if (m_project != null)
			{
				if (m_project.ShowUndefinedCharsDlg != chkShowUndefinedCharDlg.Checked ||
					m_project.IgnoreUndefinedCharsInSearches != chkIgnoreInSearches.Checked)
				{
					m_project.ShowUndefinedCharsDlg = chkShowUndefinedCharDlg.Checked;
					m_project.IgnoreUndefinedCharsInSearches = chkIgnoreInSearches.Checked;
					m_project.Save();
				}
			}

			base.OnFormClosing(e);
		}
	
		/// ------------------------------------------------------------------------------------
		private void m_grid_RowHeightInfoNeeded(object sender, DataGridViewRowHeightInfoNeededEventArgs e)
		{
			e.Height = m_defaultRowHeight;
			e.MinimumHeight = 10;
		}

		/// ------------------------------------------------------------------------------------
		private void m_gridChars_RowEnter(object sender, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex < 0)
				return;

			char currChar;
			if (m_gridChars["char", e.RowIndex].Value is char)
				currChar = (char)m_gridChars["char", e.RowIndex].Value;
			else
			{
				m_gridWhere.Rows.Clear();
				return;
			}

			if (m_gridWhere.Tag is char && (char)m_gridWhere.Tag == currChar)
				return;

			m_gridWhere.Tag = currChar;
			m_gridWhere.Rows.Clear();

			if (m_udpciList.TryGetValue(currChar, out m_currUdpcil))
			{
				pgpWhere.Text = string.Format(m_codepointHdgFmt, (int)currChar);
				m_currUdpcil = m_udpciList[currChar];
				m_gridWhere.RowCount = m_udpciList[currChar].Count;
				
				if (m_gridWhere.RowCount > 0)
					m_gridWhere.CurrentCell = m_gridWhere[0, 0];
			}
		}

		/// ------------------------------------------------------------------------------------
		private void m_gridWhere_CellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
		{
			e.Value = null;
			int row = e.RowIndex;

			if (m_currUdpcil == null)
				return;

			switch (e.ColumnIndex)
			{
				case 0: e.Value = m_currUdpcil[row].Transcription; break;
				case 1: e.Value = m_currUdpcil[row].Reference; break;
				case 2: e.Value = m_currUdpcil[row].SourceName; break;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resize the information label when it's size 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);

			using (var g = lblInfo.CreateGraphics())
			{
				const TextFormatFlags kFlags = TextFormatFlags.NoClipping | TextFormatFlags.WordBreak;
				var propSz = new Size(lblInfo.Width, int.MaxValue);
				var sz = TextRenderer.MeasureText(g, lblInfo.Text, lblInfo.Font, propSz, kFlags);
				lblInfo.Height = sz.Height + 15;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the selected row color when the grid loses focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleGridLeave(object sender, EventArgs e)
		{
			var grid = sender as DataGridView;
			if (grid != null)
			{
				grid.RowsDefaultCellStyle.SelectionForeColor = SystemColors.ControlText;
				grid.RowsDefaultCellStyle.SelectionBackColor = SystemColors.Control;

				if (grid.CurrentRow != null)
					grid.InvalidateRow(grid.CurrentRow.Index);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Change the selected row color when the grid gains focus.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void HandleGridEnter(object sender, EventArgs e)
		{
			DataGridView grid = sender as DataGridView;
			if (grid != null)
			{
				grid.RowsDefaultCellStyle.SelectionForeColor = m_defaultSelectedRowForeColor;
				grid.RowsDefaultCellStyle.SelectionBackColor = m_defaultSelectedRowBackColor;

				if (grid.CurrentRow != null)
					grid.InvalidateRow(grid.CurrentRow.Index);
			}
		}

		/// ------------------------------------------------------------------------------------
		private void btnOK_Click(object sender, EventArgs e)
		{
			Close();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Show some temp. help until the help files are ready.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, EventArgs e)
		{
			App.ShowHelpTopic(this);
		}

		#endregion
	}
}