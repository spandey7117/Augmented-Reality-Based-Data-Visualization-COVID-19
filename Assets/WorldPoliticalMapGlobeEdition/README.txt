***************************************
* WORLD POLITICAL MAP - GLOBE EDITION *
*             README FILE             *
***************************************


How to use this asset
---------------------
Firstly, you should run the Demo Scene provided to get an idea of the overall functionality.
Later, you should read the documentation and experiment with the API/prefabs.


Demo Scene
----------
There's one demo scene, located in "Demo" folder. Just go there from Unity, open "GlobeDemo" scene and run it.


Documentation/API reference
---------------------------
The PDF is located in the Doc folder. It contains instructions on how to use the prefab and the API so you can control it from your code.


Support
-------
Please read the documentation PDF and browse/play with the demo scene and sample source code included before contacting us for support :-)

* Support: contact@kronnect.me
* Website-Forum: http://kronnect.me
* Twitter: @KronnectGames


Version history
---------------

Version 9.0.2 Current release
  - Improved global performance
  - Markers added to the globe with colliders now receive a rigidbody component to enable interaction
  - VR: Support for Google VR pointer and controller touch
  - Map Editor: added option to merge provinces
  - [Fix] Country labels with World Space render method disappear when adding tickers to the globe
  - [Fix] Fixed wrong target sovereign country name shown in transfer dialog (countries and provinces)
  - [Fix] Immediate changes in inspector does not mark scene as dirty (pending save)
  - [Fix] Added workaround to prevent Unity Editor hanging when enabling "Draw All Provinces" with World Map Globe prefab in the scene
  - [Fix] Fixed shaking issue when zoom tilt and keep straight options are enabled
  - [Fix] Map Editor: Unity orbiting keys were being supressed while map editor was selected
  
Versino 9.0.1 2017.04.10
  - Improved tile download system (fewer tiles are now downloads -> less memory comsuption)
  - Added Preload Main Tiles option in inspector to show all tiles for main zoom level from start (only available if they have been downloaded and stored in local cache)

Version 9.0 2017.03.13
  - New Earth style: Tiled + dedicated section to setup integration with online map systems
  - New demo scene 14 showing the new tile system
  - Map Editor: new contexual option to equalize number of provinces across countries
  - Map Editor: new 'Fix Orphan Cities" to automatically assign cities to countries and provinces
  - Map Editor: city province field is now shown when selecting a city
  - Improved globe drag when using WASD keys
  - Added rightClickRotates and rightClickRotatingClockwise properties
  - Improved precision of high definition frontiers projection (EPSG:4326)
  - Country and province borders now use dedicated materials based on color alpha to improve performance
  - Improved cursor: pattern now remains invariable to zoom level
  - Added countryHighlightMaxScreenAreaSize to provide a maximum screen area size for highlighted countries
  - [Fix] Fixed namespace conflict with PUN+
  - [Fix] Fixed jitter when reaching minimum zoom
  
Version 8.0 - 2017.02.09
  - Support for enclaves at country and province levels
  - Redesigned Editor inspector
  - Map Editor: improved region transfer system
  - Map Editor: transfer country field now shows neighbours at top of list
  - Automatic country label fading: optimized performance
  - Optimized performance of country selection with mouse
  - AddLine: new reuseMaterial parameter to improve performance avoiding material instantiation
  - Optimized line animation system

Version 7.3 - 2017.01.10
  - Added Brightness & Contrast sliders to Earth scenic styles
  - Convenience: Added VR Enabled setting in inspector to avoid having to edit WPMInternal script to uncomment a macro
  - Compatibility with Windows Store platform
  - [Fix] Fixed minor compatibility issues with Unity 5.5
  - [Fix] Map Editor: fixed province issue when renaming country
  - [Fix] Fixed country label wrong positioning on Microsoft HoloLens
  - [Fix] Fixed location precision for cities
  - [Fix] Fixed collider expensive delayed cost warnings in profiler
  
Version 7.2 - 2016.11.25
  - Rectangle selection support. New demo scene 8.
  - New marker type: quad (API: AddMarker).
  - New API: GetVisibleCountries(rect), GetVisibleProvinces(rect), GetVisibleCities(rect), GetVisibleMountPoints(rect)
  - Added Hide/Show method for fast hiding/showing globe (faster than enabling/disabling gameobject)
  - Added brightness & contrast properties to Scenic shaders
  - VR: compatibility in normal mode (before, only worked with Inverted Mode)
  - VR: compatibility with Google VR
  - [Fix] Fixed Unlit Earth Style in deferred rendering
  
  
