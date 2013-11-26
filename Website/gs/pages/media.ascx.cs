using System;
using System.Globalization;
using System.Web.UI;
using System.Web.UI.WebControls;
using GalleryServerPro.Business;

namespace GalleryServerPro.Web.Pages
{
	/// <summary>
	/// A page-like user control that renders the left pane, center pane, and right pane.
	/// </summary>
	public partial class media : Pages.GalleryPage
	{
		#region Fields

		private Panel _allPanesContainer;
		private Panel _centerAndRightPanesContainer;
		private Panel _leftPane;
		private Panel _centerPane;
		private Panel _rightPane;

		#endregion

		#region Properties

		private bool LeftPaneVisible { get { return ShouldPageHaveTreeView(); } }
		private bool LeftPaneDocked { get { return false; } }
		private bool CenterPaneVisible { get { return ShowCenterPane; } }
		private bool RightPaneVisible { get { return ShowRightPane; } }
		private bool RightPaneDocked { get { return false; } }

		private Panel AllPanesContainer
		{
			get
			{
				if (_allPanesContainer == null)
				{
					_allPanesContainer = new Panel();
					_allPanesContainer.ID = "media";
					_allPanesContainer.CssClass = "gsp_s_c";
				}

				return _allPanesContainer;
			}
		}

		private Panel LeftPane
		{
			get
			{
				if (_leftPane == null)
				{
					_leftPane = new Panel();
					_leftPane.ClientIDMode = ClientIDMode.Static;
					_leftPane.ID = LeftPaneClientId;
					_leftPane.CssClass = "gsp_tb_s_LeftPane gsp_tb_s_pane";
					//_leftPane.Controls.Add(AlbumTreeView);
				}

				return _leftPane;
			}
		}

		//private Controls.albumtreeview AlbumTreeView
		//{
		//  get
		//  {
		//    Controls.albumtreeview albumTreeView = (Controls.albumtreeview)LoadControl(Utils.GetUrl("/controls/albumtreeview.ascx"));

		//    albumTreeView.RequiredSecurityPermissions = SecurityActions.ViewAlbumOrMediaObject;
		//    //albumTreeView.TreeViewTheme = "gsp";
		//    albumTreeView.ShowCheckbox = false;
		//    albumTreeView.NavigateUrl = Utils.GetCurrentPageUrl();

		//    int albumId = GetAlbumId();
		//    if (albumId > int.MinValue)
		//    {
		//      albumTreeView.SelectedAlbumIds.Add(albumId);
		//    }

		//    if (albumTreeView.TreeView.Nodes.Count > 0)
		//    {
		//      AlbumTreeViewIsVisible = true;
		//    }

		//    return albumTreeView;
		//  }
		//}

		private Panel CenterAndRightPanesContainer
		{
			get
			{
				if (_centerAndRightPanesContainer == null)
				{
					_centerAndRightPanesContainer = new Panel();
					_centerAndRightPanesContainer.ID = "mediaCR";
					_centerAndRightPanesContainer.CssClass = "gsp_tb_s_CenterAndRightPane";
				}

				return _centerAndRightPanesContainer;
			}
		}

		private Panel CenterPane
		{
			get
			{
				if (_centerPane == null)
				{
					_centerPane = new Panel();
					_centerPane.CssClass = "gsp_tb_s_CenterPane gsp_tb_s_pane";

					if (PageId == Web.PageId.album)
					{
						_centerPane.Controls.Add(AlbumThumbnails);
					}
					else
					{
						_centerPane.Controls.Add(MediaView);
					}

					// Add the GSP logo, if needed
					var license = AppSetting.Instance.License.LicenseType;
					if (license == LicenseLevel.NotSet || license == LicenseLevel.Gpl)
					{
						_centerPane.Controls.Add(GspLogo);
					}
				}

				return _centerPane;
			}
		}

		private Controls.thumbnailview AlbumThumbnails
		{
			get
			{
				return (Controls.thumbnailview)LoadControl(Utils.GetUrl("/controls/thumbnailview.ascx"));
			}
		}

		private Controls.mediaview MediaView
		{
			get
			{
				return (Controls.mediaview)LoadControl(Utils.GetUrl("/controls/mediaview.ascx"));
			}
		}

