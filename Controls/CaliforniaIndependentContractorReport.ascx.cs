using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;
using System.Web.UI;

using Rock;
using Rock.Attribute;
using Rock.Data;
using Rock.Model;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "California Independent Contractor Report" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Used in conjunction with ShelbyNEXT to generate an Independent Contractor report file that can be uploaded to the EDD.." )]

    #region Block Attributes

    [TextField( "Shelby Next Database",
        description: "The name of the ShelbyNext database that contains the vendor data.",
        required: true,
        defaultValue: "",
        order: 0,
        key: AttributeKeys.ShelbyNextDatabase )]

    [TextField( "Legal Business Name",
        description: "The legal business name to use on the exported file.",
        required: true,
        defaultValue: "",
        order: 1,
        key: AttributeKeys.LegalBusinessName )]

    [TextField( "Federal Employer Identification Number",
        description: "The EIN assigned to your church.",
        required: true,
        defaultValue: "",
        order: 2,
        key: AttributeKeys.FederalEmployerIdentificationNumber )]

    [TextField( "EDD Employer Account Number",
        description: "The account number assigned to your church by the state of California EDD.",
        required: false,
        defaultValue: "",
        order: 3,
        key: AttributeKeys.EddEmployerAccountNumber )]

    #endregion

    public partial class CaliforniaIndependentContractorReport : RockBlock
    {
        protected static class AttributeKeys
        {
            public const string ShelbyNextDatabase = "ShelbyNextDatabase";
            public const string LegalBusinessName = "LegalBusinessName";
            public const string FederalEmployerIdentificationNumber = "FederalEmployerIdentificationNumber";
            public const string EddEmployerAccountNumber = "EddEmployerAccountNumber";
        }

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( EventArgs e )
        {
            base.OnLoad( e );

            if ( !Page.IsPostBack )
            {
                dpRunDate.SelectedDate = RockDateTime.Now.Date;
                dpLastRunDate.SelectedDate = SystemSettings.GetValue( "com.shepherdchurch.CaliforniaIndependentContractorReport.LastRunDate" ).AsDateTime();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the service recipient record.
        /// </summary>
        /// <returns></returns>
        private ServiceRecipientRecord GetServiceRecipientRecord()
        {
            Location location = null;

            using ( var rockContext = new RockContext() )
            {
                var locationService = new LocationService( rockContext );

                location = locationService.Get( GlobalAttributesCache.Value( "OrganizationAddress" ).AsGuid() );
            }

            var record = new ServiceRecipientRecord
            {
                FederalEmployerIdentificationNumber = GetAttributeValue( AttributeKeys.FederalEmployerIdentificationNumber ),
                EmployerAccountNumber = GetAttributeValue( AttributeKeys.EddEmployerAccountNumber ),
                Name = GetAttributeValue( AttributeKeys.LegalBusinessName ),
                PhoneNumber = GlobalAttributesCache.Value( "OrganizationPhone" )
            };

            if ( location != null )
            {
                record.StreetAddress = location.Street1;
                record.City = location.City;
                record.State = location.State;
                record.PostalCode = location.PostalCode;
            }

            return record;
        }

        /// <summary>
        /// Gets the service provider records.
        /// </summary>
        /// <returns></returns>
        private List<ServiceProviderRecord> GetServiceProviderRecords()
        {
            Dictionary<string, object> parameters = new Dictionary<string, object>();
            var sproc = string.Format( "{0}.[dbo].[_cust_sp_shep_independent_contractor_data]", GetAttributeValue( AttributeKeys.ShelbyNextDatabase ) );

            var runDate = dpRunDate.SelectedDate.Value;

            parameters.Add( "TargetAmount", 600 );
            parameters.Add( "RunDate", runDate );
            parameters.Add( "LastRunDate", ( object ) dpLastRunDate.SelectedDate ?? DBNull.Value );

            var results = DbService.GetDataTable( sproc, CommandType.StoredProcedure, parameters, null );
            var records = new List<ServiceProviderRecord>();

            foreach ( DataRow row in results.Rows )
            {
                var record = new ServiceProviderRecord
                {
                    FirstName = row.Field<string>( "first_name" ),
                    MiddleInitial = row.Field<string>( "middle_name" ).Left( 1 ),
                    LastName = row.Field<string>( "last_name" ),
                    SocialSecurityNumber = row.Field<string>( "social_security" ) ?? "",
                    StreetAddress = row.Field<string>( "street_address_1" ),
                    City = row.Field<string>( "city" ),
                    State = row.Field<string>( "state" ),
                    PostalCode = row.Field<string>( "postal_code" ),
                    ContractStartDateTime = new DateTime( runDate.Year, 1, 1 ),
                    ContractAmount = row.Field<decimal>( "total" ),
                    ContractExpiration = new DateTime( runDate.Year, 12, 31 ),
                    OngoingContract = false
                };

                records.Add( record );
            }

            return records;
        }

        /// <summary>
        /// Binds the preview grid.
        /// </summary>
        private void BindPreviewGrid()
        {
            var pics = GetServiceProviderRecords();
            foreach ( var pic in pics )
            {
                if ( pic.SocialSecurityNumber != null && pic.SocialSecurityNumber.Length > 4 )
                {
                    pic.SocialSecurityNumber = Regex.Replace( pic.SocialSecurityNumber.Left( pic.SocialSecurityNumber.Length - 4 ), @"\d", "*" ) + pic.SocialSecurityNumber.Right( 4 );
                }
            }

            gPreview.DataSource = pics;
            gPreview.DataBind();
        }

        #endregion

        #region Events

        /// <summary>
        /// Handles the Click event of the lbPreview control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbPreview_Click( object sender, EventArgs e )
        {
            BindPreviewGrid();
            pnlPreview.Visible = true;
            pnlSetup.Visible = false;
        }

        /// <summary>
        /// Handles the GridRebind event of the gPreview control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="Rock.Web.UI.Controls.GridRebindEventArgs"/> instance containing the event data.</param>
        protected void gPreview_GridRebind( object sender, Rock.Web.UI.Controls.GridRebindEventArgs e )
        {
            BindPreviewGrid();
        }

        /// <summary>
        /// Handles the Click event of the lbDownload control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbDownload_Click( object sender, EventArgs e )
        {
            var records = new List<object>
            {
                GetServiceRecipientRecord()
            };

            var pics = GetServiceProviderRecords();
            records.AddRange( pics );

            records.Add( new TotalRecord
            {
                RecordCount = pics.Count
            } );

            SystemSettings.SetValue( "com.shepherdchurch.CaliforniaIndependentContractorReport.LastRunDate", dpRunDate.SelectedDate.Value.ToString() );

            Response.Clear();
            Response.ContentType = "text/plain";
            Response.Headers.Add( "Content-Disposition", "attachment; filename=\"INDCONTR\"" );

            foreach ( var record in records )
            {
                Response.Write( record.ToString() + "\r\n" );
            }
            Response.End();
        }

        /// <summary>
        /// Handles the Click event of the lbCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void lbCancel_Click( object sender, EventArgs e )
        {
            pnlPreview.Visible = false;
            pnlSetup.Visible = true;
        }

        #endregion

        #region Support Classes

        /// <summary>
        /// Identifies a business or government entity whose independent contractor information
        /// is being reported.
        /// </summary>
        public class ServiceRecipientRecord
        {
            public string FederalEmployerIdentificationNumber { get; set; }

            public string EmployerAccountNumber { get; set; }

            public string SocialSecurityNumber { get; set; }

            public string Name { get; set; }

            public string StreetAddress { get; set; }

            public string City { get; set; }

            public string State { get; set; }

            public string PostalCode { get; set; }

            public string PhoneNumber { get; set; }

            public ServiceRecipientRecord()
            {
                FederalEmployerIdentificationNumber = string.Empty;
                EmployerAccountNumber = string.Empty;
                SocialSecurityNumber = string.Empty;
                Name = string.Empty;
                StreetAddress = string.Empty;
                City = string.Empty;
                State = string.Empty;
                PostalCode = string.Empty;
                PhoneNumber = string.Empty;
            }

            public override string ToString()
            {
                var postalCode = PostalCode.Replace( "-", "" );

                if ( postalCode.Length != 5 && postalCode.Length != 9 )
                {
                    throw new ArgumentOutOfRangeException( "PostalCode", "Must be either 5 or 9 digits in length." );
                }

                var phoneNumber = Regex.Replace( PhoneNumber, "[^\\d]", string.Empty );
                if ( phoneNumber.Length == 11 )
                {
                    phoneNumber = phoneNumber.Substring( 1 );
                }
                else if ( phoneNumber.Length != 10 )
                {
                    throw new ArgumentOutOfRangeException( "PhoneNumber", "Must be 10 numerical digits in length." );
                }

                return "RIC" +
                    FederalEmployerIdentificationNumber.Replace( "-", "" ).Left( 9 ).PadRight( 9 ) +
                    EmployerAccountNumber.Replace( "-", "" ).Left( 8 ).PadRight( 8 ) +
                    SocialSecurityNumber.Replace( "-", "" ).Left( 9 ).PadRight( 9 ) +
                    Name.ToUpper().Left( 45 ).PadRight( 45 ) +
                    StreetAddress.ToUpper().Left( 40 ).PadRight( 40 ) +
                    City.ToUpper().Left( 25 ).PadRight( 25 ) +
                    State.ToUpper().Left( 2 ).PadRight( 2 ) +
                    postalCode.Substring( 0, 5 ) +
                    postalCode.Substring( 5 ).PadRight( 4 ) +
                    phoneNumber +
                    "".PadRight( 15 );
            }
        }

        /// <summary>
        /// Used to report the independent contract data. A separate ServiceProviderRecord must
        /// be generated for each independent contractor to be reported.
        /// </summary>
        public class ServiceProviderRecord
        {
            public string SocialSecurityNumber { get; set; }

            public string FirstName { get; set; }

            public string MiddleInitial { get; set; }

            public string LastName { get; set; }

            public string StreetAddress { get; set; }

            public string City { get; set; }

            public string State { get; set; }

            public string PostalCode { get; set; }

            public DateTime ContractStartDateTime { get; set; }

            public decimal ContractAmount { get; set; }

            public DateTime? ContractExpiration { get; set; }

            public bool OngoingContract { get; set; }

            public ServiceProviderRecord()
            {
                SocialSecurityNumber = string.Empty;
                FirstName = string.Empty;
                MiddleInitial = string.Empty;
                LastName = string.Empty;
                StreetAddress = string.Empty;
                City = string.Empty;
                State = string.Empty;
                PostalCode = string.Empty;
                ContractStartDateTime = DateTime.Now.Date;
            }

            public override string ToString()
            {
                var postalCode = PostalCode.Replace( "-", "" );

                if ( postalCode.Length != 5 && postalCode.Length != 9 )
                {
                    throw new ArgumentOutOfRangeException( "PostalCode", "Must be either 5 or 9 digits in length." );
                }

                return "PIC" +
                    SocialSecurityNumber.Replace( "-", "" ).Left( 9 ).PadRight( 9 ) +
                    FirstName.ToUpper().Left( 16 ).PadRight( 16 ) +
                    MiddleInitial.ToUpper().Left( 1 ).PadRight( 1 ) +
                    LastName.ToUpper().Left( 30 ).PadRight( 30 ) +
                    StreetAddress.ToUpper().Left( 40 ).PadRight( 40 ) +
                    City.ToUpper().Left( 25 ).PadRight( 25 ) +
                    State.ToUpper().Left( 2 ).PadRight( 2 ) +
                    postalCode.Substring( 0, 5 ) +
                    postalCode.Substring( 5 ).PadRight( 4 ) +
                    ContractStartDateTime.ToString( "yyyyMMdd" ) +
                    ContractAmount.ToString( "F2" ).Replace( ".", "" ).PadLeft( 11, '0' ) +
                    ( ContractExpiration.HasValue ? ContractExpiration.Value.ToString( "yyyyMMdd" ) : "" ).PadRight( 8 ) +
                    ( OngoingContract ? "Y" : " " ) +
                    "".PadRight( 12 );
            }
        }

        /// <summary>
        /// Contains the total number of ServiceProviderRecords reported since the last
        /// ServiceRecipientRecord. A TotalRecord must be generated fo reach ServiceRecipientRecord.
        /// </summary>
        public class TotalRecord
        {
            public int RecordCount { get; set; }

            public override string ToString()
            {
                return "TIC" +
                    RecordCount.ToString().PadLeft( 11, '0' ) +
                    "".PadRight( 161 );
            }
        }

        #endregion
    }
}