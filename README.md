# Enemy AI Project
Gameplay gym project containing two enemy types with unique AI behaviors.

## 📖 About The Project

This repository contains the Unity game Enemy AI Project by JulianMM

It is a work-in-progress showcase for a 3D first-person horror game which currently contains a gameplay gym with two enemies. The properties of these enemies are described below.


## ✨ Features

This project includes the following main game features:

### Feature 1: Patrol Enemy AI

* **Description:** Enemy programmed to patrol between points using Unity's nav agent system. Pursues and attacks any player it sees.
* **Characteristics:**
    * Patrols between points using Unity's nav agent system.
    * Pursues and attacks any player it sees.
    * Will stop the pursuit if loses sight of the player for some time.


### Feature 2: Follower Enemy AI

* **Description:** Enemy programmed to behave like a 'weeping angel' kind of monster
* **Characteristics:**
    * Does not move if the player is looking at it.
    * Has a slight delay before it freezes.
    * Constantly pursues the player from behind, instantly attacking them if it can get close enough.
