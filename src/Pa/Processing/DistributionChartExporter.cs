using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.Pa.PhoneticSearching;
using SIL.Pa.Properties;
using SIL.Pa.UI.Controls;
using SilTools;

namespace SIL.Pa.Processing
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class provides a way to export a vowel or consonant chart to an XML format that
	/// is transformed into an html file with an accompanying cascading style sheet.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DistributionChartExporter : ExporterBase
	{
		private readonly Dictionary<DataGridViewColumn, DistributionChartExceptionInfo> m_colExceptions =
			new Dictionary<DataGridViewColumn, DistributionChartExceptionInfo>();

		private readonly Dictionary<DataGridViewRow, DistributionChartExceptionInfo> m_rowExceptions =
			new Dictionary<DataGridViewRow, DistributionChartExceptionInfo>();

		/// ------------------------------------------------------------------------------------
		public static bool ToHtml(PaProject project, string outputFileName,
			XYGrid distChartGrid, bool openAfterExport)
		{
			return Process(project, outputFileName, OutputFormat.XHTML, distChartGrid,
				openAfterExport, Settings.Default.AppThatOpensHtml,
				Pipeline.ProcessType.ExportToXHTML);
		}

		/// ------------------------------------------------------------------------------------
		public static bool ToWordXml(PaProject project, string outputFileName,
			XYGrid distChartGrid, bool openAfterExport)
		{
			return Process(project, outputFileName, OutputFormat.WordXml, distChartGrid,
				openAfterExport, Settings.Default.AppThatOpensWordXml,
				Pipeline.ProcessType.ExportToWord);
		}

		/// ------------------------------------------------------------------------------------
		public static bool ToXLingPaper(PaProject project, string outputFileName,
			XYGrid distChartGrid, bool openAfterExport)
		{
			return Process(project, outputFileName, OutputFormat.XHTML, distChartGrid,
				openAfterExport, Settings.Default.AppThatOpensXLingPaperXML,
				new[] { Pipeline.ProcessType.ExportToXLingPaper });
		}

		/// ------------------------------------------------------------------------------------
		private static bool Process(PaProject project, string outputFileName,
			OutputFormat outputFormat, DataGridView grid, bool openAfterExport,
			string appToOpenOutput, Pipeline.ProcessType finalPipeline)
		{
			return Process(project, outputFileName, outputFormat, grid, openAfterExport,
				appToOpenOutput, Pipeline.ProcessType.ExportDistributionChart, finalPipeline);
		}

		/// ------------------------------------------------------------------------------------
		private static bool Process(PaProject project, string outputFileName,
			OutputFormat outputFormat, DataGridView grid, bool openAfterExport,
			string appToOpenOutput, params Pipeline.ProcessType[] pipeline)
		{
			var exporter = new DistributionChartExporter(project, outputFileName, outputFormat, grid);

			var result = exporter.InternalProcess(
				Settings.Default.KeepTempDistributionChartExportFile, pipeline);

			if (result && openAfterExport)
				CallAppToExportedFile(appToOpenOutput, outputFileName);

			return result;
		}

		/// ------------------------------------------------------------------------------------
		private DistributionChartExporter(PaProject project, string outputFileName,
			OutputFormat outputFormat, DataGridView distChartGrid)
			: base(project, outputFileName, outputFormat, distChartGrid)
		{
			BuildHeaderErrorLists();
		}

		/// ------------------------------------------------------------------------------------
		protected override string Title
		{
			get
			{
				return (string.IsNullOrEmpty(((XYGrid)m_grid).ChartName) ?
					"Distribution Chart" : ((XYGrid)m_grid).ChartName);
			}
		}

		/// ------------------------------------------------------------------------------------
		protected override string Name
		{
			get { return ((XYGrid)m_grid).ChartName; }
		}

		/// ------------------------------------------------------------------------------------
		protected override string View
		{
			get { return "Distribution Chart"; }
		}

		/// ------------------------------------------------------------------------------------
		protected override string TableClass
		{
			get { return "distribution chart"; }
		}

		/// ------------------------------------------------------------------------------------
		protected override string NumberOfRecords
		{
			get { return App.WordCache.Count.ToString(); }
		}

		/// ------------------------------------------------------------------------------------
		protected override IEnumerable<KeyValuePair<string, Font>> GetFormattingFieldInfo()
		{
			yield return new KeyValuePair<string, Font>("Phonetic", FontHelper.PhoneticFont);
		}

		/// ------------------------------------------------------------------------------------
		protected override IEnumerable<DataGridViewRow> GetGridRows()
		{
			// Skip the first row because in a distribution chart, it is like a heading row.
			return from x in m_grid.Rows.Cast<DataGridViewRow>()
				   where x.Visible && x.Index > 0 && x.Index != m_grid.NewRowIndex
				   select x;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override IEnumerable<DataGridViewColumn> GetGridColumns()
		{
			// Skip the first column because, in a distribution chart, it is like a row heading.
			return from x in m_grid.Columns.Cast<DataGridViewColumn>()
				   orderby x.DisplayIndex
				   where x.Visible && x.Index > 0 && x.Index < m_grid.ColumnCount - 1
				   select x;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Verifies the search patterns in the row and column headings.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void BuildHeaderErrorLists()
		{
			foreach (var col in GetGridColumns())
			{
				var envQuery = col.Tag as SearchQuery;
				if (envQuery != null && !string.IsNullOrEmpty(envQuery.Pattern))
				{
					var query = new SearchQuery("[V]/" + envQuery.Pattern);
					m_colExceptions[col] = GetException(query);
				}
			}

			foreach (var row in GetGridRows())
			{
				var srchItem = row.Cells[0].Value as string;
				if (srchItem != null)
				{
					var query = new SearchQuery(srchItem + "/*_*");
					m_rowExceptions[row] = GetException(query);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		private DistributionChartExceptionInfo GetException(SearchQuery query)
		{
			SearchQueryException e;
			var engine = query.GetSearchEngine(out e);

			if (e != null)
				return new DistributionChartExceptionInfo { Class = "error", Message = e.QueryErrorMessage };

			var msg = engine.GetCombinedErrorMessages(false);
			if (msg != null)
				return new DistributionChartExceptionInfo { Class = "error", Message = msg };

			var invalidPhones = query.GetPhonesNotInCache() as string[];
			if (invalidPhones != null)
			{
				msg = App.L10NMngr.LocalizeString("ChartHtmlPopupInfoInvalidPhonesMsg",
					"This pattern contains the following phone(s) not found in the data: ",
					"HTMLExport.Distribution Charts");

				foreach (var phone in invalidPhones)
					msg += (phone + ", ");

				return new DistributionChartExceptionInfo {Class = "caution", Message = msg.Trim().TrimEnd(',', ' ') };
			}

			var invalidSymbols = query.GetSymbolsNotInInventory() as char[];
			if (invalidSymbols != null)
			{
				msg = App.L10NMngr.LocalizeString("ChartHtmlPopupInfoUndefinedSymbolsMsg",
					"This pattern contains the following undefined phonetic symbol(s): ",
					"HTMLExport.Distribution Charts");

				var fmt = App.L10NMngr.LocalizeString(
					"ChartHtmlPopupUndefinedSymbolFormatMsg", "{0} (U+{1}), ",
					"HTMLExport.Distribution Charts");

				var bldr = new StringBuilder(msg);
				foreach (var c in invalidSymbols)
					bldr.AppendFormat(fmt, c, ((int)c).ToString("X4"));

				return  new DistributionChartExceptionInfo { Class = "caution", Message = bldr.ToString().Trim().TrimEnd(',', ' ') };
			}

			return null;
		}

		/// ------------------------------------------------------------------------------------
		protected override void WriteTableHeadingColumnGroups()
		{
			// Write group for far left column containing search item.
			ProcessHelper.WriteColumnGroup(m_writer, 1);

			// Write group for each column after the search item column, but not including
			// the last column which is always empty to provide a space for the user to
			// add another search pattern column.
			ProcessHelper.WriteColumnGroup(m_writer, GetGridColumns().Count());
		}

		/// ------------------------------------------------------------------------------------
		protected override void WriteTableHeadingContent()
		{
			// Write an empty element for the first column, because it is like a row heading.
			ProcessHelper.WriteEmptyElement(m_writer, "th");

			foreach (var col in GetGridColumns())
			{
				DistributionChartExceptionInfo dcei;

				if (!m_colExceptions.TryGetValue(col, out dcei) || dcei == null)
				{
					ProcessHelper.WriteStartElementWithAttrib(m_writer, "th", "class", "Phonetic");
					m_writer.WriteAttributeString("scope", "col");
					m_writer.WriteString(m_grid[col.Index, 0].Value as string);
					m_writer.WriteEndElement();
				}
				else
				{
					ProcessHelper.WriteStartElementWithAttrib(m_writer, "th", "class", "Phonetic " + dcei.Class);
					m_writer.WriteAttributeString("scope", "col");
					m_writer.WriteAttributeString("title", dcei.Message);
					m_writer.WriteStartElement("strong");
					m_writer.WriteString(m_grid[col.Index, 0].Value as string);
					m_writer.WriteEndElement();
					m_writer.WriteEndElement();
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		protected override void WriteTableRowContent(DataGridViewRow row)
		{
			DistributionChartExceptionInfo dcei;
			if (m_rowExceptions.TryGetValue(row, out dcei) && dcei != null)
			{
				WriteTableRowError(row);
				return;
			}

			ProcessHelper.WriteStartElementWithAttrib(m_writer, "th", "class", "Phonetic");
			m_writer.WriteAttributeString("scope", "row");
			m_writer.WriteString(row.Cells[0].Value as string);
			m_writer.WriteEndElement();

			foreach (var col in GetGridColumns())
				WriteTableRowCell(row.Cells[col.Index]);
		}

		/// ------------------------------------------------------------------------------------
		private void WriteTableRowError(DataGridViewRow row)
		{
			ProcessHelper.WriteStartElementWithAttrib(m_writer,
				"th", "class", "Phonetic " + m_rowExceptions[row].Class);
			
			m_writer.WriteAttributeString("scope", "row");
			m_writer.WriteAttributeString("title", m_rowExceptions[row].Message);
			m_writer.WriteStartElement("strong");
			m_writer.WriteString(row.Cells[0].Value as string);
			m_writer.WriteEndElement();
			m_writer.WriteEndElement();

			foreach (var col in GetGridColumns())
			{
				ProcessHelper.WriteStartElementWithAttrib(m_writer, "td", "class", "error");
				m_writer.WriteEndElement();
			}
		}

		/// ------------------------------------------------------------------------------------
		private void WriteTableRowCell(DataGridViewCell cell)
		{
			m_writer.WriteStartElement("td");

			var value = cell.Value;

			if (value is SearchQueryException)
				m_writer.WriteAttributeString("class", "error");
			else
			{
				var count = (value != null && value.GetType() == typeof(int) ?
					((int)value).ToString() : value as string);

				if (!string.IsNullOrEmpty(count))
				{
					if (count != "0")
						m_writer.WriteString(count);
					else
					{
						m_writer.WriteAttributeString("class", cell.Tag == null ? "zero" : "caution");
						m_writer.WriteString(count);
					}
				}
			}

			m_writer.WriteEndElement();
		}
	}

	/// ----------------------------------------------------------------------------------------
	public class DistributionChartExceptionInfo
	{
		public string Class { get; set; }
		public string Message { get; set; }
	}
}
