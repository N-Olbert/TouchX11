# TouchX11 
TouchX11 (TX11) is a cross-plattform "X Window System"-Server implementation written in Xamarin/C#

It is optimised for touch screen use (well, at least a bit :wink:).

Currently, supported platforms are: iOS, Android, Windows (WPF)

# User manual 
- A tap on the screen is equal to a left click

- Well... nothing more yet, everything else should be self-explanatory

# Remarks
While more or less fully functional most code which has not been ported is undocumented. This is due to the fact that this project is part of a time-bound research paper.


# Build
1. Clone repro
2. Open solution in Visual Studio (tested with Visual Studio 2019)
3. Set appropriate startup project
4. Build

(Additional tasks may be required to set up the environment, f. e. install vcremote on Mac)

# Problems
Well... it's X11 so you'll get some structural problems for free. Refer to Don Hopkins ["The X-Windows Disaster"](https://medium.com/@donhopkins/the-x-windows-disaster-128d398ebd47) for a good (but humorous) overview.

Concrete problems of this app include:
1. Missing extensions: Some X11-Apps might not work as this app only supports three extensions (BIGREQUESTES, XSHAPE, XTEST). This app is (according to the X11-Protocol) a completely valid X11-Server, though...
2. Performance: You will notice that slightly more complex apps than "xtrem" won't run very well, especially over a network. While this is a structural issue I bet there are also some performance killers within the code of this app
3. Security: All communication between this app and any X11-Client is not encrypted! Be sure to use this app within a trusted environment only (or setup a VPN)
4. Usability: Well... X11 wasn't made for mobile devices and mobile devices weren't made with X11 in mind. A LOT of things will be annoying, for example you dont have a "Control"-key on an iOS-keyboard.
5. ...

# Credits
 This project is based upon Matt Kwahn's "Android X server"-project which is licensed under MIT-License.
For further info see:
- https://my20percent.wordpress.com/2012/02/27/android-x-server/
- https://code.google.com/archive/p/android-xserver/
