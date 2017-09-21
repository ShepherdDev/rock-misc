<%@ Control Language="C#" AutoEventWireup="true" CodeFile="EventDepositSlips.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.EventDepositSlips" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

        <asp:Panel ID="pnlDetails" runat="server">
            <div class="panel panel-block">
                <div class="panel-heading">
                    <h1 class="panel-title"><i class="fa fa-print"></i> Payments</h1>
                </div>
                <div class="panel-body">
                    <Rock:ModalAlert ID="mdPaymentsGridWarning" runat="server" />
                    <div class="grid grid-panel">
                        <Rock:GridFilter ID="fPayments" runat="server" OnDisplayFilterValue="fPayments_DisplayFilterValue">
                            <Rock:DateRangePicker ID="drpPaymentDateRange" runat="server" Label="Date Range" />
                            <Rock:RockCheckBoxList ID="cblCurrencyType" runat="server" Label="Currency Type" RepeatDirection="Horizontal" />
                        </Rock:GridFilter>

                        <Rock:Grid ID="gPayments" runat="server" DisplayType="Full" AllowSorting="true" RowItemText="Payment">
                            <Columns>
                                <Rock:SelectField></Rock:SelectField>
                                <Rock:RockBoundField DataField="AuthorizedPersonAlias.Person.FullNameReversed" HeaderText="Person"
                                    SortExpression="AuthorizedPersonAlias.Person.LastName,AuthorizedPersonAlias.Person.NickName" />
                                <Rock:RockBoundField DataField="TransactionDateTime" HeaderText="Date / Time" SortExpression="TransactionDateTime" />
                                <Rock:CurrencyField DataField="TotalAmount" HeaderText="Amount" SortExpression="TotalAmount" />
                                <Rock:RockBoundField DataField="FinancialPaymentDetail.CurrencyTypeValue.Value" HeaderText="Currency Type" />
                                <Rock:RockTemplateFieldUnselected HeaderText="Registrar">
                                    <ItemTemplate>
                                        <asp:Literal ID="lRegistrar" runat="server" />
                                    </ItemTemplate>
                                </Rock:RockTemplateFieldUnselected>
                                <Rock:RockTemplateField HeaderText="Registrant(s)">
                                    <ItemTemplate>
                                        <asp:Literal ID="lRegistrants" runat="server" />
                                    </ItemTemplate>
                                </Rock:RockTemplateField>
                            </Columns>
                        </Rock:Grid>
                    </div>
                </div>
            </div>

            <asp:LinkButton ID="btnPrint" runat="server" CssClass="btn btn-primary" OnClick="btnPrint_Click">Print</asp:LinkButton>
        </asp:Panel>

        <asp:Panel ID="pnlPrint" runat="server" Visible="false">
            <asp:Literal ID="ltFormattedOutput" runat="server"></asp:Literal>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>