Version 7.1 - 2016.09.23
  - New demo scene 5: sorting cities
  - New demo scene 6: city travel / path traversal
  - New demo scene 7: Earth graffiti!
  - New Earth styles: 2K Standard Shader, 8K Standard Shader.
  - New City Combine Meshes option for best performance when lot of cities are visible
  - Unlit styles now receive shadows
  - Demo scene 1: added new button "Fire Bullet!"
  - [Fix] Frontiers, Mount Points, Cursor, Latitude Lines and other objects created dynamically now use the same layer than globe
  - [Fix] Fixed RespectOtherUI behaviour on mobile
  - [Fix] Fixed drag issue on Windows 10

Version 7.0 - 2016.08.18
  - New Earth styles supporting multi-tile texture up to 16K (4x8K): 16K Scenic, 16K Scenic + CityLights, 16K Scatter and 16K Scatter + City Lights
  - Performance improvement of Automatic Labels Fade feature


Version 6.1 - 2016.08.15

New Features:
  - Option to follow device GPS coordinates on the map
  
Improvements:
  - API: methods used to colorize countries now can also add a colored outline
  - Map Editor: added Update button next to country continent
  
Fixes:
  - Fixed camera bug when calling FlyTo() while a rotation operation was being executed in CameraRotate mode
  - Fixed Upright Labels option when inverted view is enabled
  - Map Editor: fixed issue with target country being deselected when transferring countries
  

Version 6.0 - 2016.06.29

New Features:
  - New Scenic City Lights and Scenic Scatter City Lights styles
  - New Zoom Tilt option to skew view when approaching Earth
  - New Upright Labels option to ensure country labels are always easily readable
  
Improvements:
  - New APIs: GetCountryIndex(spherePosition), GetCountryNearToPosition(spherePosition), GetProvinceIndex(spherePosition), GetProvinceNearToPosition(spherePosition)
  - New APIs: GetVisibleCountries, GetVisibleProvinces, GetVisibleCities, GetVisibleMountPoints.
  - New events: OnCountryBeforeEnter, OnProvinceBeforeEnter
  - Can use transparent colors when coloring countries or provinces
  - Animated lines general optimization

Fixes:
  - Fixed bug when flying to target location with globe scale >1 and navigation mode set to Camera Rotates.
  - Improved runtime performance and reduced garbage collection
  

Version 5.4 - 2016.05.24

New Features:
  - New label rendering method: World Space
  - New global default font setting for all labels

Improvements:
  - New APIs: GetCountryRegionSurfaceGameObject and GetProvinceRegionSurfaceGameObject for retrieving the colored surface (game object) of any country or province (for example to read the current color)
  
Fixes:
  - Fixed issues with country transfer options in Map Editor
  - Fixed inland frontiers line renderer which was scaling too much at short distances  
  - Fixed duplicate province names

Version 5.3 - 2016.04.27

New features:
  - Now, by default, coastal frontiers won't be drawn, which is useful in combination with inland frontiers (avoid overdraw, increase performance) (API: showCoastalFrontiers)

Improvements:
  - New option to draw all province borders (API: drawlAllProvinces)
  - Ability to constraint rotation around a position (APIs: constraintPosition, constraintAngle, constraintPositionEnabled)
  - Increased color contrast of Scenic Scatter style
  - Thicker lines for inland frontiers
  - Added OnLeftClick and OnRightClick events
  - Added CIRCLE_PROJECTION marker type
  
Fixes:
  - Fixed issues in inverted mode and globe scale greater than 2
  - Fixed issue with latitude and longitude lines not being drawn when option enabled
  - Fixed issue with SetSimulatedMouseClick not working with GamePads
  - Fixed Pala (Chad) province
  - Fixed circle drawing when crossing 180 degree longitude
  - Fixed black globe issue under some circumstances


Version 5.2 - 2016.02.16

New features:
  - New high density mesh option and ability to replace the Earth mesh
  - New distance calculator (improved inspector and new API).
  
Improvements:
  - FlyTo is now compatible with simultaneous globe translations
  - New option to prevent interaction with other UI elements (Respect Other UI)
  - Labels now fade in/out automatically depending on camera distance and screen size  
  
