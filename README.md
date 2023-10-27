

                                  ____   __    __   ____  __  ____     __   ____  __  
                                 (  _ \ /  \  / _\ (    \(  )(  __)   / _\ (  _ \(  ) 
                                  )   /(  O )/    \ ) D ( )(  ) _)   /    \ ) __/ )(  
                                 (__\_) \__/ \_/\_/(____/(__)(____)  \_/\_/(__)  (__) 


 
Roadie
======
Powerful API Music Server. Serve (stream) your high quality MP3 files.

Roadie is an API server that works well with [roadie-vuejs](https://github.com/sphildreth/roadie-vuejs) as a graphical UI Front End and also has a full [Subsonic compatible API](http://www.subsonic.org/pages/apps.jsp) that works with many Subsonic mobile applications. Roadie was built to be able to handle music collections with [hundreds of thousands of tracks](http://www.redferret.net/?page_id=38781).

[![License](https://img.shields.io/badge/License-MIT-blue.svg)](https://opensource.org/licenses/MIT)
<a target="_blank" href="https://github.com/sphildreth/roadie/pulls">
    <img src="https://img.shields.io/badge/PRs-welcome-brightgreen.svg" alt="PRs Welcome" />
</a>
[![Build Status](https://travis-ci.org/sphildreth/roadie.svg?branch=master)](https://travis-ci.org/sphildreth/roadie)



Built on:
---------
* [.Net Core](https://docs.microsoft.com/en-us/dotnet/core/)
* [EF Core](https://docs.microsoft.com/en-us/ef/core/)

Core Features:
---------
* Ability to scan folder and add music to library.
* Track playlists managed by users.
* Release collections managed by "editor" users.
* Metadata engines for ID3 Tag and Image lookups.
* Full Subsonic API emulation allowing for any Subsonic client to be used with Roadie.
* [UPnP and DLNA](https://github.com/sphildreth/roadie/wiki/DLNA)

Database Providers:
---------
* SQLite
* File (one table per file in a folder of your choice)
* MySQL/MariaDB

Metadata Providers:
---------
* ID3 Tags (via [idsharp.tagging](https://github.com/RandallFlagg/IdSharpCore))
* Discogs
* Last.FM (via [inflatable.lastfm](https://github.com/inflatablefriends/lastfm)) ( Scrobbling support as well)
* iTunes
* Musicbrainz
* Wikipedia
* Spotify

Artist and Release Image Search Providers:
---------
* Bing
* iTunes

Support:
------------
[Discord server](https://discord.gg/pZyznJN)

Installation
------------
Please see [the wiki](https://github.com/sphildreth/roadie-dotnetcore/wiki)

License
-------
MIT

