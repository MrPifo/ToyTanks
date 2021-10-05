# Welcome to ToyTanks

This is one of my most ambitious projects I have done yet and I am planning for a full Steam release.

## What is ToyTanks?

The inspiration for my game comes from the original [WiiTanks](https://nintendo.fandom.com/wiki/Tanks!) minigame originally released on the Wii.
My idea is to expand on this fun minigame. That's why I decided to redo this game in Unity in my own personal way and bring in my own ideas.
Currently the project is named **ToyTanks**, but this name doesn't make too 100% happy, so I may change it later.

<img src="https://sperlich.at/assets/project_pictures/toytanks_preview.png" style="width:25vw" />

## Gameplay

The player controls the blue tank and needs to clear multiple levels in multiple worlds. Each world currently contains up to 10 levels, each with a boss level at the end.
Each world also brings new tank variants with it and a unique theme.

<img src="https://sperlich.at/assets/project_pictures/toytanks_1.png" style="width:25vw" />


There are also 4 difficulties available for more challening gameplay.

<img src="https://sperlich.at/assets/project_pictures/toytanks_0.png" style="width:25vw" />


## Graphics

The artstyle is a mix of cartoonish and realistic graphics. It is supposed to look like little toys, but mixed with bits of violence to express more power.

#### Lightmapping

Campaign levels include baked Lightmaps, which are swapped out and applied if a level is loaded. This does not apply to user created levels, since it is not possible to bake at Runtime. To save space and upload time, these Lightmaps won't be uploaded here on GitHub and need therefore to be rebaked if desired.

## Editor

The editor is a tool to build your own levels and make usage of already prexisting level elements. With the same tool I also create the campaign levels.
Currently this is a basic feature which can be expanded in future.

<img src="https://sperlich.at/assets/project_pictures/toytanks_2.png" style="width:25vw" />

## Roadmap

You can find my current Roadmap on [Trello](https://trello.com/b/6AdUI6QP/toytanks) and can get a look at what's coming next! <br><br>
Link: https://trello.com/b/6AdUI6QP/toytanks

## Steam Release

Aiming for my first ever Steam Release I need to make sure this game is as polished as possible. Also, I haven't included any Steam related features yet.

## Roadmap
* Create worlds and levels (There is barely anything to play at the moment)
  * Create more world themes
  * Create more Tank variants and bosses
* Polish and refine UI even more
* Audio (One of my weakest abilities, there is still a lot of missing Audio)
  * Music as well
* Overhaul at some point Pathfinding algorithm (Selfwritten and is very inefficient, but easy to use)
* Better lighting (I have already messed so much with it, but I'm still not 100% happy with it)
* Include Steam features (Cloud saving, Achievements, Workshop at a later point maybey)
  * Online Levels (Upload your levels for other players)
  * Eventually Multiplayer (Low priority)

# Licesning

You are free to download, try and mess with the project for your personal use. It is prohibited to publish, sell and reupload it somewhere else.
You can make a Pull Request and help me to make better code or bring in your own ideas as you like.
Unity Asset store assets are not included.

### Package Dependicies

This Unity Project contains various Asset Store plugins which are not allowed be uploaded here in GitHub.
Here is a list of required packages to make the project work:

* [Audio Manager](https://assetstore.unity.com/packages/tools/audio/audio-manager-cg-149123)
* [Clean UI Pack](https://assetstore.unity.com/packages/2d/gui/modern-and-clean-ui-pack-198475)
* [Coroutine Extensions](https://assetstore.unity.com/packages/tools/utilities/coroutine-extensions-179211)
* [Custom Passes](https://github.com/alelievr/HDRP-Custom-Passes)
* [DOTween](https://assetstore.unity.com/packages/tools/animation/dotween-hotween-v2-27676)
* [Effect Examples](https://assetstore.unity.com/packages/essentials/asset-packs/unity-particle-pack-5-x-73777)
* [Feel](https://assetstore.unity.com/packages/tools/particles-effects/feel-183370)
* [IniFileParser](https://github.com/rickyah/ini-parser)
* [Shapes](https://assetstore.unity.com/packages/tools/particles-effects/shapes-173167)
* [Unity UI Extensions](https://github.com/JohannesDeml/unity-ui-extensions)
* [GG Camera Shake](https://github.com/gasgiant/Camera-Shake)
* [Unity Command Terminal](https://github.com/stillwwater/command_terminal)
* TextMeshPro

<img src="https://sperlich.at/assets/project_pictures/toytanks_extensions.png" style="width:25vw" />
