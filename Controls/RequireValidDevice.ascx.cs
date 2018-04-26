using System;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Require Valid Device" )]
    [Category( "com_shepherdchurch > Misc" )]
    [Description( "If the device currently viewing this page is not a valid Device then redirect them to the specified page." )]

    [LinkedPage( "Redirect Page", "The page to direct the user to if they are not coming from a verified device.", order: 0 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.DEVICE_TYPE, "Allowed Device Types", "Select the device types to be allowed to access this page.", true, true, order: 1 )]
    [BooleanField( "Enable Device Match By Name", "Enable a match by computer name by doing reverse IP lookup to get computer name based on IP address", false, "", 2, "EnableReverseLookup" )]
    public partial class RequireValidDevice : RockBlock
    {
        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            base.BlockUpdated += RequireValidDevice_BlockUpdated;
        }

        /// <summary>
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                ProcessRedirect();
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Process if the user should be redirected.
        /// </summary>
        protected void ProcessRedirect()
        {
            bool enableReverseLookup = GetAttributeValue( "EnableReverseLookup" ).AsBoolean( false );

            using ( var rockContext = new RockContext() )
            {
                var deviceService = new DeviceService( rockContext );
                var guids = GetAttributeValue( "AllowedDeviceTypes" )
                    .Split( ',' )
                    .Select( g => g.Trim() )
                    .Where( g => !string.IsNullOrEmpty( g ) );

                foreach ( var guid in guids )
                {
                    var checkInDeviceTypeId = DefinedValueCache.Read( guid ).Id;
                    var device = deviceService.GetByIPAddress( RockPage.GetClientIpAddress(), checkInDeviceTypeId, !enableReverseLookup );

                    if ( device != null )
                    {
                        nbRedirect.Text = string.Empty;
                        nbRedirect.Visible = false;

                        return;
                    }
                }

                Redirect();
            }
        }

        /// <summary>
        /// Redirect the user to the Redirect Page. If they are an Administrator then
        /// show a warning with a hyperlink to where they would be redirected to.
        /// </summary>
        void Redirect()
        {
            var target = new PageReference( GetAttributeValue( "RedirectPage" ) );

            if ( UserCanAdministrate )
            {
                PageReference self = new PageReference( RockPage.PageId );

                nbRedirect.Text = string.Format(
                    "If you were not an Administrator you would have been redirected to <a href=\"{0}\">{0}</a> because your IP address {1} does not match an allowed device.",
                    target.BuildUrl(), RockPage.GetClientIpAddress() );
                nbRedirect.Visible = true;
            }
            else
            {
                Response.Redirect( target.BuildUrl() );
                Response.End();
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void RequireValidDevice_BlockUpdated( object sender, EventArgs e )
        {
            ProcessRedirect();
        }

        #endregion
    }
}