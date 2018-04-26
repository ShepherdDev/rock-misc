<%@ Control Language="C#" AutoEventWireup="true" CodeFile="RequireValidDevice.ascx.cs" Inherits="Plugins.com_shepherdchurch.Misc.RequireValidDevice" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbRedirect" runat="server" NotificationBoxType="Warning"></Rock:NotificationBox>
    </ContentTemplate>
</asp:UpdatePanel>