<%@ Control Language="C#" AutoEventWireup="true" CodeFile="DashboardBlocks.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.DashboardBlocks" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />

        <asp:Panel ID="pnlDashboard" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <div class="pull-left">
                    <h1 class="panel-title">
                        <i class="<%= GetAttributeValue( "IconCSSClass" ) %>"></i> <%= GetAttributeValue( "Title" ) %>
                    </h1>
                </div>
                <div class="pull-right">
                    <asp:LinkButton ID="btnOptions" runat="server" CssClass="btn btn-xs btn-link" OnClick="btnOptions_Click"><i class="fa fa-cog"></i></asp:LinkButton>
                </div>
            </div>
            <div class="panel-body">
                <asp:PlaceHolder ID="phControls" runat="server"></asp:PlaceHolder>
            </div>
        </asp:Panel>

        <Rock:ModalDialog ID="mdlOptions" runat="server" Title="Options" OnSaveClick="mdlOptions_SaveClick" ValidationGroup="Options">
            <Content>
                <Rock:RockControlWrapper ID="cwLayout" runat="server" Label="Layout" Help="The column configuration you want to use.">
                    <asp:Repeater ID="rpLayouts" runat="server" OnItemCommand="rpLayouts_ItemCommand" OnItemDataBound="rpLayouts_ItemDataBound">
                        <ItemTemplate>
                            <div class="margin-b-sm">
                                <asp:RadioButtonList ID="rblOptionsLayout" runat="server" RepeatDirection="Horizontal" />
                                <asp:LinkButton ID="btnRemoveLayout" runat="server" CssClass="btn btn-xs btn-danger margin-l-sm" CommandName="RemoveLayout"><i class="fa fa-times"></i></asp:LinkButton>
                            </div>
                        </ItemTemplate>
                    </asp:Repeater>
                </Rock:RockControlWrapper>

                <asp:LinkButton ID="btnAddLayout" runat="server" CssClass="btn btn-link" Text="Add Layout" OnClick="btnAddLayout_Click" />

                <Rock:RockCheckBoxList ID="cblOptionsBlocks" runat="server" Label="Widgets" Help="Which widgets are visible on your dashboard." RepeatDirection="Vertical" />

                <Rock:NotificationBox ID="nbOptionsRequiredBlocks" runat="server" NotificationBoxType="Info"></Rock:NotificationBox>
            </Content>
        </Rock:ModalDialog>

        <Rock:ModalDialog ID="mdlSettings" runat="server" Title="Settings" OnSaveClick="mdlSettings_SaveClick" ValidationGroup="Settings">
            <Content>
                <Rock:PagePicker ID="ppSettingsSourcePage" runat="server" Label="Source Page" Help="The page that contains the blocks to display to the user." Required="true" />

                <Rock:RockTextBox ID="tbSettingsTitle" runat="server" Label="Title" Help="The title to display in the dashboard container." Required="true" />

                <Rock:RockTextBox ID="tbSettingsIconCSSClass" runat="server" Label="Icon CSS Class" Help="The CSS class to use for an icon, such as 'fa fa-tasks'." />

                <Rock:RockRadioButtonList ID="rblSettingsDefaultLayout" runat="server" Label="Default Layout" Help="The column configuration you want users to use by default." RepeatDirection="Horizontal" Required="true" />

                <Rock:RockControlWrapper ID="cwSettingsBlocks" runat="server" Label="Blocks">
                    <Rock:Grid ID="gSettingsBlocks" runat="server" Title="Blocks" OnGridRebind="gSettingsBlocks_GridRebind" OnRowDataBound="gSettingsBlocks_RowDataBound">
                        <Columns>
                            <asp:BoundField DataField="BlockId" HeaderStyle-CssClass="hidden" ItemStyle-CssClass="hidden" />
                            <asp:BoundField DataField="BlockCache.Name" HeaderText="Block" />
                            <Rock:RockTemplateField HeaderText="Required" ItemStyle-CssClass="grid-select-field">
                                <ItemTemplate>
                                    <Rock:RockCheckBox ID="cbRequired" runat="server" Checked='<%# Eval("Required") %>' OnCheckedChanged="cbRequired_CheckedChanged" AutoPostBack="true" />
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:RockTemplateField HeaderText="Visible By Default" ItemStyle-CssClass="grid-select-field">
                                <ItemTemplate>
                                    <Rock:RockCheckBox ID="cbVisibleByDefault" runat="server" Checked='<%# Eval("DefaultVisible") %>' OnCheckedChanged="cbVisibleByDefault_CheckedChanged" AutoPostBack="true" />
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:RockTemplateField HeaderText="Default Column" ItemStyle-HorizontalAlign="Center">
                                <ItemTemplate>
                                    <Rock:NumberUpDown ID="nudDefaultColumn" runat="server" CssClass="input-sm" Required="false" Minimum="0" Maximum="4" Value='<%# Eval("DefaultColumn") %>' />
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:RockTemplateField HeaderText="Default Order" ItemStyle-HorizontalAlign="Center">
                                <ItemTemplate>
                                    <Rock:NumberUpDown ID="nudDefaultOrder" runat="server" CssClass="input-sm" Required="false" Minimum="0" Maximum="999" Value='<%# Eval("DefaultOrder") %>' />
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                        </Columns>
                    </Rock:Grid>
                </Rock:RockControlWrapper>

                <Rock:NotificationBox ID="nbCssTips" runat="server" NotificationBoxType="Info">
                    <p>The follow CSS classes can be added to blocks to improve their appearence when displayed on the dashboard:</p>
                    <ul>
                        <li><code>dashboard-panel-block</code>: Removes some of the extra padding around the block when it displays its own full panel.</li>
                        <li><code>dashboard-panel-hide-title</code>: Hides the title bar of the block if it includes its own full panel.</li>
                        <li><code>icon-</code>: This prefix can be used to add CSS icons to the dashboard block.
                            The <code>icon-</code> is stripped out and then used as the icon class.
                            Example: <code>icon-fa icon-fa-line-chart</code> would render as <code>fa fa-line-chart</code>.</li>
                    </ul>
                </Rock:NotificationBox>
            </Content>
        </Rock:ModalDialog>
    </ContentTemplate>
</asp:UpdatePanel>
