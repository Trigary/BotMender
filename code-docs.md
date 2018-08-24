# Codebase documentation

A place where some programming related things are explained.

## Adding new blocks, systems

New blocks are specified in the `BlockFactory` class.
Prefabs are retrieved using the `BlockType` enum's name.
Multiblocks' "connect-sides" should be specified so that all "outbound"
(read: can connect the multiblock with another block) connection sides are connected together.
It is required because of how the "are these block groups connected" algorithm works.  
If the block comes with a system, it has to be specified in the `SystemFactory` class.
Weapon and active systems must also be specified in the `GetWeaponType` and
`IsActiveSystem` methods respectively.
Weapons must also have a child `GameObject` named "Turret" attached to them.
This turret mustn't have its own collider.
Constants which are specific to a block type can be registered in the
`SystemConstantsContainer` class for later retrieval.

## Bot, block serialization

A bot can be serialized by serializing its blocks, but other pieces of information
(eg. max health) may also get saved in the future for caching reasons.
A placed block can be serialized by serializing its position, rotation and type.
The blocks are serialized in the order of their X,Y,Z coordinates in ascending order.
The following table describes how many bits each serialized piece of data takes and also their order:

Data | Size
---: | :---:
Type | 14
X | 7
Y | 7
Z | 7
Rotation | 5
*Total* | 40

## Networking

An open-source library made by [Trigary](https://github.com/Trigary) named
[DoubleSocket](https://github.com/Trigary/DoubleSocket) is used to handle the networking.
This allows the bypassing of Unity's limited and poorly documented HLAPI and LLAPI.
The library uses a synchronized TCP and UDP socket for each party,
therefore taking advantage of TCP's flow and congestion control while also allowing
UDP packets to be used when reliability and ordered packets are not required.
This library is wrapped in classes found in the `Networking` namespace.
The `public` parts of that namespace can safely be accessed from the main Unity thread.
For information regarding specific packets, please refer to the [packet documentation](packets.md).
