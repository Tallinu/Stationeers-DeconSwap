# Frame & Wall DeconSwap

Reduce the chance of hull breaches and improve tool usage consistency by swapping the use of the wrench and grinder for deconstructing certain build states of frames, walls, and other structures with similar tool usage. The time to grind open an airtight frame is also slightly increased, from 0.5 to 0.75 second (this can be adjusted/disabled).

Requires **BepInEx** and **StationeersLaunchPad**, see below.

*The changes made by this mod are not retained in your save games, so you can freely try it and remove it.*

This mod ***does not*** modify any tools or components used for ***assembly*** tasks, and I have no plans nor desire to do so.

## Why swap?

"Unwelding" a sealed frame with a wrench is way too easy to do accidentally while working on your plumbing, causing dangerous hull breaches! (You've never had that happen, or nearly happen? Play long enough, and it will.) And a wrench isn't even an appropriate tool for a *weld-cutting* job.

Meanwhile, the initial placement of frames, walls, landing pads, and several others, which you perform with your bare hands, requires a grinder and battery charge to undo -- even though most construction tasks that are reversed by a grinder are *welding operations*.

This mod increases safety and tool usage consistency by editing some build states of structures which have these bizarre tool use patterns. It primarily swaps the use of wrench and grinder around so that welding steps are undone with the grinder, and initial placement stages are picked up just as easily with the wrench. A few edits to other strange tool uses are made; See the end for the full list.

By default, the time required to grind open the final, airtight stage of a frame is also slightly increased to help prevent accidents if you do need to grind something with an important frame behind it. The exact time factor can be configured in the game's Workshop menu. A setting of 100% disables this. Larger values, such as the default of 150%, make it take proportionally longer.

In terms of game balance, you'll use more grinder charge for dismantling fully welded frames, but won't need it for walls, and most other affected structures like landing pads still require the same operations, just in reverse order. Seems close enough to me, and well worth the benefits.

With DeconSwap, dismantling pipes won't vent your base if you misclick, twitch, or hold the button a moment too long, and the tools needed for dismantling may be a little easier to remember.

## Multiplayer

DeconSwap works fine in multiplayer, as long as all parties have the mod installed. It will not let you join a hosted game or server if you have the mod enabled and they don't, or vice versa. (Testing showed that the mismatched tools would prevent you from being able to dismantle any of the modified frame or wall states that require the wrench or grinder.) I'm afraid you'll just have to convince your whole Stationeer team to install it! *wink*

## Installing the mod loader

If you haven't done so already, you'll need to install StationeersLaunchPad and BepInEx in order for plugin mods like this one to work.

Start by going to the home of StationeersLaunchPad on GitHub:
https://github.com/StationeersLaunchPad/StationeersLaunchPad

Scroll down a little to its 'Readme' for full instructions, including the link to BepInEx and instructions for installing that. If you need help figuring out how to do this correctly, I recommend visiting the Stationeers discord for real-time help.
