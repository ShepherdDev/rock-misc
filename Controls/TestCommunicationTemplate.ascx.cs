using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Test Communication Template" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Sends a test e-mail to the current user." )]
    [CustomDropdownListField("Communication Type", "The type of test communications to use.", "Template,System", true, "Template" )]
    public partial class TestCommunicationTemplate : RockBlock
    {
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
                PopulateDropDown();
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Populate the drop down list as should be for block configuration.
        /// </summary>
        private void PopulateDropDown()
        {
            var rockContext = new RockContext();

            ddlEmail.Items.Clear();
            ddlEmail.Items.Add( new ListItem() );

            if ( GetAttributeValue( "CommunicationType" ) == "System" )
            {
                ltTitle.Text = "Test System Email";
                ddlEmail.Label = "System Email";

                var emails = new SystemEmailService( rockContext )
                    .Queryable()
                    .OrderBy( c => c.Category.Name )
                    .ThenBy( c => c.Title );

                foreach ( var email in emails )
                {
                    ddlEmail.Items.Add( new ListItem( string.Format( "{0} > {1}", email.Category.Name, email.Title ), email.Guid.ToString() ) );
                }
            }
            else
            {
                ltTitle.Text = "Test Communication Template";
                ddlEmail.Label = "Communication Template";

                var entityTypeId = EntityTypeCache.Read( "Rock.Communication.Medium.Email" ).Id;

                var emails = new CommunicationTemplateService( rockContext )
                    .Queryable()
                    .Where( c => c.MediumEntityTypeId == entityTypeId )
                    .OrderBy( c => c.Name );

                foreach ( var email in emails )
                {
                    ddlEmail.Items.Add( new ListItem( email.Name, email.Guid.ToString() ) );
                }
            }
        }

        /// <summary>
        /// Gets the communication.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <param name="peopleIds">The people ids.</param>
        /// <returns></returns>
        private Communication GetCommunication( RockContext rockContext, List<int> peopleIds )
        {
            var communicationService = new CommunicationService( rockContext );
            var recipientService = new CommunicationRecipientService( rockContext );

            var MediumData = GetTemplateData();
            if ( MediumData != null )
            {
                Communication communication = new Communication();
                communication.Status = CommunicationStatus.Transient;
                communication.SenderPersonAliasId = CurrentPersonAliasId;
                communicationService.Add( communication );
                communication.IsBulkCommunication = true;
                communication.MediumEntityTypeId = EntityTypeCache.Read( "Rock.Communication.Medium.Email" ).Id;
                communication.FutureSendDateTime = null;

                // add each person as a recipient to the communication
                if ( peopleIds != null )
                {
                    foreach ( var personId in peopleIds )
                    {
                        if ( !communication.Recipients.Any( r => r.PersonAlias.PersonId == personId ) )
                        {
                            var communicationRecipient = new CommunicationRecipient();
                            communicationRecipient.PersonAlias = new PersonAliasService( rockContext ).GetPrimaryAlias( personId );
                            communication.Recipients.Add( communicationRecipient );
                        }
                    }
                }

                // add the MediumData to the communication
                communication.MediumData.Clear();
                foreach ( var keyVal in MediumData )
                {
                    if ( !string.IsNullOrEmpty( keyVal.Value ) )
                    {
                        communication.MediumData.Add( keyVal.Key, keyVal.Value );
                    }
                }

                if ( communication.MediumData.ContainsKey( "Subject" ) )
                {
                    communication.Subject = communication.MediumData["Subject"];
                    communication.MediumData.Remove( "Subject" );
                }

                return communication;
            }

            return null;
        }

        /// <summary>
        /// Gets the template data.
        /// </summary>
        /// <exception cref="System.Exception">Missing communication template configuration.</exception>
        private Dictionary<string, string> GetTemplateData()
        {
            if ( string.IsNullOrWhiteSpace( ddlEmail.SelectedValue ) )
            {
                return null;
            }

            var template = new CommunicationTemplateService( new RockContext() ).Get( ddlEmail.SelectedValue.AsGuid() );
            if ( template == null )
            {
                return null;
            }

            var mediumData = template.MediumData;
            var MediumData = new Dictionary<string, string>();
            if ( !mediumData.ContainsKey( "Subject" ) )
            {
                mediumData.Add( "Subject", template.Subject );
            }

            foreach ( var dataItem in mediumData )
            {
                if ( !string.IsNullOrWhiteSpace( dataItem.Value ) )
                {
                    if ( MediumData.ContainsKey( dataItem.Key ) )
                    {
                        MediumData[dataItem.Key] = dataItem.Value;
                    }
                    else
                    {
                        MediumData.Add( dataItem.Key, dataItem.Value );
                    }
                }
            }

            return MediumData;
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
            PopulateDropDown();
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSendTest_Click( object sender, EventArgs e )
        {
            var recipients = new List<RecipientData>();
            var personDict = new Dictionary<string, object>();

            personDict.Add( "Person", CurrentPerson );
            recipients.Add( new RecipientData( CurrentPerson.Email, personDict ) );

            if ( GetAttributeValue( "CommunicationType" ) == "System" )
            {
                Email.Send( ddlEmail.SelectedValueAsGuid().Value, recipients );
            }
            else
            {
                var communication = GetCommunication( new RockContext(), null );

                var testCommunication = new Communication();
                testCommunication.SenderPersonAliasId = communication.SenderPersonAliasId;
                testCommunication.Subject = communication.Subject;
                testCommunication.IsBulkCommunication = communication.IsBulkCommunication;
                testCommunication.MediumEntityTypeId = communication.MediumEntityTypeId;
                testCommunication.MediumDataJson = communication.MediumDataJson;
                testCommunication.AdditionalMergeFieldsJson = communication.AdditionalMergeFieldsJson;

                testCommunication.FutureSendDateTime = null;
                testCommunication.Status = CommunicationStatus.Approved;
                testCommunication.ReviewedDateTime = RockDateTime.Now;
                testCommunication.ReviewerPersonAliasId = CurrentPersonAliasId.Value;

                var testRecipient = new CommunicationRecipient();
                if ( communication.Recipients.Any() )
                {
                    var recipient = communication.Recipients.FirstOrDefault();
                    testRecipient.AdditionalMergeValuesJson = recipient.AdditionalMergeValuesJson;
                }
                testRecipient.Status = CommunicationRecipientStatus.Pending;
                testRecipient.PersonAliasId = CurrentPersonAliasId.Value;
                testCommunication.Recipients.Add( testRecipient );

                var rockContext = new RockContext();
                var communicationService = new CommunicationService( rockContext );
                communicationService.Add( testCommunication );
                rockContext.SaveChanges();

                var medium = testCommunication.Medium;
                if ( medium != null )
                {
                    medium.Send( testCommunication );
                }

                communicationService.Delete( testCommunication );
                rockContext.SaveChanges();
            }

            nbSuccess.Text = string.Format( "Sent test at {0}", DateTime.Now );
        }

        #endregion
    }
}