		private Panel RightPane
		{
			get
			{
				if (_rightPane == null)
				{
					_rightPane = new Panel();
					_rightPane.ClientIDMode = ClientIDMode.Static;
					_rightPane.ID = RightPaneClientId;
					_rightPane.CssClass = "gsp_tb_s_RightPane gsp_tb_s_pane";
					//_rightPane.Controls.Add(new LiteralControl("Right pane"));
				}

				return _rightPane;
			}
		}

		private string LeftPaneHtmlTmplClientId
		{
			get { return String.Concat(ClientID, "_lpHtmlTmpl"); }
		}

		private string LeftPaneScriptTmplClientId
		{
			get { return String.Concat(ClientID, "_lpScriptTmpl"); }
		}

		private string RightPaneHtmlTmplClientId
		{
			get { return String.Concat(ClientID, "_rpHtmlTmpl"); }
		}

		private string RightPaneScriptTmplClientId
		{
			get { return String.Concat(ClientID, "_rpScriptTmpl"); }
		}


		#endregion

		#region Constructors

		protected media()
		{
			this.BeforeHeaderControlsAdded += MediaBeforeHeaderControlsAdded;
		}

		#endregion

		#region Events

		protected void Page_Load(object sender, EventArgs e)
		{
			AddPanes();

			RegisterJavascript();
		}

		private void AddPanes()
		{
			Panel pnl;
			if (LeftPaneVisible)
			{
				pnl = AllPanesContainer;
				pnl.Controls.Add(LeftPane);
				pnl.Controls.Add(CenterAndRightPanesContainer);
			}
			else
			{
				pnl = CenterAndRightPanesContainer;
			}

			if (CenterPaneVisible)
			{
				CenterAndRightPanesContainer.Controls.Add(CenterPane);
			}

			if (RightPaneVisible)
			{
				CenterAndRightPanesContainer.Controls.Add(RightPane);
			}

			this.Controls.Add(pnl);
		}

		private void MediaBeforeHeaderControlsAdded(object sender, EventArgs e)
		{
			//ShowLeftPaneForAlbum = this.GalleryControl.ShowLeftPaneForAlbum.GetValueOrDefault(false);
		}

		#endregion

		#region Functions

		private void RegisterJavascript()
		{
			// Add left and right pane templates, then invoke their scripts.
			// Note that when the header is visible, we wait for it to finish rendering before running our script.
			// We do this so  that the splitter's height calculations are correct.
			string script = String.Format(CultureInfo.InvariantCulture, @"
{0}
{1}
<script>
	$().ready(function () {{
		var runPaneScripts = function() {{
			{2}
			{3}
		}};

		if (window.{4}.gspData.Settings.ShowHeader)
			$(document.documentElement).on('gspHeaderLoaded.{4}', runPaneScripts);
		else
			runPaneScripts();
	}});
</script>
",
				GetLeftPaneTemplates(), // 0
				GetRightPaneTemplates(), // 1
				GetLeftPaneScript(), // 2
				GetRightPaneScript(), // 3
				GspClientId // 4
				);

			this.Page.ClientScript.RegisterStartupScript(this.GetType(), String.Concat(this.ClientID, "_mediaScript"), script, false);
		}

		private string GetLeftPaneTemplates()
		{
			var uiTemplate = UiTemplates.Get(UiTemplateType.LeftPane, GetAlbum());

			return String.Format(CultureInfo.InvariantCulture, @"
<script id='{0}' type='text/x-jsrender'>
{1}
</script>
<script id='{2}' type='text/x-jsrender'>
{3}
</script>
",
																		LeftPaneHtmlTmplClientId, // 0
																		uiTemplate.HtmlTemplate, // 1
																		LeftPaneScriptTmplClientId, // 2
																		uiTemplate.ScriptTemplate // 3
);
		}

		private string GetRightPaneTemplates()
		{
			var uiTemplate = UiTemplates.Get(UiTemplateType.RightPane, GetAlbum());

			return String.Format(CultureInfo.InvariantCulture, @"
<script id='{0}' type='text/x-jsrender'>
{1}
</script>
<script id='{2}' type='text/x-jsrender'>
{3}
</script>
",
																		RightPaneHtmlTmplClientId, // 0
																		uiTemplate.HtmlTemplate, // 1
																		RightPaneScriptTmplClientId, // 2
																		uiTemplate.ScriptTemplate // 3
);
		}

