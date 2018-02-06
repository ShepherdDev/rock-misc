using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;

using Rock.Web.UI.Controls;

namespace com.shepherdchurch.Misc.UI.Controls
{
    public class MaskedEmailBox : EmailBox
    {
        private RegularExpressionValidator _maskedRegexValidator;

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

                if ( text.Contains( "*" ) )
                {
                    return ( string ) ViewState["OriginalTextValue"];
                }

                return text;
            }
            set
            {
                if ( value != null && !value.Contains( "*" ) )
                {
                    ViewState["OriginalTextValue"] = value;
                }

                string maskedValue = value;
                if ( !string.IsNullOrWhiteSpace( maskedValue ) )
                {
                    var parts = maskedValue.Split( '@' );
                    if ( parts.Length == 2 )
                    {
                        var user = parts[0].Substring( 0, parts[0].Length > 2 ? 2 : parts[0].Length ) + "****";
                        var domain = "****" + parts[1].Substring( parts[1].Length > 6 ? parts[1].Length - 6 : 0 );

                        maskedValue = user + "@" + domain;
                    }
                }

                base.Text = maskedValue;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is valid.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is valid; otherwise, <c>false</c>.
        /// </value>
        public override bool IsValid
        {
            get
            {
                EnsureChildControls();
                return base.IsValid && _maskedRegexValidator.IsValid;
            }
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();
            Controls.RemoveAt( Controls.Count - 1 ); /* Remove old regex validator */

            _maskedRegexValidator = new RegularExpressionValidator();
            _maskedRegexValidator.ID = this.ID + "_MRE";
            _maskedRegexValidator.ControlToValidate = this.ID;
            _maskedRegexValidator.Display = ValidatorDisplay.Dynamic;
            _maskedRegexValidator.CssClass = "validation-error help-inline";
            _maskedRegexValidator.ValidationExpression = @"(\w+([-+.]\w+)*|\w{1,2}\*{4})@(\*{4}\w+|\w+)([-.]\w+)*\.\w+([-.]\w+)*";
            _maskedRegexValidator.ErrorMessage = "The email address provided is not valid";
            Controls.Add( _maskedRegexValidator );
        }

        /// <summary>
        /// Gets or sets the group of controls for which the <see cref="T:System.Web.UI.WebControls.TextBox" /> control causes validation when it posts back to the server.
        /// </summary>
        /// <returns>The group of controls for which the <see cref="T:System.Web.UI.WebControls.TextBox" /> control causes validation when it posts back to the server. The default value is an empty string ("").</returns>
        public override string ValidationGroup
        {
            get
            {
                return base.ValidationGroup;
            }
            set
            {
                base.ValidationGroup = value;

                EnsureChildControls();
                _maskedRegexValidator.ValidationGroup = value;
            }
        }

        /// <summary>
        /// Renders any data validator.
        /// </summary>
        /// <param name="writer">The writer.</param>
        protected override void RenderDataValidator( HtmlTextWriter writer )
        {
            var ptr = typeof( RockTextBox ).GetMethod( "RenderDataValidator", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance ).MethodHandle.GetFunctionPointer();
            var renderDataValidator = ( Action<HtmlTextWriter> ) Activator.CreateInstance( typeof( Action<HtmlTextWriter> ), this, ptr );
            renderDataValidator( writer );

            _maskedRegexValidator.RenderControl( writer );
        }
    }
}
