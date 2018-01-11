<%@ Control Language="C#" AutoEventWireup="true" CodeFile="S2Control.ascx.cs" Inherits="RockWeb.Plugins.com_shepherdchurch.Misc.S2Control" %>

<style>
    .btn-location {
        margin: 24px;
        padding-top: 18px;
    }

    .btn-location > i.fa {
        display: block;
        font-size: 8em;
    }

    .btn-location > span {
        font-size: 2em;
    }
</style>

<asp:Panel ID="pnlJS" runat="server" CssClass="text-center">
</asp:Panel>

<asp:UpdatePanel ID="upContent" runat="server">
    <ContentTemplate>
        <asp:HiddenField ID="hfLocations" runat="server" />

        <script>
            (function () {
                var valid_locations = $('#<%= hfLocations.ClientID %>').val().split('|');
                var timer = null;
                var ajax = null;

                function update_buttons() {
                    if (timer != null) {
                        clearTimeout(timer);
                    }

                    ajax = $.ajax({
                        url: '?command=status',
                        success: function (data) {
                            var $panel = $('#<%= pnlJS.ClientID %>');

                            $panel.empty();

                            if (data && data.locations) {
                                var locations = data.locations['1'];
                                for (var i = 0; i < locations.length; i++) {
                                    if (valid_locations.indexOf(locations[i].name) == -1) {
                                        continue;
                                    }

                                    var $loc = $('<div class="btn btn-lg btn-location"></div>');
                                    $loc.data('id', locations[i].id);
                                    $loc.on('click', toggle_button);

                                    if (locations[i].threatlevel.name == '') {
                                        $loc.addClass('btn-success');
                                        $loc.append('<i class="fa fa-unlock-alt"></i>');
                                    }
                                    else {
                                        $loc.addClass('btn-danger');
                                        $loc.append('<i class="fa fa-lock"></i>');
                                    }

                                    $loc.append('<span> ' + locations[i].name + '</span>');

                                    $panel.append($loc);
                                }
                            }
                        },
                        complete: function (data) {
                            if (timer != null) {
                                clearTimeout(timer);
                            }
                            if (data.statusText != "abort") {
                                timer = setTimeout(update_buttons, 5000);
                            }
                        }
                    });
                }

                function toggle_button() {
                    var cmd = $(this).hasClass('btn-danger') ? 'unlock' : 'lock';

                    clearTimeout(timer);
                    ajax.abort();
                    $(this).find('i.fa').attr('class', 'fa fa-refresh fa-spin')

                    $.ajax({
                        url: '?command=' + cmd + '&id=' + $(this).data('id'),
                        success: function (data) {
                            if (data == false) {
                                alert('Failed to change the threat level.');
                            }
                        },
                        complete: function () {
                            update_buttons();
                        }
                    });
                }

                $(document).ready(update_buttons);
            })();
        </script>
    </ContentTemplate>
</asp:UpdatePanel>
