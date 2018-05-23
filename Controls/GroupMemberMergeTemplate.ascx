<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupMemberMergeTemplate.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.GroupMemberMergeTemplate" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <Triggers>
        <asp:PostBackTrigger ControlID="lbMerge" />
    </Triggers>
    <ContentTemplate>
        <Rock:NotificationBox ID="nbWarningMessage" runat="server" NotificationBoxType="Warning" />

        <asp:Panel ID="pnlDetails" runat="server" CssClass="panel panel-block">
            <div class="panel-heading">
                <h1 class="panel-title"><i class="fa fa-copy"></i> Merge Members To Template</h1>
            </div>

            <div class="panel-body">
                <asp:Panel ID="pnlPost" runat="server" Visible="true">
                    <Rock:RockRadioButtonList ID="rblSource" runat="server" Label="Source" Help="Where to get the list of individuals from." RepeatDirection="Horizontal" OnSelectedIndexChanged="rblSource_SelectedIndexChanged" AutoPostBack="true" CausesValidation="false">
                        <asp:ListItem Value="Group" Text="Group" Selected="True"></asp:ListItem>
                        <asp:ListItem Value="Data View" Text="Data View"></asp:ListItem>
                    </Rock:RockRadioButtonList>

                    <asp:Panel ID="pnlGroup" runat="server" Visible="true">
                        <Rock:GroupPicker ID="gpGroup" runat="server" Label="Group" Help="Members of this group or any descendant group will be merged." Required="true" />
                    </asp:Panel>

                    <asp:Panel ID="pnlDataView" runat="server" Visible="false">
                        <div class="row">
                            <div class="col-md-6">
                                <Rock:DataViewPicker ID="dvDataView" runat="server" Label="Data View" Required="true" />
                            </div>
                            
                            <div class="col-md-6"></div>
                        </div>
                    </asp:Panel>

                    <Rock:MergeTemplatePicker ID="mtPicker" runat="server" Label="Merge Template" OnSelectItem="mtPicker_SelectItem" />

                    <Rock:NotificationBox ID="nbMergeError" runat="server" NotificationBoxType="Warning" CssClass="js-merge-error"/>

                    <div class="actions">
                        <asp:LinkButton ID="lbMerge" runat="server" CssClass="btn btn-primary" Text="Merge" OnClientClick="$('.js-merge-error').hide()" OnClick="lbMerge_Click" />
                    </div>
                </asp:Panel>
            </div>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>
