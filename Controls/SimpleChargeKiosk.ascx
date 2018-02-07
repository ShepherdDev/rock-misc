<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SimpleChargeKiosk.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.SimpleChargeKiosk" %>

<asp:UpdatePanel ID="upnlContent" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfSwipe" runat="server" ClientIDMode="Static" />

        <script>
            var isTouchDevice = 'ontouchstart' in document.documentElement;
            //
            // setup swipe detection
            //
            var lastKeyPress = 0;
            var keyboardBuffer = '';
            var swipeProcessing = false;
            $(document).keypress(function (e) {
                console.log('Keypressed: ' + e.which + ' - ' + String.fromCharCode(e.which));
                var date = new Date();
                if ($(".js-swipe").is(":visible")) {
                    if (e.which == 37 && (date.getTime() - lastKeyPress) > 500) { // start buffering if first character of the swipe (always '%')
                        //console.log('Start the buffering');
                        keyboardBuffer = String.fromCharCode(e.which);
                    } else if ((date.getTime() - lastKeyPress) < 100) {  // continuing the reading into the buffer if the stream of characters is still coming
                        keyboardBuffer += String.fromCharCode(e.which);
                    }
                    // if the character is a line break stop buffering and call postback
                    if (e.which == 13 && keyboardBuffer.length != 0) {
                        if (!swipeProcessing) {
                            $('#hfSwipe').val(keyboardBuffer);
                            keyboardBuffer = '';
                            swipeProcessing = true;
                            __doPostBack('hfSwipe', 'Swipe_Complete');
                        }
                    }
                    // stop the keypress
                    e.preventDefault();
                } else {
                    // if not the swipe panel ignore characters from the swipe
                    if (e.which == 37 || ((date.getTime() - lastKeyPress) < 50)) {
                        //console.log('Swiper... no swiping...');
                        e.preventDefault();
                    }
                }
                lastKeyPress = date.getTime();
            });

            Sys.Application.add_load(function () {
                //
                // Amount entry
                //
                if ($(".js-pnlamountentry").is(":visible")) {
                    // setup digits buttons
                    $('.js-pnlamountentry .tenkey a.digit').on('click', function () {
                        $amount = $(".input-group.active .form-control");
                        $amount.val($amount.val() + $(this).html());
                        return false;
                    });
                    $('.js-pnlamountentry .tenkey a.clear').on('click', function () {
                        $amount = $(".input-group.active .form-control");
                        $amount.val('');
                        return false;
                    });
                    $('.form-control').on('click', function () {
                        $('.input-group').removeClass("active");
                        $(this).closest('.input-group').addClass("active");
                    });
                }
            });
        </script>

        <Rock:NotificationBox ID="nbBlockConfigErrors" runat="server" NotificationBoxType="Danger" />

        <asp:Panel ID="pnlAmountEntry" runat="server" CssClass="js-pnlamountentry" Visible="false" DefaultButton="lbAmountEntryNext">
            <header>
                <h1>Enter An Amount</h1>
            </header>
            
            <main>
                <div class="row">
                    <div class="col-md-8">
                        <Rock:NotificationBox ID="nbAmountEntry" runat="server" NotificationBoxType="Warning"></Rock:NotificationBox>

                        <asp:Label ID="lblPayingAs" runat="server"></asp:Label>

                        <div class="form-group margin-t-md">
                            <Rock:CurrencyBox ID="tbAmount" runat="server" CssClass="input-amount active" />
                        </div>

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
                                <a href="#" class="btn btn-default btn-lg digit">.</a>
                                <a href="#" class="btn btn-default btn-lg digit">0</a>
                                <a href="#" class="btn btn-default btn-lg command clear">Clear</a>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-4">
                        <asp:LinkButton ID="lbAmountEntryNext" runat="server" OnClick="lbAmountEntryNext_Click" CssClass="btn btn-primary btn-kiosk btn-kiosk-lg">Next</asp:LinkButton>
                    </div>
                </div>
                
            </main>
            
            <footer>
                <div class="container">
                    <asp:LinkButton ID="lbAmountEntryCancel" runat="server" OnClick="lbAmountEntryCancel_Click" CssClass="btn btn-default btn-kiosk">Cancel</asp:LinkButton>
                </div>
            </footer>
        </asp:Panel>

        <asp:Panel ID="pnlSwipe" CssClass="js-swipe" runat="server" Visible="false">
            <header>
                <h1>Please Swipe Your Card</h1>
            </header>

            <main>
                <asp:Literal id="lSwipeErrors" runat="server" />

                <Rock:NotificationBox ID="nbSwipeAmount" runat="server" NotificationBoxType="Info"></Rock:NotificationBox>

                <div class="swipe">
                    <div class="swipe-cards">
                        <i class="fa fa-cc-visa fa-2x"></i>
                        <i class="fa fa-cc-mastercard fa-2x"></i>
                        <i class="fa fa-cc-amex fa-2x"></i>
                        <i class="fa fa-cc-discover fa-2x"></i>
                    </div>
                    <asp:Image ID="imgSwipe" runat="server" ImageUrl="~/Assets/Images/Kiosk/card_swipe.png" />
                </div>
            </main>
            
            <footer>
                <div class="container">
                    <asp:LinkButton ID="lbSwipeBack" runat="server" OnClick="lbSwipeBack_Click" CssClass="btn btn-default btn-kiosk">Back</asp:LinkButton>
                    <asp:LinkButton ID="lbSwipeCancel" runat="server" OnClick="lbSwipeCancel_Click" CssClass="btn btn-default btn-kiosk">Cancel</asp:LinkButton>
                </div>
            </footer>
        </asp:Panel>

        <asp:Panel ID="pnlReceipt" runat="server" ClientIDMode="Static" Visible="false">
            <header>
                <h1>Thank You!</h1>
            </header>

            <main>
                <div class="row">
                    <div class="col-md-8">
                        <asp:Literal id="lReceiptContent" runat="server" />
                    </div>
                    <div class="col-md-4">
                        <asp:LinkButton id="lbReceiptDone" runat="server" OnClick="lbReceiptDone_Click" CssClass="btn btn-primary btn-kiosk btn-kiosk-lg">Done</asp:LinkButton>
                    </div>
                </div>
            </main>
        </asp:Panel>
    </ContentTemplate>
</asp:UpdatePanel>