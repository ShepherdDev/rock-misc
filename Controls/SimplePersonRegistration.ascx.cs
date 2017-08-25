using System;
using System.Collections.Generic;
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
    [DisplayName( "Simple Person Registration" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Simple block to register a new person in the system and then redirect them to a specific page." )]

    [LinkedPage( "Registration Complete Page", "The page to direct the user to once they have registered.", order: 0 )]
    [CustomDropdownListField( "Pass Person", "Pass the registered person to the registration complete page via the query string parameter Person.", "None,Id,Guid", true, "None", order: 1 )]
    [CustomDropdownListField( "Home Phone", "Whether or not to request/require the home phone number be filled in by the user.", "Hidden,Optional,Required", true, "Hidden", order: 2 )]
    [CustomDropdownListField( "Mobile Phone", "Whether or not to request/require the mobile phone number be filled in by the user.", "Hidden,Optional,Required", true, "Hidden", order: 3 )]
    [CustomDropdownListField( "Home Address", "Whether or not to request/require the home address be filled in by the user.", "Hidden,Optional,Required", true, "Hidden", order: 4 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_CONNECTION_STATUS, "Connection Status", "The connection status to use for new individuals (default: 'Web Prospect'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_CONNECTION_STATUS_WEB_PROSPECT, order: 5 )]
    [DefinedValueField( Rock.SystemGuid.DefinedType.PERSON_RECORD_STATUS, "Record Status", "The record status to use for new individuals (default: 'Pending'.)", true, false, Rock.SystemGuid.DefinedValue.PERSON_RECORD_STATUS_PENDING, order: 6 )]
    [TextField( "Include Parameters", "A comma separated list of Page Parameters or Query String Parameters that will be passed on to the Registration Complete page. If left blank then all parameters are passed.", false, "", order: 7 )]
    public partial class SimplePersonRegistration : RockBlock
    {
        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            base.BlockUpdated += SimplePersonRegistration_BlockUpdated;
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
        /// Show the form details.
        /// </summary>
        protected void ShowDetails()
        {
            var homePhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
            var mobilePhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );

            pnHome.Label = homePhoneType.Value;
            pnMobile.Label = mobilePhoneType.Value;

            if ( CurrentPerson != null )
            {
                tbFirstName.Text = CurrentPerson.FirstName;
                tbLastName.Text = CurrentPerson.LastName;
                tbEmail.Text = CurrentPerson.Email;
                acHomeAddress.SetValues( CurrentPerson.GetHomeLocation() );

                var homePhone = CurrentPerson.PhoneNumbers.Where( p => p.NumberTypeValueId == homePhoneType.Id ).FirstOrDefault();
                pnHome.Number = homePhone != null ? homePhone.NumberFormatted : string.Empty;

                var mobilePhone = CurrentPerson.PhoneNumbers.Where( p => p.NumberTypeValueId == mobilePhoneType.Id ).FirstOrDefault();
                pnMobile.Number = mobilePhone != null ? mobilePhone.NumberFormatted : string.Empty;
            }

            acHomeAddress.Visible = GetAttributeValue( "HomeAddress" ) != "Hidden";
            acHomeAddress.Required = GetAttributeValue( "HomeAddress" ) == "Required";
            colHomePhone.Visible = GetAttributeValue( "HomePhone" ) != "Hidden";
            pnHome.Required = GetAttributeValue( "HomePhone" ) == "Required";
            colMobilePhone.Visible = GetAttributeValue( "MobilePhone" ) != "Hidden";
            pnMobile.Required = GetAttributeValue( "MobilePhone" ) == "Required";
        }

        /// <summary>
        /// Set the phone number of the person.
        /// </summary>
        /// <param name="person">The Person whose phone number is to be changed.</param>
        /// <param name="phoneType">The phone number type of the phone number.</param>
        /// <param name="pnBox">The value of the phone number.</param>
        /// <param name="changes">The list of changes to later be recorded to history.</param>
        protected void SetPhone( Person person, DefinedValueCache phoneType, Rock.Web.UI.Controls.PhoneNumberBox pnBox, List<string> changes )
        {
            var phoneNumber = person.PhoneNumbers.FirstOrDefault( n => n.NumberTypeValueId == phoneType.Id );
            string oldPhone = string.Empty;

            if ( phoneNumber == null )
            {
                phoneNumber = new PhoneNumber { NumberTypeValueId = phoneType.Id };
                person.PhoneNumbers.Add( phoneNumber );
            }
            else
            {
                oldPhone = phoneNumber.NumberFormattedWithCountryCode;
            }

            phoneNumber.CountryCode = PhoneNumber.CleanNumber( pnBox.CountryCode );
            phoneNumber.Number = PhoneNumber.CleanNumber( pnBox.Number );

            History.EvaluateChange( changes, string.Format( "{0} Phone", phoneType.Value ), oldPhone, phoneNumber.NumberFormattedWithCountryCode );
        }

        /// <summary>
        /// Retrieves the standard navigation parameters that will be passed to linked pages.
        /// </summary>
        /// <returns>Dictionary of strings which identifies the navigation parameters</returns>
        Dictionary<string, string> GetNavigationParameters()
        {
            var parameters = PageParameters();

            //
            // If they have not defined any included parameters, include everything except
            // the PageId as that is a standard parameter in all requests.
            //
            if ( string.IsNullOrWhiteSpace( PageParameter( "IncludeParameters" ) ) )
            {
                return parameters
                    .Keys
                    .Where( k => k != "PageId" )
                    .ToDictionary( k => k, k => parameters[k].ToString() );
            }

            //
            // Otherwise make a list of the parameters they want to include.
            //
            List<string> include = PageParameter( "IncludeParameters" )
                .Split( ',' )
                .Select( s => s.Trim() )
                .ToList();

            //
            // And generate navigation parameters based on that.
            //
            return parameters
                .Keys
                .Where( k => include.Contains( k ) )
                .ToDictionary( k => k, k => parameters[k].ToString() );
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void SimplePersonRegistration_BlockUpdated( object sender, EventArgs e )
        {
            ShowDetails();
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnSubmit_Click( object sender, EventArgs e )
        {
            var queryParameters = GetNavigationParameters();
            var person = GetPerson( new RockContext() );

            if ( GetAttributeValue( "PassPerson" ) == "Id" )
            {
                queryParameters.Add( "Person", person.Id.ToString() );
            }
            else if ( GetAttributeValue( "PassPerson" ) == "Guid" )
            {
                queryParameters.Add( "Person", person.Guid.ToString() );
            }

            NavigateToLinkedPage( "RegistrationCompletePage", queryParameters );
        }

        /// <summary>
        /// Gets the person.
        /// </summary>
        /// <param name="rockContext">The rock context.</param>
        /// <returns></returns>
        private Person GetPerson( RockContext rockContext )
        {
            var personService = new PersonService( rockContext );
            var homeAddressType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.GROUP_LOCATION_TYPE_HOME );
            var homePhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_HOME.AsGuid() );
            var mobilePhoneType = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_PHONE_TYPE_MOBILE.AsGuid() );
            var changes = new List<string>();
            var familyChanges = new List<string>();
            Person person = null;
            Group family = null;
            GroupLocation homeLocation = null;
            bool isNew = false;

            //
            // Look for existing person.
            //
            person = personService.GetByMatch( tbFirstName.Text, tbLastName.Text, tbEmail.Text ).FirstOrDefault();

            if ( person == null )
            {
                //
                // Create new person.
                //
                DefinedValueCache dvcConnectionStatus = DefinedValueCache.Read( GetAttributeValue( "ConnectionStatus" ).AsGuid() );
                DefinedValueCache dvcRecordStatus = DefinedValueCache.Read( GetAttributeValue( "RecordStatus" ).AsGuid() );

                person = new Person();
                person.FirstName = tbFirstName.Text;
                person.LastName = tbLastName.Text;
                person.Email = tbEmail.Text;
                person.IsEmailActive = true;
                person.EmailPreference = EmailPreference.EmailAllowed;
                person.RecordTypeValueId = DefinedValueCache.Read( Rock.SystemGuid.DefinedValue.PERSON_RECORD_TYPE_PERSON.AsGuid() ).Id;
                if ( dvcConnectionStatus != null )
                {
                    person.ConnectionStatusValueId = dvcConnectionStatus.Id;
                }

                if ( dvcRecordStatus != null )
                {
                    person.RecordStatusValueId = dvcRecordStatus.Id;
                }

                family = PersonService.SaveNewPerson( person, rockContext, null, false );
                isNew = true;
            }
            else
            {
                //
                // Find existing family.
                //
                var families = person.GetFamilies( rockContext );
                if ( acHomeAddress.Visible )
                {
                    foreach ( var f in families )
                    {
                        homeLocation = f.GroupLocations
                            .Where( l => l.GroupLocationTypeValueId == homeAddressType.Id && l.IsMappedLocation )
                            .FirstOrDefault();

                        if ( homeLocation != null )
                        {
                            family = f;
                            break;
                        }
                    }
                }

                if ( family == null )
                {
                    family = families.First();
                }
            }

            //
            // Set the home phone.
            //
            if ( colHomePhone.Visible && !string.IsNullOrWhiteSpace( pnHome.Number ) )
            {
                SetPhone( person, homePhoneType, pnHome, changes );
            }

            //
            // Set the work phone.
            //
            if ( colMobilePhone.Visible && !string.IsNullOrWhiteSpace( pnMobile.Number ) )
            {
                SetPhone( person, mobilePhoneType, pnMobile, changes );
            }

            //
            // Set home address.
            //
            if ( acHomeAddress.Visible && !string.IsNullOrWhiteSpace( acHomeAddress.Street1 ) )
            {
                var location = new LocationService( rockContext ).Get( acHomeAddress.Street1, acHomeAddress.Street2, acHomeAddress.City, acHomeAddress.State, acHomeAddress.PostalCode, acHomeAddress.Country );
                if ( location != null )
                {
                    string oldLocation = homeLocation != null ? homeLocation.Location.ToString() : string.Empty;
                    string newLocation = location.ToString();

                    if ( homeLocation == null )
                    {
                        homeLocation = new GroupLocation { GroupLocationTypeValueId = homeAddressType.Id };
                        family.GroupLocations.Add( homeLocation );
                    }

                    homeLocation.Location = location;
                    History.EvaluateChange( familyChanges, "Home Location", oldLocation, newLocation );
                }
            }

            //
            // Save all changes.
            //
            rockContext.SaveChanges();
            if ( !isNew )
            {
                HistoryService.SaveChanges( rockContext, typeof( Person ),
                    Rock.SystemGuid.Category.HISTORY_PERSON_DEMOGRAPHIC_CHANGES.AsGuid(), person.Id, changes );
                HistoryService.SaveChanges( rockContext, typeof( Person ),
                    Rock.SystemGuid.Category.HISTORY_PERSON_FAMILY_CHANGES.AsGuid(), person.Id, familyChanges );
            }

            return person;
        }

        #endregion
    }
}