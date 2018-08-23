# Packet documentation

A place where all of the packets are explained.

# UDP

 - All UDP packet's identifiers follow the following format: "Sender_Name"
 - The "Sender" can be "Client" or "Server" depending on which party is allowed to send the packet.
 - The "Name" is the name of the packet, it must make the packet identifier unique.

**Client_InputUpdate**  
6 bits: rightward | leftward | upward | downward | forward | backward  
+12 bytes: Vector3-TargetPosition  
The client informs the server of its current inputs.  
Each of the first 6 bits represents whether the specified movement direction axis is pressed or not.  
The following vector specifies the position towards which the weapons are rotating.

**Server_StateUpdate**  
X * (53 bytes + size of "Client_InputUpdate"): Byte-BotId, Client_InputDate, Vector3-Position, Quaternion-Rotation, Vector3-Velocity, Vector3-AngularVelocity  
The server informs the client of X many bots' states.

# TCP

 - All TCP packets' identifiers follow the following format: "Sender_Category_[Subcategories]_Name"
 - The "Sender" can be "Client", "Server" or "Both" depending on which parties are allowed to send the packet.
 - The "Category" and the optional "Subcategories" are one/multiple of the categories present in this documentation.
 - The "Name" is the name of the packet, it must make the packet identifier unique.

## Initialization

**Server_State_Joined**  
X bytes: the IDs of the X players who joined.  
X will be 1 when a new player joins and the local client is already connected.  
X will be bigger than one if the local client just connected and there are other, already connected clients.

**Server_State_Left**  
1 byte: the ID of the player who left.
