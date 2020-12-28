# _What's EvlWatcher 2?_

It's basically a fail2ban for windows. It also tries focus on *pre-configured, no-initial-fucking-around-with-scripts-or-config-files, install-and-done* magic of the original fail2ban.

## Also, we love issues!

If anyone needs something or has questions about something, please feel free to open an issue. 
We are especially happy to get issues about log-entry samples we don't react on, or ideas of how we can support more protocols. 

## NEWS 

### 2020-12-28 preparing the release of v2.1 
- first, i want to say THANK YOU, to everyone who donated
- finally, we have received enough donations, so we can sign the next release. (and afford 3 beers on top of that)
- it contains minor bugfixes and corrections, but nothing interesting apart from that.
- it is planned to be released in mid-January, depending on when the dongle from certum.eu arrives (it's already ordered)
  

### 2020-11-10 release of v2.0 finally complete
- So, this was about time that software got a new paint applied. The release of EvlWatcher 2.0 could finally be made, many thanks go to NukeDev and shimuldn.
- And it gives you a Windows Defender warning right away. What the heck? Damn we urgently need to fix that.
- You can get the whole story of the release [here](https://github.com/devnulli/EvlWatcher/pull/31), or you just check the [release notes](https://github.com/devnulli/EvlWatcher/blob/master/Versions/v2/EvlWatcher-v2.0%20release%20notes.txt)
- YEAH! I'm pretty pleased it's finally finished. I was procrastenating that for over a year after I promised it.

# A bit more detailed description of what EvlWatcher does

## Scenario: there are those bad people out there, taunting your service (RDP and whatnot) with brute force attempts.

- You can see it clearly in the Windows Event-Log. 
- You have searched the web and yea, there are plenty of tools, scripts, and what not to read the event-log and automatically ban the attackers IP.
- You however, are lazy. All these tools need configuration of some sorts. Damn it, there needs to be something like fail2ban, with a preconfigured set of rules to just RUN right away and it works. But then, it still needs enough flexibility to completely configure it, should you wish to do so.

## EvlWatcher does that. It scans the Windows-Event-Log, and reacts. 

It works by installing a service that scans the event log for unsuccessful login attempts. When one of its rules are violated (e.g. trying to log in without correct credentials, more than 5 times in 2 minutes) it will place that poor bastard into a generic firewall rule, and therby ban the attacker for 2 hours.

Also, when someone is repeatedly trying, there is a permanent ban list for that, where you defaultly land when you had three strikes.

You can, of course, adjust the rules to your liking. They are basically a consisting of an Event Source, and a Regex to extract an IP, its pretty simple.

# Installation

Run the setup executable. It is not required that you remove previous versions, the installer will take care of that.

## After you have installed EvlWatcher

You now have 2 things installed, a Windows Service (called EvlWatcher), with a default configuration file, and a management console.

## The Service

Well, it makes a firewall rule called EvlWatcher. And updates it every 30 seconds, based on your event log. Simple as that.
Just one thing: Its normal when the rule is disabled. When there are no IPs banned, its automatically disabled. Dont worry, EvlWatcher will enable it as soon as there is the first ban victim.

## The Configuration

It's made to cover all sorts of brute force attacks out of the box, but can also be expanded.

## The Console.

You can use the console (in the binary directory of EvlWatcher) (NO, we dont make desktop links, start menu entries, ...) to see how your service is doing.

There are several tabs.

### Overview Tab

![image](https://user-images.githubusercontent.com/3720480/98728537-eee6be80-2399-11eb-9420-9926cc3704f0.png)

### Live Tab

![image](https://user-images.githubusercontent.com/3720480/98728504-e2626600-2399-11eb-987c-c101a22003e8.png)

### Global Settings Tab

![image](https://user-images.githubusercontent.com/3720480/98728386-bb0b9900-2399-11eb-9792-d3e770334316.png)

### Rule Tester Tab

![image](https://user-images.githubusercontent.com/3720480/98728355-ab8c5000-2399-11eb-918f-3b9a8e316516.png)

# Community

## If you want to support Evlwatcher practically
- Please feel free to contribute
- We always need good devs and testers to support us.
- Please, if you have an MSSQL Server or FTP or whatever open to the webs, help up to also cover that with EvlWatcher, by providing us Events.

[![Gitter](https://badges.gitter.im/EvlWatcher/community.svg)](https://gitter.im/EvlWatcher/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

## If you want to support EvlWatcher monetarily

To be honest, that chapter is only there because I was asked to do it.
EvlWatcher doesnt have a lot of expenses, except the initial cost of code-signing, which were already covered by donations,
and about 25€ / year for keeping up the certificate. Therefore, we don't really need much monetary support. 

But if you want to say thanks, I would be happy if you would buy me a coffee or a beer here:

<a href='https://ko-fi.com/F2F02MKY9' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=2' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

Or you could just donate to your favorite charity.

## Why is that a Virus according to my virus-scanner?

Welp, yes, we published the version 2.0 without signing. (its expensive, about € 150,-) 
And the problem is, that while it's not signed, some heuristics say its a virus. Thats because of some program code they find "sus" (like playing around with the firewall).
However, since it really sucks, we also received some donations to go and buy ourselves a certificate. Which is what we did.

**So versions beginning with 2.1 and above are signed, and should not make trouble.**

Until then, you can make sure that it's NOT a virus, by:
- first, you need to win the struggle against your browser.  
- only download it from here (github)

In case you download it from somewhere else, check its MD5:
  - for v2.0 the MD5 is `d658718ea9cc794e704b02b7c252365e`
  - to check an MD5 on Windows, type `CertUtil -hashfile "EvlWatcher-v2.0 setup.exe" MD5`
  
EvlWatcher is written in C#, and therefore easily readable and changeable.
So is it a virus when you didnt make sure of the above steps? Possibly, yea. Probably not, but possibly.

Cya..

Mike
