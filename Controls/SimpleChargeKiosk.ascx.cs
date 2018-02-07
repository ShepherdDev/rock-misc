using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Financial;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Simple Charge Kiosk" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Block used to do simple swipe to pay charges. Pass in amount and memo in query string to charge.")]

    [FinancialGatewayField( "Credit Card Gateway", "The payment gateway to use for Credit Card transactions.", true, order: 0 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.FINANCIAL_TRANSACTION_TYPE, "Transaction Type", "The Financial Transaction Type to use when creating transactions.", true, order: 1 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.FINANCIAL_SOURCE_TYPE, "Source", "The Financial Source Type to use when creating transactions", true, defaultValue: Rock.SystemGuid.DefinedValue.FINANCIAL_SOURCE_TYPE_KIOSK, order: 2 )]
    [AccountField( "Account", "The account to use when entering a new transaction.", true, order: 3 )]
    [TextField( "Batch Name Prefix", "The prefix to add to the financial batch.", true, defaultValue: "Kiosk Payment", order: 4 )]
    [LinkedPage( "Homepage", "Homepage of the kiosk.", true, order: 5 )]
    [CodeEditorField( "Receipt Lava", "Lava to display for the receipt panel.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 300, true, defaultValue: @"<div class=""alert alert-success"">
    Thank you for your purchase.
</div>

<div class=""panel panel-default"">
    <div class=""panel-body"">
        <h4>Purchase Details</h4>
        <p>
            <strong>Confirmation Number:</strong> {{ TransactionCode }}
        </p>
        <p>
            <strong>Memo:</strong> {{ Memo }}
        </p>
        <p>
            <strong>Amount:</strong> {{ TotalAmount | FormatAsCurrency }}
        </p>
    </div>
</div>", order: 6 )]
    public partial class SimpleChargeKiosk : RockBlock
    {
        #region Fields

        string _transactionCode = string.Empty;

        #endregion

        #region Properties

        /// <summary>
        /// The person who will be charged for this transaction.
        /// </summary>
        protected Guid SelectedPersonGuid
        {
            get
            {
                return ( Guid ) ViewState["SelectedPersonGuid"];
            }
            set
            {
                ViewState["SelectedPersonGuid"] = value;
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

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            RockPage.AddScriptLink( "~/Scripts/Kiosk/kiosk-core.js" );
            RockPage.AddScriptLink( "~/Scripts/Kiosk/jquery.scannerdetection.js" );
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
                if ( CheckSettings() )
                {
                    ShowInitialPanel();
                }
            }
            else
            {
                // check for swipe event
                if ( Request.Form["__EVENTARGUMENT"] != null )
                {
                    if ( Request.Form["__EVENTARGUMENT"] == "Swipe_Complete" )
                    {
                        ProcessSwipe( hfSwipe.Value );
                    }
                }
            }
        }

        #endregion

        #region Events

        #region Amount Entry Events

        /// <summary>
        /// Cancel the charge operation.
        /// </summary>
        /// <param name="sender">The object that originated this event.</param>
        /// <param name="e">The arguments that describe this event.</param>
        protected void lbAmountEntryCancel_Click( object sender, EventArgs e )
        {
            GoHome();
        }

        /// <summary>
        /// An amount has been entered and we are ready to charge.
        /// </summary>
        /// <param name="sender">The object that originated this event.</param>
        /// <param name="e">The arguments that describe this event.</param>
        protected void lbAmountEntryNext_Click( object sender, EventArgs e )
        {
            decimal amount = 0;

            decimal.TryParse( tbAmount.Text, out amount );

            if ( amount <= 0 )
            {
                nbAmountEntry.Text = "Please enter a valid amount.";
            }
            else
            {
                nbAmountEntry.Text = string.Empty;
                ShowSwipePanel();
            }

        }

        #endregion

        #region Swipe Panel Events

        /// <summary>
        /// Process the data read from the card reader and generate the transaction.
        /// </summary>
        /// <param name="swipeData">The data read from the card.</param>
        private void ProcessSwipe( string swipeData )
        {
            try
            {
                using ( var rockContext = new RockContext() )
                {
                    // create swipe object
                    SwipePaymentInfo swipeInfo = new SwipePaymentInfo( swipeData );
                    swipeInfo.Amount = tbAmount.Text.AsDecimal();

                    // add comment to the transation
                    swipeInfo.Comment1 = PageParameter( "Memo" );

                    // get gateway
                    FinancialGateway financialGateway = null;
                    GatewayComponent gateway = null;
                    Guid? gatewayGuid = GetAttributeValue( "CreditCardGateway" ).AsGuidOrNull();
                    if ( gatewayGuid.HasValue )
                    {
                        financialGateway = new FinancialGatewayService( rockContext ).Get( gatewayGuid.Value );

                        if ( financialGateway != null )
                        {
                            financialGateway.LoadAttributes( rockContext );
                        }

                        gateway = financialGateway.GetGatewayComponent();
                    }

                    if ( gateway == null )
                    {
                        lSwipeErrors.Text = "<div class='alert alert-danger'>Invalid gateway provided. Please provide a gateway. Transaction not processed.</div>";
                        return;
                    }

                    //
                    // Process the transaction.
                    //
                    string errorMessage = string.Empty;
                    var transaction = gateway.Charge( financialGateway, swipeInfo, out errorMessage );

                    if ( transaction == null )
                    {
                        lSwipeErrors.Text = String.Format( "<div class='alert alert-danger'>An error occurred while process this transaction. Message: {0}</div>", errorMessage );
                        return;
                    }

                    _transactionCode = transaction.TransactionCode;

                    //
                    // Set some common information about the transaction.
                    //
                    transaction.AuthorizedPersonAliasId = new PersonService( rockContext ).Get( SelectedPersonGuid ).PrimaryAliasId;
                    transaction.TransactionDateTime = RockDateTime.Now;
                    transaction.FinancialGatewayId = financialGateway.Id;
                    transaction.TransactionTypeValueId = DefinedValueCache.Read( GetAttributeValue( "TransactionType" ) ).Id;
                    transaction.SourceTypeValueId = DefinedValueCache.Read( GetAttributeValue( "Source" ) ).Id;
                    transaction.Summary = swipeInfo.Comment1;

                    //
                    // Ensure we have payment details.
                    //
                    if ( transaction.FinancialPaymentDetail == null )
                    {
                        transaction.FinancialPaymentDetail = new FinancialPaymentDetail();
                    }
                    transaction.FinancialPaymentDetail.SetFromPaymentInfo( swipeInfo, gateway, rockContext );

                    var transactionDetail = new FinancialTransactionDetail();
                    transactionDetail.Amount = swipeInfo.Amount;
                    transactionDetail.AccountId = new FinancialAccountService( rockContext ).Get( GetAttributeValue( "Account" ).AsGuid() ).Id;
                    transaction.TransactionDetails.Add( transactionDetail );

                    var batchService = new FinancialBatchService( rockContext );

                    // Get the batch 
                    var batch = batchService.Get(
                        GetAttributeValue( "BatchNamePrefix" ),
                        swipeInfo.CurrencyTypeValue,
                        swipeInfo.CreditCardTypeValue,
                        transaction.TransactionDateTime.Value,
                        financialGateway.GetBatchTimeOffset() );

                    var batchChanges = new List<string>();

                    if ( batch.Id == 0 )
                    {
                        batchChanges.Add( "Generated the batch" );
                        History.EvaluateChange( batchChanges, "Batch Name", string.Empty, batch.Name );
                        History.EvaluateChange( batchChanges, "Status", null, batch.Status );
                        History.EvaluateChange( batchChanges, "Start Date/Time", null, batch.BatchStartDateTime );
                        History.EvaluateChange( batchChanges, "End Date/Time", null, batch.BatchEndDateTime );
                    }

                    decimal newControlAmount = batch.ControlAmount + transaction.TotalAmount;
                    History.EvaluateChange( batchChanges, "Control Amount", batch.ControlAmount.FormatAsCurrency(), newControlAmount.FormatAsCurrency() );
                    batch.ControlAmount = newControlAmount;

                    transaction.BatchId = batch.Id;
                    batch.Transactions.Add( transaction );

                    rockContext.WrapTransaction( () =>
                    {
                        rockContext.SaveChanges();

                        HistoryService.SaveChanges(
                            rockContext,
                            typeof( FinancialBatch ),
                            Rock.SystemGuid.Category.HISTORY_FINANCIAL_BATCH.AsGuid(),
                            batch.Id,
                            batchChanges
                        );
                    } );

                    ShowReceiptPanel();
                }
            }
            catch ( Exception ex )
            {
                lSwipeErrors.Text = String.Format( "<div class='alert alert-danger'>An error occurred while process this transaction. Message: {0}</div>", ex.Message );
            }

        }

        /// <summary>
        /// Go back to entering an amount.
        /// </summary>
        /// <param name="sender">The object that originated this event.</param>
        /// <param name="e">The arguments that describe this event.</param>
        protected void lbSwipeBack_Click( object sender, EventArgs e )
        {
            HidePanels();
            ShowAmountPanel();
        }

        /// <summary>
        /// Cancel the charge.
        /// </summary>
        /// <param name="sender">The object that originated this event.</param>
        /// <param name="e">The arguments that describe this event.</param>
        protected void lbSwipeCancel_Click( object sender, EventArgs e )
        {
            GoHome();
        }

        #endregion

        #region Receipt Panel Events

        /// <summary>
        /// They are done looking at the receipt.
        /// </summary>
        /// <param name="sender">The object that originated this event.</param>
        /// <param name="e">The arguments that describe this event.</param>
        protected void lbReceiptDone_Click( object sender, EventArgs e )
        {
            GoHome();
        }

        #endregion

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Block_BlockUpdated( object sender, EventArgs e )
        {
            if ( CheckSettings() )
            {
                HidePanels();
                ShowInitialPanel();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Show the initial panel that should be displayed when the page loads.
        /// </summary>
        private void ShowInitialPanel()
        {
            if ( tbAmount.Text.AsDecimal() == 0 )
            {
                ShowAmountPanel();
            }
            else
            {
                ShowSwipePanel();
            }
        }

        /// <summary>
        /// Show the amount panel on screen.
        /// </summary>
        private void ShowAmountPanel()
        {
            var person = new PersonService( new RockContext() ).Get( this.SelectedPersonGuid );

            lblPayingAs.Text = String.Format( "Paying as {0} {1}", person.FirstName, person.LastName );

            // show panels
            HidePanels();
            pnlAmountEntry.Visible = true;
        }

        /// <summary>
        /// Show the swipe panel on screen.
        /// </summary>
        private void ShowSwipePanel()
        {
            var person = new PersonService( new RockContext() ).Get( this.SelectedPersonGuid );

            nbSwipeAmount.Text = string.Format( "Will charge {0} to {1} {2}.",
                tbAmount.Text.AsDecimal().FormatAsCurrency(), person.FirstName, person.LastName );

            HidePanels();
            pnlSwipe.Visible = true;
        }

        /// <summary>
        /// Show the receipt panel on screen.
        /// </summary>
        private void ShowReceiptPanel()
        {
            var mergeFields = GetMergeFields( null );

            string template = GetAttributeValue( "ReceiptLava" );

            lReceiptContent.Text = template.ResolveMergeFields( mergeFields );

            HidePanels();
            pnlReceipt.Visible = true;
        }

        /// <summary>
        /// Get the merge fields related to this swipe charge.
        /// </summary>
        /// <param name="person">The person that was charged.</param>
        /// <returns>The merge fields to use when parsing Lava.</returns>
        private Dictionary<string, object> GetMergeFields( Person person )
        {
            RockContext rockContext = new RockContext();

            if ( person == null )
            {
                person = new PersonService( rockContext ).Get( this.SelectedPersonGuid );
            }

            // setup lava
            var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( this.RockPage, this.CurrentPerson );

            mergeFields.Add( "Person", person );
            mergeFields.Add( "TotalAmount", tbAmount.Text.AsDecimal() );
            mergeFields.Add( "TransactionCode", _transactionCode );
            mergeFields.Add( "Memo", PageParameter( "Memo" ) );

            return mergeFields;
        }

        /// <summary>
        /// Hide all panels from view.
        /// </summary>
        private void HidePanels()
        {
            pnlAmountEntry.Visible = false;
            pnlSwipe.Visible = false;
            pnlReceipt.Visible = false;
        }

        /// <summary>
        /// Redirect to the configured Homepage setting.
        /// </summary>
        private void GoHome()
        {
            NavigateToLinkedPage( "Homepage" );
        }

        /// <summary>
        /// Verify that all settings have been configured correctly.
        /// </summary>
        /// <returns>True if the block has been configured.</returns>
        private bool CheckSettings()
        {
            nbBlockConfigErrors.Title = string.Empty;
            nbBlockConfigErrors.Text = string.Empty;

            RockContext rockContext = new RockContext();
            FinancialAccountService accountService = new FinancialAccountService( rockContext );

            if ( accountService.Get( GetAttributeValue( "Account" ).AsGuid() ) == null )
            {
                nbBlockConfigErrors.Heading = "No Account Configured";
                nbBlockConfigErrors.Text = "<p>There is currently no account configured.</p>";

                return false;
            }

            if ( DefinedValueCache.Read( GetAttributeValue( "Source" ) ) == null )
            {
                nbBlockConfigErrors.Heading = "No Source";
                nbBlockConfigErrors.Text = "<p>There is currently no transaction source configured.</p>";

                return false;
            }

            if ( DefinedValueCache.Read( GetAttributeValue( "TransactionType" ) ) == null )
            {
                nbBlockConfigErrors.Heading = "No Transaction Type";
                nbBlockConfigErrors.Text = "<p>There is currently no transaction type configured.</p>";

                return false;
            }

            //
            // Hide the back button if we have been passed a value in.
            //
            if ( !string.IsNullOrWhiteSpace( PageParameter( "Amount" ) ) )
            {
                lbSwipeBack.Visible = false;

                decimal amount;
                if ( !decimal.TryParse( PageParameter( "Amount" ), out amount ) || amount <= 0 )
                {
                    nbBlockConfigErrors.Heading = "Invalid amount";
                    nbBlockConfigErrors.Text = "<p>An invalid amount was given.</p>";

                    return false;
                }

                tbAmount.Text = amount.ToString();
            }

            //
            // Get the person they have currently selected.
            //
            Guid selectedPersonGuid;
            if ( !Guid.TryParse( PageParameter( "Person" ), out selectedPersonGuid ) )
            {
                nbBlockConfigErrors.Heading = "No Person";
                nbBlockConfigErrors.Text = "<p>A person must be passed.</p>";

                return false;
            }

            if ( new PersonService( rockContext ).Get( selectedPersonGuid ) == null )
            {
                nbBlockConfigErrors.Heading = "No Person";
                nbBlockConfigErrors.Text = "<p>A valid person must be passed.</p>";

                return false;
            }

            SelectedPersonGuid = selectedPersonGuid;

            return true;
        }

        #endregion 
    }
}
