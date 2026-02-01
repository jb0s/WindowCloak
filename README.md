# WindowCloak
System tray utility that hides windows from screenshares / screen recordings

## Usage guide
The tool lives in your system tray. You can configure the app from there. Settings include:

- Enable Cloak: Enables / disables the tool.
- Allow by default (Recommended ON\*): When enabled, windows **will not** be cloaked unless checked in the Windows list. When disabled, windows **will** be cloaked unless checked in the Windows list. * Turning this off will make the tool touch every process with a window on your computer. This may lead to undesireable side effects and/or bugs.
- Fully hide windows: When enabled, windows will be fully hidden instead of blacked out.

## Credits
Credits to [shalzuth's WindowSharingHider](https://github.com/shalzuth/WindowSharingHider) for WindowHandler.cs, I honestly just took it from there because I couldn't be bothered trying to reinvent the wheel bypassing the restrictions on SetWindowDisplayAffinity.
