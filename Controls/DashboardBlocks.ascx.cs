using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;

using Newtonsoft.Json;

using Rock;
using Rock.Attribute;
using Rock.Security;
using Rock.Web;
using Rock.Web.Cache;
using Rock.Web.UI;

namespace RockWeb.Plugins.com_shepherdchurch.Misc
{
    [DisplayName( "Dashboard Blocks" )]
    [Category( "Shepherd Church > Misc" )]
    [Description( "Display blocks on the page while allowing the user to show/hide and reorganize the blocks at will." )]

    [LinkedPage( "Source Page", "The page that contains the blocks to display to the user.", true, "", order: 0 )]
    [TextField( "Title", "The title to display in the dashboard container.", true, "Dashboard", order: 1 )]
    [TextField( "Icon CSS Class", "The CSS class to use for an icon, such as 'fa fa-tasks'.", false, "", order: 2 )]

    [TextField( "Available Blocks", "The blocks that have been enabled.", false, "", "CustomSetting" )]
    [TextField( "Default Layout", "The default layout that will be used by new users.", false, DashboardBlocks.THREE_COLUMN, "CustomSetting" )]
    public partial class DashboardBlocks : RockBlockCustomSettings
    {
        private const string TWO_COLUMN = "2-col";
        private const string THREE_COLUMN = "3-col";
        private const string FOUR_COLUMN = "4-col";
        private const string ONE_TWO_COLUMN = "1-2-col";
        private const string TWO_ONE_COLUMN = "2-1-col";

        private List<string> OptionsLayouts
        {
            get
            {
                return ViewState["OptionsLayouts"] as List<string>;
            }
            set
            {
                ViewState["OptionsLayouts"] = value;
            }
        }
        private bool OptionsCanDeleteLayout;

        /// <summary>
        /// The list of available blocks as they are being edited in the settings modal.
        /// </summary>
        private List<DashboardBlockType> AvailableBlocksLive
        {
            get
            {
                return JsonConvert.DeserializeObject<List<DashboardBlockType>>( ( string )ViewState["AvailableBlocksLive"] );
            }
            set
            {
                ViewState["AvailableBlocksLive"] = value != null ? JsonConvert.SerializeObject( value ) : null;
            }
        }

        #region Base Method Overrides

        /// <summary>
        /// Raises the <see cref="E:System.Web.UI.Control.Init" /> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
        protected override void OnInit( EventArgs e )
        {
            base.OnInit( e );

            RockPage.AddCSSLink( ResolveRockUrl( "~/Plugins/com_shepherdchurch/Misc/Styles/DashboardBlocks.css" ) );

            // this event gets fired after block settings are updated. it's nice to repaint the screen if these settings would alter it
            this.BlockUpdated += Block_BlockUpdated;
            this.AddConfigurationUpdateTrigger( upnlContent );

            //
            // Setup any parts of the UI that need to be configured at page initialization.
            //
            SetupLayoutButtonList( rblSettingsDefaultLayout );
            gSettingsBlocks.ShowActionRow = false;
            gSettingsBlocks.AllowPaging = false;

            //
            // We don't need to do further processing if this is an AJAX request. When the Lava API is added
            // to the core Arena system this would be good to switch to that for better performance.
            //
            if ( !string.IsNullOrWhiteSpace( Request.QueryString["expand"] ) || !string.IsNullOrWhiteSpace( Request.QueryString["remove"] ) )
            {
                return;
            }

            BuildBlocks( GetConfig() );

            RegisterScripts();
        }

        /// <summary>
        /// Initialize basic information about the page structure and setup the default content.
        /// </summary>
        /// <param name="sender">Object that is generating this event.</param>
        /// <param name="e">Arguments that describe this event.</param>
        protected void Page_Load( object sender, EventArgs e )
        {
            if ( ProcessCustomEvents() )
            {
                return;
            }

            if ( !IsPostBack )
            {
                pnlDashboard.Visible = false;

                if ( string.IsNullOrEmpty( GetAttributeValue( "SourcePage" ) ) )
                {
                    nbWarning.Text = "Block has not been configured.";
                    return;
                }

                if ( GetAttributeValue( "SourcePage" ).AsGuid() == RockPage.Guid )
                {
                    nbWarning.Text = "<p>Error... Trying to display blocks from myself on myself... but... can't... procccccc...... Bzzzzt.</p><p>Unable to use self as Source Page.</p>";
                    return;
                }

                pnlDashboard.Visible = true;

                //
                // Check for an AJAX request to expand/collapse a widget.
                //
                if ( !string.IsNullOrWhiteSpace( Request.QueryString["expand"] ) )
                {
                    var cmd = Request.QueryString["expand"].Split( ',' );
                    var config = GetConfig();
                    var blockId = cmd[0].AsInteger();
                    var blockConfig = config.Blocks.Where( b => b.BlockId == blockId ).FirstOrDefault();

                    if ( blockConfig != null )
                    {
                        blockConfig.Expanded = cmd[1].AsBoolean();
                        SaveConfig( config );
                    }

                    return;
                }

                //
                // Check for an AJAX request to remove (hide) a widget.
                //
                if ( !string.IsNullOrWhiteSpace( Request.QueryString["remove"] ) )
                {
                    var config = GetConfig();
                    var blockId = Request.QueryString["remove"].AsInteger();
                    var blockConfig = config.Blocks.Where( b => b.BlockId == blockId ).FirstOrDefault();

                    if ( blockConfig != null )
                    {
                        blockConfig.Visible = false;
                        SaveConfig( config );
                    }

                    return;
                }
            }
        }

