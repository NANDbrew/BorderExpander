# Border Expander
- Expands the world border to include all islands, plus a padding value (6 degrees for now). This is really only useful for future modded islands.
- Doesn't shrink the border at all, so the eastern edge (out past Chronos) is still out at 32°E.
- Dynamically adjusts sunrise and sunset to work better at a greater range of latitudes.
- Option for "free sailing", which moves the north/south borders to 70/-70 and enables east/west circumnavigation.
- Option to make the sun compass work south of the equator by locking the gnommon and dial, requiring manual compass alignment.
- Makes the border friendlier by displaying a non-blocking notification the border and waking sleeping players outside the border. (technically the notification appears at the border and recovery happens 1° past)
- Support for custom trade wind regions (to be added by other mods).

## Modder info
- add new trade wind regions with `BorderExpander.AddRegion(TradeWindRegion region)`
- the `TradeWindRegion` class uses Unity's `Rect` for borders
  - also overrides the vanilla trade wind influence per-region, so you can make your custom regions more or less consistent than vanilla
  - examples here: https://github.com/NANDbrew/BorderExpander/blob/master/WindPatch.cs#L12
    - these are the replacements/overrides of the vanilla regions, plus a couple new southern-hemisphere ones