Fixes:
  - Fixed new scenic atmosphere scattering style on mobile
  

Version 5.1 - 2016.01.29

New features:
  - New Scenic High Resolution with physically-based Atmosphere Scattering effect
  - New demo scene featuring sprites/billboard positioning

Improvements:
  - Can hide completely individual countries using decorators, Editor or through API (country.hidden property)
  - VR: compatibility with Virtual Reality gaze
  - Performance improvement in city look up functions
  - Increased sphere mesh density for sharper geometry

Fixes:
  - Min population filter now returns to previous value when closing the map editor
  - Fixed cities and markers rotation when inverted mode is enabled
  

Version 5.0 - 2015.12.24

New features:
  - New Camera navigation mode (can rotate camera instead of Earth). Supports user drag, Constant Drag Speed, Keep Straight, FlyTo methods. Updated Scenic Shader. 
  - Mount Points. Allows you to define custom landmarks for special purposes. Mount Points attributes includes a name, position, a type and a collection of tags. Manual updated.
  - Country and region capitals. Different icons + colors, new class filter (cityClassFilter), new "cityClass" property and editor support.
  - Added Hidden GameObjects Tool for dealing with hidden residual gameobjects (located under GameObject main menu)
   
Improvements:
  - New options for country decorator and for country object (now can hide/rotate/offset country label)
  - New line drawing method compatible with inverted mode (still needs some work)
  - Right clicking on a province now centers on that province instead of its country center
  - Improved zoom acceleration in inverted mode
  - (Set/Get)ZoomLevel now works in inverted mode
  - "Constant drag speed" and "Keep straight" now work in inverted mode
  - API: added new events: OnCityClick, OnCountryClick, OnProvinceClick
  - API: added GetCountryUnderSpherePosition and GetProvinceUnderSpherePosition
  - Editor: country's continent is displayed and can be renamed
  - Editor: continent can be destroyed, including countries, provinces and cities
  - Editor: deleting a country now deletes all cities belonging to that country as well
  - Editor: new options to delete a country, a province or all provinces belonging to a country
 
Fixes:
  - Cities were not being visible when inverted mode was enabled
  - Some countries and provinces surrounded by other countries/provinces could not be highlighted
  - Right click to center was not working when inverted mode was enabled


Version 4.2 - 2015.12.02
  
 New features:
  - Number of cities increased from 1249 to 7144
 
 Improvements:
  - Improved performance when cities are visible on the map
  - Improved straightening of globe at current position (right click or new improved API: StraightenGlobe)
  - Added dragConstantSpeed to prevent rotation acceleration
  - Added keepStraight to maintain the globe always straight
  - Added zoom max/min distance
  - Country is now highlighted as well when provinces are shown
  - API: new overload for GetCityIndex to fetch the index of the nearest city around a location (lat/lon or sphere position).
  - API: new events: OnCityEnter, OnCityExit, OnCountryEnter, OnCountryExit, OnProvinceEnter, OnProvinceExit

 Fixes:
  - Fixed geodata issues with Republic of Congo, South Sudan and provinces of British Columbia, Darién, Atlántico Sur, Saskatchewan and Krasnoyarsk
  - Minor fixes regarding province highlighting and some lines crossing Earth
  - Fixed a bug in FlyToxxx() methods when globe is not a 0,0,0 position
  

Version 4.1 - 11/11/2015

 New features:
  - Option to show inland frontiers
  - Improved Scenic shaders including a new 8K + Scenic style (atmosphere falloff + scattering effect)
  
 Improvements:
  - New option to invert zoom direction when using mouse wheel (invertZoomDirection property)
  - New option to automatically hide cursor on the globe if mouse if not over it (cursorAlwaysVisible)
  - New API to obtain the country reference under any sphere position (GetCountryUnderSpherePosition) 
  - New option to mask grid so it only appears over oceans
  - Improved Earth glow
  
 Fixes:
  - Globe interaction is now properly blocked when mouse is hovering an UI element (Canvas, ScrollRect, ...)
  - Labels shadows were not being drawn due to a regression bug


