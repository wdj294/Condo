---------------------------------------------------
TENKOKU - DYNAMIC SKY

Copyright ©2017 Tanuki Digital
Version 1.1.7
---------------------------------------------------


----------------------------
THANK YOU FOR YOUR PURCHASE!
----------------------------
Thank you for buying TENKOKU and supporting Tanuki Digital!
It's people like you that allow us to build and improve our software! 
if you have any questions, comments, or requests for new features
please visit the Tanuki Digital Forums and post your feedback:

http://tanukidigital.com/forum/

or email us directly at: konnichiwa@tanukidigital.com



----------------------
REGISTER YOUR PURCHASE
----------------------
Did you purchase Tenkoku - Dynamic Sky on the Unity Asset Store?
Registering at Tanuki Digital.com gives you immediate access to new downloads, updates, and exclusive content as well as Tenkoku and Tanuki Digital news and info.  Fill out the registration forum using your Asset Store "OR" Order Number here:

http://www.tanukidigital.com/tenkoku/index.php?register=1



----------------------
SUPPORT
----------------------
If you have questions about Tenkoku, need help with a feature, or think you've identified a bug please let us know either in the Unity forum or on the Tanuki Digital forum below.

Unity Forum Thread: http://forum.unity3d.com/threads/tenkoku-dynamic-sky.318166/
Tanuki Digital Forum: http://tanukidigital.com/forum/

You can also email us directly at: konnichiwa@tanukidigital.com



----------------------
DOCUMENTATION
----------------------
Please read the Tenkoku documentation files for more in-depth customization information.
http://tanukidigital.com/tenkoku/documentation



-------------
INSTALLATION
-------------
I. IMPORT TENKOKU FILES INTO YOUR PROJCT
Go to: “Assets -> Import Package -> Custom Package...” in the Unity Menu and select the “tenkoku_dynamicsky_ver1.x.unitypackage” file. This will open an import dialog box. Click the import button and all the Tenkoku files will be imported into your project list.

II. ADD THE TENKOKU MODULE TO YOUR SCENE
1) Drag the Tenkoku DynamicSky prefab located in the “/PREFABS” folder into your scene list.
2) If it isn’t set already, make sure to set the Tenkoku DynamicSky’s position in the transform settings to 0,0,0

III. ADD TENKOKU EFFECTS TO YOUR CAMERA
1) Click on your main camera object and add the Tenkoku Fog effect by going to Component-->Image Effects-->Tenkoku-->Tenkoku Fog.
Note: For best results this effect should be placed to render BEFORE your Tonemapping effect(if applicable).

(optional)
2) Click on your main camera object and add the Tenkoku Sun Shaft effect by going to Component-->Image Effects-->Tenkoku-->Tenkoku Sun Shafts.
Note: For best results this effect should be placed to render AFTER your Tonemapping effect(if applicable).


A Note About Scene Cameras:
Tenkoku relies on tracking your main scene camera in order to properly update in the scene.  By default Tenkoku attempts to auto locate your camera by selecting the camera in your scene with the ‘MainCamera’ tag.  Alternatively you can set it to manual mode and drag the appropriate camera into the ‘Scene Camera’ slot.




-------------
NOTES
-------------
A Note On Accuracy:
Moon and planet position calculations are currently accurate for years ranging between 1900ca - 2100ca.  The further away from the year 2000ca that you get (in either direction) the more noticeable calculation errors will become.  Additional calculation methods are currently being looked at to increase the accuracy range for these objects.

Integration with CTS - Complete Terrain Shader:
CTS is an advanced terrain shader for Unity with built-in settings to control wetness, rain, snow, and seasonal tinting directly on the terrain itself.  Tenkoku provides an integration script which will automatically drive the effects in CTS according to the weather settings you use in Tenkoku.  To enable this integration, install CTS and Tenkoku in your project, add the CTS weather Manager, thenf inally drag the /SCRIPTS/Tenkoku_CTS_Weather component onto the 'CTS Weather Manager' object in your scene.
Learn more about CTS here:  https://www.assetstore.unity3d.com/en/#!/content/91938




-------------------------------
RELEASE NOTES - Version 1.1.7
-------------------------------

WHAT'S NEW
- 


CHANGES
- Longitude and TimeZone +/- settings were reversed, and have now been corrected.  Check your projects!
- Edited module and library to use Static gameobject references for improved performance.
- Code optimizations to eliminate GameObject.Find().

BUG FIXES
- Implicitly removed Unity fog calculations from sun object shader.
- Improved accuracy for total solar eclipse rendering and positions.
- Added glCore pragma to temporal aliasing shaders.  Should fix render failure on Mac / Linux.




----------------------------
CREDITS
----------------------------
- Cloud System adapted from prototype work by Keijiro Takahashi.  Used with permission.
https://github.com/keijiro

- Temporal Reprojection is modified from Playdead Games' public implementation, available here:
https://github.com/playdeadgames/temporal

- Oskar Elek Atmospheric model originally developed in 2009 by Oskar Elek.  Model has been adapted for Unity by Michal Skalsky (2016) as part of his Public Domain work regarding volumetric atmospheric rendering:
http://www.oskee.wz.cz/stranka/oskee.php
https://github.com/SlightlyMad

- Lunar image adapted from texture work by James Hastings-Trew.  Used with permission.
http://planetpixelemporium.com

- Galaxy image adapted from an open source image made available by the European Southern Observatory(ESO/S. Brunier):
https://www.eso.org/public/usa/images/eso0932a/

- Star position and magnitude data is taken from the Yale Bright Star Catalog, 5th Edition:
http://tdc-www.harvard.edu/catalogs/bsc5.html (archive)
http://heasarc.gsfc.nasa.gov/W3Browse/star-catalog/bsc5p.html (data overview)

- Calculation algorithms for Sun, Moon, and Planet positions have been adapted from work published by Paul Schlyter, in his paper 'Computing Planetary Positions'.  I've taken liberties with his procedure where I thought appropriate or where I found it best suits the greater Tenkoku system.  You can read his original paper here:
http://www.stjarnhimlen.se/comp/ppcomp.html  
