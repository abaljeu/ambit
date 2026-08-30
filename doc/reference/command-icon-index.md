# Command dock icon index

Maps our SVG symbol ids (in [[src/Server/wwwroot/command-dock.svg]]) to Lucide source icons in [[other/lucide/icons/]]. Regenerate Lucide symbols with [[other/fetch-command-dock-sprite.sh]] after changing mappings; custom symbols are copied from [[doc/preview/command-dock-demo/index.html]].

Lucide package: `lucide-static` (see [[other/fetch-lucide.sh]]). Browse originals at [lucide.dev](https://lucide.dev/icons/).

| F# name (`CommandIcons.fs`) | SVG symbol id | Lucide icon | Used for |
| --- | --- | --- | --- |
| `undo` | `amb-icon-undo` | `undo-2` | Undo |
| `redo` | `amb-icon-redo` | `redo-2` | Redo |
| `zoomOut` | `amb-icon-zoom-out` | `zoom-out` | Zoom out |
| `zoomIn` | `amb-icon-zoom-in` | `zoom-in` | Zoom in |
| `moveTools` | `amb-icon-move-tools` | *(custom)* | Move tools trigger |
| `selectTools` | `amb-icon-select-tools` | *(custom)* | Select tools trigger |
| `find` | `amb-icon-find` | `search` | Find |
| `jump` | `amb-icon-jump` | `link-external` | Jump to Target |
| `more` | `amb-icon-more` | `ellipsis` | More commands trigger |
| `close` | `amb-icon-close` | `x` | Close sub-panels |
| `selUp` | `amb-icon-sel-up` | *(custom)* | Selection up |
| `selDown` | `amb-icon-sel-down` | *(custom)* | Selection down |
| `selLeft` | `amb-icon-sel-left` | *(custom)* | Selection left |
| `selRight` | `amb-icon-sel-right` | *(custom)* | Selection right |
| `moveUp` | `amb-icon-move-up` | *(custom)* | Move Up |
| `moveDown` | `amb-icon-move-down` | *(custom)* | Move Down |
| `moveLeft` | `amb-icon-move-left` | *(custom)* | Outdent |
| `moveRight` | `amb-icon-move-right` | *(custom)* | Indent |
| `moveToStart` | `amb-icon-move-to-start` | *(custom)* | Move Selection to Start |
| `moveToEnd` | `amb-icon-move-to-end` | *(custom)* | Move Selection to End |
| `selToStart` | `amb-icon-sel-to-start` | *(custom)* | Select to Start |
| `selToEnd` | `amb-icon-sel-to-end` | *(custom)* | Select to End |
| `palette` | `amb-icon-palette` | `command` | Command palette |
| `copy` | `amb-icon-copy` | `copy` | Copy content |
| `duplicate` | `amb-icon-duplicate` | `copy-plus` | Duplicate (link) |
| `editClasses` | `amb-icon-edit-classes` | `tags` | Edit classes |
| `moveSelected` | `amb-icon-move-selected` | `move` | Move Selected |
| `rename` | `amb-icon-rename` | `pencil` | Rename |
| `run` | `amb-icon-run` | `play` | Run |

To swap a Lucide icon: pick a name from `other/lucide/icons/{name}.svg`, update the pair in `other/fetch-command-dock-sprite.sh`, run the script, then merge custom symbols from [[doc/preview/command-dock-demo/index.html]] and update this table. Direction prototypes (hollow = select, solid = move) and tool triggers are custom throughout.
