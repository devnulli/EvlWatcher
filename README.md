## NEWS 

### 2020-11-10 release of v2.0 finally complete
So, this was about time that software got a new paint applied. The release of EvlWatcher 2.0 could finally be made, many thanks go to NukeDev and shimuldn.
You get the release [here](https://github.com/devnulli/EvlWatcher/raw/master/Versions/v2/EvlWatcher-v2.0%20setup.exe). And it gives you a Windows Defender warning right away. What the heck? Damn.. anyway.. back to the release:

You can get the whole story of the release [here](https://github.com/devnulli/EvlWatcher/pull/31), or you just check the [release notes](https://github.com/devnulli/EvlWatcher/blob/master/Versions/v2/EvlWatcher-v2.0%20release%20notes.txt)

If anyone needs something or has questions about it, feel free to open an issue. YEAH! I'm pretty pleased it's finally finished. I was procrastenating that for over a year after I promised it.

# Short description of what EvlWatcher does

It's basically a fail2ban for windows. 

## Scenario: there are those bad people out there, hammering my RDP Server (or other service) with brute force attempts.

- You have searched the web and yea, there are plenty of tools, scripts, and what not to solve you problem.
- You however, are lazy. All these tools need configuration of some sorts. Damn it, there needs to be something like fail2ban, with a preconfigured set of rules to just RUN right away and it works. But then, it still needs enough flexibility to completely configure it, should you wish to do so.

## EvlWatcher does that

It works by installing a service that scans the event log for unsuccessful login attempts. When one of its rules are violated (e.g. trying to log in without correct credentials, more than 5 times in 2 minutes) it will place that poor bastard into a generic firewall rule, and therby ban the attacker for 2 hours.

Also, when someone is repeatedly trying, there is a permanent ban list for that, where you defaultly land when you had three strikes.

You can, of course, adjust the rules to your liking. They are basically a consisting of an Event Source, and a Regex to extract an IP, its pretty simple.

# Installation

Run the setup executable. It is not required that you remove previous versions, the installer will take care of that.

## After you have installed EvlWatcher 2.0 

Basically, youre done.

## The Service

Well, it makes a firewall rule called EvlWatcher. And updates it every 30 seconds, based on your event log. Simple as that.
Just one thing: Its normal when the rule is disabled. When there are no IPs banned, its automatically disabled. Dont worry, EvlWatcher will enable it as soon as there is the first ban victim.

## The Console.

You can use the console (in the binary directory of EvlWatcher) (NO, we dont make desktop links, start menu entries, ...) to see how your service is doing.

Theres several tabs.

### Overview Tab

![image](https://user-images.githubusercontent.com/3720480/98728537-eee6be80-2399-11eb-9420-9926cc3704f0.png)

### Live Tab

![image](https://user-images.githubusercontent.com/3720480/98728504-e2626600-2399-11eb-987c-c101a22003e8.png)

### Global Settings Tab

![image](https://user-images.githubusercontent.com/3720480/98728386-bb0b9900-2399-11eb-9792-d3e770334316.png)

### Rule Tester Tab

![image](https://user-images.githubusercontent.com/3720480/98728355-ab8c5000-2399-11eb-918f-3b9a8e316516.png)

# Community

## General
- Please feel free to contribute
- We always need good devs and testers to support us.
- Please, if you have an MSSQL Server or FTP or whatever open to the webs, help up to also cover that with EvlWatcher, by providing us Events.

[![Gitter](https://badges.gitter.im/EvlWatcher/community.svg)](https://gitter.im/EvlWatcher/community?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge)

## If you want to support EvlWatcher monetarily

I had to change the optimistic, world is good, text below, because, turns out, its not true [see here](https://github.com/devnulli/EvlWatcher/issues/32)

~~To be honest, that chapter is only there because I was asked to do it.~~
~~EvlWatcher doesnt have a lot of expenses, and therefore doesn't really need much monetary support.~~


If you want to say thanks, I would be happy if you would ~~buy me a coffe or a beer~~ help me afford a certificate so I can sign the source code, either here: 
<a href='https://ko-fi.com/F2F02MKY9' target='_blank'><img height='36' style='border:0px;height:36px;' src='https://cdn.ko-fi.com/cdn/kofi2.png?v=2' border='0' alt='Buy Me a Coffee at ko-fi.com' /></a>

or here by donating to Bitcoin address `bc1q5hk4xum577t6zcfhxgtk2jrjjmxc2nq3uurvg3`.

or just donate to you favorite charity.

## Why is that a Virus according to my virus-scanner?
Its pretty sure not a virus, when you make sure of the following:
- only download it from here (github)

If you download it from somewhere else:
- check its MD5
  - for v2.0 the MD5 is `d658718ea9cc794e704b02b7c252365e`
  - to check an MD5 on Windows, type `CertUtil -hashfile "EvlWatcher-v2.0 setup.exe" MD5`
  
EvlWatcher 2.0 is written in C# and therefore easily readable and changeable.
So is it a virus when you didnt make sure of the above steps? Possibly, yea. Probably not, but possibly.

## When will you fix that?

While it's not certified, some heuristics say its a virus, because of some program code they find "sus". (like playing around with the firewall?)

### Will-Fix-O-Meter
**135€** est. price of certification
**27€** donations so far
~~**12€** donations so far~~

## EvlWatcher is, and always will be, ad-free and without costs (including me).

Cya..

Mike


