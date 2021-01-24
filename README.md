# _What's EvlWatcher 2?_

It's basically a fail2ban for windows. Its goals are also mainly what we love about fail2ban:
- *pre-configured*
- *no-initial-fucking-around-with-scripts-or-config-files*
- *install-and-forget*

You can download it [here (v.2.1.1)](https://github.com/devnulli/EvlWatcher/raw/master/Versions/v2/EvlWatcher-v2.1.1%20setup.exe).

## Also, we love issues!

If anyone needs something or has questions about something, please feel free to open an issue. 
We are especially happy to get issues about log-entry samples we don't react on, or ideas of how we can **support more protocols**. 

# A bit more detailed description of what EvlWatcher does.

## Scenario: there are those bad people out there, hammering your service (RDP and whatnot) with brute force attempts.

- You can see them and their IPs clearly in the Windows Event-Log. 
- You have searched the web and yea, there are plenty of tools, scripts, and all that, to read the event-log and automatically ban the attackers IP.
- *You however, are lazy.* You needs something like fail2ban, with a preconfigured set of rules to just RUN right away and it works. 
- But then, it still needs enough flexibility to completely configure it, should you wish to do so.

## EvlWatcher does that. It scans the Windows-Event-Log, and reacts. 

It works by installing a service that scans the event log for unsuccessful login attempts. When one of its rules are violated (e.g. trying to log in without correct credentials, more than 5 times in 2 minutes) it will place that poor bastard into a generic firewall rule, and therby ban the attacker for 2 hours.

Also, when someone is repeatedly trying, there is a permanent ban list for that, where you defaultly land when you had three strikes.

You can, of course, adjust the rules to your liking. They are basically a consisting of an Event Source, and a Regex to extract an IP, its pretty simple.

# Installation

Run the setup executable. It is not required that you remove previous versions of EvlWatcher, the installer will take care of that.

## After you have installed EvlWatcher

You now have 2 things installed, 
 - a Windows Service that will immediately start running (called EvlWatcher) with its default configuration file
 - a management Console (in the binary directory)

## The Service

You can see it in your Services as "EvlWatcher". It is set to local system and auto start - meaning it cannot communicate over the network, and will always run

The service makes a firewall rule called EvlWatcher. And updates it every 30 seconds, based on your event log. Simple as that.
Just one thing: Its normal when the rule is disabled. When there are no IPs banned, its automatically disabled. Dont worry, EvlWatcher will enable it as soon as there is the first ban victim.

## The Configuration

You can see it as config.xml in the binary directory. 
It's made to cover all sorts of brute force attacks out of the box, but can also be expanded. Just take a look inside, if you want.

## The Console (EvlWatcherConsole.exe).

You can use the console (in the binary directory of EvlWatcher) *(NO, we dont make desktop links, start menu entries, ...)* to see how your service is doing.

There are several tabs in the console.

### Overview Tab

Shows you which IPS are currently banned or whitelisted

![image](https://user-images.githubusercontent.com/3720480/98728537-eee6be80-2399-11eb-9420-9926cc3704f0.png)

### Live Tab

Shows you what the service is doing currently thinking about.

![image](https://user-images.githubusercontent.com/3720480/98728504-e2626600-2399-11eb-987c-c101a22003e8.png)

### Global Settings Tab

![image](https://user-images.githubusercontent.com/3720480/98728386-bb0b9900-2399-11eb-9792-d3e770334316.png)

### Rule Tester Tab

When you find something you want automatically banned, you copy your Windows Event-Log XML here and try to find a for it.
Once you did that, you can either build a new ban task in your config, or post an issue here, so we add it to the config globally.

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

Cya..

Mike
