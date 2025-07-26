- Don't add useless comments explaing what the code does.  Comments should explain why the code is doing something, not what it is doing, and even then only sparingly.
- Everything in the "server" folder relates to SpacetimeDB
- Everythign in the "client" folder relates to Unity game engine

## server
- There is an instructions file at https://spacetimedb.com/llms.txt to explain how it works.
- You should run the VSCode "Format Document" command after making changes.
- Tables, Types and Reducers should all be created in individual files in the respective folders.
- Table names should be plural based on the class name.
- When I ask for a new reducer, assume it should be empty unless I specify otherwise.
- You can test if code builds using the `spacetime build` command in the terminal.