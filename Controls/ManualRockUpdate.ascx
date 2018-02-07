<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ManualRockUpdate.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.ManualRockUpdate" %>

<asp:UpdatePanel ID="upPanel" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-cloud-download"></i> Manual Rock Update</h1>
            </div>

            <div class="panel-body">
                <asp:Panel ID="pnlUpload" runat="server" Visible="true">
                    <div class="row">
                        <div class="col-md-6">
                    <Rock:RockDropDownList ID="ddlPackage" runat="server" Required="true" Label="Version" />
                        </div>
                    </div>

                    <div class="margin-t-md">
                        <asp:LinkButton ID="lbInstall" runat="server" CssClass="btn btn-success" Text="Install" OnClick="lbInstall_Click" />
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlUpdateSuccess" runat="server" Visible="false">

                    <div class="well well-message well-message-success">
                        <h1>Eureka, Pay Dirt!</h1>
                        <i class="fa fa-exclamation-triangle"></i>
                        <p>Update completed successfully... You're now running <asp:Literal ID="lSuccessVersion" runat="server" /> .</p>

                        <button type="button" id="btn-restart" data-loading-text="Restarting..." class="btn btn-success">Restart</button>
                    </div>
                </asp:Panel>

                <asp:Panel ID="pnlError" runat="server" Visible="false">
                    <div class="well well-message well-message-danger">
                        <h1>Whoa... That Wasn't Suppose To Happen</h1>
                        <i class="fa fa-exclamation-circle"></i>
                        <p>An error ocurred during the update process.</p>
                    </div>
            
                    <asp:Literal ID="lMessage" runat="server"></asp:Literal>
                
                    <Rock:NotificationBox ID="nbErrors" runat="server" NotificationBoxType="Danger" Heading="Here's what happened..." />
                </asp:Panel>       
            </div>
        </div>

    </ContentTemplate>
</asp:UpdatePanel>
