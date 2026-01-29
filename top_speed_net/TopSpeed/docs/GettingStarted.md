# Getting Started (audio-only track authoring)

This guide assumes you have read Tracks.md first. It uses the same terms and does not re-explain what shapes, areas, sectors, portals, guides, and branches are. If any term here feels unclear, go back to Tracks.md and then return.

## Where to put your file

Create a plain text file with the extension .tsm. Save it in the Tracks folder that ships with the game or in your custom tracks folder. The game loads all .tsm files at startup. Pick a short, clear filename so you can find it by name in the menu.

## A simple plan you can imagine

We will build a basic rectangle loop. Picture a 200 by 200 meter square. The road is a loop around the outside. The south straight is from X 0 to 200 and Z 0 to 40. The west straight is from X 0 to 40 and Z 40 to 200. The north straight is from X 40 to 200 and Z 200 to 240. The east straight is from X 200 to 240 and Z 40 to 200. The corners are 40 by 40 rectangles that connect those straights.

This layout is large enough for turning, and the numbers are easy to remember. You can change them later once you are comfortable.

## Step 1: Meta section

Start with the [meta] section. Give the track a name and a start position. Use the same coordinate system you will use for the shapes. If you want the player to start on the south straight facing east, set start_x to a point on that straight and set start_z to a value between 0 and 40. Use start_heading=east or a degree value like 90.

## Step 2: Define the shapes

Create one rectangle shape for each straight and each corner. Use clear names, for example south_straight, west_straight, north_straight, east_straight, and corner_sw, corner_nw, corner_ne, corner_se. Each rectangle should touch the next rectangle so there are no gaps. If you leave a gap, the player will hit a boundary and will not be able to move through.

When you define a rectangle, remember that X and Z are the top-left corner. Width extends to the right in X. Height extends downward in Z. If you want a rectangle that starts at X 0, Z 0 and goes east 200 meters and north 40 meters, you would set width=200 and height=40.

## Step 3: Create areas for each shape

For every shape, create an area. Most of these areas should be type=zone because they are drivable. Give them a surface and noise so the audio feels consistent. If you want to hear a different sound on a corner, you can use a different noise value, but keep it simple at first.

If you want a safe zone around the track, you can add another area later. Do not try to do everything in the first pass.

## Step 4: Add sectors to describe behavior

Add a sector for each area. Use type=straight for the straights and type=turn or type=corner for the corners, depending on your naming. The sector is where the game learns how to talk about that space. Without sectors, guidance and branching will not work correctly.

## Step 5: Place portals at corners

For each corner, place an entry portal and an exit portal. The entry portal should sit near the start of the corner, facing the direction the player is traveling before the turn. The exit portal should sit near the end of the corner, facing the direction the player should travel after the turn.

For example, at the south-to-east corner, the entry portal faces east and the exit portal faces north. Use headings in degrees if you prefer exact angles.

## Step 6: Define guides for turn announcements

Create a guide for each corner sector. Link it to the sector, set the entry portal and exit portal, and set a guidance range. This allows the game to announce a turn before the player reaches it. If you want early warnings, increase the guidance range. If you want the beacon to play only close to the turn, use a smaller beacon range or configure the beacon mode.

## Step 7: Add branches where choices exist

If a sector has more than one exit, add a branch. The branch lists the exit portals and can specify a preferred exit. On a simple loop, each corner usually has only one exit, so branches are optional. On a track with intersections, branches become essential so the game can describe options clearly.

## Step 8: Walls and gaps

If you discover that the player can walk into empty space, add a wall or extend the area. Auto-walls are the easiest option. Add auto_walls=true and choose wall_edges on the areas you want to seal. Walls are meant to make boundaries feel intentional. Do not rely on walls to hide mistakes; use them to clarify where the road ends.

## Step 9: Test and iterate

Load the track in exploration mode and walk it. If you hear "track boundary" inside a section, the area is too small or not connected. If the guidance feels late, increase the guidance range. If the beacon feels wrong, check portal headings and guide settings.

## Minimal template you can copy

This is the smallest useful structure for a track file. It is not a full loop, but it shows the structure so you can expand it.

```
[meta]
name=My First Track
start_x=10
start_z=10
start_heading=90

[shape: south_straight]
type=rectangle
x=0
z=0
width=100
height=40

[area: south_area]
type=zone
shape=south_straight
surface=asphalt
noise=nonoise

[sector: south_sector]
type=straight
area=south_area
```
