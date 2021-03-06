﻿function scHorizontalSplitter() {
    this.dragging = false;
}

scHorizontalSplitter.prototype.createOutline = function (bounds, tag) {
    var result = document.createElement("div");
    result.style.border = "2px ridge";
    result.style.position = "absolute";
    result.style.zIndex = "9999";
    result.style.cursor = "col-resizer";
    result.style.font = "1pt tahoma";

    this.bounds.apply(result);

    ctl = document.body;

    if (ctl != null) {
        ctl.appendChild(result);
    }

    return result;
};

scHorizontalSplitter.prototype.mouseDown = function (tag, evt, id) {
    if (!this.dragging) {
        this.bounds = new scRect();
        this.bounds.getControlRect(tag);
        this.bounds.clientToScreen(tag);

        this.trackCursor = new scPoint();
        this.trackCursor.setPoint(evt.screenX, evt.screenY);

        this.dragging = true;
        this.delta = 0;

        scForm.browser.setCapture(tag);
        sc.Form.browser.clearEvent(evt, true, false);
    }
};

scHorizontalSplitter.prototype.mouseMove = function (tag, evt, id) {
    if (this.dragging) {
        if (this.outline == null) {
            this.outline = this.createOutline(this.bounds, tag);
        }

        var dy = evt.screenY - this.trackCursor.y;

        this.bounds.offset(0, dy);

        this.delta += dy;

        this.bounds.apply(this.outline);

        this.trackCursor.setPoint(evt.screenX, evt.screenY);

        scForm.browser.clearEvent(evt, true, false);
    }
}

scHorizontalSplitter.prototype.mouseUp = function (tag, evt, id, target, nopost) {
    if (this.dragging) {
        this.dragging = false;

        scForm.browser.clearEvent(evt, true, false);

        scForm.browser.releaseCapture(tag);

        if (this.outline != null) {
            scForm.browser.removeChild(this.outline);
            this.outline = null;
        }

        var ctl = tag;

        while (ctl != null && ctl.tagName != "TD") {
            ctl = ctl.parentNode;
        }

        if (ctl != null) {
            var prev = scForm.browser.getPreviousSibling(ctl.parentNode).children[0];
            var next = scForm.browser.getNextSibling(ctl.parentNode).children[0];

            var top = prev.offsetHeight;
            var bottom = next.offsetHeight;

            if (target == "top") {
                prev.style.height = top + this.delta - 6 + "px";
            }

            if (target == "bottom") {
                next.style.height = bottom - this.delta + "px";
            }

            if (nopost != "nopost") {
                scForm.postEvent(tag, evt, id + ".Release(\"" + prev.offsetHeight.toString() + "\", \"" + next.offsetHeight.toString() + "\")");
            }
            codeconsole.resizeEditor();
        }
    }
}

scHSplit = new scHorizontalSplitter();