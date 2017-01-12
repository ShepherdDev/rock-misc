using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
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
    [Description( "" )]

    [LinkedPage( "Cancel Page", "The page to direct the user to if they click the cancel button.", order: 0 )]
    [LinkedPage( "Continue Page", "The page to direct the user to if they select their record.", order: 1 )]
//    [LinkedPage( "Register Page", "If set the user will be given the option to register themselves after a failed search.", false )]
    [BooleanField( "Use Person GUID", "Use the Person GUID rather than the ID number when passing to the Continue Page.", false, order: 2 )]
    [TextField( "Query String Key", "The key to use when passing the Person ID to the Continue Page.", true, "person", order: 3 )]
    [CustomDropdownListField( "Search Type", "The type of search to perform.", "Phone,Name", true, "Phone", order: 4 )]

    [IntegerField( "Minimum Length", "The minimum number of digits the user must enter to perform a search.", true, 4, "Phone Search", 0, "MinimumPhoneLength" )]
    [IntegerField( "Maximum Length", "The maximum number of digits the user may enter.", true, 11, "Phone Search", 1, "MaximumPhoneLength" )]
    [CustomDropdownListField( "Search Style", "The type of phone number search to perform.", "Contains,Starts With,Ends With", true, "Contains", "Phone Search", 2, "PhoneSearchStyle" )]

    [IntegerField( "Minimum Length", "The minimum number of letters the user must enter to perform a search.", true, 3, "Name Search", 0, "MinimumNameLength" )]
    public partial class SelfServeSearch_Kiosk : RockBlock
    {
        private List<PersonDto> PeopleResults
        {
            get { return ( List<PersonDto> )ViewState["PeopleResults"]; }
            set { ViewState["PeopleResults"] = value; }
        }

        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddScriptLink( "~/Scripts/iscroll.js" );
            RockPage.AddScriptLink( "~/Scripts/Kiosk/kiosk-core.js" );
        }

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

        protected void lbSearch_Click( object sender, EventArgs e )
        {
            if ( pnlSearchName.Visible )
            {
                SearchByName();
            }
            else
            {
                SearchByPhone();
            }
        }

        void ShowSearchPanel()
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
                pnlSearchPhone.Visible = true;
                pnlSearchName.Visible = false;
                Page.Form.DefaultButton = lbPhoneSearch.UniqueID;
            }

            pnlPersonSelect.Visible = false;
        }

        private void SearchByName()
        {
            var searchResults = new List<PersonDto>();

            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            string name = tbName.Text;
            bool reversed;

            if ( name.Length < GetAttributeValue( "MinimumNameLength" ).AsInteger() )
            {
                nbNameSearch.Text = string.Format( "You must enter at least {0} letters", GetAttributeValue( "MinimumNameLength" ).AsInteger() );
                return;
            }

            var people = personService.GetByFullName( name, false, true, false, out reversed );

            foreach ( var person in people.ToList() )
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

            this.PeopleResults = searchResults;

            pnlSearchName.Visible = false;
            pnlPersonSelect.Visible = true;

            BuildPersonControls();
        }

        private void SearchByPhone()
        {
            var searchResults = new List<PersonDto>();

            RockContext rockContext = new RockContext();
            PersonService personService = new PersonService( rockContext );
            string phoneNumber = tbPhone.Text;

            if ( phoneNumber.Length < GetAttributeValue( "MinimumPhoneLength" ).AsInteger() )
            {
                nbPhoneSearch.Text = string.Format( "You must enter at least {0} digits", GetAttributeValue( "MinimumPhoneLength" ).AsInteger() );
                return;
            }

            var people = personService.GetByPhonePartial( phoneNumber, false, true );

            if ( GetAttributeValue( "PhoneSearchStyle" ) == "Starts With" )
            {
                people = people.Where( p => p.PhoneNumbers.Where( pn => pn.Number.StartsWith( phoneNumber ) ).Any() );
            }
            else if ( GetAttributeValue( "PhoneSearchStyle" ) == "Ends With" )
            {
                people = people.Where( p => p.PhoneNumbers.Where( pn => pn.Number.EndsWith( phoneNumber ) ).Any() );
            }

            foreach ( var person in people.ToList() )
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

            this.PeopleResults = searchResults;

            pnlSearchPhone.Visible = false;
            pnlPersonSelect.Visible = true;

            BuildPersonControls();
        }

        protected void lbBack_Click( object sender, EventArgs e )
        {
            ShowSearchPanel();
        }

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

        void personName_Click( object sender, EventArgs e )
        {
            LinkButton lb = ( LinkButton )sender;
            PersonDto dto = new PersonDto( lb.CommandArgument );
            Dictionary<string, string> queryParams = Request.QueryString.AllKeys.ToDictionary( p => p, p => Request.QueryString[p] );

            queryParams.AddOrReplace( GetAttributeValue( "QueryStringKey" ), dto.PersonId );

            NavigateToLinkedPage( "ContinuePage", queryParams );
        }

        private void BuildPersonControls()
        {
            lbPersonSelectAdd.Visible = false;

            // display results
            if ( this.PeopleResults.Count > 0 )
            {
                foreach ( var unit in this.PeopleResults )
                {
                    LinkButton lb = new LinkButton();
                    lb.ID = "lbUnit_" + unit.PersonId.ToString();
                    lb.CssClass = "btn btn-primary btn-kioskselect";
                    phPeople.Controls.Add( lb );
                    lb.CommandArgument = unit.CommandArg;
                    lb.Click += new EventHandler( personName_Click );
                    lb.Text = string.Format( "{0}, <small>{1}</small>", unit.LastName, unit.FirstName );
                }
            }
            else
            {
                string message;

                if ( !string.IsNullOrEmpty( GetAttributeValue( "RegisterPage" ) ) )
                {
                    message = "There were not any families found with the phone number you entered. You can add yourself using the 'Register' button below.";
                    lbPersonSelectAdd.Visible = true;
                }
                else
                {
                    message = "There were not any families found with the phone number you entered.";
                }

                phPeople.Controls.Add( new LiteralControl( string.Format( "<div class='alert alert-info'>{0}</div>", message ) ) );
            }
        }

        protected void lbPersonSelectAdd_Click( object sender, EventArgs e )
        {
            NavigateToLinkedPage( "RegisterPage" );
        }
    }

    [Serializable]
    class PersonDto
    {
        public string PersonId { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }

        public string CommandArg
        {
            get { return string.Format( "{0}|{1}|{2}", PersonId, LastName, FirstName ); }
        }

        public PersonDto( string personId, string lastName, string firstNames )
        {
            PersonId = personId;
            LastName = lastName;
            FirstName = firstNames;
        }

        public PersonDto( string commandArg )
        {
            string[] parts = commandArg.Split( '|' );

            PersonId = parts[0];
            LastName = parts[1];
            FirstName = parts[2];
        }
    }
}
