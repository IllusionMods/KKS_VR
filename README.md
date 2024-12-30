# KKS_VR - VR Plugin for Koikatsu and Koikatsu Sunshine
A BepInEx plugin for Koikatsu (KK) and Koikatsu Sunshine (KKS) that allows you to play the main game and studio (Sunshine only) in VR. 
The difference from the official VR modules is that you have access to the full game/studio, while the official modules have limited features and spotty mod support.

Currently only the standing (aka room-scale) mode is fully supported.

The main game part is a fork/port of the KoikatuVR/KK_MainGameVR plugin developed by mosirnik, vrhth, KoikatsuVrThrowaway and Ooetksh.

The studio part is a fork of the [KKS_CharaStudioVR](https://vr-erogamer.com/archives/1065) plugin.

## Prerequisites

* Koikatsu or Koikatsu Sunshine
* Latest version of BepInEx 5.x and KKSAPI/ModdingAPI
* SteamVR
* A VR headset supported by SteamVR
* VR controllers


## Installation

1. Make sure BepInEx, KKSAPI and all their dependencies have been installed.
2. Download the latest [release](https://github.com/IllusionMods/KKS_VR/releases) for the corresponding game.
3. Extract the zip into the game folder (where the abdata and BepInEx folders are).
4. Create a shortcut to Koikatu.exe and/or KoikatsuSunshine.exe and/or CharaStudio.exe, and add `--vr` to the command line.

The game (not the studio) also can be launched without any added arguments if SteamVR is running.

## Tips
 * Be advised to set InterPupillary Distance (IPD) in the settings to change the scale of the world according to own taste and used hardware.
 * If VR mode doesn't launch, make sure that neither of controllers is asleep during game launch.*

## Controls Game
### Overview

There are two controllers for each hand with identical functional without any tools or modes.  
There is no input customization or helping texts. Designed to be able to do any action with a single hand.  
The only means of movement are native in-game functions and *GripMove*, no *Warp*.  
No double clicks, only *Short* or *LongPress* for buttons and *DirectionalInput* for *Touchpad*. The sole function of *Menu* button is to toggle the floating menu's visibility.

The plugin assumes that VR controller has:
* **Grip** used as a **Grab** button. Grabs things to move them around.  
* **Trigger** used as an **Action** button. Performs actions where applicable or completes their wait period if one is already queued but not yet determined whether it's a *Short* or *LongPress*.  
* **Touchpad** aka *Thumbstick* aka *Joystick* used as a **Generic** button. Never requires a click in non-centered positions.

### Modules and their inputs:

### GripMove  
Grab the world and move around oneself.  
Available in **Any Scene** as the last priority action i.e. when no better actions are available.   
* **Grip** to start *GripMove*.  
* **Trigger** while in *GripMove* to manipulate **Yaw** while using controller as an axis.  
* **Touchpad** while in *GripMove* with pressed *Trigger* to manipulate **Rotation** of the camera.  
* **Touchpad** while in *GripMove* without pressed *Trigger* to become **Upright**. Registers after *LongPress*.

Has settings for stabilization. Depending on the context may behave differently.  

### Impersonation aka PoV
Assume orientation of a character head and follow it loosely.  
Available in **H Scene** outside of character interactions.  
* **Touchpad** to start, stop, change or reset *Impersonation*. Registers after *LongPress*.  
* **Touchpad** while in *Impersonation* and in *GripMove* with pressed *Trigger* to set custom offset.  

Has settings for gender preferences and automatization.

### Assisted kiss/lick
Attach the camera to a partner's PoI to follow it.  
Available in **H Scene** when the camera is in direct proximity to the said PoI. Outside of the caress positions requires *GripMove*.  
* **Grip** while *Assisted* to start altered version of *GripMove* to acquire precise offsets on the fly. The long gap between the camera and the PoI will cause disengagement.
* **Trigger** while *Assisted* and not in *GripMove* to stop the action and disengage.

Has plenty of settings for customization. 

### Controller representation
Native in-game items serving as the controller representation.  
Available in **Any Scene** as the last priority action i.e. when no better actions are available.  
They won't go inside of things easily, preferring instead to stick to the surface.
* **Touchpad** with pressed *Trigger* to cycle available items.
* **DirectionHorizontal** with pressed *Trigger* to cycle through item animations.

### IK Manipulator aka Grasp
Alter currently playing animation on the fly.  
Available in **H, Talk and Text Scenes** when interacting with a character i.e. controller is in close proximity to it.  
* **Grip** to start *Grasp* i.e. hold relevant bodyParts and reposition them with the controller movements.
* **Trigger** while in *Grasp* and the visual cue of the held bodyPart is green to attach it.  
  Currently only to self/different character or controller. ~~Hand holding.~~
* **Trigger** while in *Grasp* to extend the amount of held bodyParts, up to the whole character. Registers after *ShortPress*.
* **Trigger** while in *Grasp* to extend the amount of held bodyParts temporarily. Registers after *LongPress*.
* **Touchpad** while in *Grasp* to reset currently held bodyParts to default offsets.
* **Touchpad** while not in *Grasp* to reset relevant bodyPart to the default offset. Registers after *LongPress*.
* **Touchpad** while not in *Grasp* but in *Impersonation* to start or stop the synchronization of a relevant bodyPart with the controller. Registers after *LongPress*.
* **DirectionHorizontal** while in *Grasp* and the main held bodyPart is the hand to scroll through it's animations. Goes full circle then resets to the animation's default.
* **DirectionHorizontal** while in *Grasp* and holding the whole character to change *Yaw*.
* **DirectionVertical** while in *Grasp* and holding the whole character to move in direction of the camera.
* **DirectionVertical** while in *Grasp* to Show/Hide guide objects of held bodyParts. Temporarily overrides setting.

Setting *Maintain limb orientation* changes drastically behavior of arms.

### Menu interaction
Available in **Any Scene** when aiming controller at the floating in-game *Menu*.
* **Grip** to grab *Menu*.
* **Touchpad** while holding *Menu* with pressed *Trigger* to abandon it in the world.
* **DirectionHorizontal** while holding *Menu* to change it's size.
* **DirectionVertical** while holding *Menu* to move it in controller direction.

### H Interpreter
Available in **H Scene**, relies heavily on [SensibleH](https://github.com/lotsofbears/KK_SensibleH), without it many functions will be unavailable.  
Described horizonal directions assume the right controller, for the left controller the directions will be mirrored.

#### Generic
* **DirectionLeft** to choose random position from the current category. Registers after *LongPress*. Add *Trigger* for any available position.
* **DirectionRight** to enter *PointMove*. Registers after *LongPress*.
* **DirectionVertical** on partner's bodyPart to (un)dress it.

#### PointMove
* **DirectionLeft** to exit *PointMove*.
* **DirectionRight** to choose one at random and exit *PointMove*. Registers after *LongPress*.
* **DirectionVertical** to scroll through available categories.

#### Caress
*AutoCaress* can be overtaken in any way by *Assisted kiss/lick*.  
* **Grip** on attached caress item while in *AutoCaress* to take the manual control.
* **Trigger** on attached caress item to start *AutoCaress*. Registers after *LongPress*.
* **Trigger** on attached caress item while in *AutoCaress* to stop it.
* **Trigger** while in manual control of a caress item to squeeze. Might not always work if *AutoCaress* still runs some other item.
* **DirectionDown** on attached caress item while not in *AutoCaress* to detach it.
* **DirectionDown** while not in *AutoCaress* to prompt the partner to initiate the kiss. Limited to the caress positions. Registers after *LongPress*.
* **DirectionHorizontal** on attached caress item to toggle it's visibility.
* **DirectionHorizontal** while an attached caress item is present to scroll through animations. Limited to caress positions.

#### Service, Intercourse
* **DirectionUp** to insert, start, finish, change speed. Registers after *LongPress*.
* **DirectionUp** with pressed *Trigger* to opt for an options without voice . Registers after *LongPress*.
* **DirectionUp** with pressed *Touchpad* to opt for anal if applicable. Can be used with pressed *Trigger*. Registers after *LongPress*.
* **DirectionDown** to set condom, pullout, stop, change to outside during climax, change speed. Registers after *LongPress*.
* **DirectionHorizontal** to scroll through animations.

### Talk/Text Interpreter
Available in **Talk and Text Scenes**. 
* **Trigger** on partner's bodyPart to provoke a reaction.
* **DirectionVertical** on partner's bodyPart to (un)dress it.
* **DirectionVertical** to scroll buttons on the left side of the screen or choices from the text scenario.
* **DirectionHorizontal** to select/deselect current button/category.
* **DirectionHorizontal** to click on previous action. Registers after *LongPress*.
* **DirectionHorizontal** while text is visible to advance the text scenario.
* **DirectionHorizontal** while text is visible to toggle *Auto*. Registers after *LongPress*.

### Roaming Interpreter
Available in **Roaming Scene**.
* **Trigger** to start locomotion.
* **DirectionUp** to interact or speed up.
* **DirectionDown** to crouch or stand up.
* **Horizontal direction** to turn.

## Controls Studio
**Warning: This section was written for [KK_MainGameVR](https://github.com/mosirnik/KK_MainGameVR) and serves as a loose, vague reference for an actual functional.**

This plugin assumes that your VR controller has the following buttons/controls:

* Application menu button
* Trigger button
* Grip button
* Touchpad or Thumbstick

You may need to tweak button assignments in SteamVR's per-game settings if your
controllers don't natively have these. See the Controller Support section for
a list of known-to-work controllers.

In the game, each of the controllers has 3 tools: Menu, Warp and School/Hand. Only
one of them can be active at a time. You can cycle through the tools by pressing
the Application menu button. Each controller has a cyan icon indicating which
tool is currently active.

When any of the tools is active, you can press and hold the Application menu
button to see in-game help on button roles.

### Menu tool <img src="https://raw.githubusercontent.com/mosirnik/KK_MainGameVR/master/doc/img/icon_menu.png" height="30">

The menu tool comes with a little screen on which game menus, icons and texts
are shown. You can use the other controller as a virtual laser pointer, and
pull the Trigger to click on the screen. Most game interactions (specifically,
the ones that don't involve touching 3D objects) are done this way.

Pressing the Grip button while the Menu tool is active causes the screen
to be detached and left at the current position in the 3D space. Pressing it
again reclaims the screen.

A laser pointer can also generate a right click (Touchpad right), middle click
(Touchpad center) and scroll up/down (Touchpad up/down). You can also grab
a detached screen by holding Grip. Press and hold the Application menu button
while the laser is visible to see help about this.

### Warp tool <img src="https://raw.githubusercontent.com/mosirnik/KK_MainGameVR/master/doc/img/icon_warp.png" height="30">

The warp tool allows you to move around in the 3D space. 

Use the touchpad to teleport. Before you finish teleporting, you can draw a
circle along the rim of the trackpad (or similarly rotate the thumbstick)
to change your would-be orientation after teleporting.

Holding the Grip button takes you into grab action. Here you can move around
by "grabbing" the world. If you additionally press Trigger, you can also rotate
the world. Pressing both Trigger and the touchpad gives you the full power
of general 3D rotation, allowing you to turn a wall into the floor, for
example. Double click the touchpad to become upright again.

Grab action is also avaible in the school and hand tools.

### School tool <img src="https://raw.githubusercontent.com/mosirnik/KK_MainGameVR/master/doc/img/icon_school.png" height="30"> and Hand tool <img src="https://raw.githubusercontent.com/mosirnik/KK_MainGameVR/master/doc/img/icon_hand.png" height="30">

These tools are collections of Koikatsu-specific action commands and simulated
mouse/keyboard inputs. The hand tool is for H scenes, and the school tool is for
all other scenes. Other than that, these two are similar to each other. The
button mappings are configurable for each of them separately.
The default for the school tool is:

* Trigger: Walk (Roam mode)
* Grip: Grab action
* Touchpad up: F3
* Touchpad down: F1
* Touchpad left: Turn left
* Touchpad right: Turn right
* Touchpad center: Right mouse button

For the hand tool:

* Trigger: Left mouse button
* Grip: Grab action
* Touchpad up: Mouse wheel scroll up
* Touchpad down: Mouse wheel scroll down
* Touchpad left: (unassigned)
* Touchpad right: Right mouse button
* Touchpad center: Middle mouse button

For touchpad inputs, you need to press the touchpad or click the thumbstick.
Just touching the touchpad or tilting the thumbstick won't be recognized.
Exceptions to this rule are mouse wheel scroll actions and rotate actions,
which only require touching.

## Configuration

**Warning: This section was written for KK_MainGameVR and might not be accurate, especially in Studio.**

This plugin has a lot of configuration options. It is recommended that you use
[ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager),
which allows you to change settings of this plugin from within the game.

## Controller Support

At the moment, most VR controllers seem to work out of the box with this plugin.
Below is an incomplete list of the current support status. If your controllers
are not listed here, please let us know if they work or not (either edit this 
file or create a new issue).

### Works out of the box
* Oculus Rift / Rift S / Quest 2
* Valve Index
* Vive

### HTC Vive

Works out of the box.

### HP motion controllers

The following button assignments are needed:

* Enumlated trackpad: (remove assignments)
* B and Y buttons: Application Menu Button
* Joystick: Trackpad position & value

In addition, you need to make it "pretend to be Oculus Touch controllers".

## Common issues

### Can't click on the virtual screen

This plugin requires that the game window on the Windows desktop is visible and
not covered by something else.

### Framerate is low

If you experience a framerate drop when the camera approaches a character,
particularly in an H scene, then the bottleneck is likely your GPU. I'd suggest
turning down the antialiasing setting using the
[GraphicsSettings](https://github.com/BepInEx/BepInEx.GraphicsSettings) plugin.
If that is not enough, consider disabling some visual effects or reducing the rendering
resolution in SteamVR.

If you experience a low framerate when roaming in main game, try disabling expensive plugins 
or reducing the number of characters that can be loaded at the same time (on left in roster).

## Building (for developers)

You should be able to open the solution in Visual Studio 2019 and just hit Build to build everything.
