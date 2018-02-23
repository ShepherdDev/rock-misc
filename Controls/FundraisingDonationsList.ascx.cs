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

    [ContextAware]
    public partial class FundraisingDonationsList : RockBlock
    {
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
                pnlDetails.Visible = true;
                BindGrid();
            }
        }

        /// <summary>
        /// Bind the grid to the donations that should be visible for the proper context.
        /// </summary>
        protected void BindGrid()
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
                    Participant = groupMembers[d.EntityId.Value],
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
            BindGrid();
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