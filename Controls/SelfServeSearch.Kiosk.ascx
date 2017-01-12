<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SelfServeSearch.Kiosk.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.SelfServeSearch_Kiosk" %>
<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>

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

        <asp:Panel ID="pnlSearchName" runat="server" CssClass="js-pnlsearch" DefaultButton="lbNameSearch" Visible="false">
            <header>
                <h1>Please Enter Your Name</h1>
            </header>

            <main>
                <div class="container">
                    <div class="row">
                        <div class="col-md-12">
                            <Rock:NotificationBox ID="nbNameSearch" runat="server" NotificationBoxType="Warning"></Rock:NotificationBox>
                    
                            <Rock:RockTextBox ID="tbName" CssClass="kiosk-phoneentry" runat="server" Label="Name" />
                        </div>
                    </div>

                    <div class="row">
                        <div class="col-md-8">
                            &nbsp;
                        </div>
                        <div class="col-md-4">
                            <asp:LinkButton ID="lbNameSearch" runat="server" OnClick="lbSearch_Click" CssClass="btn btn-primary btn-kiosk btn-kiosk-lg">Search</asp:LinkButton>
                        </div>
                    </div>
                </div>
            </main>

            <footer>
                <div class="container">
                    <div class="row">
                        <div class="col-md-8">
                            <asp:LinkButton ID="lbNameCancel" runat="server" OnClick="lbCancel_Click" CssClass="btn btn-default btn-kiosk">Cancel</asp:LinkButton>
                        </div>
                    </div>
                </div>
            </footer>
            
        </asp:Panel>

        <asp:Panel ID="pnlSearchPhone" runat="server" CssClass="js-pnlsearch" DefaultButton="lbPhoneSearch">
            <header>
                <h1>Please Enter Your Phone Number</h1>
            </header>

            <main>
                <div class="container">
                    <div class="row">
                        <div class="col-md-12">
                            <Rock:NotificationBox ID="nbPhoneSearch" runat="server" NotificationBoxType="Warning"></Rock:NotificationBox>
                    
                            <Rock:RockTextBox ID="tbPhone" CssClass="kiosk-phoneentry" runat="server" Label="Phone Number" />
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-md-8">
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
                        <div class="col-md-4">
                            <asp:LinkButton ID="lbPhoneSearch" runat="server" OnClick="lbSearch_Click" CssClass="btn btn-primary btn-kiosk btn-kiosk-lg">Search</asp:LinkButton>
                        </div>
                    </div>
                </div>
            </main>

            <footer>
                <div class="container">
                    <div class="row">
                        <div class="col-md-8">
                            <asp:LinkButton ID="lbPhoneCancel" runat="server" OnClick="lbCancel_Click" CssClass="btn btn-default btn-kiosk">Cancel</asp:LinkButton>
                        </div>
                    </div>
                </div>
            </footer>
            
        </asp:Panel>

        <asp:Panel ID="pnlPersonSelect" runat="server" Visible="false" CssClass="js-pnlpersonselect js-kioskscrollpanel">
            <header>
                <h1>Select Your Name</h1>
            </header>

            <main class="clearfix js-scrollcontainer">
                <div class="scrollpanel">
                    <div class="scroller">
                        <asp:PlaceHolder ID="phPeople" runat="server"></asp:PlaceHolder>
                    </div>
                </div>
            </main>

            <footer>
                <div class="container">
                    <div class="row">
                        <div class="col-md-8">
                            <asp:LinkButton ID="lbPersonSelectBack" runat="server" OnClick="lbBack_Click" CssClass="btn btn-default btn-kiosk">Back</asp:LinkButton>
                            <asp:LinkButton ID="lbPersonSelectCancel" runat="server" OnClick="lbCancel_Click" CssClass="btn btn-default btn-kiosk">Cancel</asp:LinkButton>
                        </div>
                        <div class="col-md-4 text-right">
                            <asp:LinkButton ID="lbPersonSelectAdd" runat="server" OnClick="lbPersonSelectAdd_Click" CssClass="btn btn-primary btn-kiosk">Register</asp:LinkButton>
                        </div>
                    </div>
                </div>
            </footer>
        </asp:Panel>

    </ContentTemplate>
</asp:UpdatePanel>