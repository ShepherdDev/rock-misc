<%@ Control Language="C#" AutoEventWireup="true" CodeFile="TestCommunicationTemplate.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.TestCommunicationTemplate" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlContent" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-paper-plane-o"></i> <asp:Literal ID="ltTitle" runat="server">Test Communication Template</asp:Literal></h1>
            </div>

            <div class="panel-body">
                <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />
                <Rock:NotificationBox ID="nbSuccess" runat="server" NotificationBoxType="Success" />

                <Rock:RockDropDownList ID="ddlEmail" runat="server" Label="Template" Required="true"></Rock:RockDropDownList>

                <asp:Button ID="btnSendTest" runat="server" CssClass="btn btn-primary" Text="Send Test" OnClick="btnSendTest_Click" />
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
