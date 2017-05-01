using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    [DisplayName( "Group Member Profile Details" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Allow Group Leaders to view profile details of their group members as well as make changes to specific profile information." )]

    [DataViewField( "Disable Editing People In View", "If a DataView is selected here and the target person is in the selected DataView then edits will not be permitted to this individual.", false, "", "Rock.Model.Person", order: 0 )]
    [WorkflowTypeField( "Email Change Workflow", "Allowing Group Leaders the ability to directly edit a user's e-mail address has numerous security implications. If you select a WorkflowType here then this Workflow will be initiated when an edit to the e-mail address is detected. The Workflow's Entity will be the Person being edited. If a Workflow Attribute of 'Email' exists then it will be set to the new e-mail address. The initiator of the Workflow will be the Person who is making the edit.", false, false, order: 1 )]
    [GroupTypesField( "Group Types", "The set of valid GroupTypes that the current person must be a leader of and the identified person must be a member of.", true, order: 2)]
    [BooleanField( "Allow Edit To Co-Leaders", "Allow one leader to edit the information of another leader in the same group.", true, order: 3, key: "AllowEditsToCoLeaders" )]
    [CodeEditorField( "Success Template", "The message that is displayed to the user after the save operation has completed. If no message is set then the user is immedietely returned to the parent page.", CodeEditorMode.Lava, required: false, order: 4 )]

    [BooleanField( "Nick Name", "Allow leader to edit the member's nick name.", true, "Allow Edits To", order: 0, key: "EditNickName" )]
    [BooleanField( "Email", "Allow leader to edit the member's e-mail address.", true, "Allow Edits To", order: 1, key: "EditEmail" )]
    [BooleanField( "Birthday", "Allow leader to edit the member's birthday.", true, "Allow Edits To", order: 2, key: "EditBirthday" )]
    [BooleanField( "Grade", "Allow leader to edit the member's grade.", true, "Allow Edits To", order: 3, key: "EditGrade" )]
    [BooleanField( "MaritalStatus", "Allow leader to edit the member's marital status and anniversary date.", true, "Allow Edits To", order: 4, key: "EditMaritalStatus" )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_PHONE_TYPE, "Phone Types", "Allow leader to edit the member's phone number types selected.", false, true, "", "Allow Edits To", order: 5, key: "EditPhoneTypes" )]
    [GroupLocationTypeField( Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY, "Address Type",
        "The type of address to be displayed / edited.", false, "", "Allow Edits To", order: 6 )]
    public partial class GroupMemberProfileDetails : RockBlock
    {
        /// <summary>
        /// The person that is being edited.
        /// </summary>
        protected int TargetPersonId
        {
            set { ViewState["TargetPersonId"] = value; }
            get { return ViewState["TargetPersonId"].ToString().AsInteger(); }
        }

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            ddlMaritalStatus.BindToDefinedType( DefinedTypeCache.Read( new Guid( Rock.SystemGuid.DefinedType.PERSON_MARITAL_STATUS ) ), true );

            ScriptManager.RegisterStartupScript( ddlGradePicker, ddlGradePicker.GetType(), "grade-selection-" + BlockId.ToString(), ddlGradePicker.GetJavascriptForYearPicker( ypGraduation ), true );

            string smsScript = @"
    $('.js-sms-number').click(function () {
        if ($(this).is(':checked')) {
            $('.js-sms-number').not($(this)).prop('checked', false);
        }
    });
";
            ScriptManager.RegisterStartupScript( rContactInfo, rContactInfo.GetType(), "sms-number-" + BlockId.ToString(), smsScript, true );

            base.BlockUpdated += GroupMemberProfileDetails_BlockUpdated;
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
                if ( string.IsNullOrWhiteSpace( GetAttributeValue( "GroupTypes" ) ) )
                {
                    nbConfigError.Text = "Block has not been configured yet. Please edit the block settings.";
                    pnlEdit.Visible = false;

                    return;
                }

                //
                // Find and verify we have a real person.
                //
                var rockContext = new RockContext();
                var personService = new PersonService( rockContext );
                TargetPersonId = PageParameter( "PersonId" ).AsInteger();
                var person = personService.Get( TargetPersonId );
                if ( person == null || person.Id == 0 )
                {
                    nbInvalidPerson.Text = "The requested person could not be found.";
                    pnlEdit.Visible = false;

                    return;
                }

                //
                // Check if the user is either a group leader or an administrator.
                //
                if ( !IsGroupLeaderFor( person ) && !UserCanAdministrate )
                {
                    nbInvalidPerson.Text = "You are not a group leader for this person.";
                    pnlEdit.Visible = false;

                    return;
                }

                //
                // Verify if this person is in the "do not edit" list.
                //
                if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "DisableEditingPeopleInView" ) ) )
                {
                    var dataView = new DataViewService( rockContext ).Get( GetAttributeValue( "DisableEditingPeopleInView" ).AsGuid() );
                    var errorMessages = new List<string>();
                    var parameterExpression = personService.ParameterExpression;

                    var whereExpression = dataView.GetExpression( personService, parameterExpression, out errorMessages );
                    var list = personService.Get( parameterExpression, whereExpression ).ToList();
                    if ( personService.Get( parameterExpression, whereExpression ).Where( p => p.Id == person.Id ).Any() )
                    {
                        nbInvalidPerson.Text = "The requested person can not be edited by this page.";
                        pnlEdit.Visible = false;

                        return;
                    }
                }

                ShowDetails( true );
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Determine if the currently logged in person is a valid group leader for
        /// the identified person. This takes into account all Block Settings.
        /// </summary>
        /// <param name="person">The person who must be a member of a group lead by CurrentPerson.</param>
        /// <returns>true if the CurrentPerson is a valid leader of `person`.</returns>
        bool IsGroupLeaderFor( Person person )
        {
            if ( CurrentPerson != null )
            {
                using ( var rockContext = new RockContext() )
                {
                    var groupMemberService = new GroupMemberService( rockContext );
                    var groupTypeGuids = GetAttributeValues( "GroupTypes" ).Select( g => new Guid( g ) );
                    var leaderGroups = groupMemberService
                        .GetByPersonId( CurrentPerson.Id )
                        .Where( gm => gm.GroupRole.IsLeader )
                        .Where( gm => groupTypeGuids.Contains( gm.Group.GroupType.Guid ) );
                    var memberGroups = groupMemberService
                        .GetByPersonId( person.Id )
                        .Where( gm => groupTypeGuids.Contains( gm.Group.GroupType.Guid ) );

                    if ( !GetAttributeValue( "AllowEditsToCoLeaders" ).AsBoolean( true ) )
                    { 
                        memberGroups = memberGroups.Where( gm => !gm.GroupRole.IsLeader );
                    }

                    return leaderGroups
                        .Select( gm => gm.GroupId )
                        .Intersect( memberGroups.Select( gm => gm.GroupId ) )
                        .Any();
                }
            }

            return false;
        }

        /// <summary>
        /// Shows the details panel and fills in all the default information about
        /// the person being edited.
        /// </summary>
        /// <param name="setValues">If true then set the default values of the person being edited.</param>
        void ShowDetails( bool setValues )
        {
            RockContext rockContext = new RockContext();
            Person person = new PersonService( rockContext ).Get( TargetPersonId );

            ltFullName.Text = person.FullName;
            rblGender.SelectedValue = person.Gender.ToString();

            //
            // Setup the nickname conditional panel.
            //
            if ( GetAttributeValue( "EditNickName" ).AsBoolean( true ) )
            {
                tbNickName.Text = person.NickName;
                pnlNickName.Visible = true;
            }
            else
            {
                pnlNickName.Visible = false;
            }

            //
            // Setup the birthday conditional panel.
            //
            if ( GetAttributeValue( "EditBirthday" ).AsBoolean( true ) )
            {
                bpBirthDay.SelectedDate = person.BirthDate;
                bpBirthDay.Visible = true;
            }
            else
            {
                bpBirthDay.Visible = false;
            }

            //
            // Setup the grade conditional panel.
            //
            if ( GetAttributeValue( "EditGrade" ).AsBoolean( true ) )
            {
                ypGraduation.SelectedYear = person.GraduationYear;
                if ( !person.HasGraduated ?? false )
                {
                    int gradeOffset = person.GradeOffset.Value;
                    var maxGradeOffset = ddlGradePicker.MaxGradeOffset;

                    while ( !ddlGradePicker.Items.OfType<System.Web.UI.WebControls.ListItem>().Any( a => a.Value.AsInteger() == gradeOffset ) && gradeOffset <= maxGradeOffset )
                    {
                        gradeOffset++;
                    }

                    ddlGradePicker.SetValue( gradeOffset );
                }

                pnlGrade.Visible = true;
            }
            else
            {
                pnlGrade.Visible = false;
            }

            //
            // Setup the Marital Status conditional panel.
            //
            if ( GetAttributeValue( "EditMaritalStatus" ).AsBoolean( true ) )
            {
                ddlMaritalStatus.SelectedValue = person.MaritalStatusValueId.HasValue ? person.MaritalStatusValueId.ToString() : string.Empty;
                dpAnniversaryDate.SelectedDate = person.AnniversaryDate;

                pnlMaritalStatus.Visible = true;
            }
            else
            {
                pnlMaritalStatus.Visible = false;
            }

            //
            // Setup the Email conditional panel.
            //
            if ( GetAttributeValue( "EditEmail" ).AsBoolean( true ) )
            {
                tbEmail.Text = person.Email;
                pnlEmail.Visible = true;
            }
            else
            {
                pnlEmail.Visible = false;
            }

            //
            // Setup the Phones conditional panel.
            //
            if ( GetAttributeValues( "EditPhoneTypes" ).Any() )
            {
                List<PhoneNumber> phoneNumbers = new List<PhoneNumber>();
                foreach ( var phoneType in GetAttributeValues( "EditPhoneTypes" ) )
                {
                    var phoneTypeGuid = new Guid( phoneType );

                    var phoneNumber = person.PhoneNumbers.Where( pn => pn.NumberTypeValue.Guid == phoneTypeGuid ).FirstOrDefault();
                    if ( phoneNumber == null )
                    {
                        phoneNumber = new PhoneNumber();
                        phoneNumber.NumberTypeValue = new DefinedValueService( rockContext ).Get( phoneTypeGuid );
                        phoneNumber.NumberTypeValueId = phoneNumber.NumberTypeValue.Id;
                    }

                    phoneNumbers.Add( phoneNumber );
                }

                rContactInfo.DataSource = phoneNumbers;
                rContactInfo.DataBind();

                pnlPhones.Visible = true;
            }
            else
            {
                pnlPhones.Visible = false;
            }

            //
            // Setup the address conditional panel.
            //
            Guid? locationTypeGuid = GetAttributeValue( "AddressType" ).AsGuidOrNull();
            if ( locationTypeGuid.HasValue )
            {
                pnlAddress.Visible = true;
                var addressTypeDv = DefinedValueCache.Read( locationTypeGuid.Value );

                // if address type is home enable the move and is mailing/physical
                if ( addressTypeDv.Guid == Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() )
                {
                    cbIsMailingAddress.Visible = true;
                    cbIsPhysicalAddress.Visible = true;
                }
                else
                {
                    cbIsMailingAddress.Visible = false;
                    cbIsPhysicalAddress.Visible = false;
                }

                lAddressTitle.Text = addressTypeDv.Value + " Address";

                var familyGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuidOrNull();

                if ( familyGroupTypeGuid.HasValue )
                {
                    var familyGroupType = GroupTypeCache.Read( familyGroupTypeGuid.Value );

                    var familyAddress = new GroupLocationService( rockContext ).Queryable()
                                        .Where( l => l.Group.GroupTypeId == familyGroupType.Id
                                             && l.GroupLocationTypeValueId == addressTypeDv.Id
                                             && l.Group.Members.Any( m => m.PersonId == person.Id ) )
                                        .FirstOrDefault();
                    if ( familyAddress != null )
                    {
                        acAddress.SetValues( familyAddress.Location );

                        cbIsMailingAddress.Checked = familyAddress.IsMailingLocation;
                        cbIsPhysicalAddress.Checked = familyAddress.IsMappedLocation;
                    }
                }

                pnlAddress.Visible = true;
            }
            else
            {
                pnlAddress.Visible = false;
            }
        }

        /// <summary>
        /// Launch a workflow designed to process an e-mail address change request.
        /// </summary>
        /// <param name="workflowType">The type of workflow to be launched.</param>
        /// <param name="member">The member whose e-mail address is going to be changed.</param>
        /// <param name="emailAddress">The new e-mail address of the member.</param>
        void LaunchEmailWorkflow( Guid workflowTypeGuid, Person member, string emailAddress )
        {
            var rockContext = new RockContext();
            var workflowType = new WorkflowTypeService( rockContext ).Get( workflowTypeGuid );

            var workflow = Workflow.Activate( workflowType, string.Format( "E-mail Change For {0}", member.FullName ) );
            workflow.LoadAttributes( rockContext );

            if ( workflow.Attributes.ContainsKey( "Email" ) )
            {
                workflow.SetAttributeValue( "Email", emailAddress );
            }

            var errorMessages = new List<string>();
            new WorkflowService( rockContext ).Process( workflow, member, out errorMessages );
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void GroupMemberProfileDetails_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetails( true );
        }

        /// <summary>
        /// Handles the Click event of the btnSave control. Perform a save operation on
        /// the person being edited and then navigate to the return page.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSave_Click( object sender, EventArgs e )
        {
            RockContext rockContext = new RockContext();
            Person person = new PersonService( rockContext ).Get( TargetPersonId );
            Guid emailWorkflowTypeGuid = Guid.Empty;

            rockContext.WrapTransaction( () =>
            {
                var changes = new List<string>();

                //
                // Process the nick name.
                //
                if ( GetAttributeValue( "EditNickName" ).AsBoolean( true ) )
                {
                    History.EvaluateChange( changes, "Nick Name", person.NickName, tbNickName.Text.Trim() );
                    person.NickName = tbNickName.Text.Trim();
                }

                //
                // Process the gender.
                //
                var newGender = rblGender.SelectedValue.ConvertToEnum<Gender>();
                History.EvaluateChange( changes, "Gender", person.Gender, newGender );
                person.Gender = newGender;

                //
                // Process the birthday.
                //
                if ( GetAttributeValue( "EditBirthday" ).AsBoolean( true ) )
                {
                    var birthMonth = person.BirthMonth;
                    var birthDay = person.BirthDay;
                    var birthYear = person.BirthYear;
                    var birthday = bpBirthDay.SelectedDate;

                    if ( birthday.HasValue )
                    {
                        // If setting a future birthdate, subtract a century until birthdate is not greater than today.
                        var today = RockDateTime.Today;
                        while ( birthday.Value.CompareTo( today ) > 0 )
                        {
                            birthday = birthday.Value.AddYears( -100 );
                        }

                        person.BirthMonth = birthday.Value.Month;
                        person.BirthDay = birthday.Value.Day;
                        if ( birthday.Value.Year != DateTime.MinValue.Year )
                        {
                            person.BirthYear = birthday.Value.Year;
                        }
                        else
                        {
                            person.BirthYear = null;
                        }
                    }
                    else
                    {
                        person.SetBirthDate( null );
                    }

                    History.EvaluateChange( changes, "Birth Month", birthMonth, person.BirthMonth );
                    History.EvaluateChange( changes, "Birth Day", birthDay, person.BirthDay );
                    History.EvaluateChange( changes, "Birth Year", birthYear, person.BirthYear );
                }

                //
                // Process the grade / graduation year.
                //
                if ( GetAttributeValue( "EditGrade" ).AsBoolean( true ) )
                {
                    int? graduationYear = null;
                    if ( ypGraduation.SelectedYear.HasValue )
                    {
                        graduationYear = ypGraduation.SelectedYear.Value;
                    }

                    History.EvaluateChange( changes, "Graduation Year", person.GraduationYear, graduationYear );
                    person.GraduationYear = graduationYear;
                }

                //
                // Process the marital status and anniversary date.
                //
                if ( GetAttributeValue( "EditMaritalStatus" ).AsBoolean( true ) )
                {
                    int? newMaritalStatusId = ddlMaritalStatus.SelectedValueAsInt();
                    History.EvaluateChange( changes, "Marital Status", DefinedValueCache.GetName( person.MaritalStatusValueId ), DefinedValueCache.GetName( newMaritalStatusId ) );
                    person.MaritalStatusValueId = newMaritalStatusId;

                    History.EvaluateChange( changes, "Anniversary Date", person.AnniversaryDate, dpAnniversaryDate.SelectedDate );
                    person.AnniversaryDate = dpAnniversaryDate.SelectedDate;
                }

                //
                // Process the e-mail address.
                //
                if ( GetAttributeValue( "EditEmail" ).AsBoolean( true ) )
                {
                    emailWorkflowTypeGuid = GetAttributeValue( "EmailChangeWorkflow" ).AsGuid();

                    if ( emailWorkflowTypeGuid == Guid.Empty )
                    {
                        History.EvaluateChange( changes, "Email", person.Email, tbEmail.Text.Trim() );
                        person.Email = tbEmail.Text.Trim();
                    }
                }

                //
                // Process all the phone numbers for this person.
                //
                bool smsSelected = false;
                var phoneNumberTypeIds = new List<int>();
                foreach ( RepeaterItem item in rContactInfo.Items )
                {
                    HiddenField hfPhoneType = item.FindControl( "hfPhoneType" ) as HiddenField;
                    PhoneNumberBox pnbPhone = item.FindControl( "pnbPhone" ) as PhoneNumberBox;
                    CheckBox cbSms = item.FindControl( "cbSms" ) as CheckBox;

                    if ( hfPhoneType != null &&
                        pnbPhone != null &&
                        cbSms != null )
                    {
                        if ( !string.IsNullOrWhiteSpace( PhoneNumber.CleanNumber( pnbPhone.Number ) ) )
                        {
                            int phoneNumberTypeId;
                            if ( int.TryParse( hfPhoneType.Value, out phoneNumberTypeId ) )
                            {
                                var phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == phoneNumberTypeId );
                                string oldPhoneNumber = string.Empty;
                                if ( phoneNumber == null )
                                {
                                    phoneNumber = new PhoneNumber { NumberTypeValueId = phoneNumberTypeId };
                                    person.PhoneNumbers.Add( phoneNumber );
                                }
                                else
                                {
                                    oldPhoneNumber = phoneNumber.NumberFormattedWithCountryCode;
                                }

                                phoneNumber.CountryCode = PhoneNumber.CleanNumber( pnbPhone.CountryCode );
                                phoneNumber.Number = PhoneNumber.CleanNumber( pnbPhone.Number );

                                // Only allow one number to have SMS selected
                                if ( smsSelected )
                                {
                                    phoneNumber.IsMessagingEnabled = false;
                                }
                                else
                                {
                                    phoneNumber.IsMessagingEnabled = cbSms.Checked;
                                    smsSelected = cbSms.Checked;
                                }

                                phoneNumberTypeIds.Add( phoneNumberTypeId );

                                History.EvaluateChange(
                                    changes,
                                    string.Format( "{0} Phone", DefinedValueCache.GetName( phoneNumberTypeId ) ),
                                    oldPhoneNumber,
                                    phoneNumber.NumberFormattedWithCountryCode );
                            }
                        }
                    }
                }

                //
                // Remove any blank numbers.
                //
                var phoneNumberService = new PhoneNumberService( rockContext );
                foreach ( var phoneNumber in person.PhoneNumbers
                    .Where( n => n.NumberTypeValueId.HasValue && !phoneNumberTypeIds.Contains( n.NumberTypeValueId.Value ) )
                    .ToList() )
                {
                    History.EvaluateChange(
                        changes,
                        string.Format( "{0} Phone", DefinedValueCache.GetName( phoneNumber.NumberTypeValueId ) ),
                        phoneNumber.ToString(),
                        string.Empty );

                    person.PhoneNumbers.Remove( phoneNumber );
                    phoneNumberService.Delete( phoneNumber );
                }

                //
                // If the person is valid then save the person and begin
                // working on the family (address).
                //
                if ( person.IsValid )
                {
                    if ( rockContext.SaveChanges() > 0 )
                    {
                        if ( changes.Any() )
                        {
                            HistoryService.SaveChanges(
                                rockContext,
                                typeof( Person ),
                                Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(),
                                person.Id,
                                changes );

                            changes.Clear();
                        }
                    }

                    //
                    // Save the family address information.
                    //
                    if ( pnlAddress.Visible )
                    {
                        Guid? familyGroupTypeGuid = Rock.SystemGuid.GroupType.GROUPTYPE_FAMILY.AsGuidOrNull();
                        if ( familyGroupTypeGuid.HasValue )
                        {
                            var familyGroup = new GroupService( rockContext ).Queryable()
                                            .Where( f => f.GroupType.Guid == familyGroupTypeGuid.Value
                                                && f.Members.Any( m => m.PersonId == person.Id ) )
                                            .FirstOrDefault();
                            if ( familyGroup != null )
                            {
                                Guid? addressTypeGuid = GetAttributeValue( "AddressType" ).AsGuidOrNull();
                                if ( addressTypeGuid.HasValue )
                                {
                                    var groupLocationService = new GroupLocationService( rockContext );

                                    var addressTypeDv = DefinedValueCache.Read( addressTypeGuid.Value );
                                    var familyAddress = groupLocationService.Queryable().Where( l => l.GroupId == familyGroup.Id && l.GroupLocationTypeValueId == addressTypeDv.Id ).FirstOrDefault();
                                    if ( familyAddress != null && string.IsNullOrWhiteSpace( acAddress.Street1 ) )
                                    {
                                        // delete the current address
                                        History.EvaluateChange( changes, familyAddress.GroupLocationTypeValue.Value + " Location", familyAddress.Location.ToString(), string.Empty );
                                        groupLocationService.Delete( familyAddress );
                                        rockContext.SaveChanges();
                                    }
                                    else
                                    {
                                        if ( !string.IsNullOrWhiteSpace( acAddress.Street1 ) )
                                        {
                                            var previousAddressValue = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_PREVIOUS.AsGuid() );

                                            if ( familyAddress == null )
                                            {
                                                familyAddress = new GroupLocation();
                                                groupLocationService.Add( familyAddress );
                                                familyAddress.GroupLocationTypeValueId = addressTypeDv.Id;
                                                familyAddress.GroupId = familyGroup.Id;
                                                familyAddress.IsMailingLocation = true;
                                                familyAddress.IsMappedLocation = true;
                                            }
                                            else if ( addressTypeDv.Guid == Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME.AsGuid() && previousAddressValue != null )
                                            {
                                                var isChanged = familyAddress.Location.Street1 != acAddress.Street1 ||
                                                    familyAddress.Location.Street2 != acAddress.Street2 ||
                                                    familyAddress.Location.City != acAddress.City ||
                                                    familyAddress.Location.State != acAddress.State ||
                                                    familyAddress.Location.PostalCode != acAddress.PostalCode;

                                                var hasPrevious = groupLocationService
                                                    .Queryable( "Location" )
                                                    .Where( l => l.GroupLocationTypeValueId == previousAddressValue.Id && l.GroupId == familyGroup.Id )
                                                    .Where( l => l.Location.Street1 == familyAddress.Location.Street1 )
                                                    .Where( l => l.Location.Street2 == familyAddress.Location.Street2 )
                                                    .Where( l => l.Location.City == familyAddress.Location.City )
                                                    .Where( l => l.Location.State == familyAddress.Location.State )
                                                    .Where( l => l.Location.PostalCode == familyAddress.Location.PostalCode )
                                                    .Where( l => l.Location.Country == familyAddress.Location.Country )
                                                    .Any();

                                                //
                                                // Only save the previous address if it has actually changed and there is not
                                                // already a matching previous address.
                                                //
                                                if ( isChanged && !hasPrevious )
                                                {
                                                    var previousAddress = new GroupLocation();
                                                    groupLocationService.Add( previousAddress );

                                                    previousAddress.GroupLocationTypeValueId = previousAddressValue.Id;
                                                    previousAddress.GroupId = familyGroup.Id;

                                                    Location previousAddressLocation = new Location();
                                                    previousAddressLocation.Street1 = familyAddress.Location.Street1;
                                                    previousAddressLocation.Street2 = familyAddress.Location.Street2;
                                                    previousAddressLocation.City = familyAddress.Location.City;
                                                    previousAddressLocation.State = familyAddress.Location.State;
                                                    previousAddressLocation.PostalCode = familyAddress.Location.PostalCode;
                                                    previousAddressLocation.Country = familyAddress.Location.Country;

                                                    previousAddress.Location = previousAddressLocation;
                                                }
                                            }

                                            familyAddress.IsMailingLocation = cbIsMailingAddress.Checked;
                                            familyAddress.IsMappedLocation = cbIsPhysicalAddress.Checked;

                                            var updatedHomeAddress = new Location();
                                            acAddress.GetValues( updatedHomeAddress );

                                            History.EvaluateChange( changes, addressTypeDv.Value + " Location", familyAddress.Location != null ? familyAddress.Location.ToString() : string.Empty, updatedHomeAddress.ToString() );

                                            familyAddress.Location = updatedHomeAddress;
                                            rockContext.SaveChanges();
                                        }
                                    }

                                    HistoryService.SaveChanges(
                                        rockContext,
                                        typeof( Person ),
                                        Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(),
                                        person.Id,
                                        changes );
                                }
                            }
                        }
                    }
                }
            } );

            if ( emailWorkflowTypeGuid != Guid.Empty )
            {
                LaunchEmailWorkflow( emailWorkflowTypeGuid, person, tbEmail.Text.Trim() );
            }

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "SuccessTemplate" ) ) )
            {
                pnlEdit.Visible = false;
                pnlSuccess.Visible = true;

                nbSuccess.Text = GetAttributeValue( "SuccessTemplate" ).ResolveMergeFields( new Dictionary<string, object>() );
            }
            else
            {
                NavigateToParentPage();
            }
        }

        /// <summary>
        /// Handles the Click event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnCancel_Click( object sender, EventArgs e )
        {
            NavigateToParentPage();
        }

        #endregion
    }
}