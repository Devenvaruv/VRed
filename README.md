# Stats Lab VR

## Experience Snapshot
- Step into a stylized learning lab where a quiet classroom interior, a library nook, and moody medieval wings can be swapped in via ready-made room prefabs (AI Room, Classroom Dim, Standard Room, Medieval Room) to set the tone for each statistics lesson.
- A friendly drone host hovers nearby with an illuminated face plate and floating text canvas, narrating each activity in approachable language and prompting you through the story beat by beat.
- Every lesson hinges on tangible, full-scale manipulatives—color-coded books, numbered spheres, sliders, and pedestals—that invite you to move, grab, and rearrange data with your hands instead of staring at charts.

## Guided Learning Flow

### Assistant Drone & Mode Bookshelf
The flow begins beside a towering bookshelf. The drone “scans” the shelves, announces that blue is currently the mode, then challenges you to hunt down every red book and regroup them on the left side. As you move books, the drone reads out live counts and celebrates once red becomes the most frequent color, closing with a recap that “mode = the most frequent category.”

### Mean & Median Balance Board
A long balance board bristling with numbered sockets turns the notion of mean into a kinetic toy. Unique numbered spheres (digits 0–9, each with its own material color) hide across the room; you are tasked with grabbing specific ones (e.g., ball 1 near the bookshelf and ball 10 near the microscope) and placing them on matching slots. The board tilts toward whichever side has the higher average index, while an optional median marker glides along the deck to show the middle of the distribution. Finishing all placements completes the “Learn Mean Board” task and unlocks hints for what to do next.

### Slider-Based Sorting Story
A futuristic slider console drives another mini-lesson. Pushing the handle through its discrete detents teleports a spotlighted object through curated world positions (seven hotspots laid out across the lab). The UI tracks progress (“Step 2/3: Move the Slider around!”), fires a first-touch event to confirm engagement, and triggers a second completion once you park the slider on the target value, making it perfect for multi-step scavenger hunts or data storytelling beats.

### Word-Match Pedestals & Vocabulary Stations
Pedestals fitted with XR Word Match sockets act like smart receptacles: a block will only snap in if the text printed on it matches the accepted vocabulary (ideal for math terms or color labels). Valid objects glow blue when hovering; mismatches flash red and refuse to latch. It’s a low-friction way to reinforce new words without breaking immersion.

## Player Toolkit & Flow Control
- **Start Lesson Zone** – Stepping into a glowing floor ring summons a “Start Lesson?” world-space prompt in front of your headset. Accepting it locks locomotion providers, focuses attention on the upcoming objective, and fires start/cancel/completion events so the wider scene can react.
- **Task Board** – Tapping the right-hand primary button projects a floating task canvas just beyond arm’s length. Locomotion and ray interactors suspend while it’s open, ensuring you can read the checklist and its contextual hints (e.g., “Ball 10 is near the microscope, behind the box”) without drifting away. Tapping again either recenters the board or closes it based on your settings.
- **Quick HUD** – A lightweight screen overlay mirrors the current guidance line (“Step 2/3…”, “Find every red book…”) and fills a radial bar as you progress, so you always have a peripheral reminder of what’s left.
- **Teleport Hotspots** – Dedicated anchors (e.g., “Median Player Teleport”) align your entire rig with curated viewpoints so you can reset between activities without fiddling with stick locomotion.

## Interactables & Manipulatives
- Numbered spheres spawn with guaranteed-unique digits and colors, making it easy to see which values you’ve already placed on the mean/median board.
- Books sport crisp spine colors so you can visually classify and reorganize mode groups at a glance.
- Large props can be grabbed with one or two hands; a two-hand rotation layer lets you aim devices naturally, while certain objects even respond differently based on whether your left or right controller initiated the grab.
- A handheld multi-transform tool lets creators (or advanced learners) hold any object and use the trigger/primary buttons with the thumbstick to switch between move, rotate, and scale adjustments without diving back to the editor.

## Spaces & Atmosphere
- Switchable environments (AI-inspired classroom, dim lecture hall, standard modern room, or medieval stone chamber) keep repeated lessons feeling fresh.
- Layered audio brings each scene to life: classroom ambiance for murmuring crowds, gentle outdoor birds for courtyard breaks, subtle book-grab Foley, and a “Professor Introduction” voice clip for cinematic lesson starts.

## Creator & Ops Utilities
- A world-space transform panel can inspect whichever prop you last grabbed, display its position/rotation/scale, duplicate it on demand, and even beam a JSON snapshot to a remote endpoint for archival or analytics.
- The grab monitor auto-wires every XR grab interactable to that panel, so any new object you pick up is instantly available for inspection or duplication.
- An input debugger quietly logs trigger/AB/joystick activity from the right-hand controller, making in-headset troubleshooting painless.
- Addressable zone loaders let you stream in labeled content packs (e.g., a “RoomA” layout) the moment the XR Origin crosses a trigger, freeing you to stage multiple lessons in one build without bloating memory.

## Trailer
_Add your trailer embed or link here once it’s ready._
