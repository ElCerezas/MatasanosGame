# 🩺 Matasanos

**A cooperative online party game about operating on an alien in the cheapest clinic in the galaxy.**

🏆 **Technical Excellence award, TecnoGames 2026** · 🎮 [Play on itch.io](https://unagranota.itch.io/matasanos)

---

## About the game

You and up to three friends are rookie doctors in a low-cost clinic on a floating space station. An alien patient is bleeding out on the operating table, and the equipment is as cheap as the staff is inexperienced. Heal every wound before the alien's health reaches zero.

Some tasks need **two players at the same time**, like holding the alien's mouth open while someone else works inside. The alien reacts to every treatment (shedding parasites, sneezing, groaning), so each operation becomes a puzzle of controlled chaos.

- **Players:** 2-4, online co-op (one player hosts, the others join)
- **Platform:** PC
- **Style:** cartoon, low-poly, ragdoll physics, in-world (diegetic) UI

## Gameplay

- **Cooperative treatment:** open the alien's mouth to clean its tongue or swap a bad tooth, mix medicines with a randomised recipe from an in-game guide, print teeth and bandages on a 3D printer, focus the surgical light on wounds, disinfect and bandage them, and keep the blood supply running.
- **Power management:** the light, the printer and the mixer share a limited power grid. Overload it and the clinic goes dark; two players must lift the fuses together in the electrical room.
- **A patient with moods:** the alien moves between *restless*, *calm*, *altered* and *bleeding out*. A wrong treatment makes it frantic and it spits out parasites that block your view and make you drop tools.
- **Chaos hazards:** blood bags burst into puddles that make players slip and ragdoll. Loose cables and a dirty sponge add to the mess.
- **Customisation:** change your character's face and hat at the custom machine.
- **Win / lose:** heal every wound to win; if the alien's health hits zero, you lose.

## Technical highlights

### Networked physics with ownership
In an online party game where several players grab, throw and hit the same objects, physics easily drift apart between clients. We used **Netcode for GameObjects** with a host-based model and solved the problem with **ownership**: the player who grabs an object becomes its owner, and physics are simulated only on the owner's side. Every object has a single source of truth, and ownership is transferred when it changes hands.

### Modular architecture
Gameplay code is split into independent modules (grabbing, interaction, snapping, power, machines) so features can be added or changed without breaking the rest.

<!-- TODO: link to ARCHITECTURE.md once it exists -->

## Built with

- **Engine:** Unity 6000.3.12f1
- **Language:** C#
- **Networking:** Netcode for GameObjects
- **Audio:** FMOD
- **Art:** Blender / 3ds Max, Adobe Photoshop
- **Workflow:** GitHub, Trello, Discord dailies

## My contributions (programming)

- **Initial game architecture** (modular design).
- **Player:** grabbing and interaction, input handling, camera sync, head and hand IK, animation tree, initial ragdoll, HUD effects.
- **Physical items:** physical grab with spring-based hold, tool orientation and **ownership transfer** between players.
- **Online rooms:** UI, password-protected rooms, host and player disconnection handling, match states.
- **Power system:** architecture, extension cables, cable tension, blackout, sparks and smoke effects, and the fuse mechanic.
- **Machines:** logic for the 3D printer, mixer, blood machine, snap zones and the custom machine (faces, hats, random ID).
- **Alien:** IK system for opening the mouth, animation tree and implementation, parasite particles.
- **Shaders and VFX:** liquids, 3D printing, body stains and parasites, parasite HUD effect, soap and spark particles.
- **Sponge and cleaning system**, blood bag refill logic and explosion sensitivity.

## Team

Made by a team of 7 students at Tecnocampus:

| Member | Main role |
|---|---|
| Adrià de la Iglesia | Programming (architecture, grabbing, ownership, rooms, power, shaders) |
| Izan Diaz | Programming (alien state machine, event bus, online rooms, machines) and SFX |
| Jan Capdevila | Programming (player movement, ragdoll, wounds and parasites, snappable items) |
| Víctor Ramos | Game design, level design, 3D modelling and lighting |
| Aimar Gomez | Design, texturing, lighting, FMOD audio and menus |
| Isaac Ureña | 3D modelling, texturing, 2D art and player animations |
| Rain Forcada | Alien 3D model, rig and animations |

## What we learned

- Making the game online cost us a lot of development time, and we spent more time fixing multiplayer bugs than building content. Next time we would scope features around that cost.
- Deliverables piled up in the last days. Earlier, steadier milestones would have helped.
- Descriptive commit messages make the project history much easier to follow.