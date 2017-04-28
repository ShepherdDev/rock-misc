<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TraceSecurity.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.TraceSecurity" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlSecurity" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-lock"></i> Trace Security</h1>
            </div>

            <div class="panel-body">
                <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />

                <Rock:PersonPicker ID="ppPerson" runat="server" Label="Person" Help="The person to check security against, if not set then your security is checked." />
                <Rock:EntityTypePicker ID="pEntityType" runat="server" Label="Entity Type" Required="true" IncludeGlobalOption="false" />
                <Rock:RockTextBox ID="tbEntityId" runat="server" Label="Entity Id" Help="Integer ID or Guid of the entity." />

                <asp:Button ID="btnCheck" runat="server" CssClass="btn btn-primary" Text="Check" OnClick="btnCheck_Click" />
                
                <asp:Panel ID="pnlResult" runat="server" CssClass="well margin-t-lg" Visible="false">
                    <div class="margin-b-md">
                        Found the following explicit permissions for the selected entity.
                    </div>

                    <Rock:Grid ID="gResults" runat="server" AllowPaging="false" AllowSorting="false">
                        <Columns>
                            <Rock:RockBoundField DataField="Action" HeaderText="Action"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="EntityType" HeaderText="From Entity Type"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="EntityId" HeaderText="From Entity Id"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="EntityName" HeaderText="From Entity Name"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Role" HeaderText="User / Role"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Access" HeaderText="Access" HtmlEncode="false"></Rock:RockBoundField>
                        </Columns>
                    </Rock:Grid>
                </asp:Panel>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