		private string GetLeftPaneScript()
		{
			if (!LeftPaneVisible)
				return String.Empty;

			// If isTouchScreen = true & center and right pane is visible, then hide left pane
			// But what if someone changes the left pane template and *wants* it visible?
			// Move logic to left pane template script?

			// Call splitter jQuery plug-in that sets up the split between the left and center panes
			// The splitter is only called when the gallery control's width is greater than 750px, because
			// we don't want it on small media screens (like smart phones)
			return String.Format(CultureInfo.InvariantCulture, @"
$.templates({{{0}: $('#{1}').html() }});
(new Function($('#{2}').render(window.{3}.gspData)))();
{4}
",
					LeftPaneTmplName, // 0
					LeftPaneHtmlTmplClientId, // 1
					LeftPaneScriptTmplClientId, // 2
					GspClientId, // 3
					GetLeftPaneSplitterScript() // 4
					);
		}

		private string GetLeftPaneSplitterScript()
		{
			if (!CenterPaneVisible)
				return String.Empty;

			// Call splitter jQuery plug-in that sets up the split between the left and center panes
			// The splitter is only called for non-touch screens when the gallery control's width is greater than 750px, 
			// because we don't want it on small media screens (like smart phones) and the splitter isn't touchable.
			return String.Format(CultureInfo.InvariantCulture, @"
if (!window.Gsp.isTouchScreen() && $('#{0}').width() >= 750) {{
	$('#{1}').splitter({{
		type: 'v',
		outline: false,
		minLeft: 100, sizeLeft: {2}, maxLeft: 600,
		dock: 'left',
		dockSpeed: 200,
		anchorToWindow: true,
		accessKey: 'L',
		splitbarClass: 'gsp_vsplitbar',
		cookie: 'gsp_left-pane_{1}',
		cookiePath: '/'
	}});
}}
",
					GspClientId, // 0
					AllPanesContainer.ClientID, // 1
					LeftPaneDocked ? "0" : "true" // 2
					);
		}

		private string GetRightPaneScript()
		{
			if (!RightPaneVisible)
				return String.Empty;

			// Call splitter jQuery plug-in that sets up the split between the center and right panes.
			// The splitter is only called when the gallery control's width is greater than 750px, because
			// we don't want it on small media screens (like smart phones)
			return String.Format(CultureInfo.InvariantCulture, @"
$.templates({{{0}: $('#{1}').html() }});
(new Function($('#{2}').render(window.{3}.gspData)))();

{4}
",
			RightPaneTmplName, // 0
			RightPaneHtmlTmplClientId, // 1
			RightPaneScriptTmplClientId, // 2
			GspClientId, // 3
			GetRightPaneSplitterScript()
			);
		}

		private string GetRightPaneSplitterScript()
		{
			// Call splitter jQuery plug-in that sets up the split between the center and right panes.
			// The splitter is only called for non-touch screens when the gallery control's width is greater than 750px, 
			// because we don't want it on small media screens (like smart phones) and the splitter isn't touchable.
			if (!CenterPaneVisible)
				return String.Empty;

			return String.Format(CultureInfo.InvariantCulture, @"
if (!window.Gsp.isTouchScreen() && $('#{0}').width() >= 750) {{
	$('#{1}').splitter({{
		type: 'v',
		outline: false,
		minRight: 100, sizeRight: {2}, maxRight: 1000,
		dock: 'right',
		dockSpeed: 200{3},
		accessKey: 'R',
		splitbarClass: 'gsp_vsplitbar',
		cookie: 'gsp_right-pane_{1}',
		cookiePath: '/'
	}});
}}
",
			GspClientId, // 0
			CenterAndRightPanesContainer.ClientID, // 1
			RightPaneDocked ? "0" : "true", // 2
			LeftPaneVisible ? String.Empty : ",anchorToWindow: true" // 3
			);

		}

		private bool ShouldPageHaveTreeView()
		{
			// The only pages that should display an album treeview are the album and media object pages.
			switch (PageId)
			{
				case PageId.album:
					return ShowLeftPaneForAlbum;

				case PageId.mediaobject:
					return ShowLeftPaneForMediaObject;

				default:
					return false;
			}
		}

		#endregion
	}
}