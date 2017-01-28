using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Text.RegularExpressions;

using Rock;
using Rock.Attribute;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "File Browser" )]
    [Category( "com_shepherdchurch > Misc" )]
    [Description( "Provides a simple file browser that allows users to download files from the file system." )]

    [TextField( "Content Root", "The root folder, relative to the Rock installation root, to display files form.", true, "~/Content", "", order: 0 )]
    [CodeEditorField( "Lava Template", "The Lava used to render the output of the file list. Two out-of-the-box files are available, FileBrowserButtons.lava and FileBrowserList.lava. Or you can build your own.", CodeEditorMode.Lava, defaultValue: "{% include '~/Plugins/com_shepherdchurch/Misc/Assets/FileBrowserList.lava' %}", order: 1 )]
    public partial class FileBrowser : RockBlock
    {
        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddCSSLink( ResolveRockUrl( "~/Plugins/com_shepherdchurch/Misc/Styles/FileBrowser.css" ) );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );
        }

        /// <summary>
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            ScriptManager.GetCurrent( this.Page ).RegisterPostBackControl( btnDownload );

            if ( !IsPostBack )
            {
                DisplayFolder( "/" );
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Display the contents of the given folder, which is relative to the ContentRoot block setting.
        /// </summary>
        /// <param name="folder">The path whose contents need to be displayed.</param>
        void DisplayFolder( string folder )
        {
            string LavaTemplate = GetAttributeValue( "LavaTemplate" );
            string virtualPath = VirtualPathUtility.RemoveTrailingSlash( GetAttributeValue( "ContentRoot" ) ) + folder;
            string realPath = Request.MapPath( VirtualPathUtility.AppendTrailingSlash( virtualPath ) );
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage );

            //
            // Validate they aren't trying to get outside of the root.
            //
            string rootPath = Request.MapPath( VirtualPathUtility.AppendTrailingSlash( GetAttributeValue( "ContentRoot" ) ) );
            if ( rootPath != realPath && !realPath.StartsWith( rootPath ) )
            {
                nbError.Text = "Invalid path. Please try loading the page again.";
                ltItems.Text = string.Empty;

                return;
            }

            //
            // Setup the basic merge fields.
            // Path = The relative path that can be displayed to the user. (e.g. "/Images")
            // ContentPath = The direct path to the directory as the client would need it. (e.g. "/Content/Images")
            // BrowseJavascript = The Javascript function name to call to browse to another directory.
            //
            mergeFields.Add( "Path", VirtualPathUtility.AppendTrailingSlash( folder ) );
            mergeFields.Add( "ContentPath", ResolveUrl( VirtualPathUtility.AppendTrailingSlash( virtualPath ) ) );
            mergeFields.Add( "BrowseJavascript", string.Format( "filebrowser_{0}_submit", ClientID ) );
            mergeFields.Add( "DownloadJavascript", string.Format( "filebrowser_{0}_download", ClientID ) );

            //
            // Add in the directories to the merge fields.
            //
            List<object> directories = new List<object>();
            mergeFields.Add( "Directories", directories );
            foreach ( var entry in new System.IO.DirectoryInfo( realPath ).GetDirectories() )
            {
                directories.Add( entry.GetType().GetProperties().ToDictionary( x => x.Name, x => ( x.GetGetMethod().Invoke( entry, null ) == null ? "" : x.GetGetMethod().Invoke( entry, null ).ToString() ) ) );
            }

            //
            // Add in the files to the merge fields.
            //
            List<object> files = new List<object>();
            mergeFields.Add( "Files", files );
            foreach ( var entry in new System.IO.DirectoryInfo( realPath ).GetFiles() )
            {
                files.Add( entry.GetType().GetProperties().ToDictionary( x => x.Name, x => ( x.GetGetMethod().Invoke( entry, null ) == null ? "" : x.GetGetMethod().Invoke( entry, null ).ToString() ) ) );
            }

            //
            // Render the content and set the hidden field for our current path.
            //
            ltItems.Text = LavaTemplate.ResolveMergeFields( mergeFields );
            hfPath.Value = VirtualPathUtility.AppendTrailingSlash( folder );
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            btnBrowse_Click( this, null );
        }

        #endregion

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnBrowse_Click( object sender, EventArgs e )
        {
            //
            // If there is a ".." in the path then remove it and the preceding path element.
            //
            string path = new Regex( "\\/[^\\/]+\\/\\.\\." ).Replace( hfPath.Value, "" );

            DisplayFolder( !string.IsNullOrWhiteSpace( path ) ? path : "/" );
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnDownload_Click( object sender, EventArgs e )
        {
            string path = VirtualPathUtility.AppendTrailingSlash( hfPath.Value ) + hfFile.Value;
            string virtualPath = VirtualPathUtility.RemoveTrailingSlash( GetAttributeValue( "ContentRoot" ) ) + path;
            string realPath = Request.MapPath( virtualPath );

            //
            // Validate they aren't trying to get outside of the root.
            //
            string rootPath = Request.MapPath( VirtualPathUtility.AppendTrailingSlash( GetAttributeValue( "ContentRoot" ) ) );
            if ( rootPath != realPath && !realPath.StartsWith( rootPath ) )
            {
                nbError.Text = "Invalid path. Please try loading the page again.";
                ltItems.Text = string.Empty;

                return;
            }

            //
            // Setup the response.
            //
            Context.Response.Clear();
            Context.Response.ContentType = MimeMapping.GetMimeMapping( hfFile.Value );
            if ( string.IsNullOrWhiteSpace( Context.Response.ContentType ) )
            {
                Context.Response.ContentType = "application/octet-stream";
            }
            Context.Response.AddHeader( "Content-disposition", string.Format("{0}; filename=\"{1}\"", hfDownloadType.Value == "1" ? "attachment" : "inline", hfFile.Value ) );
            Context.Response.WriteFile( realPath );
            Context.Response.End();
        }
    }
}