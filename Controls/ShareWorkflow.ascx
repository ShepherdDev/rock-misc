<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ShareWorkflow.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.ShareWorkflow" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlView" runat="server" CssClass="panel panel-block">
        
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-share-square"></i> Share Workflow</h1>
            </div>
            <div class="panel-body">
                <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />

                <div class="row">
                    <div class="col-md-6">
                        <Rock:WorkflowTypePicker ID="wtpExport" runat="server" Label="Workflow Type" Help="The workflow type to be exported." />
                        <asp:Button ID="btnExport" runat="server" Text="Export" CssClass="btn btn-primary" OnClick="btnExport_Click" />
                        <asp:Button ID="btnPreview" runat="server" Text="Preview" CssClass="btn btn-info" OnClick="btnPreview_Click" />
                    </div>

                    <div class="col-md-6">
                        <Rock:FileUploader ID="fuImport" runat="server" Label="Import File" OnFileUploaded="fuImport_FileUploaded" />
                    </div>
                </div>

                <pre><asp:Literal ID="ltDebug" runat="server"></asp:Literal></pre>
            </div>
        
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>