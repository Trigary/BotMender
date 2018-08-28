# Packet documentation

A place where all of the packets are explained.

# UDP

 - All UDP packet's identifiers follow the following format: "Sender_Name"
 - The "Sender" can be "Client" or "Server" depending on which party is allowed to send the packet.
 - The "Name" is the name of the packet, it must make the packet identifier unique.

**Client_InputUpdate**  
6 bits: rightward | leftward | upward | downward | forward | backward + TrackedPosition  
The client informs the server of its current inputs.  
Each of the first 6 bits represents whether the specified movement direction axis is pressed or not.  
The following vector specifies the position towards which the weapons are rotating.

**Server_StateUpdate**  
6Bytes-Timestamp, BotId, Client_InputUpdate, Position, Rotation, Velocity, AngularVelocity  
The server informs the client of X many bots' states at the timestamp.

# TCP

 - All TCP packets' identifiers follow the following format: "Sender_Category_[Subcategories]_Name"
 - The "Sender" can be "Client", "Server" or "Both" depending on which parties are allowed to send the packet.
 - The "Category" and the optional "Subcategories" are one/multiple of the categories present in this documentation.
 - The "Name" is the name of the packet, it must make the packet identifier unique.

## State

**Server_State_Joined**  
The IDs of the X players who joined.  
X will be 1 when a new player joins and the local client is already connected.  
X will be bigger than one if the local client just connected and there are other, already connected clients.

**Server_State_Left**  
ID of the player who left.

## System

**Client_System_StartFiring**  
The client informs the server that it would like to fire its weapon repeatedly until otherwise specified.

**Client_System_StopFiring**  
The client informs the server that it would like to stop firing its weapons.

**Server_System_FireWeapon**  
BotId, SystemId, unspecified (specified by weapon type)  
The server tells the client to fire the specified weapon.
