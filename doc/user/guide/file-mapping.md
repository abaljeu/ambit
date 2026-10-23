# File Mapping

Ambit supports mapping any text file to and from outline, and works hard to preserve formatting in the document and differential editing in the outline.
Many file types offer special mapping to make the semantic organization of the file explicit in the outline structure.
Outline Nodes get special css classes to indicate particular file structures, such as a a markdown heading.
Two processes, Parse and Persist, run in the background to keep the outline and files in sync.
Load of a Workspace Node, a Directory Node, or a File Node asks the server to parse. A mapped desktop folder sends its files first.
File nodes will emit all the nodes owned by that file, and sometimes links to nodes owned by other files.
Directory nodes emit to special directory files.  This is often invisible to the user, but it enables organizing information within a directory, while keeping the standard directory structure when transmitting information via a workspace connection.

[x] Edit of a mapped file: that file is written in the background.
[x] Parse of a mapped file: the outline takes the file. That read does not write the file.
