# Isometric Tilemap Setup

1. Open `SampleScene`.
2. Run `Tools > Fortress > Paint Ready > Ground`.
3. Unity opens Tile Palette and sets `GroundTilemap` as paint target.
4. If the palette is empty, drag these tile assets into it:
   - `Assets/Tiles/Ground_Auto.asset`
   - `Assets/Tiles/Path_Auto.asset`
5. In Tile Palette, click one tile first, then select the Brush tool.
6. Paint in the `Scene` view (not in `Game` view).
7. Run `Tools > Fortress > Paint Ready > Path` when you want to paint the path layer.
8. Use `Tools > Fortress > Paint Target > Props` for decorations.

Notes:
- Keep all tile sprites with same Pixels Per Unit.
- Use Point filter for pixel-art look.
- If paint does nothing, rerun `Tools > Fortress > Paint Ready > Ground` and try a single click near world origin.