Version 4.0 - 23/10/2015
  New features:
  - Map Editor: new extra component for editing countries, provinces and cities.
  
  Improvements:
  - New APIs for setting/getting normalized zoom factor (SetZoomLevel/GetZoomLevel)
  - New APIs in the Calculator component (prettyCurrentLatLon, toLatCardinal, toLonCardinal, from/toSphereLocation)
  - New API variant for adding circle markers (AddMarker)
  - New API for getting cities from a specified country - GetCities(country)
  - New APIs for getting/setting cities information to a packed string - map.editor.GetCityGeoData/map.ReadCitiesPackedString
  - Option for changing city icon size
  - Can assign custom font to individual country labels
  - Even faster country/province hovering detection
  - Better polygon outline (thicker line and best positioning thanks to new custom shader for outline)
  - Country outline is shown when show provinces mode is activated
  - Improved low-res map generator including Douglas-Peucker implementation + automatic detection of self-crossing polygons
  
 Fixes:
  - Removed requirement of SM3 for the Scenic shader

    
Version 3.2 - 21/09/2015
  New features:
  - New markers and line drawing and animation support
  - New "Scenic" style with custom shader (relief + cloud effects)
  
  Improvements:
  - Pinch in/out support for mobile
  - Improved resolution of high-def frontiers while reducing data file size
  - Single city catalogue with improved size
  - Significant performance improvement in detecting country hover
  - More efficient highlghting system
  - New option in inspector: labels elevation
 
 Fixes:
  - Corrected frontiers distortion
  - Population of cities fixed and approximated to the metro area
 

Version 3.1 - 28/08/2015
  New features:
  - New Inverted Mode view (toggle in the inspector)
  - Bake Earth texture command (available from gear's icon in the inspector title bar)
  
  Improvements:
  - New buttons to straighten and tilt the Earth (also available in API)
  - New option to adjust the drag speed
  - New option to enable rotation using keyboard (WASD)
  - x2 speed increase of colorize/highlight system
  
  Fixes:
  - Fixed bug related to labels drawing when the Earth is rotated on certain angles
  - Fixed colorizing countries when field of view of camera was not default 60
  
  

Version 3.0.1 - 11.08.2015
  New features:
  
  Improvements:
  - Better outline implementation with improved performance)
  
  Fixes:
  - Calculator component: fixed an error in spherical to degree conversion
  - Colorize shader was not showing when Earth was not visible
  - A few countries had parts visible when colorized and Earth rotates



Version 3.0 - 11.08.2015
  New features:
  - New component: World Map Calculator
  - New component: World Map Ticker
  - New component: World Map Decorator
  
  Improvements:
  - Some shaders have been optimized
  - Improved algorithm for centering destinations (produces a straighten view)
  - New option: right click centers on a selected country
  - Lots of internal changes and new APIs
  
  Fixes:
  - Fixed country label positioning bug when some labels overlap
  - Fixed colorizing of some countries which appeared inverted


Version 2.1 - 3.08.2015
  New features:
  - Option to draw country labels with automatic placement
    
  Improvements:
  - Additional high-res (8K) Earth texture
  
  Fixes:
  - Some countries highlight were rendered incorrectly when using high detail frontiers

Version 2.0 - 31/07/2015
  New features:
  - Second detail level for country frontiers
  - New option to draw provinces/states for active country
  - Option to draw an outline around highlighted/colored countries
  - New options to show a cursor over custom/mouse position
  - New options to show latitude/longitude lines
    
  Improvements:
  - Even faster frontier line rendering (+20%)
  - Tweaked triangulation algorithm to improve poly-fill
  - Can locate a country from the inspector
  - Cities are now drawn as small circular dots, instead of small boxes
  - Can change the color of the cities
  - Additional Earth style: CutOut
  
  Fixes:
  - Some new properties were not being correctly saved from Editor
  - Colored countries hide correctly when Earth rotates

Version 1.1 - 25.07.2015
  New features:
  - added 3 new material/textures for Earth
  - extended city catalog (now 1249 cities included!)
  - can filter cities by population
  Improvements: 
  - better frontiers line quality and fasterer render
  - better poly-fill algorithm
  - can change navigation time in Editor
  - moved mouse interactions (rotation/zoom) to the main script and expose that as part of the API
  - reorganized project folder structure
  Fixes:
  - setting navigation time to zero causes error
  - some properties where not being persisted


Version 1.0 - Initial launch 16.07.2015



Credits
-------

All code, data files and images, otherwise specified, is (C) Copyright 2015 Kronnect
Non high-res Earth textures derived from NASA source (Visible Earth)
Flag images: Licensed under Public Domain via Wikipedia


