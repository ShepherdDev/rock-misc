using Rock;
using Rock.Attribute;
using Rock.Communication;
using Rock.Data;
using Rock.Model;
using Rock.Web.Cache;
using Rock.Web.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Web.UI.WebControls;

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

                var entityTypeId = EntityTypeCache.Get( "Rock.Communication.Medium.Email" ).Id;

                var emails = new CommunicationTemplateService( rockContext )
                    .Queryable()
                    .OrderBy( c => c.Name );

                foreach ( var email in emails )
                {
                    ddlEmail.Items.Add( new ListItem( email.Name, email.Guid.ToString() ) );
                }
            }
        }

        /// <summary>
        /// Updates a communication model with the user-entered values
        /// </summary>
        /// <param name="communicationService">The service.</param>
        /// <returns></returns>
        private Rock.Model.Communication UpdateCommunication( RockContext rockContext, Guid templateGuid )
        {
            var communicationService = new CommunicationService( rockContext );
            var communicationAttachmentService = new CommunicationAttachmentService( rockContext );
            var communicationRecipientService = new CommunicationRecipientService( rockContext );
            var MediumEntityTypeId = EntityTypeCache.Get( "Rock.Communication.Medium.Email" ).Id;

            Rock.Model.Communication communication = null;
            IQueryable<CommunicationRecipient> qryRecipients = null;

            CommunicationDetails CommunicationData = new CommunicationDetails();
            var template = new CommunicationTemplateService( new RockContext() ).Get( templateGuid );
            if ( template != null )
            {
                CommunicationDetails.Copy( template, CommunicationData );
                CommunicationData.EmailAttachmentBinaryFileIds = template.EmailAttachmentBinaryFileIds;
            }

            if ( communication == null )
            {
                communication = new Rock.Model.Communication();
                communication.Status = CommunicationStatus.Transient;
                communication.SenderPersonAliasId = CurrentPersonAliasId;
                communicationService.Add( communication );
            }

            if ( qryRecipients == null )
            {
                qryRecipients = communication.GetRecipientsQry( rockContext );
            }

            communication.IsBulkCommunication = false;
            var medium = MediumContainer.GetComponentByEntityTypeId( MediumEntityTypeId );
            if ( medium != null )
            {
                communication.CommunicationType = medium.CommunicationType;
            }

            communication.CommunicationTemplateId = template.Id;

            //GetMediumData();

            foreach ( var recipient in communication.Recipients )
            {
                recipient.MediumEntityTypeId = MediumEntityTypeId;
            }

            CommunicationDetails.Copy( CommunicationData, communication );

            // delete any attachments that are no longer included
            foreach ( var attachment in communication.Attachments.Where( a => !CommunicationData.EmailAttachmentBinaryFileIds.Contains( a.BinaryFileId ) ).ToList() )
            {
                communication.Attachments.Remove( attachment );
                communicationAttachmentService.Delete( attachment );
            }

            // add any new attachments that were added
            foreach ( var attachmentBinaryFileId in CommunicationData.EmailAttachmentBinaryFileIds.Where( a => !communication.Attachments.Any( x => x.BinaryFileId == a ) ) )
            {
                communication.AddAttachment( new CommunicationAttachment { BinaryFileId = attachmentBinaryFileId }, CommunicationType.Email );
            }

            communication.FutureSendDateTime = null;

            return communication;
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
                var message = new RockEmailMessage( ddlEmail.SelectedValueAsGuid().Value );
                message.SetRecipients( recipients );
                message.Send();
            }
            else
            {
                // Get existing or new communication record
                var communication = UpdateCommunication( new RockContext(), ddlEmail.SelectedValueAsGuid().Value );
                if ( communication != null )
                {
                    using ( var rockContext = new RockContext() )
                    {
                        // Using a new context (so that changes in the UpdateCommunication() are not persisted )
                        var testCommunication = communication.Clone( false );
                        testCommunication.Id = 0;
                        testCommunication.Guid = Guid.NewGuid();
                        testCommunication.CreatedByPersonAliasId = this.CurrentPersonAliasId;
                        testCommunication.CreatedByPersonAlias = new PersonAliasService( rockContext ).Queryable().Where( a => a.Id == this.CurrentPersonAliasId.Value ).Include( a => a.Person ).FirstOrDefault();

                        testCommunication.ForeignGuid = null;
                        testCommunication.ForeignId = null;
                        testCommunication.ForeignKey = null;

                        testCommunication.FutureSendDateTime = null;
                        testCommunication.Status = CommunicationStatus.Approved;
                        testCommunication.ReviewedDateTime = RockDateTime.Now;
                        testCommunication.ReviewerPersonAliasId = CurrentPersonAliasId;

                        foreach ( var attachment in communication.Attachments )
                        {
                            var cloneAttachment = attachment.Clone( false );
                            cloneAttachment.Id = 0;
                            cloneAttachment.Guid = Guid.NewGuid();
                            cloneAttachment.ForeignGuid = null;
                            cloneAttachment.ForeignId = null;
                            cloneAttachment.ForeignKey = null;

                            testCommunication.Attachments.Add( cloneAttachment );
                        }

                        var testRecipient = new CommunicationRecipient();
                        if ( communication.Recipients.Count > 0 )
                        {
                            var recipient = communication.GetRecipientsQry( rockContext ).FirstOrDefault();
                            testRecipient.AdditionalMergeValuesJson = recipient.AdditionalMergeValuesJson;
                        }

                        testRecipient.Status = CommunicationRecipientStatus.Pending;
                        testRecipient.PersonAliasId = CurrentPersonAliasId.Value;
                        testRecipient.MediumEntityTypeId = EntityTypeCache.Get( "Rock.Communication.Medium.Email" ).Id;
                        testCommunication.Recipients.Add( testRecipient );

                        var communicationService = new CommunicationService( rockContext );
                        communicationService.Add( testCommunication );
                        rockContext.SaveChanges();

                        foreach ( var medium in testCommunication.GetMediums() )
                        {
                            medium.Send( testCommunication );
                        }

                        testRecipient = new CommunicationRecipientService( rockContext )
                            .Queryable().AsNoTracking()
                            .Where( r => r.CommunicationId == testCommunication.Id )
                            .FirstOrDefault();

                        communicationService.Delete( testCommunication );
                        rockContext.SaveChanges();
                    }
                }
            }

            nbSuccess.Text = string.Format( "Sent test at {0}", DateTime.Now );
        }

        #endregion
    }
}