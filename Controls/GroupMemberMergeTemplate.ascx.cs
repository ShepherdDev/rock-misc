using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI;
using System.Web.UI.WebControls;
using Rock;
using Rock.Data;
using Rock.MergeTemplates;
using Rock.Model;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Group Member Merge Template" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Used for merging group member records into a merge template." )]
    public partial class GroupMemberMergeTemplate : RockBlock
    {
        #region Base Control Methods

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            dvDataView.EntityTypeId = Rock.Web.Cache.EntityTypeCache.GetId( "Rock.Model.Group" );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the btnMerge control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbMerge_Click( object sender, EventArgs e )
        {
            // NOTE: This is a full postback (not a partial like most other blocks)

            var rockContext = new RockContext();

            List<object> mergeObjectsList = GetMergeObjectList( rockContext );

            MergeTemplate mergeTemplate = new MergeTemplateService( rockContext ).Get( mtPicker.SelectedValue.AsInteger() );
            if ( mergeTemplate == null )
            {
                nbWarningMessage.Text = "Unable to get merge template";
                nbWarningMessage.NotificationBoxType = NotificationBoxType.Danger;
                nbWarningMessage.Visible = true;
                return;
            }

            MergeTemplateType mergeTemplateType = this.GetMergeTemplateType( rockContext, mergeTemplate );
            if ( mergeTemplateType == null )
            {
                nbWarningMessage.Text = "Unable to get merge template type";
                nbWarningMessage.NotificationBoxType = NotificationBoxType.Danger;
                nbWarningMessage.Visible = true;
                return;
            }

            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );
            BinaryFile outputBinaryFileDoc = null;

            try
            {
                outputBinaryFileDoc = mergeTemplateType.CreateDocument( mergeTemplate, mergeObjectsList, mergeFields );

                if ( mergeTemplateType.Exceptions != null && mergeTemplateType.Exceptions.Any() )
                {
                    if ( mergeTemplateType.Exceptions.Count == 1 )
                    {
                        this.LogException( mergeTemplateType.Exceptions[0] );
                    }
                    else if ( mergeTemplateType.Exceptions.Count > 50 )
                    {
                        this.LogException( new AggregateException( string.Format( "Exceptions merging template {0}. See InnerExceptions for top 50.", mergeTemplate.Name ), mergeTemplateType.Exceptions.Take( 50 ).ToList() ) );
                    }
                    else
                    {
                        this.LogException( new AggregateException( string.Format( "Exceptions merging template {0}. See InnerExceptions", mergeTemplate.Name ), mergeTemplateType.Exceptions.ToList() ) );
                    }
                }

                var uri = new UriBuilder( outputBinaryFileDoc.Url );
                var qry = System.Web.HttpUtility.ParseQueryString( uri.Query );
                qry["attachment"] = true.ToTrueFalse();
                uri.Query = qry.ToString();
                Response.Redirect( uri.ToString(), false );
                Context.ApplicationInstance.CompleteRequest();
            }
            catch ( Exception ex )
            {
                this.LogException( ex );
                if ( ex is System.FormatException )
                {
                    nbMergeError.Text = "Error loading the merge template. Please verify that the merge template file is valid.";
                }
                else
                {
                    nbMergeError.Text = "An error occurred while merging";
                }

                nbMergeError.Details = ex.Message;
                nbMergeError.Visible = true;
            }
        }

        /// <summary>
        /// Handles the SelectItem event of the mtPicker control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mtPicker_SelectItem( object sender, EventArgs e )
        {
            nbMergeError.Visible = false;
        }

        /// <summary>
        /// Handles the SelectedIndexChanged event of the rblSource control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void rblSource_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( rblSource.SelectedValue == "Group" )
            {
                pnlGroup.Visible = true;
                pnlDataView.Visible = false;
            }
            else if ( rblSource.SelectedValue == "Data View" )
            {
                pnlGroup.Visible = false;
                pnlDataView.Visible = true;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the type of the merge template.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="mergeTemplate">The merge template.</param>
        /// <returns></returns>
        private MergeTemplateType GetMergeTemplateType( RockContext rockContext, MergeTemplate mergeTemplate )
        {
            mergeTemplate = new MergeTemplateService( rockContext ).Get( mtPicker.SelectedValue.AsInteger() );
            if ( mergeTemplate == null )
            {
                return null;
            }

            return mergeTemplate.GetMergeTemplateType();
        }

        private List<object> GetMergeObjectList( RockContext rockContext )
        {
            List<GroupMember> records = new List<GroupMember>();

            if ( rblSource.SelectedValue == "Group" )
            {
                int groupId = gpGroup.SelectedValueAsId().Value;

                var groupIds = new GroupService( rockContext ).GetAllDescendents( groupId )
                    .Select( g => g.Id )
                    .ToList();
                groupIds.Add( groupId );

                records = new GroupMemberService( new RockContext() ).Queryable()
                    .Where( m => groupIds.Contains( m.GroupId ) && m.GroupMemberStatus == GroupMemberStatus.Active )
                    .ToList();
            }
            else if ( rblSource.SelectedValue == "Data View" )
            {
                int dataviewId = dvDataView.SelectedValueAsId().Value;

                var groupService = new GroupService( rockContext );
                var parameterExpression = groupService.ParameterExpression;
                var dv = new DataViewService( rockContext ).Get( dataviewId );
                List<string> errorMessages;
                var whereExpression = dv.GetExpression( groupService, parameterExpression, out errorMessages );

                records = groupService
                    .Get( parameterExpression, whereExpression )
                    .SelectMany( g => g.Members )
                    .Where( m => m.GroupMemberStatus == GroupMemberStatus.Active )
                    .ToList();
            }

            switch ( rblSortBy.SelectedValue )
            {
                case "Group":
                    records = records.OrderBy( m => m.Group.Name )
                        .ThenBy( m => m.Person.FirstName )
                        .ThenBy( m => m.Person.LastName )
                        .ToList();
                    break;

                case "Last":
                    records = records.OrderBy( m => m.Person.LastName )
                        .ThenBy( m => m.Person.FirstName )
                        .ThenBy( m => m.Group.Name )
                        .ToList();
                    break;

                case "First":
                default:
                    records = records.OrderBy( m => m.Person.FirstName )
                        .ThenBy( m => m.Person.LastName )
                        .ThenBy( m => m.Group.Name )
                        .ToList();
                    break;
            }

            return records.Cast<object>().ToList();
        }

        #endregion
    }
}
