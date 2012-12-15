Mutimonitor Hot Corner Barriers for Windows
===============================

Windows 8 introduced hot corners for triggering core services, but applications
  that use them have existed long before. Apps like
  [Switcher](http://insentient.net/) allow you to specify a corner to trigger
  events, but their use is relatively limited in a multimonitor setup.
  
Since the target is a 1x1 pixel, hot corners that are present in the seam
  between monitors are all but impossible to trigger. To solve this, I took
  a feature I've enjoyed in Gnome Shell, cursor barriers, and ported them to
  Windows.

What are cursor barriers?
-------------------------

Cursor barriers prevent the mouse from crossing from one monitor to another. 

[![demoss](blob/master/res/msdemo.png)](http://blogs.msdn.com/b/b8/archive/2012/05/21/enhancing-windows-8-for-multiple-monitors.aspx)

As you can see in the image above, a small (~6px) section at the top and
  bottom of each monitor is blocked off. If your mouse is in those corners, 
  it cannot cross from one monitor to the other until you leave the corner.
  This helps turn the 1x1 px corner into a virtually infinite target
  area (cf. [Fitt's Law](http://d3rxqy8m5km8r7.cloudfront.net/features/visualizing-fittss-law/))


Features
========

* Toggle left and right top corner barriers on and off.
* Configurable barrier height.
* Option to enable on startup.
* No installation needed. Double click to run, then use the task tray
  context menu to enable on startup.

Limitations
===========

* Barrier height cannot be larger than the height of the smallest
  attached screen -- otherwise you would be stuck on that screen.
* Vertically stacked monitors probably will not work.

TODO
====

* Set up barriers at bottom corners.