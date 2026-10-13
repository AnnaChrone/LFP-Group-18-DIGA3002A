================================================================
  TINY QUESTERS  —  Free NPC Pack (2 Villagers)
  2 top-down pixel-art town & village NPCs from
  "Tiny Questers — Complete Pixel RPG Asset Pack" by Bobddadoo
  (https://bobddadoo.itch.io)
================================================================

Thanks for downloading! This free pack brings your towns and
villages to life with 2 fully-animated villagers, each with idle
and walk animations in all 4 directions. Use them in your
personal and commercial games, free of charge.

      THE 2 VILLAGERS
        Village Man  ·  Female Villager

  * * *  UPDATE — SIDE-FACING IDLE  * * *
  Both villagers now have hand-drawn idle_left and idle_right
  clips (8 frames each), so they face left/right while standing
  still. Each new side idle ships in TWO timing versions:
    smooth  — the full 8 frames at 75 ms/frame
    classic — half the frames (4) at 150 ms/frame, same total
              speed, a chunkier retro cadence
  Files are named <clip> (smooth) and <clip>_classic.

These NPCs share the same 64 x 64 size and feet-center pivot as
the Tiny Questers hero classes (Warrior, Mage, ...), so heroes
and townsfolk drop into the same animator setup and line up on
the same tile grid.

Like these two? The full "Tiny Questers — 8 NPC Pack" adds a
Blacksmith, Guard, Old Man, Town Girl, Merchant and Barmaid
(plus bonus shop signs & icons) on the same itch.io page.

Follow Bobddadoo on itch.io to be notified when more NPCs and
hero classes drop, and to get launch-week discounts.


----------------------------------------------------------------
  WHAT'S INSIDE  (2 NPCs, 8 base clips + side idles = 20 clips)
----------------------------------------------------------------

  Every NPC ships these clips (64 x 64, shared feet-center pivot):
    idle   — down / up (back view) / left / right
    walk   — down / up (back view) / left / right

  The side idles (idle_left / idle_right) are the new additions
  and come in smooth + classic timing (see UPDATE note above), so
  each is present twice: idle_left / idle_left_classic and
  idle_right / idle_right_classic.

  Each clip is provided as: individual PNG frames (single/),
  a horizontal sprite sheet (atlas/), POT-padded copies of both
  (single_pot/, atlas_pot/), and preview GIFs at 1x and 5x.


----------------------------------------------------------------
  FOLDER STRUCTURE
----------------------------------------------------------------

npc_pack_free/
  README.txt              ... this file
  LICENSE.txt             ... license terms (please read)

  png/
    single/               ... individual PNG frames
      <npc>/              ... village_man, female_villager
        idle_down/        ...   idle_down1.png  ~ ...
        idle_up/          ...   idle_up1.png    ~ ...   (back view)
        idle_left/        ...   idle_left1.png  ~ 8     (smooth, 75 ms)
        idle_left_classic/...   idle_left_classic1.png ~ 4  (150 ms)
        idle_right/       ...   idle_right1.png ~ 8     (smooth, 75 ms)
        idle_right_classic/..   idle_right_classic1.png ~ 4 (150 ms)
        walk_down/        ...   walk_down1.png  ~ ...
        walk_up/          ...   walk_up1.png    ~ ...   (back view)
        walk_left/        ...   walk_left1.png  ~ ...
        walk_right/       ...   walk_right1.png ~ ...

    atlas/                ... horizontal sprite sheets, 64 px/frame
                              one PNG per clip, named <npc>_<clip>.png
      village_man_idle_down.png       ...  5 frames  (320 x 64)
      village_man_idle_up.png         ...  4 frames  (256 x 64)
      village_man_walk_down.png       ...  4 frames  (256 x 64)
      village_man_walk_up.png         ...  4 frames  (256 x 64)
      village_man_walk_left.png       ...  6 frames  (384 x 64)
      village_man_walk_right.png      ...  6 frames  (384 x 64)
      female_villager_idle_down.png   ...  5 frames  (320 x 64)
      female_villager_idle_up.png     ...  4 frames  (256 x 64)
      female_villager_walk_down.png   ...  4 frames  (256 x 64)
      female_villager_walk_up.png     ...  4 frames  (256 x 64)
      female_villager_walk_left.png   ...  4 frames  (256 x 64)
      female_villager_walk_right.png  ...  4 frames  (256 x 64)

    single_pot/           ... power-of-two padded versions of single/
                              (original art at top-left (0,0),
                              transparent padding on the right/bottom —
                              pivot and per-frame coordinates unchanged)
      (same folder layout and filenames as single/)
    atlas_pot/            ... power-of-two padded versions of atlas/
                              (original sheet at top-left (0,0),
                              transparent padding on the right/bottom —
                              slice with the SAME 64 px/frame as atlas/)

  gif/                    ... preview GIFs, transparent background
    1x/                   ... original size
    5x/                   ... 5x upscaled, nearest-neighbor
                              one GIF per clip (20 total each) — for
                              quick visual reference and for store /
                              itch.io page screenshots. Side-idle GIFs
                              come as <clip> (smooth) and
                              <clip>_classic.


----------------------------------------------------------------
  ANIMATION FRAMES  (per NPC: Idle Down / Up, Walk Down / Up / Left / Right)
----------------------------------------------------------------

  Village Man ....... 5 / 4 / 4 / 4 / 6 / 6
  Female Villager ... 5 / 4 / 4 / 4 / 4 / 4

  All frames use a consistent 64 x 64 size and a shared pivot
  (feet center), so every NPC stands on the same ground line and
  you can swap clips at runtime without the sprite shifting.
  Left and Right are both hand-drawn (not mirrored), so each
  facing reads naturally.

  Side idles (idle_left / idle_right): 8 frames each, hand-drawn
  per facing. Smooth runs the full 8 frames at 75 ms; classic
  uses 4 frames at 150 ms for the same loop speed with a chunkier
  cadence.

  Up (idle_up / walk_up) is the back view — the NPC seen from
  behind, walking away from the camera.

  Both individual frames (png/single/) and horizontal sprite
  sheets (png/atlas/) are included — pick whichever your engine
  prefers. Power-of-two padded copies are in png/single_pot/
  and png/atlas_pot/ for engines / GPUs that require POT
  textures. The original art sits at top-left (0,0) with
  transparent padding on the right/bottom, so per-frame pixel
  sizes and the pivot are identical to the non-POT versions.

  Engine-agnostic — works in Unity, Godot, GameMaker, Construct,
  RPG Maker, and more.


----------------------------------------------------------------
  HOW TO USE
----------------------------------------------------------------

  Unity:
    1. Drop png/single/ or png/atlas/ into Assets/.
    2. Set Texture Type = Sprite (2D and UI).
    3. Set Filter Mode = Point (no filter) and Compression = None
       to keep the pixel-art look crisp.
    4a. Single frames: select all PNGs of one clip (e.g. one
        npc's walk_down/) and drag into the scene to auto-create
        an Animation clip.
    4b. Atlas: set Sprite Mode = Multiple, open Sprite Editor,
        Slice -> Grid By Cell Size, 64 x 64. For atlas_pot/ use
        the SAME 64 px/frame — extra empty cells at the right
        edge are transparent and can be left unused or deleted.

  Godot:
    1. Import png/single/ (or atlas/) into your project.
    2. In the Import dock set Filter = Off and Mipmaps = Off.
    3. Use AnimatedSprite2D / AnimationPlayer — single frames as
       separate textures, or atlas via AtlasTexture / Region.

  General:
    - Loop every clip: idle_*, walk_* are all seamless loops.
    - 4-direction movement: drive walk_down / up / left / right
      from the move vector, fall back to the matching idle_* when
      the NPC stops.
    - Timing: ~6.67 fps matches the preview GIFs.


----------------------------------------------------------------
  LICENSE — SHORT VERSION  (full text in LICENSE.txt)
----------------------------------------------------------------

  [OK] Free for unlimited personal and commercial projects
  [OK] Credit appreciated but not required
  [OK] Modify and edit freely to fit your game
  [NO] Do not resell or redistribute the assets on their own,
       or as part of another asset pack
  [NO] Do not use these assets to train AI / machine-learning
       models

  In short: use it in as many of your games as you like — just
  don't repackage and sell the art itself.


----------------------------------------------------------------
  TINY QUESTERS — THE HERO CLASSES
----------------------------------------------------------------

  These NPCs are built to share a town with the Tiny Questers
  hero classes — same pivot, same 64 x 64 grid, same art style.

    Warrior      — sword fighter (free sample available now)
    Mage         — staff-wielding spellcaster (out now)
    Knight       — heavy-armored sword & shield, the tank
    Priestess    — holy support, healing & buffs
    Bard         — lute-armed traveler, party utility
    Dark Mage    — forbidden magic, shadow & curse attacks
    Rogue        — hooded daggerman, fast and lethal
    Elf Archer   — bow-wielding ranger, long-range DPS

  Grab the free Warrior sample on the same itch.io page to test
  the import workflow, and follow Bobddadoo to be notified the
  moment new classes and NPCs drop — followers get launch-week
  discounts.


----------------------------------------------------------------
  FEEDBACK
----------------------------------------------------------------

  Spotted a misaligned frame or want a specific NPC added?
  Leave a comment on the itch.io page — feedback directly shapes
  "Tiny Questers — Complete Pixel RPG Asset Pack".

  Thanks for your support — happy dev!  :)


================================================================
  (c) Bobddadoo — https://bobddadoo.itch.io
================================================================
