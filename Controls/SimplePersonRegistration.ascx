<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SimplePersonRegistration.ascx.cs" Inherits="Plugins.com_shepherdchurch.Misc.SimplePersonRegistration" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="row">
            <div class="col-md-4 col-sm-6">
                <Rock:RockTextBox ID="tbFirstName" runat="server" Label="First Name" Required="true" />
            </div>

            <div class="col-md-4 col-sm-6">
                <Rock:RockTextBox ID="tbLastName" runat="server" Label="Last Name" Required="true" />
            </div>

            <div class="col-md-4 col-sm-6">
                <Rock:RockTextBox ID="tbEmail" runat="server" Label="Email" Required="true" />
            </div>
        </div>

        <div class="row">
        </div>

        <div class="row">
            <div id="colHomePhone" runat="server" class="col-md-4 col-sm-6">
                <Rock:PhoneNumberBox ID="pnHome" runat="server" Label="Home Phone" />
            </div>
            <div id="colMobilePhone" runat="server" class="col-md-4 col-sm-6">
                <Rock:PhoneNumberBox ID="pnMobile" runat="server" Label="Mobile Phone" />
            </div>
        </div>

        <Rock:AddressControl ID="acHomeAddress" runat="server" Label="Home Address" />

        <asp:Button ID="btnSubmit" runat="server" Text="Register" CssClass="btn btn-primary" OnClick="btnSubmit_Click" />
    </ContentTemplate>
</asp:UpdatePanel>
