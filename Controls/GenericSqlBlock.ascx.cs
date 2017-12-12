using System.ComponentModel;

using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Reporting.Dashboard;
using Rock.Web.Cache;
using Rock.Web.UI;
using Rock.Web.UI.Controls;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    /// <summary>
    /// Provide a Lava Template that can use AJAX to query for the results from a SQL statement.
    /// </summary>
    [DisplayName( "Generic SQL Block" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Display results from a SQL statement." )]
    [CodeEditorField( "Sql", "The Sql code to run to generate the data for this block.", CodeEditorMode.Sql, defaultValue: "SELECT 'Hello World' AS [Value]", order: 0 )]
    [CodeEditorField( "Lava Template", "The html and Javascript to display as the contents of the block.", CodeEditorMode.Lava, CodeEditorTheme.Rock, 400, Order = 1, DefaultValue =
@"
" )]

    public partial class GenericSqlBlock : RockBlock
    {
        /// <summary>
        /// Gets the rest URL.
        /// </summary>
        /// <value>
        /// The rest URL.
        /// </value>
        public string RestUrl
        {
            get
            {
                string result = ResolveUrl( "~/api/SC_Misc_Blocks/GetJavascriptForSqlBlock/" ) + this.BlockCache.Guid.ToString();

                return result;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Load" /> event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnLoad( System.EventArgs e )
        {
            base.OnLoad( e );

            if ( !IsPostBack )
            {
                var lava = GetAttributeValue( "LavaTemplate" );
                var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( RockPage );
                mergeFields.Add( "RestUrl", RestUrl );

                ltContent.Text = lava.ResolveMergeFields( mergeFields );
            }
        }
    }
}
