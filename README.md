Color Columns for Visual Studio 2012
====================================

A visual studio extension to highlight columns in the text editor.

Features
--------

* Colored columns at specific columns (a la set cc=80,120 in Vim)
* Colored columns when word wrapping is enabled
* Highlighting of the cursor column (a la set cuc in Vim)
* Editable colors (through visual studio's colors settings)
* An options page to set the columns that must be colored

Screenshot
----------

![Highlighted columns behind source code](https://raw.github.com/fpicalausa/Color-Column/master/screenshot.png "Color columns")

Hacking this extension
----------------------

The `ColorColumn` class contains the main logic for painting columns in the
background of the code editor. For this purpose, it uses two images: one for  
painting the user defined columns, and the other for the current cursor column. 

The `ColorColumnTextClassifier.cs` file defines a classifier that recognize
characters on the specified color columns. These characters are recognized
(tagged) as `Color Column Text` to be colored by the editor.

Revision History
----------------

- 1.0.1: Bug fixes

 - Wide characters (such as full width characters) are now counted as taking two 
   columns in the classification. This corresponds to the behaviour of Visual 
   Studio when using hiragana with e.g. MS Gothic as the font. 

- 1.0: Initial version
