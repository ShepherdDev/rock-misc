<%@ Control Language="C#" AutoEventWireup="true" CodeFile="GroupMemberProfileDetails.ascx.cs" Inherits="Plugins.com_shepherdchurch.Misc.GroupMemberProfileDetails" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <Rock:NotificationBox ID="nbInvalidPerson" runat="server" NotificationBoxType="Danger"></Rock:NotificationBox>
        <Rock:NotificationBox ID="nbConfigError" runat="server" NotificationBoxType="Danger"></Rock:NotificationBox>

        <asp:Panel ID="pnlEdit" runat="server" Visible="true">
            <div class="row">
                <div class="col-md-12">
                    <fieldset>
                        <legend>
                            <asp:Literal ID="ltFullName" runat="server" />
                        </legend>

                        <asp:Panel ID="pnlNickName" runat="server" CssClass="row">
                            <div class="col-sm-9">
                                <Rock:DataTextBox ID="tbNickName" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="NickName" autocomplete="off" />
                            </div>
                        </asp:Panel>

                        <Rock:RockRadioButtonList ID="rblGender" runat="server" RepeatDirection="Horizontal" Label="Gender">
                            <asp:ListItem Text="Male" Value="Male" />
                            <asp:ListItem Text="Female" Value="Female" />
                            <asp:ListItem Text="Unknown" Value="Unknown" />
                        </Rock:RockRadioButtonList>

                        <Rock:BirthdayPicker ID="bpBirthDay" runat="server" Label="Birthday" />

                        <asp:Panel id="pnlGrade" runat="server" CssClass="row">
                            <div class="col-sm-12">
                                <div class="pull-left">
                                    <Rock:GradePicker ID="ddlGradePicker" runat="server" UseAbbreviation="true" UseGradeOffsetAsValue="true" CssClass="input-width-md" />
                                </div>
                                <div class="pull-left margin-l-md hidden-xs">&nbsp;</div>
                                <div class="clearfix visible-xs"></div>
                                <div class="pull-left">
                                    <Rock:YearPicker ID="ypGraduation" runat="server" Label="Graduation Year" Help="High School Graduation Year." />
                                </div>
                            </div>
                        </asp:Panel>

                        <asp:Panel ID="pnlMaritalStatus" runat="server" CssClass="row">
                            <div class="col-sm-6">
                                <Rock:DefinedValuePicker ID="ddlMaritalStatus" runat="server" Label="Marital Status" />
                            </div>
                            <div class="col-sm-3">
                                <Rock:DatePicker ID="dpAnniversaryDate" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="AnniversaryDate" StartView="decade" Label="Wedding Anniversary Date" />
                            </div>
                        </asp:Panel>

                        <asp:Panel ID="pnlEmail" runat="server" CssClass="row">
                            <div class="col-sm-9">
                                <Rock:DataTextBox ID="tbEmail" PrependText="<i class='fa fa-envelope'></i>" runat="server" SourceTypeName="Rock.Model.Person, Rock" PropertyName="Email" autocomplete="off" />
                            </div>
                        </asp:Panel>
                    </fieldset>

                    <asp:Panel ID="pnlPhones" runat="server" Visible="false">
                        <fieldset>
                            <asp:Repeater ID="rContactInfo" runat="server">
                                <ItemTemplate>
                                    <div class="row">
                                        <div class="col-md-12">
                                            <div class="margin-b-md">
                                                <div class="control-label">
                                                    <label><%# Rock.Web.Cache.DefinedValueCache.Read( (int)Eval("NumberTypeValueId")).Value  %></label>
                                                </div>
                                                <div class="control-wrapper">
                                                    <div class="row">
                                                        <div class="col-sm-9">
                                                            <asp:HiddenField ID="hfPhoneType" runat="server" Value='<%# Eval("NumberTypeValueId")  %>' />
                                                            <Rock:PhoneNumberBox ID="pnbPhone" runat="server" CountryCode='<%# Eval("CountryCode") %>' Number='<%# Eval("NumberFormatted")  %>' autocomplete="off" />
                                                        </div>    
                                                        <div class="col-sm-3">
                                                            <asp:CheckBox ID="cbSms" runat="server" Text="SMS" Checked='<%# (bool)Eval("IsMessagingEnabled") %>' CssClass="js-sms-number" />
                                                        </div>
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </ItemTemplate>
                            </asp:Repeater>
                        </fieldset>
                    </asp:Panel>

                    <asp:Panel ID="pnlAddress" runat="server" Visible="false">
                        <fieldset>
                            <div class="row">
                                <div class="col-sm-12">
                                    <div class="margin-b-md">
                                        <div class="control-label">
                                            <label><asp:Literal ID="lAddressTitle" runat="server" /></label>
                                        </div>

                                        <div class="control-wrapper">
                                            <div class="row">
                                                <div class="col-md-9">
                                                    <Rock:AddressControl ID="acAddress" runat="server" />
                                                </div>
                                                <div class="col-md-3">
                                                    <div class="margin-b-md">
                                                        <Rock:RockCheckBox ID="cbIsMailingAddress" runat="server" Text="This is a mailing address" Checked="true" />
                                                        <Rock:RockCheckBox ID="cbIsPhysicalAddress" runat="server" Text="This is a physical address" Checked="true" />
                                                    </div>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </fieldset>
                    </asp:Panel>
                </div>
            </div>

            <Rock:BootstrapButton ID="btnSave" runat="server" OnClick="btnSave_Click" CssClass="btn btn-primary" Text="Save" />
            <asp:Button ID="btnCancel" runat="server" OnClick="btnCancel_Click" CssClass="btn btn-default" Text="Cancel" />
        </asp:Panel>

        <asp:Panel ID="pnlSuccess" runat="server" Visible="false">
            <Rock:NotificationBox ID="nbSuccess" runat="server" NotificationBoxType="Success"></Rock:NotificationBox>

            <asp:Button ID="btnDone" runat="server" OnClick="btnCancel_Click" CssClass="btn btn-primary" Text="Done" />
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>