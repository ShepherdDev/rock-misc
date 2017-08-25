using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Self Serve Search (Kiosk)" )]
    [Category( "com_shepherdchurch > Misc" )]
    [Description( "Provides a way to, fairly safely, allow people to search for themselves at a kiosk. No personal information is displayed to the user and the user is not logged in. After a match is selected they are redirected to the indicated page with a QueryString parameter specifying which person they selected." )]

    [LinkedPage( "Cancel Page", "The page to direct the user to if they click the cancel button.", order: 0 )]
    [LinkedPage( "Continue Page", "The page to direct the user to if they select their record.", order: 1 )]
    [LinkedPage( "Register Page", "If set the user will be given the option to register themselves after a failed search.", false, order: 2 )]
    [TextField( "Header Text", "The text to use for the header area of the page.", false, "Person Search", order: 3 )]
    [BooleanField( "Use Person GUID", "Use the Person GUID rather than the ID number when passing to the Continue Page.", false, order: 4 )]
    [TextField( "Query String Key", "The key to use when passing the Person ID to the Continue Page.", true, "person", order: 5 )]
    [TextField( "Include Parameters", "A comma separated list of Page Parameters or Query String Parameters that will be passed on to the Continue or Register page. If left blank then all parameters are passed.", false, "", order: 6 )]
    [CustomDropdownListField( "Search Type", "The type of search to perform.", "Phone,Name,Both", true, "Phone", order: 7 )]
    [DataViewField( "Filter Results", "Filter the results to only those people that are present in this data view.", false, entityTypeName: "Rock.Model.Person", order: 8 )]

    [IntegerField( "Minimum Length", "The minimum number of digits the user must enter to perform a search.", true, 4, "Phone Search", 0, "MinimumPhoneLength" )]
    [IntegerField( "Maximum Length", "The maximum number of digits the user may enter.", true, 11, "Phone Search", 1, "MaximumPhoneLength" )]
    [CustomDropdownListField( "Search Style", "The type of phone number search to perform.", "Contains,Starts With,Ends With", true, "Contains", "Phone Search", 2, "PhoneSearchStyle" )]

    [IntegerField( "Minimum Length", "The minimum number of letters the user must enter to perform a search.", true, 3, "Name Search", 0, "MinimumNameLength" )]
    public partial class SelfServeSearch_Kiosk : RockBlock
    {
        #region Properties and Fields

        /// <summary>
        /// List of PersonDto objects that identify the currently displayed result set.
        /// </summary>
        private List<PersonDto> PeopleResults
        {
            get { return ( List<PersonDto> )ViewState["PeopleResults"]; }
            set { ViewState["PeopleResults"] = value; }
        }

        #endregion

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            base.BlockUpdated += SelfServeSearch_Kiosk_BlockUpdated;

            RockPage.AddScriptLink( "~/Scripts/iscroll.js" );
            RockPage.AddScriptLink( "~/Scripts/Kiosk/kiosk-core.js" );
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
                ShowSearchPanel();
            }
            else
            {
                if (pnlPersonSelect.Visible)
                {
                    BuildPersonControls();
                }
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Show the search panel and clear initial values.
        /// </summary>
        private void ShowSearchPanel()
        {
            tbPhone.Text = string.Empty;
            tbName.Text = string.Empty;

            if ( GetAttributeValue( "SearchType" ) == "Name" )
            {
                pnlSearchName.Visible = true;
                pnlSearchPhone.Visible = false;
                Page.Form.DefaultButton = lbNameSearch.UniqueID;
            }
            else
            {
                if ( GetAttributeValue( "SearchType" ) == "Both" )
                {
                    tbPhone.Label = "Phone Number or Name";
                }
                else
                {
                    tbPhone.Label = "Phone Number";
                }

                pnlSearchPhone.Visible = true;
                pnlSearchName.Visible = false;
                Page.Form.DefaultButton = lbPhoneSearch.UniqueID;
            }

            pnlPersonSelect.Visible = false;
            pnlSearch.Visible = true;
        }

        /// <summary>
        /// Build the PersonDto object list from the results in the IQueryable.
        /// </summary>
        /// <param name="people">The IQueryable that contains the database results.</param>
        protected void BuildResultData( IQueryable<Person> people )
        {
            var searchResults = new List<PersonDto>();

            //
            // Build the list of Data Table Objects that will identify each matched person.
            //
            foreach ( var person in people )
            {
                if ( GetAttributeValue( "UsePersonGUID" ).AsBoolean() )
                {
                    searchResults.Add( new PersonDto( person.Guid.ToString(), person.LastName, person.NickName ) );
                }
                else
                {
                    searchResults.Add( new PersonDto( person.Id.ToString(), person.LastName, person.NickName ) );
                }
            }

            //
            // Store our results so they go into the ViewState.
            //
            this.PeopleResults = searchResults;
        }

        /// <summary>
        /// Search for people by name.
        /// </summary>
        private void SearchByName( string name )
        {
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            bool reversed;

            //
            // Verify they entered the minimum number of characters.
            //
            if ( name.Length < GetAttributeValue( "MinimumNameLength" ).AsInteger() )
            {
                nbNameSearch.Text = string.Format( "You must enter at least {0} letters", GetAttributeValue( "MinimumNameLength" ).AsInteger() );
                return;
            }

            //
            // Perform the search.
            //
            var people = personService.GetByFullName( name, false, true, false, out reversed );
            people = ApplyCommonQueryFilters( people, rockContext );
            BuildResultData( people );

            //
            // Toggle panel visibility and show the results.
            //
            pnlSearchName.Visible = false;
            pnlSearch.Visible = false;
            pnlPersonSelect.Visible = true;

            BuildPersonControls();
        }

        /// <summary>
        /// Perform a search on phone number.
        /// </summary>
        private void SearchByPhone( string phoneNumber )
        {
            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );

            nbPhoneSearch.Text = string.Empty;

            if ( phoneNumber[0] == '1' )
            {
                phoneNumber = phoneNumber.Substring( 1 );
            }

            //
            // Verify that they entered enough digits.
            //
            if ( phoneNumber.Length < GetAttributeValue( "MinimumPhoneLength" ).AsInteger() )
            {
                nbPhoneSearch.Text = string.Format( "You must enter at least {0} digits", GetAttributeValue( "MinimumPhoneLength" ).AsInteger() );
                return;
            }

            //
            // Verify that they entered only digits.
            //
            if ( !phoneNumber.All( char.IsDigit ) )
            {
                nbPhoneSearch.Text = string.Format( "You can only search by numbers only." );
                return;
            }

            //
            // Get the initial person search. This is basically a Contains search.
            //
            var people = personService.GetByPhonePartial( phoneNumber, false, true );

            //
            // If they want to filter on Starts With or Ends With then constrain it even more.
            //
            if ( GetAttributeValue( "PhoneSearchStyle" ) == "Starts With" )
            {
                people = people.Where( p => p.PhoneNumbers.Where( pn => pn.Number.StartsWith( phoneNumber ) ).Any() );
            }
            else if ( GetAttributeValue( "PhoneSearchStyle" ) == "Ends With" )
            {
                people = people.Where( p => p.PhoneNumbers.Where( pn => pn.Number.EndsWith( phoneNumber ) ).Any() );
            }

            people = ApplyCommonQueryFilters( people, rockContext );
            BuildResultData( people );

            //
            // Toggle panel visibility and show the results.
            //
            pnlSearchPhone.Visible = false;
            pnlSearch.Visible = false;
            pnlPersonSelect.Visible = true;

            BuildPersonControls();
        }

        /// <summary>
        /// Apply a set of common query filters to the query of people objects.
        /// </summary>
        /// <param name="people">The base query object.</param>
        /// <param name="rockContext">The context the base query object was constructed in.</param>
        private IQueryable<Person> ApplyCommonQueryFilters( IQueryable<Person> people, RockContext rockContext )
        {
            //
            // Filter the results by the dataview selected.
            //
            if ( !string.IsNullOrEmpty( GetAttributeValue( "FilterResults" ) ) )
            {
                var personService = new PersonService( rockContext );
                DataView filterSource = new DataViewService( rockContext ).Get( GetAttributeValue( "FilterResults" ).AsGuid() );
                List<string> errorMessages = new List<string>();
                var parameterExpression = personService.ParameterExpression;

                var whereExpression = filterSource.GetExpression( personService, parameterExpression, out errorMessages );
                var sourceItems = personService.Get( parameterExpression, whereExpression ).Select( q => q.Id );

                people = people.Where( p => sourceItems.Contains( p.Id ) );
            }

            //
            // Order and limit the results.
            //
            people = people.OrderBy( p => p.LastName ).ThenBy( p => p.FirstName ).Take( 100 );

            return people;
        }

        /// <summary>
        /// Build the person select controls based on the current matched list of people.
        /// </summary>
        private void BuildPersonControls()
        {
            lbPersonSelectAdd.Visible = !string.IsNullOrWhiteSpace( PageParameter( "RegisterPage" ) );
            lbPersonSelectAdd.Text = ( !string.IsNullOrWhiteSpace( PageParameter( "RegisterPage" ) ) ).ToString();
            lbPersonSelectAdd.Visible = true;

            //
            // If we have any results then build the link buttons for the user to click.
            //
            if ( this.PeopleResults.Count > 0 )
            {
                foreach ( var unit in this.PeopleResults )
                {
                    LinkButton lb = new LinkButton();

                    lb.ID = "lbUnit_" + unit.PersonId.ToString();
                    lb.CssClass = "btn btn-primary btn-kioskselect";
                    lb.Text = string.Format( "{0}, <small>{1}</small>", unit.LastName, unit.FirstName );
                    lb.CommandArgument = unit.CommandArg;
                    lb.Click += new EventHandler( personName_Click );

                    phPeople.Controls.Add( lb );
                }

                nbNoResults.Text = string.Empty;
            }
            else
            {
                //
                // There were no matched people. Display an error message telling them their options.
                //
                if ( !string.IsNullOrEmpty( GetAttributeValue( "RegisterPage" ) ) )
                {
                    nbNoResults.Text = "There were not any available people found with the search term you entered. You can add yourself using the 'Register' button below.";
                }
                else
                {
                    nbNoResults.Text = "There were not any available people found with the search term you entered.";
                }
            }
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
        /// Handles the Click event of the lbSearch control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbSearch_Click( object sender, EventArgs e )
        {
            if ( pnlSearchName.Visible )
            {
                SearchByName( tbName.Text.Trim() );
            }
            else
            {
                if ( GetAttributeValue( "SearchType" ) == "Phone" || tbPhone.Text.Trim().All( char.IsDigit ) )
                {
                    SearchByPhone( tbPhone.Text.Trim() );
                }
                else
                {
                    SearchByName( tbPhone.Text.Trim() );
                }
            }
        }

        /// <summary>
        /// Handles the Click event of the lbBack control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbBack_Click( object sender, EventArgs e )
        {
            ShowSearchPanel();
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            if ( !string.IsNullOrEmpty( PageParameter( "return" ) ) )
            {
                Response.Redirect( PageParameter( "return" ) );
                Response.End();
            }
            else
            {
                NavigateToLinkedPage( "CancelPage" );
            }
        }

        /// <summary>
        /// Handles the Click event of the dynamic person select controls.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void personName_Click( object sender, EventArgs e )
        {
            LinkButton lb = ( LinkButton )sender;
            PersonDto dto = new PersonDto( lb.CommandArgument );
            Dictionary<string, string> queryParams = GetNavigationParameters();

            queryParams.AddOrReplace( GetAttributeValue( "QueryStringKey" ), dto.PersonId );

            NavigateToLinkedPage( "ContinuePage", queryParams );
        }

        /// <summary>
        /// Handles the Click event of the lbPersonSelectAdd control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbPersonSelectAdd_Click( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "RegisterPage", GetNavigationParameters() );
        }

        /// <summary>
        /// Handles the BlockUpdated event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void SelfServeSearch_Kiosk_BlockUpdated( object sender, EventArgs e )
        {
            ShowSearchPanel();
        }

        #endregion

        #region Child Classes

        /// <summary>
        /// Defines a Person object in a simplified manner that can be stored in the
        /// ViewState.
        /// </summary>
        [Serializable]
        protected class PersonDto
        {
            /// <summary>
            /// The Identifier of this Person.
            /// </summary>
            public string PersonId { get; set; }

            /// <summary>
            /// The Last Name of this Person.
            /// </summary>
            public string LastName { get; set; }

            /// <summary>
            /// The First Name of this Person.
            /// </summary>
            public string FirstName { get; set; }

            /// <summary>
            /// Command Argument used to identify this Person in postback events.
            /// </summary>
            public string CommandArg
            {
                get { return string.Format( "{0}|{1}|{2}", PersonId, LastName, FirstName ); }
            }

            /// <summary>
            /// Constructs a new PersonDto object based on the supplied parameters.
            /// </summary>
            /// <param name="personId">The Identifier to use for this Person.</param>
            /// <param name="lastName">The Last Name to use for this Person.</param>
            /// <param name="firstNames">The First Name to use for this Person.</param>
            public PersonDto( string personId, string lastName, string firstNames )
            {
                PersonId = personId;
                LastName = lastName;
                FirstName = firstNames;
            }

            /// <summary>
            /// Construct a new PersonDto object based on the command argument.
            /// </summary>
            /// <param name="commandArg">Command argument that was used during postback.</param>
            public PersonDto( string commandArg )
            {
                string[] parts = commandArg.Split( '|' );

                PersonId = parts[0];
                LastName = parts[1];
                FirstName = parts[2];
            }
        }

        #endregion
    }
}
