<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GenericSqlBlock.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.GenericSqlBlock" %>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <asp:Literal ID="ltContent" runat="server" />
    </ContentTemplate>
</asp:UpdatePanel>