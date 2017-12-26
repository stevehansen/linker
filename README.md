linker
======

Create a hard link for files in a specified predefined location (invoked by just passing the file path as argument).

In _directory mode_ (can be invoked with --dir DIRECTORY as arguments) it will deduplicate files in .NET packages folder by creating hardlinks for duplicate (based on filename, size and md5) files.

[![Build status](https://ci.appveyor.com/api/projects/status/ggyyf34sneg9q2ci?svg=true)](https://ci.appveyor.com/project/SteveHansen/linker)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fstevehansen%2Flinker.svg?type=shield)](https://app.fossa.io/projects/git%2Bgithub.com%2Fstevehansen%2Flinker?ref=badge_shield)

**Samples**

    linker C:\Downloads\2014\Document.txt
Will create a hard link in **C:\\\_New_\Document.txt**.

    linker --dir C:\Code\
Will check all sub directories starting in **C:\Code\** and deduplicate all files in packages directories.


Version history
===============
- v1.1
 - Made format configurable
 - Added ability to create hard links for .NET packages folder
- v1.0
 - Initial version, creates a hard link to \_New_\FileName


## License
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bgithub.com%2Fstevehansen%2Flinker.svg?type=large)](https://app.fossa.io/projects/git%2Bgithub.com%2Fstevehansen%2Flinker?ref=badge_large)