# Myriad-Creation-A-Phantasm-of-Passing-Aeons

An atmospheric, dependency-free landing page with a responsive generative star field.

## Geography module

`Assets/Scripts/Geography` contains the Unity runtime model for the fourth design
chapter: free-form polygon tiles, legal and temporary ownership, roads and sea
connectivity, terrain-derived movement rules, and an incremental climate simulator.
`ClimateDirtyRecalculationSystem` schedules only changed tiles and their adjacent
polygon tiles through Unity's Job System rather than refreshing the entire map.

## Run locally

```sh
python3 -m http.server 8000
```

Then open <http://localhost:8000> in a browser.
