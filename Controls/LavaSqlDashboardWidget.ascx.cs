using System.ComponentModel;
using Rock;
using Rock.Attribute;
using Rock.Model;
using Rock.Reporting.Dashboard;
using Rock.Web.Cache;
using Rock.Web.UI.Controls;

namespace Plugins.com_shepherdchurch.Misc
{
    /// <summary>
    /// NOTE: Most of the logic for processing the Attributes is in com.ShepherdChurch.Misc.Rest.Blocks.GetHtmlForSqlBlock
    /// </summary>
    /// <seealso cref="Rock.Reporting.Dashboard.DashboardWidget" />
    [DisplayName( "Lava Sql Dashboard Widget" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Dashboard Widget from Lava using Sql query" )]
    [CodeEditorField( "Sql", "The Sql code to run to generate the data for this block.", CodeEditorMode.Sql, defaultValue:"SELECT 'Hello World' AS [Value]", order: 11)]
    [CodeEditorField( "Lava Template", "The text (or html) to display as a dashboard widget", CodeEditorMode.Lava, CodeEditorTheme.Rock, 200, Order = 12, DefaultValue =
@"
{% for row in Rows %}
    <h1>{{ row.Value }}</h1>
{% endfor %}
" )]

    public partial class LavaSqlDashboardWidget : DashboardWidget
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
                string result = ResolveUrl( "~/api/SC_Misc_Blocks/GetHtmlForSqlBlock/" ) + this.BlockCache.Guid.ToString();

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
            pnlDashboardTitle.Visible = !string.IsNullOrEmpty( this.Title );
            pnlDashboardSubtitle.Visible = !string.IsNullOrEmpty( this.Subtitle );
            lDashboardTitle.Text = this.Title;
            lDashboardSubtitle.Text = this.Subtitle;
        }
    }
}
