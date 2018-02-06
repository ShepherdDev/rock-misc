using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Rock.Web.UI.Controls;

namespace com.shepherdchurch.Misc.UI.Controls
{
    public class MaskedAddressControl : AddressControl
    {
        public string UnmaskedStreet1
        {
            get
            {
                var text = base.Street1;

                if ( text.Contains( "****" ) )
                {
                    return ( string ) ViewState["OriginalStreet1Value"];
                }

                return text;
            }
            set
            {
                if ( value != null && !value.Contains( "****" ) )
                {
                    ViewState["OriginalStreet1Value"] = value;
                }

                string maskedValue = value;
                if ( !string.IsNullOrWhiteSpace( maskedValue ) )
                {
                    var parts = maskedValue.Split( new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries );

                    for ( int i = 0; i < parts.Length; i++ )
                    {
                        if ( i >= 2 )
                        {
                            parts[i] = "****";
                            continue;
                        }

                        parts[i] = parts[i].Substring( 0, parts[i].Length > 2 ? 2 : parts[i].Length ) + "****";
                    }

                    maskedValue = string.Join( " ", parts );
                }

                base.Street1 = maskedValue;
            }
        }

        public void GetUnmaskedValues( Rock.Model.Location location )
        {
            base.GetValues( location );

            if ( location != null && !string.IsNullOrWhiteSpace( this.UnmaskedStreet1 ) )
            {
                location.Street1 = this.UnmaskedStreet1;
            }
        }

        public void SetUnmaskedValues( Rock.Model.Location location )
        {
            base.SetValues( location );

            if ( location != null )
            {
                UnmaskedStreet1 = location.Street1;
            }
        }
    }
}
