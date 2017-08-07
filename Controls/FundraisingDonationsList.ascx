<%@ Control Language="C#" AutoEventWireup="true" CodeFile="FundraisingDonationsList.ascx.cs" Inherits="Plugins.com_shepherdchurch.Misc.FundraisingDonationsList" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:Panel ID="pnlDetails" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h3 class="panel-title"><i class="fa fa-money"></i> Fundraising Donations</h3>
            </div>

            <div class="panel-body">
                <div class="grid grid-panel">
                    <Rock:Grid ID="gDonations" runat="server" DisplayType="Full" AllowSorting="true" EmptyDataText="No Donations Found" PersonIdField="DonorId" RowItemText="Donations" DataKeyNames="DonorId">
                        <Columns>
                            <Rock:SelectField />
                            <Rock:PersonField DataField="Donor" HeaderText="Donor" SortExpression="Donor.LastName, Donor.NickName" />
                            <Rock:RockBoundField DataField="Address" HeaderText="Donor Address" HtmlEncode="false" />
                            <Rock:RockBoundField DataField="Donor.Email" HeaderText="Donor Email" SortExpression="Donor.Email" />
                            <Rock:RockTemplateField  HeaderText="Participant" SortExpression="Participant.Person.LastName, Participant.Person.NickName">
                                <ItemTemplate>
                                    <a href="/GroupMember/<%# Eval( "Participant.Id" ) %>"><%# Eval( "Participant.Person.FullName" ) %></a>
                                    <a href="/Person/<%# Eval( "Participant.Person.Id" ) %>" class="pull-right btn btn-sm btn-default"><i class="fa fa-user"></i></a>
                                </ItemTemplate>
                            </Rock:RockTemplateField>
                            <Rock:DateField DataField="Date" HeaderText="Date" HeaderStyle-HorizontalAlign="Right" SortExpression="Date" />
                            <Rock:CurrencyField DataField="Amount" HeaderText="Amount" HeaderStyle-HorizontalAlign="Right" SortExpression="Amount" />
                        </Columns>
                    </Rock:Grid>
                </div>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>