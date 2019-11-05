<%@ Control Language="C#" AutoEventWireup="true" CodeFile="CaliforniaIndependentContractorReport.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.CaliforniaIndependentContractorReport" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlDetails" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h3 class="panel-title">California Independent Contractor Report</h3>
            </div>

            <div class="panel-body">
                <asp:Panel ID="pnlSetup" runat="server">
                    <div class="row">
                        <div class="col-md-6">
                            <Rock:DatePicker ID="dpRunDate" runat="server" Label="Run Date" Required="true" />
                        </div>
                        <div class="col-md-6">
                            <Rock:DatePicker ID="dpLastRunDate" runat="server" Label="Last Run Date" Required="false" />
                        </div>
                    </div>

                    <asp:LinkButton ID="lbPreview" runat="server" Text="Preview" CssClass="btn btn-primary" OnClick="lbPreview_Click" />
                </asp:Panel>

                <asp:Panel ID="pnlPreview" runat="server" Visible="false">
                    <Rock:Grid ID="gPreview" runat="server" AllowSorting="false" AllowPaging="false" AutoGenerateColumns="false" OnGridRebind="gPreview_GridRebind">
                        <Columns>
                            <Rock:RockBoundField DataField="FirstName" HeaderText="First Name" />
                            <Rock:RockBoundField DataField="MiddleInitial" HeaderText="Middle" />
                            <Rock:RockBoundField DataField="LastName" HeaderText="Last Name" />
                            <Rock:RockBoundField DataField="SocialSecurityNumber" HeaderText="SSN" />
                            <Rock:RockBoundField DataField="StreetAddress" HeaderText="Street" />
                            <Rock:RockBoundField DataField="City" HeaderText="City" />
                            <Rock:RockBoundField DataField="State" HeaderText="State" />
                            <Rock:RockBoundField DataField="PostalCode" HeaderText="Postal" />
                            <Rock:CurrencyField DataField="ContractAmount" HeaderText="Amount" />
                        </Columns>
                    </Rock:Grid>

                    <div class="margin-t-md">
                        <asp:LinkButton ID="lbDownload" runat="server" Text="Download" CssClass="btn btn-primary" OnClick="lbDownload_Click" />
                        <asp:LinkButton ID="lbCancel" runat="server" Text="Cancel" CssClass="btn btn-link" OnClick="lbCancel_Click" />
                    </div>
                </asp:Panel>
            </div>
        </asp:Panel>
    </ContentTemplate>
    <Triggers>
        <asp:PostBackTrigger ControlID="lbDownload" />
    </Triggers>
</asp:UpdatePanel>