<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ShareWorkflow.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.ShareWorkflow" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />

        <Rock:WorkflowTypePicker ID="wtpExport" runat="server" Label="Workflow Type" Help="The workflow type to be exported." />
        <asp:Button ID="btnExport" runat="server" Text="Export" CssClass="btn btn-primary" OnClick="btnExport_Click" />

        <Rock:FileUploader ID="fuImport" runat="server" Label="Import File" OnFileUploaded="fuImport_FileUploaded" />

        <pre><asp:Literal ID="ltDebug" runat="server"></asp:Literal></pre>
    </ContentTemplate>
</asp:UpdatePanel>