# Documentation

A place where some codebase related stuff are explained.

## Adding new blocks

New blocks are specified in the `BlockFactory` class.
Prefabs are retrieved using the `BlockType` enum's name.
Multiblocks' "connect-sides" should be specified so that all "outbound" (read: can connect the multiblock
with another block) connection sides are connected together.
It is required because of how the "are these block groups connected" algorithm works.
If the block comes with a system, it has to be specified in the `SystemFactory` class.
Weapon and active systems must also be specified in the `GetWeaponType` and `IsActiveSystem` methods respectively.
Weapons must also have a child `GameObject` named "Turret" attached to them.
This turret mustn't have its own collider.
Constants which are specific to a block can be registered in the `SystemConstantsContainer` class for later retrieval.
