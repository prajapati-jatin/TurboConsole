(function ($, window, console, ace, undefined) {
    $(function () {
        var editor = $("#Editor");
        var terminal = ace.edit('Terminal');
        terminal.setTheme("ace/theme/monokai");
        terminal.session.setMode("ace/mode/csharp");
        terminal.session.setValue(editor.val());
        terminal.session.on("change", function () {
            editor.val(terminal.session.getValue());
        });
        terminal.setOptions({
            fontSize: "13pt"
        });
        console.resizeEditor = function () {
            terminal.resize();
        };

        console.updateRibbon = function () {
            if (!terminal.getReadOnly()) {
                scForm.postRequest("", "", "", "tconsole:scriptchanged(modified=" + !terminal.session.getUndoManager().isClean() + ")");
                registerEventListenersForRibbonButtons();
            }
        };

        function registerEventListenersForRibbonButtons() {
            //console.log('initialize');
            [].forEach.call(document.querySelectorAll('.scRibbonToolbarSmallGalleryButton, .scRibbonToolbarLargeComboButtonBottom'), function (div) {
                div.addEventListener("click", function () {
                    clearTimeout(typingTimer);
                })
            });

            [].forEach.call(document.querySelectorAll('.scRibbonNavigatorButtonsGroupButtons > a'), function (div) {
                div.addEventListener("click", function () {
                    console.updateRibbon();
                })
            });
        };

        var typingTimer = null;

        console.updateRibbonNeeded = function () {
            clearTimeout(typingTimer);
            var timeout = 2000;
            if (document.querySelector('.scGalleryFrame') != null) {
                var timeout = 20;
            }
            typingTimer = setTimeout(console.updateRibbon, timeout);
        };

        $(window).on('resize', function () {
            console.resizeEditor();
        }).trigger('resize');

        console.appendOutput = function (outputToAppend) {
            var decoded = $("<div/>").html(outputToAppend).text();
            $("#ScriptResult").append(decoded);
        };

        window.parent.focus();
        window.focus();

        function setFocusOnConsole() {
            $("body").focus();
            $(terminal).focus();
            ("WebForm_AutoFocus" in this) && WebForm_AutoFocus && WebForm_AutoFocus("Terminal");
        }

        window.addEventListener("focus", function (event) {
            setFocusOnConsole();
        }, false);

        setTimeout(setFocusOnConsole, 1000);

        var posx = $("#PosX");
        var posy = $("#PosY");
        $("#Terminal").on("keyup mousedown", function () {
            var position = terminal.getCursorPosition();
            posx.text(position.column);
            posy.text((position.row + 1));
            console.updateRibbonNeeded();
        }).trigger('keyup');        

        registerEventListenersForRibbonButtons();        
    });
}(jQuery, window, window.console = window.console || {}, window.ace = window.ace || {}));