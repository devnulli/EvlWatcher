## NEWS 

### 2024-01-20 release of v.2.1.6 was completed
- adds support for some more protocols (MariaDB, SQLServer, SMB Server, .)
- you can now copy IPs into the clipboard by pressing Ctrl+C
- fixes a bug in the whitelist 

### 2022-04-14 release of v.2.1.5 was completed
- fixes a bug where a windows misbehaviour could return 0.0.0.0 as offending IP, thus blocking all subnets
- try to fix a bug where a false positive warning about tasks taking too long are spamming the event logs of EvlWatcher

### 2022-01-22 release of v.2.1.4 was completed
- basic ipv6 support
- certificate was renewed

### 2021-06-03 release of v.2.1.3 was completed
- a negative perma-ban setting means that perma banning is disabled 
- whitelisted ips will - though they are not really banned - no longer show up in the temp ban list of the console
- there is now a button that will add all temporary bans to the permanent ban list 
- will now protect openssh out of the box
- fixed a potential security issue (unqoted service path)
- tasks that rely on log sources that are not present are now disabled, instead of throwing an error on every iteration (30 secs default)

### 2021-06-03 release of v.2.1.2 was completed
- a small typo in the license was fixed
- severity of some messages was adjusted (moved from info to verbose) to keep a cleaner event log
- it contains minor bugfixes and corrections, but nothing interesting apart from that its signed now.
- the console app now has a start menu entry
- the console app had some beauty fixes
- added ability to remove temp bans
- fixes a bug with forgetting ips
- replaces te old 'middle finger' with a more safe for work image

### 2020-12-28 preparing the release of v2.1 
- first, i want to say THANK YOU, to everyone who donated
- finally, we have received enough donations, so we can sign the next release. (and afford 3 beers on top of that)
- it contains minor bugfixes and corrections, but nothing interesting apart from that its signed now.
- it is planned to be released in mid-January, depending on when the dongle from certum.eu arrives (it's already ordered) [update 2021-01-06: still waiting for it]
  

### 2020-11-10 release of v2.0 finally complete
- So, this was about time that software got a new paint applied. The release of EvlWatcher 2.0 could finally be made, many thanks go to NukeDev and shimuldn.
- And it gives you a Windows Defender warning right away. What the heck? Damn we urgently need to fix that.
- You can get the whole story of the release [here](https://github.com/devnulli/EvlWatcher/pull/31), or you just check the [release notes](https://github.com/devnulli/EvlWatcher/blob/master/Versions/v2/EvlWatcher-v2.0%20release%20notes.txt)
- YEAH! I'm pretty pleased it's finally finished. I was procrastenating that for over a year after I promised it.
