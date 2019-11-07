<%@ Control Language="C#" AutoEventWireup="true" CodeFile="ScheduledTransactionsWithExpiredCC.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.ScheduledTransactionsWithExpiredCC" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfCounts" runat="server" />

        <div style="max-width: 320px; margin: auto;">
            <div style="display: inline-block; float: left;">
                <Rock:RockTextBox ID="tbDaysBack" runat="server" Label="Days Back" />
            </div>
            <div style="display: inline-block; padding-top: 29px; padding-left: 12px; float:left;">
                <asp:LinkButton ID="lbUpdate" runat="server" CssClass="btn btn-primary" Text="Update" OnClick="lbUpdate_Click" />
            </div>
        </div>

        <asp:Panel ID="pnlChart" runat="server" style="height: 200px;">
             <canvas></canvas>
        </asp:Panel>

        <asp:Panel ID="pnlGridRenewed" runat="server" style="display: none;">
            <Rock:Grid ID="gRenewed" runat="server">
                <Columns>
                    <Rock:RockBoundField DataField="Name" HeaderText="Person" />
                    <Rock:DateField DataField="StartDate" HeaderText="Start" />
                    <Rock:DateField DataField="EndDate" HeaderText="End" />
                    <Rock:RockBoundField DataField="TotalAmount" HeaderText="Total Amount" />
                </Columns>
            </Rock:Grid>
        </asp:Panel>

        <asp:Panel ID="pnlGridNotRenewed" runat="server" style="display: none;">
            <Rock:Grid ID="gNotRenewed" runat="server">
                <Columns>
                    <Rock:RockBoundField DataField="Name" HeaderText="Person" />
                    <Rock:DateField DataField="StartDate" HeaderText="Start" />
                    <Rock:DateField DataField="EndDate" HeaderText="End" />
                    <Rock:RockBoundField DataField="TotalAmount" HeaderText="Total Amount" />
                </Columns>
            </Rock:Grid>
        </asp:Panel>

        <script>
            Sys.Application.add_load(function () {
                var values = JSON.parse($('#<%= hfCounts.ClientID %>').val());
                var labels = ['Renewed', 'Not Renewed'];

                if (values[0] === 0 && values[1] === 0) {
                    values[0] = 1;
                    labels = ['No Transactions'];
                }
                var chartData = {
                    datasets: [{
                        data: values,
                        backgroundColor: ['#f7464a', '#46bfbd']
                    }],
                    labels: labels
                };

                var chartOptions = {
                    series: {
                        pie: {
                            show: true
                        }
                    },
                    grid: {
                        hoverable: true
                    },
                    legend: {
                        show: true,
                        backgroundColor: 'transparent'
                    },
                    maintainAspectRatio: false,
                    onClick: function (evt, item) {
                        if (item.length > 0 && item[0]['_model'].label !== 'No Transactions') {
                            console.log(item);
                            var idx = item[0]['_index']

                            if (idx === 0) {
                                $('#<%= pnlGridRenewed.ClientID %>').toggle();
                                $('#<%= pnlGridNotRenewed.ClientID %>').hide();
                            }
                            else {
                                $('#<%= pnlGridRenewed.ClientID %>').hide();
                                $('#<%= pnlGridNotRenewed.ClientID %>').toggle();
                            }
                        }
                    }
                };

                var canvas = $('#<%= pnlChart.ClientID %> canvas').get(0);
                new Chart(canvas.getContext('2d'), {
                    type: 'pie',
                    data: chartData,
                    options: chartOptions
                });
            });
        </script>
    </ContentTemplate>
</asp:UpdatePanel>