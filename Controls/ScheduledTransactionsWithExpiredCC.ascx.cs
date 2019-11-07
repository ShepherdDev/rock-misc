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
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Scheduled Transactions With Expired Credit Card" )]
    [Category( "com_shepherdchurch > Misc" )]
    [Description( "Shows a chart of how many scheduled transactions with expired credit cards have been renewed." )]

    public partial class ScheduledTransactionsWithExpiredCC : RockBlock
    {
        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( "https://cdnjs.cloudflare.com/ajax/libs/Chart.js/2.7.1/Chart.bundle.js", false );

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
            if ( !IsPostBack )
            {
                tbDaysBack.Text = "30";
                ShowDetails();
            }
        }

        #endregion

        #region Core Methods

        private void ShowDetails()
        {
            var renewedItems = new List<TransInfo>();
            var notRenewedItems = new List<TransInfo>();

            using ( var rockContext = new RockContext() )
            {
                var limitDate = RockDateTime.Now.AddDays( -( tbDaysBack.Text.AsIntegerOrNull() ?? 30 ) );

                var transactions = new FinancialScheduledTransactionService( rockContext )
                    .Queryable()
                    .Include( t => t.FinancialPaymentDetail )
                    .Include( t => t.AuthorizedPersonAlias )
                    .Where( t => t.IsActive && t.FinancialPaymentDetail.ExpirationMonthEncrypted != null )
                    .Where( t => t.EndDate == null || t.EndDate > DateTime.Now )
                    .AsNoTracking()
                    .ToList();

                foreach ( var transaction in transactions )
                {
                    int? expirationMonthDecrypted = Encryption.DecryptString( transaction.FinancialPaymentDetail.ExpirationMonthEncrypted ).AsIntegerOrNull();
                    int? expirationYearDecrypted = Encryption.DecryptString( transaction.FinancialPaymentDetail.ExpirationYearEncrypted ).AsIntegerOrNull();

                    if ( !expirationMonthDecrypted.HasValue || !expirationMonthDecrypted.HasValue )
                    {
                        continue;
                    }

                    int expireYear = expirationYearDecrypted.Value;
                    int expireMonth = expirationMonthDecrypted.Value;
                    var expireDate = new DateTime( expireYear, expireMonth, 1 );

                    if ( expireDate < limitDate || expireDate >= RockDateTime.Now )
                    {
                        continue;
                    }

                    var newTransactions = transactions
                        .Where( t => t.Id != transaction.Id )
                        .Where( t => t.CreatedDateTime > transaction.CreatedDateTime )
                        .Where( t => t.AuthorizedPersonAlias.PersonId == transaction.AuthorizedPersonAlias.PersonId )
                        .ToList();

                    if ( newTransactions.Any() )
                    {
                        renewedItems.Add( new TransInfo
                        {
                            Name = transaction.AuthorizedPersonAlias.Person.FullNameReversed,
                            StartDate = transaction.StartDate,
                            EndDate = expireDate,//transaction.EndDate,
                            TotalAmount = transaction.TotalAmount
                        } );
                    }
                    else
                    {
                        notRenewedItems.Add( new TransInfo
                        {
                            Name = transaction.AuthorizedPersonAlias.Person.FullNameReversed,
                            StartDate = transaction.StartDate,
                            EndDate = expireDate,//transaction.EndDate,
                            TotalAmount = transaction.TotalAmount
                        } );
                    }
                }

                gRenewed.DataSource = renewedItems;
                gRenewed.DataBind();

                gNotRenewed.DataSource = notRenewedItems;
                gNotRenewed.DataBind();

                hfCounts.Value = string.Format( "[{0},{1}]", renewedItems.Count, notRenewedItems.Count );
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
            ShowDetails();
        }

        /// <summary>
        /// Handles the Click event of the lbUpdate control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbUpdate_Click( object sender, EventArgs e )
        {
            ShowDetails();
        }

        #endregion

        private class TransInfo
        {
            public string Name { get; set; }

            public decimal TotalAmount { get; set; }

            public DateTime StartDate { get; set; }

            public DateTime? EndDate { get; set; }
        }
    }
}