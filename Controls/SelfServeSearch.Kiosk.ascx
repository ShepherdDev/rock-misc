<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SelfServeSearch.Kiosk.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.SelfServeSearch_Kiosk" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <style type="text/css">
            .kiosk-body
            {
                position: relative;
                height: 100vh;
            }
            .kiosk-phone-keypad
            {
                margin-bottom: 15px;
            }
            footer {
                width: 100%;
                padding-top: 15px;
            }
        </style>

        <script>
            var isTouchDevice = 'ontouchstart' in document.documentElement;
            Sys.Application.add_load(function () {
                //
                // search 
                //
                if ($(".js-pnlsearch").is(":visible"))
                {
                    // setup digits buttons
                    $('.js-pnlsearch .tenkey a.digit').click(function ()
                    {
                        $phoneNumber = $("input[id$='tbPhone']");
                        $phoneNumber.val($phoneNumber.val() + $(this).html());
                        return false;
                    });
                    $('.js-pnlsearch .tenkey a.back').click(function ()
                    {
                        $phoneNumber = $("input[id$='tbPhone']");
                        $phoneNumber.val($phoneNumber.val().slice(0, -1));
                        return false;
                    });
                    $('.js-pnlsearch .tenkey a.clear').click(function ()
                    {
                        $phoneNumber = $("input[id$='tbPhone']");
                        $phoneNumber.val('');
                        return false;
                    });
                    // set focus to the input unless on a touch device
                    if (!isTouchDevice)
                    {
                        $('.kiosk-phoneentry').focus();
                    }
                    if ($('.kiosk-nameentry').length)
                    {
                        $('.kiosk-nameentry').focus();
                    }
                }
            });
        
        </script>

        <Rock:NotificationBox ID="nbBlockConfigErrors" runat="server" NotificationBoxType="Danger" />

        <asp:Panel ID="pnlSearch" runat="server" CssClass="kiosk-body js-pnlsearch js-kioskscrollpanel" Visible="true">
            <header>
                <h1 id="hdrText" runat="server">Person Search</h1>
            </header>

            <main>
                <div class="scrollpanel">
                    <div class="scroller">
                        <asp:Panel ID="pnlSearchName" runat="server" DefaultButton="lbNameSearch" Visible="false">
                            <div class="row">
                                <div class="col-md-12">
                                    <Rock:NotificationBox ID="nbNameSearch" runat="server" NotificationBoxType="Warning"></Rock:NotificationBox>
                    
                                    <Rock:RockTextBox ID="tbName" CssClass="kiosk-phoneentry" runat="server" Label="Name" />
                                </div>
                            </div>

                            <div class="row">
                                <div class="col-sm-offset-3 col-sm-6 col-xs-12">
                                    <asp:LinkButton ID="lbNameSearch" runat="server" OnClick="lbSearch_Click" CssClass="btn btn-primary btn-kiosk btn-kiosk-lg">Search</asp:LinkButton>
                                </div>
                            </div>
                        </asp:Panel>

                        <asp:Panel ID="pnlSearchPhone" runat="server" DefaultButton="lbPhoneSearch">
                            <div class="row">
                                <div class="col-sm-12">
                                    <Rock:NotificationBox ID="nbPhoneSearch" runat="server" NotificationBoxType="Warning"></Rock:NotificationBox>
                    
                                    <Rock:RockTextBox ID="tbPhone" CssClass="kiosk-phoneentry" runat="server" Label="Phone Number" />
                                </div>
                            </div>
                            
                            <div class="row">
                                <div class="col-sm-6">
                                    <div class="tenkey kiosk-phone-keypad">
                                        <div>
                                            <a href="#" class="btn btn-default btn-lg digit">1</a>
                                            <a href="#" class="btn btn-default btn-lg digit">2</a>
                                            <a href="#" class="btn btn-default btn-lg digit">3</a>
                                        </div>
                                        <div>
                                            <a href="#" class="btn btn-default btn-lg digit">4</a>
                                            <a href="#" class="btn btn-default btn-lg digit">5</a>
                                            <a href="#" class="btn btn-default btn-lg digit">6</a>
                                        </div>
                                        <div>
                                            <a href="#" class="btn btn-default btn-lg digit">7</a>
                                            <a href="#" class="btn btn-default btn-lg digit">8</a>
                                            <a href="#" class="btn btn-default btn-lg digit">9</a>
                                        </div>
                                        <div>
                                            <a href="#" class="btn btn-default btn-lg command back">Back</a>
                                            <a href="#" class="btn btn-default btn-lg digit">0</a>
                                            <a href="#" class="btn btn-default btn-lg command clear">Clear</a>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-sm-6">
                                    <asp:LinkButton ID="lbPhoneSearch" runat="server" OnClick="lbSearch_Click" CssClass="btn btn-primary btn-kiosk btn-kiosk-lg kiosk-search-button">Search</asp:LinkButton>
                                </div>
                            </div>
                        </asp:Panel>
                    </div>
                </div>
            </main>

            <footer>
                <asp:LinkButton ID="lbPhoneCancel" runat="server" OnClick="lbCancel_Click" CssClass="btn btn-default btn-kiosk">Cancel</asp:LinkButton>
            </footer>
        </asp:Panel>

        <asp:Panel ID="pnlPersonSelect" runat="server" Visible="false" CssClass="kiosk-body js-pnlpersonselect js-kioskscrollpanel">
            <header>
                <div class="kiosk-container">
                    <h1>Select Your Name</h1>
                </div>
            </header>

            <main>
                <div class="scrollpanel">
                    <div class="scroller">
                        <div class="kiosk-container">
                            <asp:PlaceHolder ID="phPeople" runat="server"></asp:PlaceHolder>
                        </div>
                    </div>
                </div>
            </main>

            <footer>
                <div class="kiosk-container">
                    <asp:LinkButton ID="lbPersonSelectBack" runat="server" OnClick="lbBack_Click" CssClass="btn btn-default btn-kiosk">Back</asp:LinkButton>
                    <asp:LinkButton ID="lbPersonSelectCancel" runat="server" OnClick="lbCancel_Click" CssClass="btn btn-default btn-kiosk">Cancel</asp:LinkButton>
                    <asp:LinkButton ID="lbPersonSelectAdd" runat="server" OnClick="lbPersonSelectAdd_Click" CssClass="btn btn-primary btn-kiosk pull-right">Register</asp:LinkButton>
                </div>
            </footer>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>