using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using System.Resources;
using System.IO;
using System.Reflection;

namespace SIL.Pa.Resources
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// 
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class ResourceHelper : Form
	{
		private static ResourceManager s_stringResources = null;
		private static ResourceManager s_helpResources = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// 
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ResourceHelper()
		{
			InitializeComponent();

			if (s_stringResources == null)
			{
				s_stringResources = new System.Resources.ResourceManager(
					"SIL.Pa.Resources.PaStrings", Assembly.GetExecutingAssembly());
			}

			if (s_helpResources == null)
			{
				s_helpResources = new System.Resources.ResourceManager(
					"SIL.Pa.Resources.HelpTopicPaths", Assembly.GetExecutingAssembly());
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string from the resource file using the specified resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// -----------------------------------------------------------------------------------
		private static string GetString(string stid, ResourceManager resMngr)
		{
			string str = (stid == null || resMngr == null ? null : resMngr.GetString(stid));
			return (string.IsNullOrEmpty(str) ? stid : str);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Returns a string from the resource file using the specified resource ID.
		/// </summary>
		/// <param name="stid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		public static string GetString(string stid)
		{
			if (s_stringResources == null)
			{
				s_stringResources = new System.Resources.ResourceManager(
					"SIL.Pa.Resources.PaStrings", Assembly.GetExecutingAssembly());
			}

			return GetString(stid, s_stringResources);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return a help topic or help file path.
		/// </summary>
		/// <param name="hid">String resource id</param>
		/// <returns>String</returns>
		/// -----------------------------------------------------------------------------------
		 public static string GetHelpString(string hid)
		{
			if (s_helpResources == null)
			{
				s_helpResources = new System.Resources.ResourceManager(
					"SIL.Pa.Resources.HelpTopicPaths", Assembly.GetExecutingAssembly());
			}

			if (string.IsNullOrEmpty(hid))
				hid = "hidTopicDoesNotExist";

			return s_helpResources.GetString(hid);
		}
	}
}