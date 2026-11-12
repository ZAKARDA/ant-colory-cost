# Building on Codemagic

1. Push this folder (the one containing `AntchemyUnity/`, `docs/`, `website/`) to GitHub as the repo root.
2. In Codemagic: Add application -> pick the repo -> choose "codemagic.yaml".
3. Teams > Integrations > App Store Connect: add an API key and put its name in `integrations.app_store_connect` (default `CodemagicKey`).
4. Teams > Code signing identities: let Codemagic create or upload an iOS Distribution certificate and an App Store profile for `com.antcolonycolors.game`.
5. Environment variable group `unity_credentials` (mark values Secure): `UNITY_SERIAL`, `UNITY_EMAIL`, `UNITY_PASSWORD` (Plus/Pro) or `UNITY_LICENSE` (contents of a Unity `.ulf` license file).
6. Add variable `APP_STORE_APPLE_ID` (the numeric Apple ID from App Store Connect > App Information) so the build number increments.
7. Start the `ios-release` workflow. The IPA goes to TestFlight.

Notes: Unity export runs `Antchemy.Editor.SceneSetup.BuildIOS` and writes `Builds/AntColonyColors-iOS`. Live AdMob ids are used in release builds; use a TestFlight build on a real iPhone to check ads.
