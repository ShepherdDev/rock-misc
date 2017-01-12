<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CacheAndRedirect.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.CacheAndRedirect" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />
    </ContentTemplate>
</asp:UpdatePanel>