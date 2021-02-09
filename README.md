# Networking Project
![GIF of player running about another player](https://raw.githubusercontent.com/giodestone/Networking-Project/main/Images/GIF.gif)

Simple Unity game which implements a C# .NET sockets based UDP client-server hybrid model. This was made for my networking module in university.

## Running
[Download](https://github.com/giodestone/Networking-Project/releases)

* W, A, S, D - Move character.
* Space - Shake tree (when next to it)

### Notes on Hosting
The computer firewall of both the host and the client will have to be disabled. Only supports IPv4.

### Unity Version
Made in 2019.2.0f1, launch client/server from the MainMenu scene.

## Architecture
![Picture of Game](https://raw.githubusercontent.com/giodestone/Networking-Project/main/Images/Image1.jpg)

The client server hybrid model was chosen due to the convenience it offers for the player, where they can both be the host and client. Given the implementation of code (implemented as GameObject components), little effort would be required to spin off a dedicated server.

The underlying structure is based off state machine event handlers. For example, when the client receives a `PLAYER_DISCONNECTED` packet, it removes the player (if it hasn't already) and a `DisconnectedPlayerAcknowledgemmentPacket` is sent.

The packets themselves are classes which inherit from `PacketHeader` as to provide easy decoding. The classes are encoded/decoded into/from bytes via `BinaryFormatter`, as directly sending them in binary format is much quicker and smaller than JSON etc.

Basic cheat protections are implemented, for example not allowing a player to affect another player's position as the sender is verified to be.

The player position is interpolated by using velocity.

The [presentation pdf](https://raw.githubusercontent.com/giodestone/Networking-Project/main/Presentation.pdf) is included for further details regarding implementation and improvements.

### Network Events
There are a number of network events that occur. More than one packet class is used for some of these events such as connect.
* Connection
* Position
* Time Sync
* New Player
* New Player Acknowledgement
* Player Disconnected
* Player Disconnected Acknowledgment
