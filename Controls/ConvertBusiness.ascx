<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ConvertBusiness.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.ConvertBusiness" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <div class="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-building"></i> Convert Business</h1>
            </div>

            <div class="panel-body">
                <Rock:NotificationBox ID="nbSuccess" runat="server" NotificationBoxType="Success" />
                <Rock:NotificationBox ID="nbWarning" runat="server" NotificationBoxType="Warning" />
                <Rock:NotificationBox ID="nbError" runat="server" NotificationBoxType="Danger" />

                <Rock:PersonPicker ID="ppSource" runat="server" IncludeBusinesses="true" Label="Person/Business" Help="Select the person or business that you want to convert." OnSelectPerson="ppSource_SelectPerson" />

                <asp:Panel ID="pnlToPerson" runat="server" Visible="false">
                    <Rock:NotificationBox ID="nbToPerson" runat="server" NotificationBoxType="Warning">The selected record will be converted to a Person with the values entered below.</Rock:NotificationBox>

                    <Rock:RockTextBox ID="tbPersonFirstName" runat="server" Label="First Name" Required="true" ValidationGroup="ConvertToPerson" />
                    <Rock:RockTextBox ID="tbPersonLastName" runat="server" Label="Last Name" Required="true" ValidationGroup="ConvertToPerson" />
                    <Rock:DefinedValuePicker ID="dvpPersonConnectionStatus" runat="server" Label="Connection Status" Required="true" ValidationGroup="ConvertToPerson" />
                    <asp:LinkButton ID="lbPersonSave" runat="server" Text="Save" CssClass="btn btn-primary" ValidationGroup="ConvertToPerson" OnClick="lbToPersonSave_Click" />
                </asp:Panel>

                <asp:Panel ID="pnlToBusiness" runat="server" Visible="false">
                    <Rock:NotificationBox ID="nbToBusiness" runat="server" NotificationBoxType="Warning">The selected record will be converted to a Business with the values entered below.</Rock:NotificationBox>

                    <Rock:RockTextBox ID="tbBusinessName" runat="server" Label="Name" Required="true" ValidationGroup="ConvertToBusiness" />
                    <asp:LinkButton ID="lbBusinessSave" runat="server" Text="Save" CssClass="btn btn-primary" ValidationGroup="ConvertToBusiness" OnClick="lbToBusinessSave_Click" />
                </asp:Panel>
            </div>
        </div>
    </ContentTemplate>
</asp:UpdatePanel>