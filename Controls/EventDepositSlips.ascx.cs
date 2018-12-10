using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Security;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Event Deposit Slips" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Provides a button to assist in printing cash/check deposit slips for event registrations." )]

    [CodeEditorField( "Formatted Template", "The Lava template to use when formatted the display of the deposit slip.", CodeEditorMode.Lava, height: 400, order: 0 )]
    [ContextAware( typeof( Rock.Model.RegistrationInstance ) )]
    public partial class EventDepositSlips : RockBlock, ISecondaryBlock
    {
        #region Fields

        private List<Registration> PaymentRegistrations;
        
        #endregion

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            fPayments.ApplyFilterClick += fPayments_ApplyFilterClick;
            gPayments.DataKeyNames = new string[] { "Id" };
            gPayments.Actions.ShowAdd = false;
            gPayments.ShowActionRow = false;
            gPayments.RowDataBound += gPayments_RowDataBound;
            gPayments.GridRebind += gPayments_GridRebind;
        }

        /// <summary>
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            if ( !IsPostBack )
            {
                SetFilter();

                BindPaymentsGrid();
            }
        }

        /// <summary>
        /// Allow primary blocks to hide this one. This is common when the primary block goes
        /// into edit mode.
        /// </summary>
        /// <param name="visible">true if this block should be visible, false if it should be hidden.</param>
        void ISecondaryBlock.SetVisible( bool visible )
        {
            pnlDetails.Visible = false;
        }

        #endregion

        #region Core Methods

        private void SetFilter()
        {
            cblCurrencyType.BindToDefinedType( DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE.AsGuid() ) );

            cblCurrencyType.SetValues( fPayments.GetUserPreference( "Currency Type" ).SplitDelimitedValues().AsIntegerList() );

            drpPaymentDateRange.DelimitedValues = fPayments.GetUserPreference( "Date Range" );
        }

        /// <summary>
        /// Binds the payments grid.
        /// </summary>
        private void BindPaymentsGrid()
        {
            int? instanceId = PageParameter( "RegistrationInstanceId" ).AsIntegerOrNull();
            if ( instanceId.HasValue )
            {
                using ( var rockContext = new RockContext() )
                {
                    // If configured for a registration and registration is null, return
                    int registrationEntityTypeId = EntityTypeCache.Get( typeof( Rock.Model.Registration ) ).Id;

                    // Get all the registrations for this instance
                    PaymentRegistrations = new RegistrationService( rockContext )
                        .Queryable( "PersonAlias.Person,Registrants.PersonAlias.Person" ).AsNoTracking()
                        .Where( r =>
                            r.RegistrationInstanceId == instanceId.Value &&
                            !r.IsTemporary )
                        .ToList();

                    // Get the Registration Ids
                    var registrationIds = PaymentRegistrations
                        .Select( r => r.Id )
                        .ToList();

                    // Get all the transactions relate to these registrations
                    var qry = new FinancialTransactionService( rockContext )
                        .Queryable().AsNoTracking()
                        .Where( t => t.TransactionDetails
                            .Any( d =>
                                d.EntityTypeId.HasValue &&
                                d.EntityTypeId.Value == registrationEntityTypeId &&
                                d.EntityId.HasValue &&
                                registrationIds.Contains( d.EntityId.Value ) ) );

                    // Date Range
                    var drp = new DateRangePicker();
                    drp.DelimitedValues = fPayments.GetUserPreference( "Date Range" );
                    if ( drp.LowerValue.HasValue )
                    {
                        qry = qry.Where( t => t.TransactionDateTime >= drp.LowerValue.Value );
                    }

                    if ( drp.UpperValue.HasValue )
                    {
                        DateTime upperDate = drp.UpperValue.Value.Date.AddDays( 1 );
                        qry = qry.Where( t => t.TransactionDateTime < upperDate );
                    }

                    //
                    // Filter by currency type.
                    //
                    var currencyTypes = fPayments.GetUserPreference( "Currency Type" ).SplitDelimitedValues().AsIntegerList();
                    if ( currencyTypes.Any() )
                    {
                        qry = qry.Where( t => t.FinancialPaymentDetail.CurrencyTypeValueId.HasValue && currencyTypes.Contains( t.FinancialPaymentDetail.CurrencyTypeValueId.Value ) );
                    }

                    SortProperty sortProperty = gPayments.SortProperty;
                    if ( sortProperty != null )
                    {
                        if ( sortProperty.Property == "TotalAmount" )
                        {
                            if ( sortProperty.Direction == SortDirection.Ascending )
                            {
                                qry = qry.OrderBy( t => t.TransactionDetails.Sum( d => ( decimal? ) d.Amount ) ?? 0.00M );
                            }
                            else
                            {
                                qry = qry.OrderByDescending( t => t.TransactionDetails.Sum( d => ( decimal? ) d.Amount ) ?? 0.0M );
                            }
                        }
                        else
                        {
                            qry = qry.Sort( sortProperty );
                        }
                    }
                    else
                    {
                        qry = qry.OrderByDescending( t => t.TransactionDateTime ).ThenByDescending( t => t.Id );
                    }

                    gPayments.SetLinqDataSource( qry.AsNoTracking() );
                    gPayments.DataBind();
                }
            }
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
            BindPaymentsGrid();
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnPrint_Click( object sender, EventArgs e )
        {
            var keys = new List<int>();
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage );
            var rockContext = new RockContext();
            var formattedTemplate = GetAttributeValue( "FormattedTemplate" );

            if ( gPayments.SelectedKeys.Count > 0 )
            {
                keys = gPayments.SelectedKeys.Cast<int>().ToList();
            }
            else
            {
                gPayments.AllowPaging = false;
                BindPaymentsGrid();
                gPayments.AllowPaging = true;

                foreach ( DataKey k in gPayments.DataKeys )
                {
                    keys.Add( ( int ) k.Value );
                }
            }

            int instanceId = PageParameter( "RegistrationInstanceId" ).AsInteger();
            mergeFields.Add( "RegistrationInstance", new RegistrationInstanceService( rockContext ).Get( instanceId ) );

            var transactions = new FinancialTransactionService( rockContext )
                .Queryable()
                .Where( t => keys.Contains( t.Id ) )
                .OrderBy( t => t.TransactionDateTime )
                .ToList();
            mergeFields.Add( "Transactions", transactions );

            var currencyTypes = transactions
                .GroupBy( t => t.FinancialPaymentDetail.CurrencyAndCreditCardType )
                .Select( g => new {
                    Title = g.Key,
                    Amount = g.Sum( t => t.TotalAmount )
                } )
                .ToList();
            mergeFields.Add( "CurrencyTypes", currencyTypes );

            mergeFields.Add( "Total", transactions.Sum( t => t.TotalAmount ) );

            ltFormattedOutput.Text = formattedTemplate.ResolveMergeFields( mergeFields );

            pnlDetails.Visible = false;
            pnlPrint.Visible = true;
        }

        /// <summary>
        /// Handles the ApplyFilterClick event of the fPayments control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void fPayments_ApplyFilterClick( object sender, EventArgs e )
        {
            fPayments.SaveUserPreference( "Date Range", drpPaymentDateRange.DelimitedValues );
            fPayments.SaveUserPreference( "Currency Type", cblCurrencyType.SelectedValues.AsDelimited( "," ) );

            BindPaymentsGrid();
        }

        /// <summary>
        /// Fs the payments_ display filter value.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        protected void fPayments_DisplayFilterValue( object sender, GridFilter.DisplayFilterValueArgs e )
        {
            switch ( e.Key )
            {
                case "Date Range":
                    {
                        e.Value = DateRangePicker.FormatDelimitedValues( e.Value );
                        break;
                    }
                case "Currency Type":
                    {
                        var ids = e.Value.SplitDelimitedValues().AsIntegerList();
                        e.Value = DefinedTypeCache.Get( Rock.SystemGuid.DefinedType.FINANCIAL_CURRENCY_TYPE.AsGuid() )
                            .DefinedValues
                            .Where( dv => ids.Contains( dv.Id ) )
                            .Select( dv => dv.Value )
                            .ToList()
                            .AsDelimited( ", " );
                        break;
                    }
                default:
                    {
                        e.Value = string.Empty;
                        break;
                    }
            }
        }

        /// <summary>
        /// Handles the GridRebind event of the gPayments control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void gPayments_GridRebind( object sender, EventArgs e )
        {
            BindPaymentsGrid();
        }

        /// <summary>
        /// Handles the RowDataBound event of the gPayments control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="GridViewRowEventArgs"/> instance containing the event data.</param>
        private void gPayments_RowDataBound( object sender, GridViewRowEventArgs e )
        {
            var transaction = e.Row.DataItem as FinancialTransaction;
            var lRegistrar = e.Row.FindControl( "lRegistrar" ) as Literal;
            var lRegistrants = e.Row.FindControl( "lRegistrants" ) as Literal;

            if ( transaction != null && lRegistrar != null && lRegistrants != null )
            {
                var registrars = new List<string>();
                var registrants = new List<string>();

                var registrationIds = transaction.TransactionDetails.Select( d => d.EntityId ).ToList();
                foreach ( var registration in PaymentRegistrations
                    .Where( r => registrationIds.Contains( r.Id ) ) )
                {
                    if ( registration.PersonAlias != null && registration.PersonAlias.Person != null )
                    {
                        var qryParams = new Dictionary<string, string>();
                        qryParams.Add( "RegistrationId", registration.Id.ToString() );
                        string url = LinkedPageUrl( "RegistrationPage", qryParams );
                        registrars.Add( string.Format( "<a href='{0}'>{1}</a>", url, registration.PersonAlias.Person.FullName ) );

                        foreach ( var registrant in registration.Registrants )
                        {
                            if ( registrant.PersonAlias != null && registrant.PersonAlias.Person != null )
                            {
                                registrants.Add( registrant.PersonAlias.Person.FullName );
                            }
                        }
                    }
                }

                lRegistrar.Text = registrars.AsDelimited( "<br/>" );
                lRegistrants.Text = registrants.AsDelimited( "<br/>" );
            }
        }

        #endregion
    }
}