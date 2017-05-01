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
                <Rock:EntityTypePicker ID="pEntityType" runat="server" Label="Entity Type" Required="true" IncludeGlobalOption="false" Help="Select the entity type you are wanting to verify access to. If you are trying to check the security on a page then set this to 'Page'." />
                <Rock:RockTextBox ID="tbEntityId" runat="server" Label="Entity Id" Help="Integer ID or Guid of the entity you are trying to check security of. If you are trying to check the security of the default external homepage (which is Id #1) then set this to 1." />

                <asp:Button ID="btnCheck" runat="server" CssClass="btn btn-primary" Text="Check" OnClick="btnCheck_Click" />
                
                <asp:Panel ID="pnlResult" runat="server" CssClass="wellx margin-t-lg" Visible="false">
                    <Rock:Grid ID="gResults" runat="server" AllowPaging="false" AllowSorting="false" OnRowDataBound="gResults_RowDataBound" DataKeyNames="Id">
                        <Columns>
                            <Rock:RockBoundField DataField="Action" HeaderText="Action"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="EntityType" HeaderText="Source Type"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="EntityId" HeaderText="Source Id"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="EntityName" HeaderText="Source Name"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Role" HeaderText="User / Role"></Rock:RockBoundField>
                            <Rock:RockBoundField DataField="Access" HeaderText="Access" HtmlEncode="false"></Rock:RockBoundField>
                            <Rock:LinkButtonField HeaderText="" CssClass="btn btn-default btn-sm fa fa-unlock" OnClick="gUnlock_Click"></Rock:LinkButtonField>
                        </Columns>
                    </Rock:Grid>
                </asp:Panel>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
