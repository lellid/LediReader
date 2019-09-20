# LediReader

LediReader is a WPF application to read ebooks in the .epub format. It is especially
suited for people like me, who like to read ebooks in a foreign language (needing a dictionary) and want to have them
read aloud (using speech synthesis). Thus, I have made switching between speech mode and
the lookup of words in a dictionary super easy and fast.

Some of the **features** are:
- Can open ebooks in .epub format (without DRM only).
- Can load one or multiple dictionaries in .slob format. 
- Read-aloud function using the speech synthesis of Microsoft Windows.
- Light mode and dark mode, separately chooseable for the document and for the Gui.
- Suited for high resolution screens.
- Suited for touch screens.


## Acknowledgements

It would not have been possible to create LediReader in such a short time
without the contributions from other open source projects!

Thus, many thanks to:

- **Microsoft** for providing the [XamlToFlowDocumentConverter sample](https://github.com/microsoft/WPF-Samples/blob/master/Sample%20Applications/HtmlToXamlDemo/HtmlToXamlConverter.cs).
The improved version of this sample demo can be found [here](https://github.com/lellid/HtmlToFlowDocument). This component
converts the XHTML content of the ebook into a Wpf FlowDocument that can easily be shown in the Gui.
- **Tyler Brinks** for his [HTML CSS parser](https://github.com/TylerBrinks/ExCSS), which is responsible
for parsing the CSS style files in the ebooks.
- **vers-one** for the [EpubReader component](https://github.com/vers-one/EpubReader). This component
parses the zipped .epub file and provides its content in an easy-to-use way.
- **itkach** for providing an extraordinary good [documentation](https://github.com/itkach/slob) of the SLOB file format,
which is used to store the dictionaries.
- The **many contributors** to [SharpZipLib](https://github.com/icsharpcode/SharpZipLib), a library
that can compress / decompress many formats. It is used to decompress the contents
of the dictionary files.
- **Dan Pristupov** for the [dark theme](https://github.com/DanPristupov/WpfExpressionBlendTheme/tree/master/DarkBlendTheme)
that is used in LediReader.

## Requirements

LediReader works with Windows 7-SP1 and above.
The .NET framework 4.7.1 or above is required.
Some functions (e.g. keeping the display switched on during speech synthesis) will require Windows 10 1903 or above.

## Installation

At the time of writing, there is no dedicated installer. Download the Zip file
with the binaries, unlock them by right-clicking on the .zip file and choose 'Unlock'.
Then unzip the content of the .zip file in a folder of your choice. Double click
on the main executable `LediReader.exe` to open the program. You can then register
the extension .epub with LediReader by choosing the menu `Settings -> Register `.
This ensures that the next time you double-click on an .epub file, LediReader is
opened with that file.

## Handling

Open an .epub file by using `Open book` from the main menu. Depending on the size of
the book, this may take some time. The next time you start LediReader, the book is loaded
automatically.

Open one or more dictionaries by using `Dictionary -> Open SLOB file`. If you
don't have SLOB files, you can import dictionaries from other sources
and convert them to SLOB files (see separate section dedicated to dictionaries).

Once book and dictionary are loaded, there is no need to load them
the next time you open LediReader. The paths are to book and dictionary
are stored in the settings.

Before you start using speech synthesis, open the `Settings -> Speech..` menu and choose a voice.
You will see all installed voices. If you can not find a voice for the language of
your book, you can install other voices.

There are two handling modes in LediReader, `in audio mode` and not `in audio mode`. 
Switch between the two modes by clicking (or touching) the main menu item
`in audio mode`.

### Handling when `in audio mode` is checked

- Start speech synthesis by selecting some text (a single character is enough) and then click
or touch the right margin of the application window.
- While speech synthesis is active, click or touch a word to open
the dictionary (a single click is sufficient).
- Click or touch the right margin of the application window to close the dictionary window. 
- Click or touch the right margin of the application window again to resume speech synthesis.
- Click on the left margin of the application window to stop speech synthesis.
- If you now click on the right margin, speech synthesis will resume.
- If you instead want to go to the next page, first click on the left margin again
to 'disarm' speech synthesis. Afterwards, you can use the left
margin and the right margin to go to the previous / the next page. 
- While speech synthesis is inactive, double-click on a word
to open the dictionary.

### Handling when `in audio mode` is not checked

- Click on the left or the right margin of the application window
to go to the previous / the next page.
- Double-click or double-touch onto a word to open the dictionary.
- Close the dictionary by clicking or touching the right margin of the application window.
- You can start speech synthesis by choosing `Play` from the main menu.
(if some text is selected, synthesis will start from the selection; otherwise, from the start of the page).
- Stop speech synthesis by clicking or touching the left margin of the application window.


## Downloading and opening dictionaries already in the .slob format

There are some dictionaries out there that are already in the .slob format. For example,
have a look [here](https://github.com/itkach/slob/wiki/Dictionaries).

> **Note 1:**  
> Some of the dictionaries will cause exceptions if you try to load them, 
> in particular those created from Wikipedia or Wiktionary. 
> This is because they are compressed with LZMA2 (7-Zip), and for an unknown reason
> my decompression procedure does not work correctly. If you feel you can help here,
> please have a look in the source code!

> **Note 2:**  
> Although [at the source mentioned above](https://github.com/itkach/slob/wiki/Dictionaries)
> you will also find dictionaries from the [FreeDict initiative](https://freedict.org/) already
> converted to .slob format, the content would look better if you follow the instructions
> below to import those dictionaries directly from the source .TEI XML files.


## Importing dictionaries from other formats than .slob

Currently, LediReader can import:

1. TEI XML files from the [FreeDict initiative](https://freedict.org/). Go to
the [GitHub repository of the dictionaries](https://github.com/freedict/fd-dictionaries).
There are subdirectories for many language combinations. Go into the subdirectory
of your choice and search for the file with the extension .tei. Right click on this
file and choose "Save destination as.." to save the file onto your local computer.
Now switch back to LediReader, choose from the main menu `Dictionary -> Import TEI file..`.
Select in the file browser the .tei file you just downloaded. The .tei file
is converted into a .slob file, which you then have to save. The file is now
loaded into LediReader and is used as a source for the dictionary view.

2. Plain text files. If you look for a good English-German dictionary,
there is a good source at the Technical University of Chemnitz. At the time of writing,
the dictionary could be found [in this FTP directory](https://ftp.tu-chemnitz.de/pub/Local/urz/ding/de-en/).
The name of the file is `de-en.txt`. As described above, right-click on the file,
select `Save destination as..` and save the text file on your local PC.
Now switch back to LediReader, choose from the main menu `Dictionary -> Import TU Chemnitz file..`.
Select in the file browser the .tei file you just downloaded. The .tei file
is converted into a .slob file, which you then have to save. The file is now
loaded into LediReader and is used as a source for the dictionary view.



## Building LediReader from the sources

To build the LediReader application from the sources, you will need Microsoft Visual Studio 2019.
The 'Community edition' is sufficient.

Clone the source to your PC, using something like
```
git clone https://github.com/lellid/LediReader
```

You then need to update the submodules:

```
git update submodules
```

You can then open the solution (.sln) file in the root folder to open the solution
in Visual Studio.


## Known problems

After using the read-aloud function for about 20 pages, you will hear a crackling in the audio output.
This is due to a memory leak that is known to Microsoft since the year 2010. (Congratulations, Microsoft!).
See for instance [this post](https://social.microsoft.com/Forums/en-US/ee7bd34f-20c2-4a75-9d5a-a0c5e7f1a9b2/memory-leak-with-speechsynthesizer-please-help?forum=Offtopic) and [this post](https://stackoverflow.com/questions/10707187/memory-leak-in-net-speech-synthesizer).
Stopping and restarting speech will not help. Instead, you need to close LediReader and
open it again.

I'm working on this problem now, and try to use Windows 10 speech synthesis directly, using the 
Windows 10 UWP APIs. This will of course only get rid of the problem under Windows 10.