        #endregion

        #region Core Methods

        /// <summary>
        /// Process any custom postback events initiated by javascript code.
        /// </summary>
        /// <returns>True if processing should be stopped, false otherwise.</returns>
        private bool ProcessCustomEvents()
        {
            if ( Request.Form["__EVENTARGUMENT"] != null )
            {
                var action = Request.Form["__EVENTARGUMENT"].Split( ':' );
                if ( action.Length == 2 )
                {
                    //
                    // User has re-ordered the blocks on the dashboard.
                    //
                    if ( action[0] == "re-order" )
                    {
                        var cmds = action[1].Split( ';' );
                        var config = GetConfig();

                        foreach ( var cmd in cmds )
                        {
                            var values = cmd.Split( '_' );
                            var row = values[0].AsInteger();
                            var column = values[1].AsInteger();
                            var bids = values[2].SplitDelimitedValues();
                            int order = 0;

                            foreach ( var bid in bids )
                            {
                                var blockConfig = config.Blocks.Where( b => b.BlockId == bid.AsInteger() ).FirstOrDefault();

                                if ( blockConfig != null )
                                {
                                    blockConfig.Row = row;
                                    blockConfig.Column = column;
                                    blockConfig.Order = order++;
                                }
                            }
                        }

                        SaveConfig( config );
                        NavigateToPage( CurrentPageReference );

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Setup all the Radio buttons for the layout controls. These are non-standard images so things
        /// get a little funky if we try to do it in the .ascx file.
        /// </summary>
        private void SetupLayoutButtonList(RadioButtonList rbl)
        {
            rbl.Items.Clear();
            rbl.Items.Add( new ListItem( "<div class='dashboard-layout'><div style='width: 50%'><div></div></div><div style='width: 50%'><div></div></div></div>", TWO_COLUMN ) );
            rbl.Items.Add( new ListItem( "<div class='dashboard-layout'><div style='width: 33%'><div></div></div><div style='width: 33%'><div></div></div><div style='width: 33%'><div></div></div></div>", THREE_COLUMN ) );
            rbl.Items.Add( new ListItem( "<div class='dashboard-layout'><div style='width: 25%'><div></div></div><div style='width: 25%'><div></div></div><div style='width: 25%'><div></div></div><div style='width: 25%'><div></div></div></div>", FOUR_COLUMN ) );
            rbl.Items.Add( new ListItem( "<div class='dashboard-layout'><div style='width: 50%'><div></div></div><div style='width: 25%'><div></div></div><div style='width: 25%'><div></div></div></div>", ONE_TWO_COLUMN ) );
            rbl.Items.Add( new ListItem( "<div class='dashboard-layout'><div style='width: 25%'><div></div></div><div style='width: 25%'><div></div></div><div style='width: 50%'><div></div></div></div>", TWO_ONE_COLUMN ) );
        }

        /// <summary>
        /// Registers all needed scripts into the page.
        /// </summary>
        private void RegisterScripts()
        {
            var script = string.Format( @"
    Sys.Application.add_load(function () {{
        $('#{0} .js-dashboard-column').each(function () {{
            var column = $(this).data('column');
            $(this).sortable( {{
                connectWith: ['#{0} .js-dashboard-column:not([data-column=' + column + '])'],
                handle: '.js-fa-bars',
                activate: function (event, ui) {{
                    $(this).addClass('dashboard-drop-zone');
                }},
                deactivate: function (event, ui) {{
                    $(this).removeClass('dashboard-drop-zone');
                }},
                beforeStop: function (event, ui) {{
                    var cmd = '';

                    $('#{0} .js-dashboard-column[data-bid!=""""]').each(function () {{
                        var subcmd = '';
                        $(this).children('.dashboard-block[data-bid!=""""][data-bid]').each(function () {{
                            subcmd = subcmd + ',' + $(this).data('bid');
                        }});
                        cmd = cmd + ';' + $(this).data('column') + '_' + subcmd.substring(1);
                    }});

                    __doPostBack('{1}', 're-order:' + cmd.substring(1));
                }}
            }});
        }});
    }});
", upnlContent.ClientID, upnlContent.UniqueID );

            ScriptManager.RegisterStartupScript( this, GetType(), string.Format( "dashboard-sort-{0}-script", ClientID ), script, true );

            script = string.Format( @"
    Sys.Application.add_load(function ()
    {{
        $('#{0} .js-dashboard-collapse').click(function ()
        {{
            var $panel = $(this).closest('.panel');
            $panel.children('.panel-body').slideToggle();

            $('i.dashboard-state', $panel).toggleClass('fa-chevron-down');
            $('i.dashboard-state', $panel).toggleClass('fa-chevron-up');

            var expanded = $('i.dashboard-state', $panel).hasClass('fa-chevron-up');

            $.get('{1}?expand=' + $panel.data('bid') + ',' + expanded, function (data) {{ }});
        }});

        $('#{0} .js-remove-block').click(function (event)
        {{
            var $panel = $(this).closest('.panel');
            var $block = $(this).closest('.dashboard-block');

            event.stopPropagation();
            return Rock.dialogs.confirm('Are you sure you want to remove this widget?', function (result)
            {{
                if (result)
                {{
                    $block.fadeOut();
                    $.get('{1}?remove=' + $panel.data('bid'), function (data) {{ }});
                }}
            }});
        }});
    }});
", upnlContent.ClientID, new PageReference( RockPage.PageId ).Route );

            ScriptManager.RegisterStartupScript( this, GetType(), string.Format( "dashboard-buttons-{0}-script", ClientID ), script, true );
        }

        /// <summary>
        /// Build the rows that will be available for use based on the user's preferences.
        /// </summary>
        /// <param name="config">The user configuration to use.</param>
        private List<DashboardRow> BuildRows( DashboardConfig config )
        {
            List<DashboardRow> rows = new List<DashboardRow>();
            int row = 0;

            foreach ( var layout in config.Layouts )
            {
                Panel pnlRow = new Panel { CssClass = "row" };

                phControls.Controls.Add( pnlRow );
                rows.Add( BuildColumns( layout, pnlRow, row++ ) );
            }

            return rows;
        }

        /// <summary>
        /// Build the columns that will be available for use based on the user's preferences.
        /// </summary>
        /// <param name="layout">The layout to use fo rthis row.</param>
        /// <param name="pnlRow">The panel control that will contain the columns.</param>
        private DashboardRow BuildColumns( string layout, Panel pnlRow, int row )
        {
            DashboardRow columns = new DashboardRow();
            DashboardColumn column;

            if ( layout == TWO_COLUMN )
            {
                for ( int i = 0; i < 2; i++ )
                {
                    column = new DashboardColumn( "col-md-6", string.Format( "{0}_{1}", row, i ) );
                    pnlRow.Controls.Add( column.Panel );
                    columns.Add( column );
                }
            }
            else if ( layout == FOUR_COLUMN )
            {
                for ( int i = 0; i < 4; i++ )
                {
                    column = new DashboardColumn( "col-md-3", string.Format( "{0}_{1}", row, i ) );
                    pnlRow.Controls.Add( column.Panel );
                    columns.Add( column );
                }
            }
            else if ( layout == ONE_TWO_COLUMN )
            {
                column = new DashboardColumn( "col-md-6", string.Format( "{0}_{1}", row, 0 ) );
                pnlRow.Controls.Add( column.Panel );
                columns.Add( column );

                for ( int i = 1; i < 3; i++ )
                {
                    column = new DashboardColumn( "col-md-3", string.Format( "{0}_{1}", row, i ) );
                    pnlRow.Controls.Add( column.Panel );
                    columns.Add( column );
                }
            }
            else if ( layout == TWO_ONE_COLUMN )
            {
                for ( int i = 0; i < 2; i++ )
                {
                    column = new DashboardColumn( "col-md-3", string.Format( "{0}_{1}", row, i ) );
                    pnlRow.Controls.Add( column.Panel );
                    columns.Add( column );
                }

                column = new DashboardColumn( "col-md-6", string.Format( "{0}_{1}", row, 2 ) );
                pnlRow.Controls.Add( column.Panel );
                columns.Add( column );
            }
            else /* THREE_COLUMN */
            {
                for ( int i = 0; i < 3; i++ )
                {
                    column = new DashboardColumn( "col-md-4", string.Format( "{0}_{1}", row, i ) );
                    pnlRow.Controls.Add( column.Panel );
                    columns.Add( column );
                }
            }

            return columns;
        }

        /// <summary>
        /// Build and layout all the blocks onto the web page.
        /// </summary>
        /// <param name="config">User configuration data that specifies how things should be laid out.</param>
        private void BuildBlocks( DashboardConfig config )
        {
            var sourcePageGuid = GetAttributeValue( "SourcePage" ).AsGuidOrNull();
            if ( sourcePageGuid.HasValue )
            {
                var sourcePage = PageCache.Read( sourcePageGuid.Value );

                if ( sourcePage != null )
                {
                    var blocks = GetAvailableBlocks();
                    var rows = BuildRows( config );
                    List<DashboardBlockWrapper> dashboardBlocks = new List<DashboardBlockWrapper>();
                    bool needSave = false;

                    foreach ( var block in blocks )
                    {
                        DashboardBlockConfig blockConfig = null;

                        blockConfig = config.Blocks.Where( b => b.BlockId == block.BlockId ).FirstOrDefault();
                        if ( blockConfig == null )
                        {
                            blockConfig = new DashboardBlockConfig( block.BlockId );
                            blockConfig.Visible = block.DefaultVisible;
                            config.Blocks.Add( blockConfig );
                            needSave = true;
                        }

                        if ( block.Required )
                        {
                            blockConfig.Visible = true;
                        }

                        if ( blockConfig.Visible )
                        {
                            var control = TemplateControl.LoadControl( block.BlockCache.BlockType.Path );
                            control.ClientIDMode = ClientIDMode.AutoID;

                            var blockControl = control as RockBlock;
                            bool canEdit = block.BlockCache.IsAuthorized( Authorization.EDIT, CurrentPerson );
                            bool canAdministrate = block.BlockCache.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson );

                            blockControl.SetBlock( PageCache, block.BlockCache, canEdit, canAdministrate );
                            block.BlockCache.BlockType.SetSecurityActions( blockControl );

                            var blockWrapper = new DashboardBlockWrapper( blockControl, block.BlockCache, blockConfig );
                            blockWrapper.ShowDelete = !block.Required;

                            dashboardBlocks.Add( blockWrapper );
                        }
                    }

                    //
                    // Add in all existing blocks that can be placed where they were last time.
                    //
                    List<int> existingBlocks = new List<int>();
                    foreach ( var block in dashboardBlocks.OrderBy( b => b.Config.Row ).ThenBy( b => b.Config.Column ).ThenBy( b => b.Config.Order ) )
                    {
                        DashboardRow row = null;

                        if ( block.Config.Row >= 0 && block.Config.Row < rows.Count )
                        {
                            row = rows[block.Config.Row];
                        }

                        if ( row != null && block.Config.Column >= 0 && block.Config.Column < row.Count )
                        {
                            existingBlocks.Add( block.Config.BlockId );
                            row[block.Config.Column].Placeholder.Controls.Add( block );
                        }
                    }

                    //
                    // Add in blocks that are new or don't currently fit.
                    //
                    var newBlocks = dashboardBlocks.Where( b => !existingBlocks.Contains( b.Config.BlockId ) )
                        .OrderBy( b => b.Config.Row )
                        .ThenBy( b => b.Config.Column )
                        .ThenBy( b => b.Config.Order );
                    foreach ( var block in newBlocks )
                    {
                        bool isRequired = blocks.Where( b => b.BlockId == block.Config.BlockId ).First().Required;
                        AutoConfigBlockPlacement( config, block, rows, isRequired );
                        needSave = true;
                    }

                    //
                    // Cleanup the config for any blocks that don't exist anymore.
                    //
                    var missingKeys = config.Blocks
                        .Where( b => !blocks.Where( db => b.BlockId == db.BlockId ).Any() )
                        .Select( b => b.BlockId )
                        .ToList();
                    foreach ( var missingKey in missingKeys )
                    {
                        config.Blocks.RemoveAll( b => b.BlockId == missingKey );
                        needSave = true;
                    }

                    if ( needSave )
                    {
                        SaveConfig( config );
                    }
                }
            }
        }

        /// <summary>
        /// Perform an automatic placement of the block. Also updates the configuration of
        /// the block to match where it was placed.
        /// </summary>
        /// <param name="config">The user configuration information to update.</param>
        /// <param name="block">The block wrapper tha tmus tbe placed.</param>
        /// <param name="rows">The dashboard rows in the UI.</param>
        private void AutoConfigBlockPlacement( DashboardConfig config, DashboardBlockWrapper block, List<DashboardRow> rows, bool isRequired )
        {
            var columns = rows.SelectMany( c => c ).ToList();
            var shortestColumn = columns.OrderBy( c => c.Placeholder.Controls.Count ).First();
            var row = rows.Where( r => r.Contains( shortestColumn ) ).First();

            block.Config.Column = columns.IndexOf( shortestColumn );
            block.Config.Row = rows.IndexOf( row );

            if ( isRequired )
            {
                block.Config.Order = 0;
                int i = 1;
                config.Blocks.Where( b => b.Column == block.Config.Column && b.BlockId != block.Config.BlockId )
                    .ToList()
                    .ForEach( b => b.Order = i++ );

                columns[block.Config.Column].Placeholder.Controls.AddAt(0, block );
            }
            else
            {
                block.Config.Order = 999;
                int i = 0;
                config.Blocks.Where( b => b.Column == block.Config.Column )
                    .ToList()
                    .ForEach( b => b.Order = i++ );

                columns[block.Config.Column].Placeholder.Controls.Add( block );
            }
        }

        /// <summary>
        /// Get a list of all blocks that can be configured by the administartor.
        /// </summary>
        /// <returns>A collection of BlockCache objects.</returns>
        private List<BlockCache> GetAllBlocks()
        {
            var sourcePageGuid = GetAttributeValue( "SourcePage" ).AsGuid();

            if ( sourcePageGuid != RockPage.Guid )
            {
                var sourcePage = PageCache.Read( sourcePageGuid );

                if ( sourcePage != null )
                {
                    return sourcePage.Blocks
                        .Where( b => b.PageId.HasValue )
                        .ToList();
                }
            }

            return new List<BlockCache>();
        }

        /// <summary>
        /// Get the list of available blocks that the user can enable on the dashboard.
        /// </summary>
        /// <returns>An enumerable list of block types available.</returns>
        private List<DashboardBlockType> GetAvailableBlocks()
        {
            List<DashboardBlockType> blockTypes;

            //
            // Get the current configuration settings for the list of available blocks.
            //
            try
            {
                blockTypes = JsonConvert.DeserializeObject<List<DashboardBlockType>>( GetAttributeValue( "AvailableBlocks" ) );

                if ( blockTypes == null )
                {
                    blockTypes = new List<DashboardBlockType>();
                }
            }
            catch
            {
                blockTypes = new List<DashboardBlockType>();
            }

            //
            // Get all the blocks the user can see.
            //
            var blocks = GetAllBlocks()
                .Where( b => b.IsAuthorized( Authorization.VIEW, CurrentPerson ) || b.IsAuthorized( Authorization.EDIT, CurrentPerson ) || b.IsAuthorized( Authorization.ADMINISTRATE, CurrentPerson ) )
                .ToList();

            //
            // Filter the block types to those the user has permissions to view.
            //
            blockTypes = blockTypes.Where( bt => blocks.Any( b => b.Id == bt.BlockId ) ).ToList();

            //
            // Walk each block and either initialize the block type or load it from cache.
            //
            foreach ( var block in blocks )
            {
                var blockType = blockTypes.Where( b => b.BlockId == block.Id ).FirstOrDefault();

                if ( blockType == null )
                {
                    blockType = new DashboardBlockType();
                    blockType.BlockCache = block;
                    blockType.BlockId = block.Id;

                    blockTypes.Add( blockType );
                }
                else
                {
                    blockType.BlockCache = BlockCache.Read( blockType.BlockId );
                }
            }

            return blockTypes;
        }

        /// <summary>
        /// Get the config for the current user on how they want their dashboard
        /// to look.
        /// </summary>
        /// <returns>The configuration object for the dashboard.</returns>
        private DashboardConfig GetConfig()
        {
            var configString = GetBlockUserPreference( "config" );
            DashboardConfig config = null;

            try
            {
                config = JsonConvert.DeserializeObject<DashboardConfig>( configString );
                if ( config == null )
                {
                    config = new DashboardConfig { Layouts = new List<string> { GetAttributeValue( "DefaultLayout" ) } };
                }
                
                //
                // Convert from the older layout system.
                //
                if ( !string.IsNullOrWhiteSpace( config.Layout ) )
                {
                    config.Layouts = new List<string> { config.Layout };
                    config.Layout = null;
                }
            }
            catch
            {
                config = new DashboardConfig { Layouts = new List<string> { GetAttributeValue( "DefaultLayout" ) } };
            }

            return config;
        }

        /// <summary>
        /// Saves the user configuartion about how they want their dashboard to look.
        /// </summary>
        /// <param name="config">The dashboard configuartion for the user.</param>
        private void SaveConfig( DashboardConfig config )
        {
            //
            // Explicitly set the order of the blocks.
            //
            foreach ( var blockGroup in config.Blocks.GroupBy( b => b.Column ) )
            {
                int order = 0;

                blockGroup.OrderBy( b => b.Order ).ToList().ForEach( b => b.Order = order++ );
            }

            SetBlockUserPreference( "config", JsonConvert.SerializeObject( config ) );
        }

        /// <summary>
        /// Read the current layout selection that the user has made in the repeater.
        /// </summary>
        private void ReadLayoutSelection()
        {
            List<string> layouts = new List<string>();

            foreach ( RepeaterItem row in rpLayouts.Controls )
            {
                var rblOptionsLayout = row.FindControl( "rblOptionsLayout" ) as RadioButtonList;
                layouts.Add( rblOptionsLayout.SelectedValue );
            }

            OptionsLayouts = layouts;
        }

        /// <summary>
        /// Bind the layout repeater so the user can pick which layouts they want to use.
        /// </summary>
        private void BindLayoutOptions()
        {
            OptionsCanDeleteLayout = OptionsLayouts.Count > 1;
            rpLayouts.DataSource = OptionsLayouts;
            rpLayouts.DataBind();
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
            NavigateToPage( CurrentPageReference );
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnOptions_Click( object sender, EventArgs e )
        {
            var config = GetConfig();

            OptionsLayouts = config.Layouts;
            BindLayoutOptions();

            var blocks = GetAvailableBlocks();
            cblOptionsBlocks.Items.Clear();

            //
            // Build the list of non-required blocks that the user can select from.
            //
            foreach ( var block in blocks.Where( b => !b.Required ) )
            {
                var item = new ListItem( block.BlockCache.Name, block.BlockId.ToString() );
                item.Selected = block.DefaultVisible;
                cblOptionsBlocks.Items.Add( item );
            }

            //
            // Update the selection status of the block if we have a user preference for it.
            //
            foreach ( var block in config.Blocks )
            {
                var item = cblOptionsBlocks.Items.FindByValue( block.BlockId.ToString() );
                if ( item != null )
                {
                    item.Selected = block.Visible;

                    var bb = blocks.Where( b => b.BlockId == block.BlockId ).First();
                    if ( bb.Required )
                    {
                        item.Selected = true;
                    }
                }
            }

            //
            // Build some text for the user to inform them of any widgets that are required.
            //
            var requiredBlocks = blocks.Where( b => b.Required );
            if ( requiredBlocks.Any() )
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine( "The following widgets are required and cannot be removed." );
                sb.AppendLine( "<ul>" );
                foreach ( var rb in requiredBlocks )
                {
                    sb.AppendLine( string.Format( "<li>{0}</li>", rb.BlockCache.Name ) );
                }
                sb.AppendLine( "</ul>" );

                nbOptionsRequiredBlocks.Text = sb.ToString();
            }
            else
            {
                nbOptionsRequiredBlocks.Text = string.Empty;
            }

            mdlOptions.Show();
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdlOptions_SaveClick( object sender, EventArgs e )
        {
            var config = GetConfig();

            ReadLayoutSelection();
            config.Layouts = OptionsLayouts;

            //
            // Update the selection of visible blocks based on the user's selection.
            //
            foreach ( ListItem item in cblOptionsBlocks.Items )
            {
                var block = config.Blocks.Where( b => b.BlockId == item.Value.AsInteger() ).FirstOrDefault();

                if ( block != null )
                {
                    if ( block.Visible != item.Selected )
                    {
                        block.Visible = item.Selected;

                        //
                        // Set column and order high to cause auto-layout of this "new" block.
                        //
                        if ( block.Visible )
                        {
                            block.Expanded = true;
                            block.Column = 999;
                            block.Order = 999;
                        }
                    }
                }
                else
                {
                    block = new DashboardBlockConfig( item.Value.AsInteger() );

                    block.Visible = item.Selected;

                    config.Blocks.Add( block );
                }
            }

            SaveConfig( config );
            mdlOptions.Hide();

            NavigateToPage( CurrentPageReference );
        }

        /// <summary>
        /// Handles the user command to show custom settings.
        /// </summary>
        protected override void ShowSettings()
        {
            List<DashboardBlockType> blockTypes;

            //
            // Get the current configuration settings for the list of available blocks.
            //
            try
            {
                blockTypes = JsonConvert.DeserializeObject<List<DashboardBlockType>>( GetAttributeValue( "AvailableBlocks" ) );

                if ( blockTypes == null )
                {
                    blockTypes = new List<DashboardBlockType>();
                }
            }
            catch
            {
                blockTypes = new List<DashboardBlockType>();
            }

            //
            // Get all the known blocks and remove any blocks that no longer exist from the
            // configuration data.
            //
            var blocks = GetAllBlocks();
            var removeBlockTypes = blockTypes.Where( b => !blocks.Any( bb => bb.Id == b.BlockId ) ).ToList();
            blockTypes.RemoveAll( b => removeBlockTypes.Any( rb => rb.BlockId == b.BlockId ) );

            //
            // Walk each block and either initialize the block type or load it from cache.
            //
            foreach ( var block in blocks )
            {
                var blockType = blockTypes.Where( b => b.BlockId == block.Id ).FirstOrDefault();

                if ( blockType == null )
                {
                    blockType = new DashboardBlockType();
                    blockType.BlockCache = block;
                    blockType.BlockId = block.Id;

                    blockTypes.Add( blockType );
                }
                else
                {
                    blockType.BlockCache = BlockCache.Read( blockType.BlockId );
                }
            }

            rblSettingsDefaultLayout.SelectedValue = GetAttributeValue( "DefaultLayout" );

            AvailableBlocksLive = blockTypes;
            gSettingsBlocks.DataSource = blockTypes;
            gSettingsBlocks.DataBind();

            mdlSettings.Show();
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void mdlSettings_SaveClick( object sender, EventArgs e )
        {
            var blockTypes = AvailableBlocksLive;

            foreach ( GridViewRow row in gSettingsBlocks.Rows.OfType<GridViewRow>() )
            {
                var blockId = row.Cells[0].Text.AsInteger();
                var cbRequired = row.Cells[2].Controls[0] as CheckBox;
                var cbDefaultVisible = row.Cells[3].Controls[0] as CheckBox;

                var blockType = blockTypes.Where( b => b.BlockId == blockId ).First();
                blockType.Required = cbRequired.Checked;
                blockType.DefaultVisible = cbDefaultVisible.Checked;
            }

            SetAttributeValue( "DefaultLayout", rblSettingsDefaultLayout.SelectedValue );
            SetAttributeValue( "AvailableBlocks", JsonConvert.SerializeObject( blockTypes ) );
            SaveAttributeValues();

            mdlSettings.Hide();
        }

        /// <summary>
        /// Handles the Click event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void btnAddLayout_Click( object sender, EventArgs e )
        {
            ReadLayoutSelection();

            var layouts = OptionsLayouts;
            layouts.Add( GetAttributeValue( "DefaultLayout" ) );
            OptionsLayouts = layouts;

            BindLayoutOptions();
        }

        /// <summary>
        /// Handles the ItemCommand event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rpLayouts_ItemCommand( object source, RepeaterCommandEventArgs e )
        {
            ReadLayoutSelection();

            if ( e.CommandName == "RemoveLayout" )
            {
                int index = e.CommandArgument.ToString().AsInteger();

                var layouts = OptionsLayouts;
                layouts.RemoveAt( index );
                OptionsLayouts = layouts;
            }

            BindLayoutOptions();
        }

        /// <summary>
        /// Handles the ItemDataBound event of the control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RepeaterItemEventArgs"/> instance containing the event data.</param>
        protected void rpLayouts_ItemDataBound( object sender, RepeaterItemEventArgs e )
        {
            var layout = ( string ) e.Item.DataItem;
            var rblOptionsLayout = e.Item.FindControl( "rblOptionsLayout" ) as RadioButtonList;
            var btnRemoveLayout = e.Item.FindControl( "btnRemoveLayout" ) as LinkButton;

            SetupLayoutButtonList( rblOptionsLayout );
            rblOptionsLayout.SelectedValue = layout;
            btnRemoveLayout.Visible = OptionsCanDeleteLayout;
            btnRemoveLayout.CommandArgument = e.Item.ItemIndex.ToString();
        }

        #endregion

        #region Classes

        /// <summary>
        /// Information about the BlockType and how it should be displayed. This is used in the admin
        /// interface to define how blocks are displayed by default on dashboards.
        /// </summary>
        private class DashboardBlockType
        {
            /// <summary>
            /// The Id of the block that exists in the source page.
            /// </summary>
            public int BlockId { get; set; }

            /// <summary>
            /// The cached information about the block. Contains useful information about the block
            /// such as the name.
            /// </summary>
            [JsonIgnore]
            public BlockCache BlockCache { get; set; }

            /// <summary>
            /// Whether or not this block is required and must always exist on a user's dashboard.
            /// </summary>
            public bool Required { get; set; }

            /// <summary>
            /// True if the block is visible by default for a new user.
            /// </summary>
            public bool DefaultVisible { get; set; }
        }

        /// <summary>
        /// The configuration of the entire dashboard for this user.
        /// </summary>
        public class DashboardConfig
        {
            /// <summary>
            /// The layout type to use for displaying this dashboard.
            /// </summary>
            public string Layout { get; set; }

            /// <summary>
            /// The new version of the layouts to use, multiple rows supported.
            /// </summary>
            public List<string> Layouts { get; set; }

            /// <summary>
            /// The configuration of each block on this dashboard.
            /// </summary>
            public List<DashboardBlockConfig> Blocks { get; set; }

            /// <summary>
            /// Initialize a new default configuration for this user.
            /// </summary>
            public DashboardConfig()
            {
                Layouts = new List<string>();
                Blocks = new List<DashboardBlockConfig>();
            }
        }

        /// <summary>
        /// Contains all the information needed to layout this widget.
        /// </summary>
        public class DashboardBlockConfig
        {
            /// <summary>
            /// The Id of the block control this configuration applies to.
            /// </summary>
            public int BlockId { get; set; }

            /// <summary>
            /// Wether or not this block should be displayed on the page.
            /// </summary>
            public bool Visible { get; set; }

            /// <summary>
            /// True if the block should be expanded when displayed on the page.
            /// </summary>
            public bool Expanded { get; set; }

            /// <summary>
            /// The row number to display the block in.
            /// </summary>
            public int Row { get; set; }

            /// <summary>
            /// The column number to display the block in.
            /// </summary>
            public int Column { get; set; }

            /// <summary>
            /// The order to display this inside it's column.
            /// </summary>
            public int Order { get; set; }

            /// <summary>
            /// Initialize default values for displaying a new block on the page.
            /// </summary>
            /// <param name="blockId">The new block Id to be displayed on the page.</param>
            public DashboardBlockConfig( int blockId )
            {
                BlockId = blockId;
                Visible = true;
                Expanded = true;
                Row = 999;
                Column = 999;
                Order = 999;
            }
        }

        /// <summary>
        /// This is just to make things cleaner when dealing with the code above.
        /// </summary>
        private class DashboardRow : List<DashboardColumn>
        {
        }

        /// <summary>
        /// Simple helper class for laying out the columns on the page.
        /// </summary>
        private class DashboardColumn
        {
            /// <summary>
            /// The panel that will define the width of the column.
            /// </summary>
            public Panel Panel { get; private set; }

            /// <summary>
            /// The placeholder that will contain the widgets to be put in the column.
            /// </summary>
            public Panel Placeholder { get; private set; }

            /// <summary>
            /// Creates a new column with the bootstrap CSS column type (e.g. col-md-3).
            /// </summary>
            /// <param name="cssClass">The bootstrap CSS column class to use.</param>
            /// <param name="columnIndex">The index of the column for use in the HTML5 data-column attribute.</param>
            public DashboardColumn( string cssClass, string identifier )
            {
                Panel = new Panel { CssClass = cssClass };
                Placeholder = new Panel { CssClass = "js-dashboard-column" };
                Placeholder.Attributes.Add( "data-column", identifier );
                Panel.Controls.Add( Placeholder );
            }
        }

        /// <summary>
        /// Wraps the RockBlock control inside friendly control that includes all the borders, headers, buttons
        /// and other things needed to manage a dashboard widget.
        /// </summary>
        private class DashboardBlockWrapper : CompositeControl
        {
            #region Private Fields

            private RockBlock _rockBlock = null;
            private BlockCache _blockCache = null;

            /// <summary>
            /// The configuration that is related to this widget block.
            /// </summary>
            public DashboardBlockConfig Config { get; set; }

            /// <summary>
            /// Wether or not to show the Delete button.
            /// </summary>
            public bool ShowDelete { get; set; }

            #endregion

            /// <summary>
            /// Initializes a new instance of the <see cref="RockBlockWrapper"/> class.
            /// </summary>
            /// <param name="rockBlock">The rock block.</param>
            /// <param name="blockCache">The BlockCache information that defines this block.</param>
            /// <param name="config">The dashboard configuration for this block.</param>
            public DashboardBlockWrapper( RockBlock rockBlock, BlockCache blockCache, DashboardBlockConfig config )
            {
                _rockBlock = rockBlock;
                _blockCache = blockCache;
                ShowDelete = true;
                Config = config;
            }

            /// <summary>
            /// Ensures the block controls have been created.
            /// </summary>
            public void EnsureBlockControls()
            {
                base.EnsureChildControls();
            }

            /// <summary>
            /// Called by the ASP.NET page framework to notify server controls that use composition-based implementation
            /// to create any child controls they contain in preparation for posting back or rendering.
            /// </summary>
            protected override void CreateChildControls()
            {
                base.CreateChildControls();

                Controls.Add( _rockBlock );
            }

            /// <summary>
            /// Writes the <see cref="T:System.Web.UI.WebControls.CompositeControl" /> content to the
            /// specified <see cref="T:System.Web.UI.HtmlTextWriter" /> object, for display on the client.
            /// </summary>
            /// <param name="writer">An <see cref="T:System.Web.UI.HtmlTextWriter" /> that represents the output stream to render HTML content on the client.</param>
            protected override void Render( HtmlTextWriter writer )
            {
                string preHtml = string.Empty;
                string postHtml = string.Empty;
                string appRoot = _rockBlock.ResolveRockUrl( "~/" );
                string themeRoot = _rockBlock.ResolveRockUrl( "~~/" );

                //
                // Get any pre-/post-HTML data for the block and resolve any merge fields.
                //
                if ( _rockBlock.Visible )
                {
                    if ( !string.IsNullOrWhiteSpace( _blockCache.PreHtml ) )
                    {
                        preHtml = _blockCache.PreHtml.Replace( "~~/", themeRoot ).Replace( "~/", appRoot );
                    }

                    if ( !string.IsNullOrWhiteSpace( _blockCache.PostHtml ) )
                    {
                        postHtml = _blockCache.PostHtml.Replace( "~~/", themeRoot ).Replace( "~/", appRoot );
                    }

                    if ( preHtml.HasMergeFields() || postHtml.HasMergeFields() )
                    {
                        var mergeFields = Rock.Lava.LavaHelper.GetCommonMergeFields( _rockBlock.RockPage );
                        preHtml = preHtml.ResolveMergeFields( mergeFields );
                        postHtml = postHtml.ResolveMergeFields( mergeFields );
                    }
                }

                //
                // Determine the CSS to use when wrapping the outer widget control.
                //
                string blockTypeCss = _blockCache.BlockType != null ? _blockCache.BlockType.Name : "";
                var parts = blockTypeCss.Split( new char[] { '>' } );
                if ( parts.Length > 1 )
                {
                    blockTypeCss = parts[parts.Length - 1].Trim();
                }
                blockTypeCss = blockTypeCss.Replace( ' ', '-' ).ToLower();
                string blockInstanceCss = "dashboard-block " +
                    blockTypeCss +
                    ( string.IsNullOrWhiteSpace( _blockCache.CssClass ) ? "" : " " + _blockCache.CssClass.Trim() );

                //
                // Build the outer DIV container for the entire widget.
                //
                writer.AddAttribute( HtmlTextWriterAttribute.Id, string.Format( "bid_{0}", _blockCache.Id ) );
                writer.AddAttribute( "data-bid", _blockCache.Id.ToString() );
                writer.AddAttribute( "data-zone-location", _blockCache.BlockLocation.ToString() );
                writer.AddAttribute( HtmlTextWriterAttribute.Class, blockInstanceCss );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                //
                // Build the DIV container for the panel.
                //
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "panel panel-block" );
                writer.AddAttribute( "data-bid", _blockCache.Id.ToString() );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                //
                // Build the DIV container for the panel-heading.
                //
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "panel-heading clearfix" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                //
                // Build the DIV containers for the title.
                //
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "pull-left" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "panel-title clickable js-dashboard-collapse" );
                writer.RenderBeginTag( HtmlTextWriterTag.H3 );
                writer.Write( _rockBlock.BlockName );
                writer.RenderEndTag();  // panel-title
                writer.RenderEndTag();  // pull-left

                //
                // Build the DIV container for the control buttons.
                //
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "pull-right" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );
                writer.Write( "<a class='btn btn-xs btn-link js-fa-bars'><i class='fa fa-bars'></i></a>" );
                writer.Write( string.Format( "<a class='btn btn-xs btn-link'><i class='fa dashboard-state {0} js-dashboard-collapse'></i></a>", Config.Expanded ? "fa-chevron-up" : "fa-chevron-down" ) );
                if ( ShowDelete )
                {
                    writer.Write( "<a class='btn btn-xs btn-danger js-remove-block'><i class='fa fa-times'></i></a>" );
                }
                writer.RenderEndTag();  // pull-right

                writer.RenderEndTag();  // panel-heading

                //
                // Build the DIV container for the panel-body.
                //
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "panel-body" );
                if ( !Config.Expanded )
                {
                    writer.AddAttribute( HtmlTextWriterAttribute.Style, "display: none;" );
                }
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                //
                // Build the DIV container for the block-content.
                //
                writer.AddAttribute( HtmlTextWriterAttribute.Class, "block-content" );
                writer.RenderBeginTag( HtmlTextWriterTag.Div );

                //
                // Render the RockBlock itself and it's pre-/post-HTML data.
                //
                writer.Write( preHtml );
                _rockBlock.RenderControl( writer );
                writer.Write( postHtml );

                writer.RenderEndTag();  // block-content
                writer.RenderEndTag();  // panel-body
                writer.RenderEndTag();  // panel
                writer.RenderEndTag();  // block-instance
            }
        }

        #endregion
    }
}