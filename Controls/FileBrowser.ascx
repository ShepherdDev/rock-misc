<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FileBrowser.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.FileBrowser" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />
        <Rock:NotificationBox ID="nbError" runat="server" NotificationBoxType="Danger" />

        <asp:HiddenField ID="hfPath" runat="server" />
        <asp:HiddenField ID="hfFile" runat="server" />
        <asp:HiddenField ID="hfDownloadType" runat="server" />
        <asp:Button ID="btnBrowse" runat="server" CssClass="hidden" OnClick="btnBrowse_Click" Text="Browse" />
        <asp:Button ID="btnDownload" runat="server" CssClass="hidden" OnClick="btnDownload_Click" Text="Download" />

        <asp:Literal ID="ltItems" runat="server"></asp:Literal>

        <script type="text/javascript">
            function filebrowser_<%= ClientID %>_submit(path)
            {
                if (path == '/') {
                    $('#<%= hfPath.ClientID %>').val('/');
                }
                else {
                    $('#<%= hfPath.ClientID %>').val( $('#<%= hfPath.ClientID %>').val() + path);
                }

                $('#<%= btnBrowse.ClientID %>').click();
                
                return false;
            }

            function filebrowser_<%= ClientID %>_download(file, type)
            {
                $('#<%= hfFile.ClientID %>').val(file);
                $('#<%= hfDownloadType.ClientID %>').val(type === false ? '0' : '1');

                $('#<%= btnDownload.ClientID %>').click();
                
                return false;
            }
        </script>
    </ContentTemplate>
</asp:UpdatePanel>