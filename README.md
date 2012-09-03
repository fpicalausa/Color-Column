Color Columns for Visual Studio 2012
====================================

A visual studio extension to highlight user-defined columns.

Features
--------

* Colored columns at specific columns (ala set cc=80,120)
* Colored columns when word wrapping is enabled
* Highlighting of the cursor column (ala set cuc)
* Editable colors (through visual studio's colors settings)
* An options page to set the columns that must be colored

Screenshot
----------

![Highlighted columns behind source code](raw.github.com/fpicalausa/Color-Column/master/screenshot.png "Color columns")

Hacking this extension
----------------------

The highlighted columns are painted on two images by `Colorcolumn` objets. The
images correspond to the user defined columns, and the current cursor column. 
The `ColorColumnTextClassifier` is also used to classify characters that sit on
the specified color columns, so that they get colored.
