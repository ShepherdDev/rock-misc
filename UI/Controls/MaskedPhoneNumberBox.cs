using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI;

using Rock.Web.UI.Controls;

namespace com.shepherdchurch.Misc.UI.Controls
{
    public class MaskedPhoneNumberBox : PhoneNumberBox
    {
        protected override void OnPreRender( EventArgs e )
        {
            base.OnPreRender( e );

            this.CssClass += " masked-phone-number-box";
            var script = string.Format( @"
    $('#{0}').off('change');
    $('#{0}').on('change', function(e) {{
        if ($(this).val().indexOf('#') === -1) {{
            phoneNumberBoxFormatNumber($(this));
        }}
    }});
", this.ClientID );
            ScriptManager.RegisterStartupScript( this, this.GetType(), string.Format( "masked-phone-number-box-{0}", this.ClientID ), script, true );
        }

        public override string Text
        {
            get
            {
                var trace = new System.Diagnostics.StackTrace( false );
                bool isRendering = trace.GetFrames().Any( f => f.GetMethod().Name == "Render" && f.GetMethod().DeclaringType.Name == "TextBox" );
                var text = base.Text;

                if ( isRendering )
                {
                    return text;
                }

                if ( text.Contains( "#" ) )
                {
                    return ( string ) ViewState["OriginalTextValue"];
                }

                return text;
            }
            set
            {
                if ( value != null && !value.Contains( "#" ) )
                {
                    ViewState["OriginalTextValue"] = value;
                }

                string maskedValue = value;
                if ( maskedValue != null && maskedValue.Length > 2 )
                {
                    maskedValue = Regex.Replace( value.Substring( 0, value.Length - 2 ), "\\d", "#" );
                    maskedValue += value.Substring( value.Length - 2 );
                }

                base.Text = maskedValue;
            }
        }
    }
}
