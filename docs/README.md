# Ant Colony Colors

Original Unity (6000.6.7f1) ant-and-color puzzle for iOS.

- `AntchemyUnity/` Unity project. `tools/levelgen.py` generates and verifies levels.
- Levels: `python3 tools/levelgen.py --out AntchemyUnity/Assets/Resources --count 150 --art tools/art` (drop your own pixel-art PNGs in `tools/art/`, one per level, transparent = empty).
- First open: Antchemy menu (Unity editor menu) > Setup Main Scene, then Build.
- Release checklist: see STORE_LISTING.md. AdMob uses Google TEST ids until you enter your own in `Resources/AdMobConfiguration.json`.

## Third-party fonts
- Poppins (Bold, Medium) — Copyright 2020 The Poppins Project Authors (https://github.com/itfoundry/Poppins).
  Licensed under the SIL Open Font License 1.1 (https://scripts.sil.org/OFL). Bundled in Assets/Resources/Fonts.
