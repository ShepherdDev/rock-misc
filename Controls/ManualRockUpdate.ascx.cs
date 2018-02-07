using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

using Microsoft.Win32;

using NuGet;
using RestSharp;

using Rock;
using Rock.Constants;
using Rock.Data;
using Rock.Model;
using Rock.Services.NuGet;
using Rock.VersionInfo;
using Rock.Web.Cache;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Manual Rock Update" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Provides a way to manually update rock using a nupkg." )]
    public partial class ManualRockUpdate : Rock.Web.UI.RockBlock
    {
        #region Fields

        WebProjectManager nuGetService = null;
        private string _rockPackageId = "Rock";
        SemanticVersion _installedVersion = new SemanticVersion( "0.0.0" );

        #endregion

        #region Properties

        /// <summary>
        /// Obtains a WebProjectManager from the Global "UpdateServerUrl" Attribute.
        /// </summary>
        /// <value>
        /// The NuGet service or null if no valid service could be found using the UpdateServerUrl.
        /// </value>
        protected WebProjectManager NuGetService
        {
            get
            {
                if ( nuGetService == null )
                {
                    try
                    {
                        string siteRoot = Request.MapPath( "~/" );
                        nuGetService = new WebProjectManager( Request.MapPath( "~/App_Data" ), siteRoot );
                    }
                    catch
                    {
                        // if caught, we will return a null nuGetService
                    }
                }
                return nuGetService;
            }
        }

        #endregion

        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            string script = @"
    $('#btn-restart').click(function () {
        var btn = $(this);
        btn.button('loading');
        location = location.href;
    });
";
            ScriptManager.RegisterStartupScript( pnlUpdateSuccess, pnlUpdateSuccess.GetType(), "restart-script", script, true );
        }

        /// <summary>
        /// Invoked on page load.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            // Set timeout for up to 15 minutes (just like installer)
            Server.ScriptTimeout = 900;
            ScriptManager.GetCurrent( Page ).AsyncPostBackTimeout = 900;

            if ( !IsPostBack )
            {
                if ( NuGetService == null )
                {
                    pnlError.Visible = true;
                    nbErrors.Text = string.Format( "Your UpdateServerUrl is not valid." );
                }
                else
                {
                    RemoveOldRDeleteFiles();
                }

                ddlPackage.Items.Add( new ListItem() );
                if ( Directory.Exists( Request.MapPath( "~/App_Data/ManualUpdates" ) ) )
                {
                    foreach ( string f in Directory.EnumerateFiles( Request.MapPath( "~/App_Data/ManualUpdates" ), "*.nupkg" ) )
                    {
                        ddlPackage.Items.Add( Path.GetFileName( f ) );
                    }
                }
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Wraps the install or update process in some guarded code while putting the app in "offline"
        /// mode and then back "online" when it's complete.
        /// </summary>
        /// <param name="version">the semantic version number</param>
        private void Update( IPackage update )
        {
            WriteAppOffline();
            try
            {
                if ( !UpdateRockPackage( update ) )
                {
                    pnlError.Visible = true;
                    pnlUpdateSuccess.Visible = false;
                }
            }
            catch ( Exception ex )
            {
                pnlError.Visible = true;
                pnlUpdateSuccess.Visible = false;
                nbErrors.Text = string.Format( "Something went wrong.  Although the errors were written to the error log, they are listed for your review:<br/>{0}", ex.Message );
                LogException( ex );
            }
            RemoveAppOffline();
        }

        #endregion

        #region Methods

        /// <summary>
        /// Updates an existing Rock package to the given version and returns true if successful.
        /// </summary>
        /// <returns>true if the update was successful; false if errors were encountered</returns>
        protected bool UpdateRockPackage( IPackage update )
        {
            IEnumerable<string> errors = Enumerable.Empty<string>();
            string version = update.Version.ToString();

            try
            {
                var field = NuGetService.GetType().GetField( "_projectManager", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance );
                IProjectManager projectManager = ( IProjectManager ) field.GetValue( NuGetService );
                projectManager.UpdatePackageReference( update, false, false );

                CheckForManualFileMoves( version );

                lSuccessVersion.Text = GetRockVersion( update.Version );

                // Record the current version to the database
                Rock.Web.SystemSettings.SetValue( SystemSettingKeys.ROCK_INSTANCE_ID, version );

                // register any new REST controllers
                try
                {
                    RestControllerService.RegisterControllers();
                }
                catch ( Exception ex )
                {
                    LogException( ex );
                }
            }
            catch ( OutOfMemoryException ex )
            {
                errors = errors.Concat( new[] { string.Format( "There is a problem installing v{0}. It looks like your website ran out of memory. Check out <a href='http://www.rockrms.com/Rock/UpdateIssues#outofmemory'>this page for some assistance</a>", version ) } );
                LogException( ex );
            }
            catch ( System.Xml.XmlException ex )
            {
                errors = errors.Concat( new[] { string.Format( "There is a problem installing v{0}. It looks one of the standard XML files ({1}) may have been customized which prevented us from updating it. Check out <a href='http://www.rockrms.com/Rock/UpdateIssues#customizedxml'>this page for some assistance</a>", version, ex.Message ) } );
                LogException( ex );
            }
            catch ( System.IO.IOException ex )
            {
                errors = errors.Concat( new[] { string.Format( "There is a problem installing v{0}. We were not able to replace an important file ({1}) after the update. Check out <a href='http://www.rockrms.com/Rock/UpdateIssues#unabletoreplacefile'>this page for some assistance</a>", version, ex.Message ) } );
                LogException( ex );
            }
            catch ( Exception ex )
            {
                errors = errors.Concat( new[] { string.Format( "There is a problem installing v{0}: {1}", version, ex.Message ) } );
                LogException( ex );
            }

            if ( errors != null && errors.Count() > 0 )
            {
                pnlError.Visible = true;
                nbErrors.Text = errors.Aggregate( new StringBuilder( "<ul class='list-padded'>" ), ( sb, s ) => sb.AppendFormat( "<li>{0}</li>", s ) ).Append( "</ul>" ).ToString();
                return false;
            }
            else
            {
                pnlUpload.Visible = false;
                pnlUpdateSuccess.Visible = true;
                return true;
            }
        }

        protected string GetRockVersion( object version )
        {
            var semanticVersion = version as SemanticVersion;
            if ( semanticVersion == null )
            {
                semanticVersion = new SemanticVersion( version.ToString() );
            }

            if ( semanticVersion != null )
            {
                return "Rock " + RockVersion( semanticVersion );
            }
            else

            return string.Empty;
        }

        protected string RockVersion( SemanticVersion version )
        {
            switch ( version.Version.Major )
            {
                case 1: return string.Format( "McKinley {0}.{1}", version.Version.Minor, version.Version.Build );
                default: return string.Format( "{0}.{1}.{2}", version.Version.Major, version.Version.Minor, version.Version.Build );
            }
        }

        /// <summary>
        /// Extracts the required SemanticVersion from the package's tags.
        /// </summary>
        /// <param name="package">a Rock nuget package</param>
        /// <returns>the SemanticVersion of the package that this particular package requires</returns>
        protected SemanticVersion ExtractRequiredVersionFromTags( IPackage package )
        {
            Regex regex = new Regex( @"requires-([\.\d]+)" );
            if ( package.Tags != null )
            { 
                Match match = regex.Match( package.Tags );
                if ( match.Success )
                {
                    return new SemanticVersion( match.Groups[1].Value );
                }
            }

            throw new ArgumentException( string.Format( "There is a malformed 'requires-' tag in a Rock package ({0})", package.Version ) );
        }

        /// <summary>
        /// Removes the app_offline.htm file so the app can be used again.
        /// </summary>
        private void RemoveAppOffline()
        {
            var root = this.Request.PhysicalApplicationPath;
            var file = System.IO.Path.Combine( root, "app_offline.htm" );
            System.IO.File.Delete( file );
        }

        /// <summary>
        /// Copies the app_offline-template.htm file to app_offline.htm so no one else can hit the app.
        /// If the template file does not exist an app_offline.htm file will be created from scratch.
        /// </summary>
        private void WriteAppOffline()
        {
            var root = this.Request.PhysicalApplicationPath;

            var templateFile = System.IO.Path.Combine( root, "app_offline-template.htm" );
            var offlineFile = System.IO.Path.Combine( root, "app_offline.htm" );

            try
            {
                if ( File.Exists( templateFile ) )
                {
                    System.IO.File.Copy( templateFile, offlineFile, overwrite: true );
                }
                else
                {
                    CreateOfflineFileFromScratch( offlineFile );
                }
            }
            catch ( Exception )
            {
                if ( !File.Exists( offlineFile ) )
                {
                    CreateOfflineFileFromScratch( offlineFile );
                }
            }
        }

        /// <summary>
        /// Simply creates an app_offline.htm file so no one else can hit the app.
        /// </summary>
        private void CreateOfflineFileFromScratch( string offlineFile )
        {
            System.IO.File.WriteAllText( offlineFile, @"
<html>
    <head>
    <title>Application Updating...</title>
    </head>
    <body>
        <h1>One Moment Please</h1>
        This application is undergoing an essential update and is temporarily offline.  Please give me a minute or two to wrap things up.
    </body>
</html>
" );
        }

        /// <summary>
        /// Removes the old *.rdelete (Rock delete) files that were created during an update.
        /// </summary>
        private void RemoveOldRDeleteFiles()
        {
            var rockDirectory = new DirectoryInfo( Server.MapPath( "~" ) );

            foreach ( var file in rockDirectory.EnumerateFiles( "*.rdelete", SearchOption.AllDirectories ) )
            {
                try
                {
                    file.Delete();
                }
                catch
                {
                    //we'll try again later
                }
            }
        }

        private void CheckForManualFileMoves( string version )
        {
            var versionDirectory = new DirectoryInfo( Server.MapPath( "~/App_Data/" + version ) );
            if ( versionDirectory.Exists )
            {
                foreach ( var file in versionDirectory.EnumerateFiles( "*", SearchOption.AllDirectories ) )
                {
                    ManuallyMoveFile( file, file.FullName.Replace( @"\App_Data\" + version, "" ) );
                }

                versionDirectory.Delete( true );
            }
        }

        private void ManuallyMoveFile( FileInfo file, string newPath )
        {
            if ( newPath.EndsWith( ".dll" ) && !newPath.Contains( @"\bin\" ) )
            {
                int fileCount = 0;
                if ( File.Exists( newPath ) )
                {
                    // generate a unique *.#.rdelete filename
                    do
                    {
                        fileCount++;
                    }
                    while ( File.Exists( string.Format( "{0}.{1}.rdelete", newPath, fileCount ) ) );

                    string fileToDelete = string.Format( "{0}.{1}.rdelete", newPath, fileCount );
                    File.Move( newPath, fileToDelete );
                }
            }

            file.CopyTo( newPath, true );
        }

        /// <summary>
        /// Converts + and * to html line items (li) wrapped in unordered lists (ul).
        /// </summary>
        /// <param name="str">a string that contains lines that start with + or *</param>
        /// <returns>an html string of <code>li</code> wrapped in <code>ul</code></returns>
        public string ConvertToHtmlLiWrappedUl( string str )
        {
            if ( str == null )
            {
                return string.Empty;
            }

            bool foundMatch = false;

            // Lines that start with  "+ *" or "+" or "*"
            var re = new System.Text.RegularExpressions.Regex( @"^\s*(\+ \* |[\+\*]+)(.*)" );
            var htmlBuilder = new StringBuilder();

            // split the string on newlines...
            string[] splits = str.Split( new[] { Environment.NewLine, "\x0A" }, StringSplitOptions.RemoveEmptyEntries );
            // look at each line to see if it starts with a + or * and then strip it and wrap it in <li></li>
            for ( int i = 0; i < splits.Length; i++ )
            {
                var match = re.Match( splits[i] );
                if ( match.Success )
                {
                    foundMatch = true;
                    htmlBuilder.AppendFormat( "<li>{0}</li>", match.Groups[2] );
                }
                else
                {
                    htmlBuilder.Append( splits[i] );
                }
            }

            // if we had a match then wrap it in <ul></ul> markup
            return foundMatch ? string.Format( "<ul class='list-padded'>{0}</ul>", htmlBuilder.ToString() ) : htmlBuilder.ToString();
        }

        #endregion

        protected void lbInstall_Click( object sender, EventArgs e )
        {
            string path = Request.MapPath( "~/App_Data/ManualUpdates" );
            string filename = Path.Combine( path, ddlPackage.SelectedValue );

            var package = new ZipPackage( filename );

            Update( package );
        }
    }
}