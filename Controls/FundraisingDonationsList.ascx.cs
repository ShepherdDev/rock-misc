using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Linq.Dynamic;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Fundraising Donations List" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Lists donations in a grid for the current fundraising opportunity or participant." )]

    [BooleanField( "Show Amount", "Determines if the Amount column should be displayed in the Donation List.", true, order: 1 )]
    [BooleanField( "Show Donor Person As Link", "Determines if the Donor Person Name should be displayed as a link.", true, "Donor", order: 2 )]
    [TextField( "Donor Person Link", "The route that should be used for the the Donor Person link. Available merge fields: {DonorPersonId}, {GroupMemberPersonId}, {GroupMemberId}, and {GroupId}", false, "/Person/{DonorPersonId}", "Donor", order: 3 )]
    [BooleanField( "Show Donor Address", "Determines if the Donor's Address should be displayed in the Donation List.", true, "Donor", order: 4 )]
    [BooleanField( "Show Donor Email", "Determines if the Donor's Email should be displayed in the Donation List.", true, "Donor", order: 5 )]
    [BooleanField( "Show Participant Column", "Determines if the Participant column should be displayed in the Donation List.", true, "Participant", order: 6 )]
    [BooleanField( "Show Participant Group Member Link", "Determines if the Participant Group Member Link should be displayed in the Donation List.", true, "Participant", order: 7 )]
    [TextField( "Participant Group Member Link", "The route that should be used for the Group Member Link. Available merge fields: {DonorPersonId}, {GroupMemberPersonId}, {GroupMemberId}, and {GroupId}", false, "/GroupMember/{GroupMemberId}", "Participant", order: 8 )]
    [BooleanField( "Show Participant Person Link", "Determines if the Participant Person Link should be displayed in the Donation List.", true, "Participant", order: 9 )]
    [TextField( "Participant Person Link", "The route that should be used for to the Group Member Person Link. Available merge fields: {DonorPersonId}, {GroupMemberPersonId}, {GroupMemberId}, and {GroupId}", false, "/Person/{GroupMemberPersonId}", "Participant", order: 10 )]
    [BooleanField( "Show Communicate", "Show Communicate button in grid footer?", true, "Advanced", order: 1 )]
    [BooleanField( "Show Merge Person", "Show Merge Person button in grid footer?", true, "Advanced", order: 2 )]
    [BooleanField( "Show Bulk Update", "Show Bulk Update button in grid footer?", true, "Advanced", order: 3 )]
    [BooleanField( "Show Excel Export", "Show Export to Excel button in grid footer?", true, "Advanced", order: 4 )]
    [BooleanField( "Show Merge Template", "Show Export to Merge Template button in grid footer?", true, "Advanced", order: 5 )]

    [ContextAware]
    public partial class FundraisingDonationsList : RockBlock
    {
        public string DonorPersonLink = "/Person/{DonorPersonId}";
        public string ParticipantGroupMemberLink = "/GroupMember/{GroupMemberId}";
        public string ParticipantPersonLink = "/Person/{GroupMemberPersonId}";

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            base.BlockUpdated += FundraisingDonationsList_BlockUpdated;

            gDonations.GridRebind += gDonations_GridRebind;
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
                ShowDetails();
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Show the block content.
        /// </summary>
        protected void ShowDetails()
        {
            DonorPersonLink = GetAttributeValue( "DonorPersonLink" );
            ParticipantGroupMemberLink = GetAttributeValue( "ParticipantGroupMemberLink" );
            ParticipantPersonLink = GetAttributeValue( "ParticipantPersonLink" );
            
            var group = ContextEntity<Group>();
            var groupMember = ContextEntity<GroupMember>();

            if ( groupMember != null )
            {
                group = groupMember.Group;
            }

            pnlDetails.Visible = false;

            //
            // Only show the panel and content if the group type is a fundraising opportunity.
            //
            List<int> groupTypeIds = new List<int>();
            using ( var rockContext = new RockContext() )
            {
                var groupTypeService = new GroupTypeService( rockContext );
                var groupType = groupTypeService.Get( Rock.SystemGuid.GroupType.GROUPTYPE_FUNDRAISINGOPPORTUNITY.AsGuid() );

                if ( groupType != null )
                {
                    groupTypeIds = GetDescendantInheritedGroupTypeIds( groupType.Id );
                }
            }

            if ( group != null && groupTypeIds.Contains( group.GroupTypeId ) )
            {
                if ( !string.IsNullOrWhiteSpace( DonorPersonLink ) )
                {
                    DonorPersonLink = DonorPersonLink.Replace( "{GroupId}", group.Id.ToString() );
                }
                if ( !string.IsNullOrWhiteSpace( ParticipantGroupMemberLink ) )
                {
                    ParticipantGroupMemberLink = ParticipantGroupMemberLink.Replace( "{GroupId}", group.Id.ToString() );
                }
                if ( !string.IsNullOrWhiteSpace( ParticipantPersonLink ) )
                {
                    ParticipantPersonLink = ParticipantPersonLink.Replace( "{GroupId}", group.Id.ToString() );
                }
                pnlDetails.Visible = true;
                BindGrid();
            }
        }

        /// <summary>
        /// Bind the grid to the donations that should be visible for the proper context.
        /// </summary>
        protected void BindGrid( bool isExporting = false )
        {
            var rockContext = new RockContext();
            var groupMemberService = new GroupMemberService( rockContext );
            var financialTransactionDetailService = new FinancialTransactionDetailService( rockContext );
            var entityTypeIdGroupMember = EntityTypeCache.GetId<GroupMember>();
            Dictionary<int, GroupMember> groupMembers;

            //
            // Get the donations for the entire opportunity group or for just the
            // one individual being viewed.
            //
            if ( ContextEntity<Group>() != null )
            {
                var group = ContextEntity<Group>();

                groupMembers = groupMemberService.Queryable()
                    .Where( m => m.GroupId == group.Id )
                    .ToDictionary( m => m.Id );

                gDonations.Columns.OfType<RockTemplateField>().Where( c => c.HeaderText == "Participant" ).ToList().ForEach( c => c.Visible = true );
                gDonations.Columns.OfType<DateField>().Where( c => c.HeaderText == "Date" ).ToList().ForEach( c => c.Visible = false );
            }
            else
            {
                var groupMember = ContextEntity<GroupMember>();

                groupMembers = new Dictionary<int, GroupMember> { { groupMember.Id, groupMember } };

                gDonations.Columns.OfType<RockTemplateField>().Where( c => c.HeaderText == "Participant" ).ToList().ForEach( c => c.Visible = false );
                gDonations.Columns.OfType<DateField>().Where( c => c.HeaderText == "Date" ).ToList().ForEach( c => c.Visible = true );
            }

            var showDonorPersonAsLink = GetAttributeValue( "ShowDonorPersonAsLink" ).AsBoolean();
            var showParticipantPersonLink = GetAttributeValue( "ShowParticipantPersonLink" ).AsBoolean();
            var showParticipantGroupMemberLink = GetAttributeValue( "ShowParticipantGroupMemberLink" ).AsBoolean();

            //
            // Get the list of donation entries for the grid that match the list of members.
            //
            var groupMemberIds = groupMembers.Keys.ToList();
            var donations = financialTransactionDetailService.Queryable()
                .Where( d => d.EntityTypeId == entityTypeIdGroupMember && groupMemberIds.Contains( d.EntityId.Value ) )
                .ToList()
                .Select( d => new
                {
                    DonorId = d.Transaction.AuthorizedPersonAlias.PersonId,
                    Donor = d.Transaction.AuthorizedPersonAlias.Person,
                    DonorName = ( ( isExporting || !showDonorPersonAsLink ) ? d.Transaction.AuthorizedPersonAlias.Person.FullName : string.Format( "<a href=\"{0}\">{1}</a>", DonorPersonLink.Replace( "{DonorPersonId}", d.Transaction.AuthorizedPersonAlias.Person.Id.ToString() ).Replace( "{GroupMemberId}", groupMembers[d.EntityId.Value].Id.ToString() ).Replace( "{GroupMemberPersonId}", groupMembers[d.EntityId.Value].PersonId.ToString() ), d.Transaction.AuthorizedPersonAlias.Person.FullName ) ),
                    Email = d.Transaction.AuthorizedPersonAlias.Person.Email,
                    Participant = groupMembers[d.EntityId.Value],
                    ParticipantName = ( isExporting ? groupMembers[d.EntityId.Value].Person.FullName : 
                    ( ( showParticipantPersonLink || showParticipantGroupMemberLink ) ?
                        ( showParticipantPersonLink ? string.Format( "<a href=\"{0}\" class=\"pull-right margin-l-sm btn btn-sm btn-default\"><i class=\"fa fa-user\"></i></a>", ParticipantPersonLink.Replace( "{DonorPersonId}", d.Transaction.AuthorizedPersonAlias.Person.Id.ToString() ).Replace( "{GroupMemberId}", groupMembers[d.EntityId.Value].Id.ToString() ).Replace( "{GroupMemberPersonId}", groupMembers[d.EntityId.Value].PersonId.ToString() ) ) : string.Empty ) +
                        ( showParticipantGroupMemberLink ? string.Format( "<a href=\"{0}\">{1}</a>", ParticipantGroupMemberLink.Replace( "{DonorPersonId}", d.Transaction.AuthorizedPersonAlias.Person.Id.ToString() ).Replace( "{GroupMemberId}", groupMembers[d.EntityId.Value].Id.ToString() ).Replace( "{GroupMemberPersonId}", groupMembers[d.EntityId.Value].PersonId.ToString() ), groupMembers[d.EntityId.Value].Person.FullName ) : string.Empty )
                        : groupMembers[d.EntityId.Value].Person.FullName )
                    ),
                    Amount = d.Amount,
                    Address = d.Transaction.AuthorizedPersonAlias.Person.GetHomeLocation( rockContext ).ToStringSafe().ConvertCrLfToHtmlBr(),
                    Date = d.Transaction.TransactionDateTime
                } ).AsQueryable();

            //
            // Apply user sorting or default to donor name.
            //
            if ( gDonations.SortProperty != null )
            {
                donations = donations.Sort( gDonations.SortProperty );
            }
            else
            {
                donations = donations.Sort( new SortProperty { Property = "Donor.LastName, Donor.NickName" } );
            }

            gDonations.ObjectList = donations.Select( d => d.Donor )
                .DistinctBy( p => p.Id )
                .Cast<object>()
                .ToDictionary( p => ( ( Person ) p ).Id.ToString() );

            if ( !GetAttributeValue( "ShowDonorAddress" ).AsBoolean() )
            {
                gDonations.Columns[2].Visible = false;
            }

            if ( !GetAttributeValue( "ShowDonorEmail" ).AsBoolean() )
            {
                gDonations.Columns[3].Visible = false;
            }

            if ( !GetAttributeValue( "ShowParticipantColumn" ).AsBoolean() )
            {
                gDonations.Columns[4].Visible = false;
            }

            gDonations.Columns[6].Visible = GetAttributeValue( "ShowAmount" ).AsBoolean();

            gDonations.Actions.ShowCommunicate = GetAttributeValue( "ShowCommunicate" ).AsBoolean();
            gDonations.Actions.ShowMergePerson = GetAttributeValue( "ShowMergePerson" ).AsBoolean();
            gDonations.Actions.ShowBulkUpdate = GetAttributeValue( "ShowBulkUpdate" ).AsBoolean();
            gDonations.Actions.ShowExcelExport = GetAttributeValue( "ShowExcelExport" ).AsBoolean();
            gDonations.Actions.ShowMergeTemplate = GetAttributeValue( "ShowMergeTemplate" ).AsBoolean();

            gDonations.DataSource = donations.ToList();
            gDonations.DataBind();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void FundraisingDonationsList_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetails();
        }

        /// <summary>
        /// Handles the GridRebind event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridRebindEventArgs"/> instance containing the event data.</param>
        private void gDonations_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
        {
            BindGrid( e.IsExporting );
        }

        #endregion

        #region Required Until Added To Core

        /// <summary>
        /// Gets a list of GroupType Ids, including our own Id, that identifies the
        /// inheritence tree.
        /// </summary>
        /// <param name="groupTypeGuid">The parent group type Id to start from.</param>
        /// <returns>A list of GroupType Ids, including our own Id, that identifies the inheritence tree.</returns>
        public List<int> GetDescendantInheritedGroupTypeIds( int groupTypeId )
        {
            var rockContext = new RockContext();

            var groupTypeService = new GroupTypeService( rockContext );

            return groupTypeService.ExecuteQuery( @"
				WITH CTE AS (
		            SELECT [Id],[InheritedGroupTypeId] FROM [GroupType] WHERE [Id] = {0}
		            UNION ALL
		            SELECT [a].[Id],[a].[InheritedGroupTypeId] FROM [GroupType] [a]
		            JOIN CTE acte ON acte.[Id] = [a].[InheritedGroupTypeId]
                 )
                SELECT *
                FROM [GroupType]
                WHERE [Id] IN ( SELECT [Id] FROM CTE )", groupTypeId )
                .Select( t => t.Id )
                .ToList();
        }

        #endregion
    }
}