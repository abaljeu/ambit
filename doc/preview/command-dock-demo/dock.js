(function () {
    "use strict";

    var SPRITE = "#";

    var ICONS = {
        "Undo": "amb-icon-undo",
        "Redo": "amb-icon-redo",
        "Zoom out": "amb-icon-zoom-out",
        "Zoom in": "amb-icon-zoom-in",
        "Find": "amb-icon-find",
        "Jump to Target": "amb-icon-jump",
        "Move Up": "amb-icon-move-up",
        "Move Down": "amb-icon-move-down",
        "Selection up": "amb-icon-sel-up",
        "Selection down": "amb-icon-sel-down",
        "Selection left": "amb-icon-sel-left",
        "Selection right": "amb-icon-sel-right",
        "Indent": "amb-icon-move-right",
        "Outdent": "amb-icon-move-left",
        "Move Selection to Start": "amb-icon-move-to-start",
        "Move Selection to End": "amb-icon-move-to-end",
        "Select to Start": "amb-icon-sel-to-start",
        "Select to End": "amb-icon-sel-to-end",
        "Move Selected": "amb-icon-move-selected",
        "Command palette": "amb-icon-palette",
        "Copy content": "amb-icon-copy",
        "Duplicate (link)": "amb-icon-duplicate",
        "Edit classes": "amb-icon-edit-classes"
    };

    var TRIGGER_ICONS = {
        move: "amb-icon-move-tools",
        select: "amb-icon-select-tools",
        more: "amb-icon-more"
    };

    var BASE = [
        { type: "cmd", name: "Undo" },
        { type: "cmd", name: "Redo" },
        { type: "cmd", name: "Zoom out" },
        { type: "cmd", name: "Zoom in" },
        { type: "trigger", id: "move", label: "Move tools" },
        { type: "trigger", id: "select", label: "Select tools" },
        { type: "cmd", name: "Find" },
        { type: "cmd", name: "Jump to Target" },
        { type: "trigger", id: "more", label: "More commands" }
    ];

    var MOVE = [
        { type: "close" },
        { type: "cmd", name: "Move Up" },
        { type: "cmd", name: "Move Down" },
        { type: "cmd", name: "Outdent" },
        { type: "cmd", name: "Indent" },
        { type: "cmd", name: "Move Selection to Start" },
        { type: "cmd", name: "Move Selection to End" },
        { type: "cmd", name: "Move Selected", inactive: true }
    ];

    var SELECT = [
        { type: "close" },
        { type: "cmd", name: "Selection up" },
        { type: "cmd", name: "Selection down" },
        { type: "cmd", name: "Selection left" },
        { type: "cmd", name: "Selection right" },
        { type: "cmd", name: "Select to Start" },
        { type: "cmd", name: "Select to End" }
    ];

    var MORE = [
        { type: "close" },
        { type: "cmd", name: "Command palette" },
        { type: "cmd", name: "Copy content" },
        { type: "cmd", name: "Duplicate (link)" },
        { type: "cmd", name: "Edit classes" }
    ];

    var state = {
        moveOpen: false,
        selectOpen: false,
        moreOpen: false
    };

    function iconSvg(iconId) {
        var svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        svg.setAttribute("class", "amb-dock-icon");
        svg.setAttribute("aria-hidden", "true");
        var use = document.createElementNS("http://www.w3.org/2000/svg", "use");
        use.setAttribute("href", SPRITE + iconId);
        svg.appendChild(use);
        return svg;
    }

    function makeButton(label, iconId, extraClasses) {
        var btn = document.createElement("button");
        btn.type = "button";
        btn.className = "amb-dock-glyph";
        btn.title = label;
        btn.setAttribute("aria-label", label);
        btn.appendChild(iconSvg(iconId));
        extraClasses.forEach(function (c) {
            if (c) btn.classList.add(c);
        });
        return btn;
    }

    function dockClass(surface) {
        if (surface === "move") return "amb-dock amb-dock-move";
        if (surface === "select") return "amb-dock amb-dock-select";
        if (surface === "more") return "amb-dock amb-dock-more";
        return "amb-dock amb-dock-base";
    }

    function triggerClasses(id) {
        var open = (id === "move" && state.moveOpen)
            || (id === "select" && state.selectOpen)
            || (id === "more" && state.moreOpen);
        if (!open) return [];
        if (id === "move") return ["amb-dock-trigger-open", "amb-dock-trigger-move"];
        if (id === "select") return ["amb-dock-trigger-open", "amb-dock-trigger-select"];
        return ["amb-dock-trigger-open", "amb-dock-trigger-more"];
    }

    function closePanels() {
        state.moveOpen = false;
        state.selectOpen = false;
        state.moreOpen = false;
    }

    function renderRow(surface, slots, onClose) {
        var row = document.createElement("div");
        row.className = dockClass(surface);

        slots.forEach(function (slot) {
            if (slot.type === "close") {
                var closeBtn = makeButton("Close", "amb-icon-close", ["amb-dock-close"]);
                closeBtn.addEventListener("click", onClose);
                row.appendChild(closeBtn);
                return;
            }
            if (slot.type === "trigger") {
                var tBtn = makeButton(
                    slot.label,
                    TRIGGER_ICONS[slot.id],
                    triggerClasses(slot.id));
                tBtn.addEventListener("click", function () {
                    if (slot.id === "move") {
                        state.moreOpen = false;
                        state.moveOpen = !state.moveOpen;
                        if (state.moveOpen) state.selectOpen = false;
                    } else if (slot.id === "select") {
                        state.moreOpen = false;
                        state.selectOpen = !state.selectOpen;
                        if (state.selectOpen) state.moveOpen = false;
                    } else {
                        state.moveOpen = false;
                        state.selectOpen = false;
                        state.moreOpen = !state.moreOpen;
                    }
                    render();
                });
                row.appendChild(tBtn);
                return;
            }
            var iconId = ICONS[slot.name];
            var cmdBtn = makeButton(slot.name, iconId, []);
            if (slot.inactive) cmdBtn.classList.add("amb-inactive");
            cmdBtn.addEventListener("click", function () { /* no-op */ });
            row.appendChild(cmdBtn);
        });

        return row;
    }

    function render() {
        var container = document.querySelector(".amb-command-buttons");
        container.innerHTML = "";

        container.appendChild(renderRow("base", BASE, closePanels));

        if (state.moveOpen) {
            container.appendChild(renderRow("move", MOVE, function () {
                state.moveOpen = false;
                render();
            }));
        }
        if (state.selectOpen) {
            container.appendChild(renderRow("select", SELECT, function () {
                state.selectOpen = false;
                render();
            }));
        }

        var overlay = document.createElement("div");
        overlay.className = "amb-dock-more-overlay";
        if (state.moreOpen) overlay.classList.add("amb-dock-more-open");
        overlay.appendChild(renderRow("more", MORE, function () {
            state.moreOpen = false;
            render();
        }));
        container.appendChild(overlay);
    }

    render();
})();
