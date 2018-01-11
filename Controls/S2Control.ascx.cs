using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "S2 Control" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "If the device currently viewing this page is not a valid Kiosk then redirect them to the specified page." )]

    [EncryptedTextField( "Username", "The S2 username to use when authenticating.", true, order: 0 )]
    [EncryptedTextField( "Password", "The S2 password to use when authenticating.", true, isPassword: true, order: 1 )]
    [TextField( "Address", "The IP Address or hostname to use when connecting to S2.", true, order: 2 )]
    [TextField( "Locations", "A list of named locations to provide buttons for, each name should be separated by a | character.", order: 3 )]
    [IntegerField( "Lock Threat Level", "The ID of the threat level to use when locking.", true, 1, order: 4 )]
    [IntegerField( "Unlock Threat Level", "The ID of the threat level to use when unlocking.", true, 1, order: 5 )]
    public partial class S2Control : RockBlock
    {
        static private object _lockObject = new Object();

        protected CookieContainer CookieContainer = null;
        protected string Csrft = null;

        protected void Page_Load( object sender, EventArgs e )
        {
            if ( !IsPostBack )
            {
                if ( PageParameter( "status" ).AsBoolean() )
                {
                    Response.Clear();
                    Response.ContentType = "application/json";
                    Response.Write( "{\"message\": \"Hello World.\"}" );
                    Response.Flush();

                    Response.SuppressContent = true;
                    Context.ApplicationInstance.CompleteRequest();

                    return;
                }
                else if ( !string.IsNullOrWhiteSpace( PageParameter( "command" ) ) )
                {
                    Response.Clear();
                    Response.ContentType = "application/json";
                    Response.Write( JsonConvert.SerializeObject( ProcessCommand( PageParameter( "command" ) ) ) );
                    Response.Flush();

                    Response.SuppressContent = true;
                    Context.ApplicationInstance.CompleteRequest();

                    return;
                }

                hfLocations.Value = GetAttributeValue( "Locations" );
            }
        }

        #region Methods

        /// <summary>
        /// Get the username in plaintext form.
        /// </summary>
        /// <returns>The username in plaintext form.</returns>
        protected string GetUsername()
        {
            return Rock.Security.Encryption.DecryptString( GetAttributeValue( "Username" ) );
        }

        /// <summary>
        /// Get the password in plaintext form.
        /// </summary>
        /// <returns>The password in plaintext form.</returns>
        protected string GetPassword()
        {
            return Rock.Security.Encryption.DecryptString( GetAttributeValue( "Password" ) );
        }

        /// <summary>
        /// Login to the S2 system with the configured username and password.
        /// </summary>
        protected void Login()
        {
            string url = string.Format( "http://{0}/login/?format=json&username={1}&password={2}",
                GetAttributeValue( "Address" ), GetUsername(), GetPassword() );

            WebHeaderCollection headers;
            var webRequest = ( HttpWebRequest ) WebRequest.Create( url );
            webRequest.CookieContainer = new CookieContainer();
            webRequest.Timeout = 5000;

            ReadWebResponse<string>( webRequest, out headers );

            //
            // Replace the cookie container in cache.
            //
            CookieContainer = webRequest.CookieContainer;

            //
            // Request the standard home page so we can get the CSRFT variable.
            //
            var content = GetRequest<string>( string.Format( "http://{0}/frameset/", GetAttributeValue( "Address" ) ) );
            var match = Regex.Match( content, "csrft\\s*=\\s*\"([^\"]+)\"", RegexOptions.Multiline );
            if ( match != null && match.Success )
            {
                Csrft = match.Groups[1].Value;
                return;
            }

            throw new Exception( "Authentication failed to S2." );
        }

        #endregion

        #region Network Request Methods

        /// <summary>
        /// Performs a simple GET of the url and decodes the data into the desired type.
        /// </summary>
        /// <typeparam name="T">The type of data expected to be returned.</typeparam>
        /// <param name="url">The url to request.</param>
        /// <returns>An instance of T.</returns>
        protected T GetRequest<T>( string url )
        {
            var webRequest = ( HttpWebRequest ) WebRequest.Create( url );
            webRequest.Timeout = 5000;

            webRequest.CookieContainer = CookieContainer;

            try
            {
                WebHeaderCollection headers;
                return ReadWebResponse<T>( webRequest, out headers );
            }
            catch ( Exception )
            {
                return default( T );
            }
        }

        /// <summary>
        /// Performs a simple Post of the url and decodes the data into the desired type.
        /// </summary>
        /// <typeparam name="T">The type of data expected to be returned.</typeparam>
        /// <param name="url">The url to request.</param>
        /// <returns>An instance of T.</returns>
        protected T PostRequest<T>( string url, string data )
        {
            var webRequest = ( HttpWebRequest ) WebRequest.Create( url );
            webRequest.Timeout = 5000;
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";

            webRequest.CookieContainer = CookieContainer;

            //
            // Write the POST data.
            //
            var d = System.Text.Encoding.UTF8.GetBytes( data );
            using ( var stream = webRequest.GetRequestStream() )
            {
                stream.Write( d, 0, d.Length );
                stream.Close();
            }

            try
            {
                WebHeaderCollection headers;
                return ReadWebResponse<T>( webRequest, out headers );
            }
            catch ( Exception )
            {
                return default( T );
            }
        }

        /// <summary>
        /// Safely reads the JSON formatted response from the web request.
        /// </summary>
        /// <typeparam name="T">The data type to be decoded from the response.</typeparam>
        /// <param name="request">The request object that is ready to have it's respons read.</param>
        /// <returns>The object that was read from the response stream.</returns>
        protected T ReadWebResponse<T>( WebRequest request, out WebHeaderCollection headers )
        {
            using ( var response = ( HttpWebResponse ) request.GetResponse() )
            {
                headers = response.Headers;

                if ( typeof( T ) == typeof( byte[] ) )
                {
                    return ( T ) ( object ) response.GetResponseStream().ReadBytesToEnd();
                }
                else if ( typeof( T ) == typeof( string ) )
                {
                    using ( var reader = new StreamReader( response.GetResponseStream() ) )
                    {
                        return ( T ) ( object ) reader.ReadToEnd();
                    }
                }

                using ( var reader = new StreamReader( response.GetResponseStream() ) )
                {
                    return JsonConvert.DeserializeObject<T>( reader.ReadToEnd() );
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// Process the specified command by calling sub-handlers.
        /// </summary>
        /// <param name="command">The command issued by the client.</param>
        /// <returns>The result from the specific command sub-handler.</returns>
        protected object ProcessCommand( string command )
        {
            lock ( _lockObject )
            {
                if ( command == "status" )
                {
                    return ProcessStatusCommand();
                }
                else if ( command == "lock" )
                {
                    return ProcessLockCommand();
                }
                else if ( command == "unlock" )
                {
                    return ProcessUnlockCommand();
                }

                return new Dictionary<string, object>
                {
                    { "error", "Unknown command" }
                };
            }
        }

        /// <summary>
        /// Process the status command.
        /// </summary>
        /// <returns>An object that identifies the status response.</returns>
        protected ActivityResponse ProcessStatusCommand()
        {
            string url = string.Format( "http://{0}/activity/query?query=threatLevel", GetAttributeValue( "Address" ) );

            var response = GetRequest<ActivityResponse>( url );
            if ( response == null )
            {
                Login();

                response = GetRequest<ActivityResponse>( url );
            }

            return response;
        }

        /// <summary>
        /// Process the lock command.
        /// </summary>
        /// <returns>True if the command was successful, false otherwise.</returns>
        protected bool ProcessLockCommand()
        {
            Login();

            string url = string.Format( "http://{0}/threatLevel/change/insert/", GetAttributeValue( "Address" ) );
            string post = string.Format( "csrft={0}&json={{\"password\":\"{1}\",\"threatLevel\":{2},\"location\":{3},\"applytosublocations\":false}}",
                Csrft,
                GetPassword(),
                GetAttributeValue( "LockThreatLevel" ),
                Request.QueryString["id"] );

            var response = PostRequest<SimpleResult>( url, post );

            return response.result == "success";
        }

        /// <summary>
        /// Process the unlock command.
        /// </summary>
        /// <returns>True if the command was successful, false otherwise.</returns>
        protected object ProcessUnlockCommand()
        {
            Login();

            string url = string.Format( "http://{0}/threatLevel/change/insert/", GetAttributeValue( "Address" ) );
            string post = string.Format( "csrft={0}&json={{\"password\":\"{1}\",\"threatLevel\":{2},\"location\":{3},\"applytosublocations\":false}}",
                Csrft,
                GetPassword(),
                GetAttributeValue( "UnlockThreatLevel" ),
                Request.QueryString["id"] );

            var response = PostRequest<SimpleResult>( url, post );

            return response.result == "success";
        }

        #endregion
    }

    #region REST Classes

    public class SimpleResult
    {
        public string message { get; set; }

        public string result { get; set; }
    }

    public class ThreatLocation
    {
        public int id { get; set; }

        public string name { get; set; }

        public int? parentid { get; set; }

        public ThreatLevel threatlevel { get; set; }

        public int partitionid { get; set; }
    }

    public class ThreatLevel
    {
        public int id { get; set; }

        public string name { get; set; }

        public string image { get; set; }
    }

    public class ActivityResponse
    {
        public Dictionary<string, List<ThreatLocation>> locations { get; set; }
    }

    #endregion
}
