using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.UI.WebControls;

using Rock;
using Rock.Attribute;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Cache And Redirect" )]
    [Category( "com_shepherdchurch > Misc" )]
    [Description( "" )]

    [LinkedPage( "Setup Page", "The page to redirect the user to if there is no cached value to work with. If no page is defined then the Parent page will be used.", false, "", "", 0 )]
    [LinkedPage( "Content Page", "The page to redirect the user to if there is a valid keyed value from either the query string or the cache.", true, "", "", 1 )]
    [TextField( "URL Key", "The key that must exist in either the query string or the cache. This key will be pased to the Content Page in the query string.", true, "", "", 2 )]
    [IntegerField( "Cache Time", "Duration in minutes that the cache key is good for. This is a rolling time period, each time the block is visited the expire time is updated. Enter 0 for it to never be expired.", true, 60, "", 3 )]
    [BooleanField( "Session Unique", "If Yes, then the value is stored unique to this user's session, otherwise it is global across all users.", true, "", 4 )]
    [TextField( "Block Key", "If you wish to tie the cache of multiple blocks together then enter the same cache key on each block. A blank value will make the cache unique to this block.", false, "", "", 5 )]
    public partial class CacheAndRedirect : RockBlock
    {
        static System.Runtime.Caching.MemoryCache _cache = null;

        #region Base Method Overrides

        static CacheAndRedirect()
        {
            _cache = new System.Runtime.Caching.MemoryCache( "com_shepherdchurch_CacheAndRedirect" );
        }

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
            if (!IsPostBack)
            {
                if ( string.IsNullOrEmpty( GetAttributeValue( "ContentPage" ) ) || string.IsNullOrEmpty( GetAttributeValue( "URLKey" ) ) )
                {
                    nbWarning.Text = "Block has not been configured.";
                    return;
                }

                if ( !string.IsNullOrEmpty( PageParameter( "clearCache" ) ) )
                {
                    ClearCache( PageParameter( "clearCache" ) );
                }
                else
                {
                    CheckCacheAndRedirect();
                }
            }
        }

        #endregion

        #region Core Methods

        void CheckCacheAndRedirect()
        {
            PageReference target;
            int cacheTime = GetAttributeValue( "CacheTime" ).AsInteger();

            //
            // If they specify "forever", then cache for 1 year. If it's a leap year then you are out of luck.
            //
            if ( cacheTime <= 0 )
            {
                cacheTime = 365 * 24 * 60;
            }

            target = new PageReference( GetAttributeValue( "ContentPage" ), new Dictionary<string, string>() );

            foreach ( var urlKey in GetAttributeValues( "URLKey" ) )
            {
                bool hasValueInUrl = CurrentPageReference.QueryString.AllKeys.Select( s => s.ToLower() ).Contains( urlKey.ToLower() );
                string key = CacheKey( urlKey );
                string value = null;

                //
                // Try to get the value from the URL and then from the cache.
                //
                if ( hasValueInUrl )
                {
                    value = PageParameter( urlKey );
                }
                else
                {
                    value = ( string ) _cache[key];
                }

                if ( value == null )
                {
                    //
                    // No value. They need to be sent to the Setup Page or the parent page.
                    //
                    if ( !string.IsNullOrEmpty( GetAttributeValue( "SetupPage" ) ) )
                    {
                        target = new PageReference( GetAttributeValue( "SetupPage" ) );
                    }
                    else
                    {
                        var pageCache = PageCache.Get( RockPage.PageId );
                        if ( pageCache != null && pageCache.ParentPage != null )
                        {
                            target = new PageReference( pageCache.ParentPage.Guid.ToString() );
                        }
                        else
                        {
                            nbWarning.Text = "No Setup Page was defined and no parent page could be found.";
                            return;
                        }
                    }

                    break;
                }

                //
                // We have a value, save it to the cache.
                //
                _cache.Set( key, value, DateTimeOffset.Now.AddMinutes( cacheTime ) );
                target.Parameters.AddOrReplace( urlKey, value );
            }

            Redirect( target.BuildUrl() );
        }

        string BlockCacheKey( string parameter )
        {
            string key = "com_shepherdchurch_cacheandredirect_" + parameter;

            if ( !string.IsNullOrWhiteSpace( GetAttributeValue( "BlockKey" ) ) )
            {
                key += "_" + GetAttributeValue( "BlockKey" );
            }
            else
            {
                key += "_" + BlockId.ToString();
            }

            return key;
        }

        string CacheKey( string parameter )
        {
            string key = BlockCacheKey( parameter );

            if ( GetAttributeValue( "SessionUnique" ).AsBoolean() == true )
            {
                key += "_" + Session.SessionID.ToString();
            }

            return key;
        }

        void ClearCache( string cacheType )
        {
            if ( cacheType == "me" )
            {
                foreach ( var key in GetAttributeValues( "URLKey" ) )
                {
                    _cache.Remove( CacheKey( key ) );
                }

                nbWarning.Text = string.Format(
                    "Your cache key has been cleared. Click <a href=\"{0}\">here</a> to reload the page.",
                    new PageReference( RockPage.PageId ).BuildUrl() );
            }
            else if ( cacheType == "block" )
            {
                var keys = new List<string>();

                foreach ( var key in GetAttributeValues( "URLKey" ) )
                {
                    string cacheKey = BlockCacheKey( key );
                    keys.AddRange( _cache.Where( kvp => kvp.Key.StartsWith( cacheKey ) ).Select( kvp => kvp.Key ).ToList() );

                }

                foreach ( var k in keys )
                {
                    _cache.Remove( k );
                }

                nbWarning.Text = string.Format(
                    "{1} cache keys have been cleared. Click <a href=\"{0}\">here</a> to reload the page.",
                    new PageReference( RockPage.PageId ).BuildUrl(),
                    keys.Count );
            }
            else
            {
                nbWarning.Text = "Cannot clear cache, unknown cache type.";
            }
        }

        void Redirect( string url )
        {
            if ( UserCanAdministrate )
            {
                PageReference self = new PageReference( RockPage.PageId );

                nbWarning.Text = string.Format(
                    "If you were not an Administrator you would have been redirected to <a href=\"{0}\">{0}</a><br />Click <a href=\"{1}?clearCache=me\">here</a> to clear your cache key or <a href=\"{1}?clearCache=block\">here</a> to clear all cache keys for this block.",
                    url, self.BuildUrl() );
            }
            else
            {
                Response.Redirect( url );
                Response.End();
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
        }

        #endregion
    }
}