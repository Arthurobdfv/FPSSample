# Scripts and Architecture
## Welcome to the Scripts Session!
Here I'll brefly explain the project architecture and some design decisions that I've made so this project is extensible and easy to maintain, quick disclaimer, I'm still learning about SOLID principle and techiques so it might not be perfect but I'm sure I'm going in the right way of learning! :)

## Decision Making
First of all, before even starting the implementations I've made some decisions about the end game, for sure they might change a little bit during the development process but I've got some solid decisions that are the core mechanics I need to the game.

Game mechanics:
* Multiple Weapons - The player shoud be able to use multiple weapon types;
* Primary Secondary Weapon Carrying - Just like a lot of FPS games, the player should be able to carry one primary and one secondary weapon.
* Multiple enemies - I want the game to have some different kind of enemies, flying hovering drones and some humanoid enemies;
* Item Collection - That should be a drop and collect item system so the plyer can get some ammo / life item from the ground;
* Crates and othe destroyable objects - The game or enemies will randomply spawn crates or drops on the ground such as health recovery or ammo;

With those mechanics in mind I was able to "sketch" up the game architecture, first of all let's talk about input and shooting (the core mechanic of any FPS right!? :))

### Pew Pew!
